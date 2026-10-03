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
        [SerializeField] private bool _showDebugFeedback = true;

        private readonly RaycastHit[] _holdHits = new RaycastHit[8];
        private readonly Collider[] _holdColliders = new Collider[16];
        private float _carriedRadius = 0.25f;

        private CharacterController _characterController;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private GrabbableObject _carriedObject;
        private Transform _resolvedHoldPoint;
        private Collider[] _playerColliders;

        private bool _savedWasKinematic;
        private bool _savedUseGravity;

        public bool IsCarrying => _carriedObject != null && _carriedObject.gameObject != null;
        public GrabbableObject CarriedObject => IsCarrying ? _carriedObject : null;
        public Transform HoldPoint => ResolveHoldPoint();

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

        private void Awake()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            _detector = GetComponent<InteractionDetector>();
            _characterController = GetComponent<CharacterController>();
            _playerColliders = GetComponentsInChildren<Collider>();
        }

        private void OnEnable()
        {
            if (_interactAction != null && _interactAction.action != null)
                _interactAction.action.Enable();
        }

        private void Update()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            // Only process input for the local player
            if (_playerMovement == null || _playerMovement.LocalCamera == null)
                return;

            if (_interactAction == null || _interactAction.action == null)
                return;

            if (!_interactAction.action.enabled)
                _interactAction.action.Enable();

            if (_interactAction.action.WasPressedThisFrame())
            {
                if (IsCarrying)
                    RequestDrop();
                else
                    RequestGrab();
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
        }

        private void LateUpdate()
        {
            if (!IsCarrying)
            {
                if (_carriedObject != null)
                    _carriedObject = null;
                return;
            }

            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;
            if (camera == null)
                return;

            var holdPoint = HoldPoint;
            if (holdPoint != null && _carriedObject != null)
            {
                Vector3 targetPos = ResolveHoldPosition(holdPoint, camera, _carriedObject);
                Quaternion targetRot = holdPoint.rotation;
                _carriedObject.transform.SetPositionAndRotation(targetPos, targetRot);
            }
        }

        private void OnDisable()
        {
            if (IsCarrying)
                ExecuteDrop();
        }

        public bool RequestGrab()
        {
            if (_detector == null)
                _detector = GetComponent<InteractionDetector>();

            var target = _detector != null ? _detector.CurrentTarget as GrabbableObject : null;
            if (target == null && _detector != null && _detector.CurrentTarget is Component comp)
                target = comp.GetComponentInParent<GrabbableObject>();

            return RequestGrab(target);
        }

        public bool RequestGrab(GrabbableObject target)
        {
            if (target == null)
                return false;

            // Offline M1: Direct local execution.
            // NET-002/003: Will route intent to host via ServerRpc.
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

        public bool RequestDrop()
        {
            // Offline M1: Direct local execution.
            // NET-002/003: Will route drop intent to host.
            return ExecuteDrop();
        }

        public bool ExecuteDrop()
        {
            if (!IsCarrying)
                return false;

            DetachObject();
            return true;
        }

        private void AttachObject(GrabbableObject target)
        {
            _carriedObject = target;

            var colliders = target.Colliders;
            if (colliders != null && colliders.Length > 0)
            {
                Bounds b = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        b.Encapsulate(colliders[i].bounds);
                }
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
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            SetPlayerCollisionIgnored(target, true);

            target.OnGrab(gameObject);

            var holdPoint = HoldPoint;
            if (holdPoint != null)
            {
                if (_playerMovement == null)
                    _playerMovement = GetComponent<PlayerMovement>();
                var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;

                Vector3 targetPos = camera != null ? ResolveHoldPosition(holdPoint, camera, target) : holdPoint.position;
                Quaternion targetRot = holdPoint.rotation;
                target.transform.SetPositionAndRotation(targetPos, targetRot);
            }
        }

        private void DetachObject()
        {
            if (_carriedObject != null)
            {
                var target = _carriedObject;
                _carriedObject = null;

                DepenetrateOnDrop(target);

                SetPlayerCollisionIgnored(target, false);

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

                target.OnRelease();
            }
        }

        private void DepenetrateOnDrop(GrabbableObject target)
        {
            if (target == null)
                return;

            var targetColliders = target.Colliders;
            if (targetColliders == null || targetColliders.Length == 0)
                return;

            if (_playerColliders == null || _playerColliders.Length == 0)
                _playerColliders = GetComponentsInChildren<Collider>();

            for (var iter = 0; iter < 4; iter++)
            {
                var resolved = false;

                // 1. Periksa overlap obstacle dunia di sekitar target
                var overlapCount = Physics.OverlapSphereNonAlloc(target.transform.position, _carriedRadius + 0.5f, _holdColliders,
                    ~0, QueryTriggerInteraction.Ignore);

                // 2. Lepaskan penetrasi terhadap player capsule
                if (_playerColliders != null)
                {
                    foreach (var pCol in _playerColliders)
                    {
                        if (pCol == null || pCol.isTrigger) continue;
                        foreach (var tCol in targetColliders)
                        {
                            if (tCol == null || tCol.isTrigger) continue;

                            if (Physics.ComputePenetration(
                                tCol, tCol.transform.position, tCol.transform.rotation,
                                pCol, pCol.transform.position, pCol.transform.rotation,
                                out Vector3 pDir, out float pDist))
                            {
                                if (pDist > 0.001f)
                                {
                                    // Cek apakah dorongan pDir akan menabrak obstacle
                                    Vector3 candidatePos = target.transform.position + pDir * (pDist + 0.01f);
                                    Collider blockingObstacle = null;
                                    Vector3 obstacleDepenNormal = Vector3.zero;

                                    for (var i = 0; i < overlapCount; i++)
                                    {
                                        var obsCol = _holdColliders[i];
                                        if (obsCol == null || obsCol.isTrigger
                                            || obsCol.transform.IsChildOf(transform)
                                            || obsCol.transform.IsChildOf(target.transform))
                                            continue;

                                        Vector3 candidateColPos = candidatePos + (tCol.transform.position - target.transform.position);
                                        if (Physics.ComputePenetration(
                                            tCol, candidateColPos, tCol.transform.rotation,
                                            obsCol, obsCol.transform.position, obsCol.transform.rotation,
                                            out Vector3 oDir, out float oDist) && oDist > 0.001f)
                                        {
                                            blockingObstacle = obsCol;
                                            obstacleDepenNormal = oDir;
                                            break;
                                        }
                                    }

                                    if (blockingObstacle != null)
                                    {
                                        // Terjepit antara player dan obstacle: geser menyamping di sepanjang permukaan obstacle
                                        Vector3 sideways = Vector3.Cross(obstacleDepenNormal, Vector3.up);
                                        if (sideways.sqrMagnitude < 0.01f)
                                            sideways = transform.right;
                                        else
                                            sideways.Normalize();

                                        Vector3 fromPlayer = target.transform.position - transform.position;
                                        if (Vector3.Dot(fromPlayer, sideways) < 0f)
                                            sideways = -sideways;

                                        target.transform.position += sideways * (_carriedRadius + pDist + 0.02f);
                                    }
                                    else
                                    {
                                        target.transform.position = candidatePos;
                                    }

                                    Physics.SyncTransforms();
                                    resolved = true;
                                }
                            }
                        }
                    }
                }

                // 3. Lepaskan penetrasi terhadap obstacle dunia (dinding/ramp) agar tidak menembus
                overlapCount = Physics.OverlapSphereNonAlloc(target.transform.position, _carriedRadius + 0.5f, _holdColliders,
                    ~0, QueryTriggerInteraction.Ignore);

                for (var i = 0; i < overlapCount; i++)
                {
                    var obstacleCol = _holdColliders[i];
                    if (obstacleCol == null || obstacleCol.isTrigger
                        || obstacleCol.transform.IsChildOf(transform)
                        || obstacleCol.transform.IsChildOf(target.transform))
                        continue;

                    foreach (var tCol in targetColliders)
                    {
                        if (tCol == null || tCol.isTrigger) continue;

                        if (Physics.ComputePenetration(
                            tCol, tCol.transform.position, tCol.transform.rotation,
                            obstacleCol, obstacleCol.transform.position, obstacleCol.transform.rotation,
                            out Vector3 depenDir, out float depenDist))
                        {
                            if (depenDist > 0.001f)
                            {
                                target.transform.position += depenDir * (depenDist + 0.002f);
                                Physics.SyncTransforms();
                                resolved = true;
                            }
                        }
                    }
                }

                if (!resolved)
                    break;
            }
        }

        private Vector3 ResolveHoldPosition(Transform holdPoint, Camera camera, GrabbableObject held)
        {
            var origin = camera.transform.position;
            var toHold = holdPoint.position - origin;
            var distance = toHold.magnitude;
            if (distance < 0.01f)
                return holdPoint.position;

            var direction = toHold / distance;
            var count = Physics.SphereCastNonAlloc(origin, _carriedRadius, direction, _holdHits,
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
                    nearest = Mathf.Min(nearest, _holdHits[i].distance);
            }

            var minDistance = Mathf.Min(camera.nearClipPlane + 0.05f, distance);
            nearest = Mathf.Clamp(nearest, minDistance, distance);

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

            // Pastikan objek tidak pernah berada di belakang kamera atau menembus near-clip plane.
            // Catatan: Klem forwardDist memprioritaskan keterlihatan pada viewport kamera di atas pemisahan dunia
            // (trade-off yang disengaja agar objek tidak terpotong near-clip saat player menempel erat ke dinding vertikal).
            var toTarget = targetPos - origin;
            var forwardDist = Vector3.Dot(toTarget, camera.transform.forward);
            if (forwardDist < minDistance)
            {
                targetPos += camera.transform.forward * (minDistance - forwardDist);
            }

            return targetPos;
        }

        private void SetPlayerCollisionIgnored(GrabbableObject target, bool ignore)
        {
            if (target == null)
                return;

            if (_playerColliders == null || _playerColliders.Length == 0)
                _playerColliders = GetComponentsInChildren<Collider>();

            var targetColliders = target.Colliders;
            if (_playerColliders != null && targetColliders != null)
            {
                foreach (var pCol in _playerColliders)
                {
                    if (pCol == null) continue;
                    foreach (var tCol in targetColliders)
                    {
                        if (tCol == null) continue;
                        Physics.IgnoreCollision(pCol, tCol, ignore);
                    }
                }
            }
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
            if (camera != null)
            {
                var existing = camera.transform.Find("HoldPoint");
                if (existing != null)
                {
                    _resolvedHoldPoint = existing;
                }
                else
                {
                    var hp = new GameObject("HoldPoint");
                    hp.transform.SetParent(camera.transform, false);
                    hp.transform.localPosition = new Vector3(0.3f, -0.25f, 1.2f);
                    _resolvedHoldPoint = hp.transform;
                }
            }

            return _resolvedHoldPoint;
        }
    }
}
