using UnityEngine;
using UnityEngine.InputSystem;
using Ngecor.Player;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(InteractionDetector))]
    public class PlayerGrab : MonoBehaviour
    {
        [SerializeField] private Transform _holdPoint;
        [SerializeField, Min(0.1f)] private float _maxGrabDistance = 3.5f;
        [SerializeField] private InputActionReference _interactAction;
        [SerializeField] private InputActionReference _throwAction;
        [SerializeField, Min(0f)] private float _throwForce = 10f;
        [SerializeField, Min(0f)] private float _pinchDropDelay = 0.3f;
        [SerializeField] private bool _showDebugFeedback = true;

        private readonly RaycastHit[] _holdHits = new RaycastHit[8];
        private readonly Collider[] _holdColliders = new Collider[16];
        private float _carriedRadius = 0.25f;
        private float _pinchTimer;

        private CharacterController _characterController;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private GrabbableObject _carriedObject;
        private IHoldInteractable _heldInteractable;
        private Transform _resolvedHoldPoint;
        private Collider[] _playerColliders;
        private string _interactionFeedback;
        private float _interactionFeedbackUntil;

        private bool _savedWasKinematic;
        private bool _savedUseGravity;

        private struct SeparatingEntry
        {
            public GrabbableObject target;
            public Collider[] colliders;
        }

        private SeparatingEntry[] _separatingQueue = new SeparatingEntry[8];
        private int _separatingCount;

        public bool IsCarrying => _carriedObject != null && _carriedObject.gameObject != null;
        public bool IsUsingInteractable => _heldInteractable is Object heldObject && heldObject != null;
        public GrabbableObject CarriedObject => IsCarrying ? _carriedObject : null;
        public Transform HoldPoint => ResolveHoldPoint();
        public string InteractionFeedback => Time.unscaledTime < _interactionFeedbackUntil ? _interactionFeedback : null;
        public float CarriedRadius => _carriedRadius;
        public float PinchDropDelay
        {
            get => _pinchDropDelay;
            set => _pinchDropDelay = Mathf.Max(0f, value);
        }

        public float MaxGrabDistance
        {
            get => _maxGrabDistance;
            set => _maxGrabDistance = Mathf.Max(0.1f, value);
        }

        public InputActionReference InteractAction
        {
            get => _interactAction;
            set => _interactAction = value;
        }

        public InputActionReference ThrowAction
        {
            get => _throwAction;
            set => _throwAction = value;
        }

        public float ThrowForce
        {
            get => _throwForce;
            set => _throwForce = Mathf.Max(0f, value);
        }

        public System.Func<GrabbableObject, bool> GrabRequestHandler { get; set; }
        public System.Func<bool> DropRequestHandler { get; set; }
        public System.Func<Vector3, bool> ThrowRequestHandler { get; set; }
        public bool UpdateCarriedTransform { get; set; } = true;
        public bool AutoDropEnabled { get; set; } = true;
        public System.Action AutoDropHandler { get; set; }
        public System.Func<IHoldInteractable, bool> BeginInteractionRequestHandler { get; set; }
        public IHoldInteractable HeldInteractable => IsUsingInteractable ? _heldInteractable : null;
        // Raised once per ended hold, whoever ended it (E, the interactable itself, disable), so the network layer
        // can tell the host.
        public event System.Action InteractionEnded;

        private void Awake()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            _detector = GetComponent<InteractionDetector>();
            _characterController = GetComponent<CharacterController>();
            _playerColliders = GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            // Only process input for the local player
            if (_playerMovement == null || _playerMovement.LocalCamera == null)
                return;

            // Handle Interact Input (Grab / Drop)
            if (_interactAction != null && _interactAction.action != null && _interactAction.action.WasPressedThisFrame())
            {
                if (IsUsingInteractable)
                    ExecuteEndInteraction();
                else if (IsCarrying)
                    RequestDrop();
                else
                    RequestGrab();
            }

            // Handle Throw Input (Attack action / LMB)
            // Guard: throw is only accepted if the cursor was ALREADY locked prior to this frame's click.
            // A click that re-locks the cursor must NOT trigger a throw.
            bool canProcessThrow = _playerMovement.IsCursorLocked && !_playerMovement.CursorRelockedThisFrame;
            if (IsCarrying && canProcessThrow)
            {
                if (_throwAction != null && _throwAction.action != null && _throwAction.action.WasPressedThisFrame())
                {
                    RequestThrow();
                }
            }
        }

        private void OnGUI()
        {
            if (!_showDebugFeedback)
                return;

            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;
            if (camera == null)
                return;

            if (IsCarrying)
            {
                string objectName = _carriedObject != null ? _carriedObject.gameObject.name : "Object";
                GUI.Box(new Rect(Screen.width / 2f - 120f, Screen.height / 2f + 75f, 240f, 30f),
                    $"[Carrying]: {objectName} (Drop)");
            }
            else if (IsUsingInteractable)
            {
                var component = _heldInteractable as Component;
                string objectName = component != null ? component.gameObject.name : "Object";
                GUI.Box(new Rect(Screen.width / 2f - 120f, Screen.height / 2f + 75f, 240f, 30f),
                    $"[Using]: {objectName} (Release)");
            }
            else if (!string.IsNullOrEmpty(InteractionFeedback))
            {
                GUI.Box(new Rect(Screen.width / 2f - 180f, Screen.height / 2f + 75f, 360f, 42f), InteractionFeedback);
            }
        }

        private void LateUpdate()
        {
            if (!IsCarrying)
            {
                if ((object)_carriedObject != null)
                {
                    _carriedObject = null;
                }
                if (_playerMovement == null)
                    _playerMovement = GetComponent<PlayerMovement>();
                if (_playerMovement != null && _playerMovement.CarriedMass != 0f)
                    _playerMovement.CarriedMass = 0f;

                _pinchTimer = 0f;
                return;
            }

            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            Transform camTransform = null;
            float nearClip = 0.3f;
            if (_playerMovement != null && _playerMovement.LocalCamera != null)
            {
                camTransform = _playerMovement.LocalCamera.transform;
                nearClip = _playerMovement.LocalCamera.nearClipPlane;
            }
            else if (_playerMovement != null)
            {
                var cam = _playerMovement.GetComponentInChildren<Camera>(true);
                camTransform = cam != null ? cam.transform : transform;
                if (cam != null) nearClip = cam.nearClipPlane;
            }
            else
            {
                var cam = GetComponentInChildren<Camera>(true);
                camTransform = cam != null ? cam.transform : transform;
                if (cam != null) nearClip = cam.nearClipPlane;
            }

            var holdPoint = HoldPoint;
            if (holdPoint != null && _carriedObject != null)
            {
                bool isPinched = false;
                Vector3 targetPos = camTransform != null
                    ? ResolveHoldPosition(holdPoint, camTransform, _carriedObject, out isPinched, nearClip)
                    : holdPoint.position;

                if (isPinched && AutoDropEnabled)
                {
                    _pinchTimer += Time.deltaTime;
                    if (_pinchTimer >= _pinchDropDelay)
                    {
                        _pinchTimer = 0f;
                        if (AutoDropHandler != null)
                        {
                            AutoDropHandler();
                        }
                        else
                        {
                            ExecuteDrop();
                        }
                        return;
                    }
                }
                else
                {
                    _pinchTimer = 0f;
                }

                if (UpdateCarriedTransform)
                {
                    Quaternion targetRot = holdPoint.rotation;
                    _carriedObject.transform.SetPositionAndRotation(targetPos, targetRot);
                }
            }
        }

        private void OnDisable()
        {
            if (IsCarrying)
                ExecuteDrop();
            if (IsUsingInteractable)
                ExecuteEndInteraction();

            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();
            if (_playerMovement != null)
                _playerMovement.CarriedMass = 0f;

            _pinchTimer = 0f;

            if (_playerColliders == null || _playerColliders.Length == 0)
                _playerColliders = GetComponentsInChildren<Collider>();

            for (int i = 0; i < _separatingCount; i++)
            {
                var entry = _separatingQueue[i];
                if (entry.target != null && entry.target.gameObject != null)
                {
                    var targetColliders = entry.colliders ?? entry.target.Colliders;
                    if (CheckOverlapping(_playerColliders, targetColliders))
                    {
                        if (entry.target.gameObject.activeInHierarchy)
                        {
                            entry.target.StartCoroutine(MonitorSeparationRoutine(gameObject, _playerColliders, entry.target));
                        }
                    }
                    else
                    {
                        SetPlayerCollisionIgnored(entry.target, false);
                    }
                }
                _separatingQueue[i] = default;
            }
            _separatingCount = 0;
        }

        public bool RequestGrab()
        {
            if (_detector == null)
                _detector = GetComponent<InteractionDetector>();

            if (_detector != null && _detector.CurrentTarget is IHoldInteractable holdInteractable)
                return RequestBeginInteraction(holdInteractable);

            var target = _detector != null ? _detector.CurrentTarget as GrabbableObject : null;
            if (target == null && _detector != null && _detector.CurrentTarget is Component comp)
                target = comp.GetComponentInParent<GrabbableObject>();

            return RequestGrab(target);
        }

        private bool RequestBeginInteraction(IHoldInteractable target)
        {
            if (IsCarrying || IsUsingInteractable || target == null || _detector == null ||
                _detector.CurrentTarget != target ||
                !target.CanInteractFrom(gameObject, _detector.CurrentHit.collider) ||
                !target.CanInteract(gameObject))
                return false;

            if (Vector3.Distance(transform.position, _detector.CurrentHit.point) > _maxGrabDistance)
                return false;

            if (BeginInteractionRequestHandler != null)
                return BeginInteractionRequestHandler(target);

            return ExecuteBeginInteraction(target);
        }

        public bool ExecuteBeginInteraction(IHoldInteractable target)
        {
            if (IsCarrying || IsUsingInteractable || target == null || !target.TryBeginInteraction(gameObject))
                return false;

            _heldInteractable = target;
            return true;
        }

        public bool ExecuteEndInteraction()
        {
            if (!IsUsingInteractable)
            {
                _heldInteractable = null;
                return false;
            }

            var target = _heldInteractable;
            _heldInteractable = null;
            target.EndInteraction(gameObject);
            InteractionEnded?.Invoke();
            return true;
        }

        public void NotifyInteractionEnded(IHoldInteractable target, string feedback = null)
        {
            if (ReferenceEquals(_heldInteractable, target))
            {
                _heldInteractable = null;
                InteractionEnded?.Invoke();
            }
            if (!string.IsNullOrEmpty(feedback))
                ShowInteractionFeedback(feedback);
        }

        public void ShowInteractionFeedback(string message)
        {
            _interactionFeedback = message;
            _interactionFeedbackUntil = Time.unscaledTime + 3f;
        }

        public bool RequestGrab(GrabbableObject target)
        {
            if (target == null)
                return false;

            if (GrabRequestHandler != null)
                return GrabRequestHandler(target);

            return ExecuteGrab(target);
        }

        public bool ExecuteGrab(GrabbableObject target)
        {
            if (IsCarrying)
                return false;

            if (target == null || !target.CanInteract(gameObject))
                return false;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > _maxGrabDistance)
                return false;

            AttachObject(target);
            return true;
        }

        // A grab the host already validated (replication, late join). No distance or line-of-sight check: this peer's
        // copy of the holder may lag behind the host's.
        public bool ExecuteReplicatedGrab(GrabbableObject target)
        {
            if (IsCarrying || target == null)
                return false;

            if (target.IsHeld && target.CurrentHolder != gameObject)
                return false;

            AttachObject(target);
            return true;
        }

        public bool RequestDrop()
        {
            if (DropRequestHandler != null)
                return DropRequestHandler();

            return ExecuteDrop();
        }

        public bool ExecuteDrop()
        {
            if (!IsCarrying)
                return false;

            DetachObject();
            return true;
        }

        public bool RequestThrow()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;
            Vector3 throwDir = camera != null ? camera.transform.forward : transform.forward;

            if (ThrowRequestHandler != null)
                return ThrowRequestHandler(throwDir);

            return ExecuteThrow(throwDir);
        }

        public bool ExecuteThrow()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;
            Vector3 throwDir = camera != null ? camera.transform.forward : transform.forward;
            return ExecuteThrow(throwDir);
        }

        public bool ExecuteThrow(Vector3 throwDir)
        {
            if (!IsCarrying)
                return false;

            if (!float.IsFinite(throwDir.x) || !float.IsFinite(throwDir.y) || !float.IsFinite(throwDir.z))
                return false;

            if (throwDir.sqrMagnitude < 0.0001f)
                return false;

            Vector3 unitDir = throwDir.normalized;
            var target = _carriedObject;
            var rb = target.Rigidbody;

            DetachObject();

            if (rb != null && !rb.isKinematic)
            {
                Vector3 impulseVelocity = unitDir * (_throwForce / Mathf.Max(0.0001f, rb.mass));
                rb.linearVelocity += impulseVelocity;
            }

            return true;
        }

        private void AttachObject(GrabbableObject target)
        {
            _carriedObject = target;

            var colliders = target.Colliders;
            bool foundBounds = false;
            Bounds b = default;
            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    var col = colliders[i];
                    if (col == null || col.isTrigger || !col.enabled)
                        continue;

                    if (!foundBounds)
                    {
                        b = col.bounds;
                        foundBounds = true;
                    }
                    else
                    {
                        b.Encapsulate(col.bounds);
                    }
                }
            }

            if (foundBounds)
            {
                Vector3 extents = b.extents;
                _carriedRadius = Mathf.Clamp(Mathf.Max(extents.x, extents.y, extents.z), 0.05f, 0.5f);
            }
            else
            {
                _carriedRadius = 0.25f;
            }

            var rb = target.Rigidbody;
            if (rb != null)
            {
                _savedWasKinematic = rb.isKinematic;
                _savedUseGravity = rb.useGravity;
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            SetPlayerCollisionIgnored(target, true);

            for (int i = 0; i < _separatingCount; i++)
            {
                if (_separatingQueue[i].target == target)
                {
                    RemoveSeparatingEntryAt(i);
                    break;
                }
            }

            target.OnGrab(gameObject);

            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();
            if (_playerMovement != null && rb != null)
                _playerMovement.CarriedMass = rb.mass;

            _pinchTimer = 0f;

            var holdPoint = HoldPoint;
            if (holdPoint != null)
            {
                Transform camTransform = null;
                float nearClip = 0.3f;
                if (_playerMovement != null && _playerMovement.LocalCamera != null)
                {
                    camTransform = _playerMovement.LocalCamera.transform;
                    nearClip = _playerMovement.LocalCamera.nearClipPlane;
                }
                else if (_playerMovement != null)
                {
                    var cam = _playerMovement.GetComponentInChildren<Camera>(true);
                    camTransform = cam != null ? cam.transform : transform;
                    if (cam != null) nearClip = cam.nearClipPlane;
                }
                else
                {
                    var cam = GetComponentInChildren<Camera>(true);
                    camTransform = cam != null ? cam.transform : transform;
                    if (cam != null) nearClip = cam.nearClipPlane;
                }

                Vector3 targetPos = camTransform != null ? ResolveHoldPosition(holdPoint, camTransform, target, out _, nearClip) : holdPoint.position;
                Quaternion targetRot = holdPoint.rotation;
                if (UpdateCarriedTransform)
                {
                    target.transform.SetPositionAndRotation(targetPos, targetRot);
                }
            }
        }

        private void DetachObject()
        {
            _pinchTimer = 0f;

            if (_carriedObject != null)
            {
                var target = _carriedObject;
                _carriedObject = null;

                if (_playerMovement == null)
                    _playerMovement = GetComponent<PlayerMovement>();
                if (_playerMovement != null)
                    _playerMovement.CarriedMass = 0f;

                var rb = target.Rigidbody;
                if (rb != null)
                {
                    rb.isKinematic = _savedWasKinematic;
                    rb.useGravity = _savedUseGravity;

                    if (!rb.isKinematic)
                    {
                        if (_characterController == null)
                            _characterController = GetComponent<CharacterController>();

                        if (_characterController != null)
                        {
                            Vector3 ccVel = _characterController.velocity;
                            rb.linearVelocity = new Vector3(ccVel.x, 0f, ccVel.z);
                        }
                    }
                }

                // Ignore-until-separated: If object overlaps player capsule upon release,
                // keep collision ignored until it separates in FixedUpdate to prevent launching impulses.
                if (IsOverlappingPlayer(target))
                {
                    if (!enabled)
                    {
                        if (target.gameObject.activeInHierarchy)
                        {
                            if (_playerColliders == null || _playerColliders.Length == 0)
                                _playerColliders = GetComponentsInChildren<Collider>();
                            target.StartCoroutine(MonitorSeparationRoutine(gameObject, _playerColliders, target));
                        }
                    }
                    else
                    {
                        bool alreadyQueued = false;
                        for (int i = 0; i < _separatingCount; i++)
                        {
                            if (_separatingQueue[i].target == target)
                            {
                                _separatingQueue[i].colliders = target.Colliders;
                                alreadyQueued = true;
                                break;
                            }
                        }

                        if (!alreadyQueued)
                        {
                            if (_separatingCount >= _separatingQueue.Length)
                            {
                                System.Array.Resize(ref _separatingQueue, Mathf.Max(8, _separatingQueue.Length * 2));
                            }

                            _separatingQueue[_separatingCount++] = new SeparatingEntry
                            {
                                target = target,
                                colliders = target.Colliders
                            };
                        }
                    }
                }
                else
                {
                    SetPlayerCollisionIgnored(target, false);
                }

                target.OnRelease();
            }
        }

        private static bool CheckOverlapping(Collider[] playerColliders, Collider[] targetColliders)
        {
            if (playerColliders == null || targetColliders == null)
                return false;

            foreach (var pCol in playerColliders)
            {
                if (pCol == null || pCol.isTrigger || !pCol.enabled) continue;
                foreach (var tCol in targetColliders)
                {
                    if (tCol == null || tCol.isTrigger || !tCol.enabled) continue;

                    if (Physics.ComputePenetration(
                        tCol, tCol.transform.position, tCol.transform.rotation,
                        pCol, pCol.transform.position, pCol.transform.rotation,
                        out _, out float dist) && dist > 0.001f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsOverlappingPlayer(GrabbableObject target)
        {
            if (target == null)
                return false;

            if (_playerColliders == null || _playerColliders.Length == 0)
                _playerColliders = GetComponentsInChildren<Collider>();

            return CheckOverlapping(_playerColliders, target.Colliders);
        }

        private void FixedUpdate()
        {
            if (_separatingCount == 0)
                return;

            if (_playerColliders == null || _playerColliders.Length == 0)
                _playerColliders = GetComponentsInChildren<Collider>();

            for (int i = _separatingCount - 1; i >= 0; i--)
            {
                var entry = _separatingQueue[i];
                if (entry.target == null || entry.target.gameObject == null)
                {
                    RemoveSeparatingEntryAt(i);
                    continue;
                }

                if (entry.target.IsHeld && entry.target.CurrentHolder == gameObject)
                {
                    RemoveSeparatingEntryAt(i);
                    continue;
                }

                var targetColliders = entry.colliders ?? entry.target.Colliders;
                bool stillOverlapping = CheckOverlapping(_playerColliders, targetColliders);

                if (!stillOverlapping)
                {
                    SetPlayerCollisionIgnored(entry.target, false);
                    RemoveSeparatingEntryAt(i);
                }
            }
        }

        private static System.Collections.IEnumerator MonitorSeparationRoutine(GameObject playerOwner, Collider[] playerColliders, GrabbableObject target)
        {
            if (target == null || playerColliders == null)
                yield break;

            var targetColliders = target.Colliders;
            if (targetColliders == null || targetColliders.Length == 0)
                yield break;

            var waitForFixedUpdate = new WaitForFixedUpdate();

            while (CheckOverlapping(playerColliders, targetColliders))
            {
                yield return waitForFixedUpdate;

                if (target == null || target.gameObject == null)
                    yield break;

                if (playerOwner != null && target.IsHeld && target.CurrentHolder == playerOwner)
                    yield break;
            }

            if (target != null && target.gameObject != null)
            {
                if (playerOwner == null || !target.IsHeld || target.CurrentHolder != playerOwner)
                {
                    SetCollisionIgnored(playerColliders, targetColliders, false);
                }
            }
        }

        private void RemoveSeparatingEntryAt(int index)
        {
            _separatingCount--;
            if (index < _separatingCount)
            {
                _separatingQueue[index] = _separatingQueue[_separatingCount];
            }
            _separatingQueue[_separatingCount] = default;
        }

        private Vector3 ResolveHoldPosition(Transform holdPoint, Transform cameraTransform, GrabbableObject held, out bool isPinched, float nearClipPlane = 0.3f)
        {
            var origin = cameraTransform.position;
            var toHold = holdPoint.position - origin;
            var distance = toHold.magnitude;
            if (distance < 0.01f)
            {
                isPinched = false;
                return holdPoint.position;
            }

            var direction = toHold / distance;
            var sweepRadius = Mathf.Clamp(_carriedRadius, 0.05f, 0.25f);
            var count = Physics.SphereCastNonAlloc(origin, sweepRadius, direction, _holdHits,
                distance, ~0, QueryTriggerInteraction.Ignore);

            var nearest = distance;
            for (var i = 0; i < count; i++)
            {
                var collider = _holdHits[i].collider;
                if (collider == null || collider.isTrigger
                    || collider.transform.IsChildOf(transform)
                    || collider.transform.IsChildOf(held.transform))
                    continue;                       // abaikan player sendiri dan objek yang dibawa

                if (_holdHits[i].distance > 0.0001f)
                {
                    nearest = Mathf.Min(nearest, _holdHits[i].distance);
                }
                else
                {
                    Ray forwardRay = new Ray(origin, direction);
                    if (!collider.Raycast(forwardRay, out RaycastHit wallHit, distance))
                    {
                        forwardRay = new Ray(origin, cameraTransform.forward);
                        collider.Raycast(forwardRay, out wallHit, distance);
                    }

                    if (wallHit.collider != null && Mathf.Abs(wallHit.normal.y) < 0.7f
                        && Vector3.Dot(wallHit.normal, cameraTransform.forward) < -0.3f)
                    {
                        float clearance = Mathf.Max(0f, wallHit.distance - sweepRadius);
                        nearest = Mathf.Min(nearest, clearance);
                    }
                }
            }

            nearest = Mathf.Clamp(nearest, 0.01f, distance);

            var targetPos = origin + direction * nearest;
            var targetRot = holdPoint.rotation;

            // 3D Depenetration via native PhysX (Physics.ComputePenetration):
            // Memanfaatkan solver native PhysX untuk menghitung vektor pemisah normal collider dunia
            // (seperti ramp miring, tanjakan, atau obstacle) agar objek meluncur mulus di atas permukaan tanpa alokasi GC.
            var heldColliders = held.Colliders;
            if (heldColliders != null && heldColliders.Length > 0)
            {
                var heldRootPos = held.transform.position;
                var invHeldRot = Quaternion.Inverse(held.transform.rotation);

                for (var iter = 0; iter < 3; iter++)
                {
                    var overlapCount = Physics.OverlapSphereNonAlloc(targetPos, _carriedRadius + 0.2f, _holdColliders,
                        ~0, QueryTriggerInteraction.Ignore);

                    var resolvedAny = false;
                    for (var i = 0; i < overlapCount; i++)
                    {
                        var obstacleCol = _holdColliders[i];
                        if (obstacleCol == null || obstacleCol.isTrigger
                            || obstacleCol.transform.IsChildOf(transform)
                            || obstacleCol.transform.IsChildOf(held.transform))
                            continue;

                        foreach (var heldCol in heldColliders)
                        {
                            if (heldCol == null || heldCol.isTrigger)
                                continue;

                            var relativeRot = invHeldRot * heldCol.transform.rotation;
                            var relativePos = invHeldRot * (heldCol.transform.position - heldRootPos);
                            var colPos = targetPos + targetRot * relativePos;
                            var colRot = targetRot * relativeRot;

                            if (Physics.ComputePenetration(
                                heldCol, colPos, colRot,
                                obstacleCol, obstacleCol.transform.position, obstacleCol.transform.rotation,
                                out Vector3 depenDir, out float depenDist))
                            {
                                if (depenDist > 0.001f)
                                {
                                    targetPos += depenDir * (depenDist + 0.005f);
                                    resolvedAny = true;
                                }
                            }
                        }
                    }

                    if (!resolvedAny)
                        break;
                }
            }

            // World obstacle separation takes priority over viewport.
            // Pastikan objek tidak jatuh ke belakang kamera (ramp/slope) KECUALI jika ada rintangan di depan yang menghalangi.
            var toTarget = targetPos - origin;
            var forwardDist = Vector3.Dot(toTarget, cameraTransform.forward);
            var minDistance = nearClipPlane + 0.02f;
            if (forwardDist < minDistance)
            {
                var candidatePos = origin + cameraTransform.forward * minDistance;
                bool penetratesObstacle = false;
                if (heldColliders != null)
                {
                    var heldRootPos = held.transform.position;
                    var invHeldRot = Quaternion.Inverse(held.transform.rotation);
                    for (var i = 0; i < count; i++)
                    {
                        var obstacleCol = _holdHits[i].collider;
                        if (obstacleCol == null || obstacleCol.isTrigger
                            || obstacleCol.transform.IsChildOf(transform)
                            || obstacleCol.transform.IsChildOf(held.transform))
                            continue;

                        foreach (var heldCol in heldColliders)
                        {
                            if (heldCol == null || heldCol.isTrigger) continue;
                            var relativeRot = invHeldRot * heldCol.transform.rotation;
                            var relativePos = invHeldRot * (heldCol.transform.position - heldRootPos);
                            var colPos = candidatePos + targetRot * relativePos;
                            var colRot = targetRot * relativeRot;

                            if (Physics.ComputePenetration(
                                heldCol, colPos, colRot,
                                obstacleCol, obstacleCol.transform.position, obstacleCol.transform.rotation,
                                out _, out float penDist) && penDist > 0.01f)
                            {
                                penetratesObstacle = true;
                                break;
                            }
                        }
                        if (penetratesObstacle) break;
                    }
                }

                if (!penetratesObstacle)
                {
                    targetPos = candidatePos;
                    toTarget = targetPos - origin;
                    forwardDist = Vector3.Dot(toTarget, cameraTransform.forward);
                }
            }

            float minClearance = 0.15f;
            isPinched = forwardDist < minClearance;

            return targetPos;
        }

        private static void SetCollisionIgnored(Collider[] playerColliders, Collider[] targetColliders, bool ignore)
        {
            if (playerColliders == null || targetColliders == null)
                return;

            foreach (var pCol in playerColliders)
            {
                if (pCol == null) continue;
                foreach (var tCol in targetColliders)
                {
                    if (tCol == null) continue;
                    Physics.IgnoreCollision(pCol, tCol, ignore);
                }
            }
        }

        private void SetPlayerCollisionIgnored(GrabbableObject target, bool ignore)
        {
            if (target == null)
                return;

            if (_playerColliders == null || _playerColliders.Length == 0)
                _playerColliders = GetComponentsInChildren<Collider>();

            SetCollisionIgnored(_playerColliders, target.Colliders, ignore);
        }

        public Transform ResolveHoldPoint()
        {
            if (_holdPoint != null)
                return _holdPoint;

            if (_resolvedHoldPoint != null)
                return _resolvedHoldPoint;

            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;
            Transform parentTransform = null;
            if (camera != null)
            {
                parentTransform = camera.transform;
            }
            else if (_playerMovement != null)
            {
                var cam = _playerMovement.GetComponentInChildren<Camera>(true);
                parentTransform = cam != null ? cam.transform : transform;
            }
            else
            {
                var cam = GetComponentInChildren<Camera>(true);
                parentTransform = cam != null ? cam.transform : transform;
            }

            if (parentTransform != null)
            {
                var existing = parentTransform.Find("HoldPoint");
                if (existing != null)
                {
                    _resolvedHoldPoint = existing;
                }
                else
                {
                    var hp = new GameObject("HoldPoint");
                    hp.transform.SetParent(parentTransform, false);
                    hp.transform.localPosition = new Vector3(0.3f, -0.25f, 1.2f);
                    _resolvedHoldPoint = hp.transform;
                }
            }

            return _resolvedHoldPoint;
        }
    }
}
