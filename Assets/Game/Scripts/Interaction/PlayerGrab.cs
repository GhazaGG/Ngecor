using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Ngecor.Player;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(InteractionDetector))]
    [DefaultExecutionOrder(-100)]
    public class PlayerGrab : MonoBehaviour
    {
        [SerializeField] private Transform _holdPoint;
        [SerializeField, Min(0.1f)] private float _maxGrabDistance = 3.5f;
        [SerializeField] private InputActionReference _interactAction;
        [SerializeField] private bool _showDebugFeedback = true;

        private readonly RaycastHit[] _holdHits = new RaycastHit[8];
        private readonly RaycastHit[] _groundHits = new RaycastHit[16];
        private readonly Collider[] _holdColliders = new Collider[16];
        [SerializeField, Min(0.1f)] private float _groundSnapDistance = 5f;
        [SerializeField, Min(0.01f)] private float _scaffoldingRotateSensitivity = 0.25f;
        [SerializeField, Min(0.01f)] private float _scaffoldingRotationSmoothTime = 0.06f;
        private float _carriedRadius = 0.25f;

        private CharacterController _characterController;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private GrabbableObject _carriedObject;
        private ScaffoldingPart _carriedScaffoldingPart;
        private Transform _resolvedHoldPoint;
        private Collider[] _playerColliders;

        private bool _savedWasKinematic;
        private bool _savedUseGravity;
        private bool[] _savedColliderEnabled;
        private Renderer[] _carriedScaffoldingRenderers;
        private bool[] _savedRendererEnabled;
        private GameObject _scaffoldingPreview;
        private Material _scaffoldingPreviewMaterial;
        private bool _scaffoldingPreviewCanConnect;
        private float _scaffoldingPreviewBaseYaw;
        private float _scaffoldingPreviewYaw;
        private float _scaffoldingPreviewCurrentYaw;
        private float _scaffoldingPreviewYawVelocity;
        private float _nextScaffoldingPreviewRefresh;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

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

            bool isLocalPlayer = _playerMovement != null && _playerMovement.LocalCamera != null;
            bool rotatingScaffolding = isLocalPlayer && _carriedScaffoldingPart != null &&
                                       Mouse.current != null && Mouse.current.rightButton.isPressed;
            if (_playerMovement != null)
                _playerMovement.LookInputSuppressed = rotatingScaffolding;

            if (rotatingScaffolding)
                _scaffoldingPreviewYaw = Mathf.Repeat(
                    _scaffoldingPreviewYaw + Mouse.current.delta.ReadValue().x * _scaffoldingRotateSensitivity,
                    360f);

            // Only process input for the local player
            if (!isLocalPlayer)
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
                string action = _carriedScaffoldingPart != null
                    ? $"[Carrying: {objectName}] Ghost: {(_scaffoldingPreviewCanConnect ? "connectable" : "free place")} | Hold RMB + move mouse: rotate | Interact: place"
                    : $"[Carrying]: {objectName} (Drop)";
                float width = _carriedScaffoldingPart != null ? 650f : 240f;
                GUI.Box(new Rect(Screen.width / 2f - width / 2f, Screen.height / 2f + 75f, width, 30f), action);
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
                Quaternion targetRot = _carriedScaffoldingPart != null
                    ? GetScaffoldingPreviewRotation()
                    : holdPoint.rotation;
                if (_carriedScaffoldingPart != null && _carriedScaffoldingPart.IsSupport)
                    targetPos = SnapSupportToGround(_carriedScaffoldingPart, targetPos, targetRot);

                _carriedObject.transform.SetPositionAndRotation(targetPos, targetRot);

                if (_carriedScaffoldingPart != null && _scaffoldingPreview != null)
                {
                    _scaffoldingPreview.transform.SetPositionAndRotation(targetPos, targetRot);
                    if (Time.unscaledTime >= _nextScaffoldingPreviewRefresh)
                        RefreshScaffoldingPreview();
                }
            }

        }

        private void OnDisable()
        {
            if (_playerMovement != null)
                _playerMovement.LookInputSuppressed = false;

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

            ScaffoldingPart droppedPart = _carriedScaffoldingPart;
            DetachObject();
            if (droppedPart != null)
            {
                droppedPart.ConnectToTouchingParts();
                droppedPart.EndPlacementPreview();
            }
            return true;
        }

        private void AttachObject(GrabbableObject target)
        {
            _carriedObject = target;
            _carriedScaffoldingPart = target.GetComponent<ScaffoldingPart>();

            if (_carriedScaffoldingPart != null)
                _carriedScaffoldingPart.DetachFromOtherParts();

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

            DisableCarriedColliders(target);

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
            _scaffoldingPreviewBaseYaw = camera != null
                ? camera.transform.eulerAngles.y
                : transform.eulerAngles.y;
                _scaffoldingPreviewYaw = 0f;
                Quaternion targetRot = _carriedScaffoldingPart != null
                    ? Quaternion.Euler(0f, _scaffoldingPreviewBaseYaw, 0f)
                    : holdPoint.rotation;
                _scaffoldingPreviewCurrentYaw = _scaffoldingPreviewBaseYaw;
                _scaffoldingPreviewYawVelocity = 0f;
                target.transform.SetPositionAndRotation(targetPos, targetRot);
            }

            if (_carriedScaffoldingPart != null)
                CreateScaffoldingPreview(_carriedScaffoldingPart);
        }

        private void DetachObject()
        {
            if (_carriedObject != null)
            {
                var target = _carriedObject;
                _carriedObject = null;
                _carriedScaffoldingPart = null;
                if (_playerMovement != null)
                    _playerMovement.LookInputSuppressed = false;

                RestoreCarriedColliders(target);
                RestoreCarriedScaffoldingRenderers();
                DestroyScaffoldingPreview();
                Physics.SyncTransforms();

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

        private void DisableCarriedColliders(GrabbableObject target)
        {
            Collider[] colliders = target.Colliders;
            _savedColliderEnabled = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                    continue;

                _savedColliderEnabled[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }
        }

        private Vector3 SnapSupportToGround(ScaffoldingPart support, Vector3 desiredPosition, Quaternion desiredRotation)
        {
            if (_groundSnapDistance <= 0f)
                return desiredPosition;

            SetCarriedCollidersEnabled(true);
            try
            {
                support.transform.SetPositionAndRotation(desiredPosition, desiredRotation);
                Physics.SyncTransforms();

                bool hasBounds = false;
                Bounds bounds = default;
                foreach (Collider collider in _carriedObject.Colliders)
                {
                    if (collider == null || !collider.enabled || collider.isTrigger)
                        continue;

                    if (!hasBounds)
                    {
                        bounds = collider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                }

                if (!hasBounds)
                    return desiredPosition;

                Vector3 origin = new Vector3(desiredPosition.x, bounds.max.y + _groundSnapDistance, desiredPosition.z);
                float rayDistance = bounds.size.y + _groundSnapDistance * 2f;
                int hitCount = Physics.RaycastNonAlloc(
                    origin,
                    Vector3.down,
                    _groundHits,
                    rayDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

                bool foundGround = false;
                float groundHeight = float.NegativeInfinity;
                for (int i = 0; i < hitCount; i++)
                {
                    Collider ground = _groundHits[i].collider;
                    if (ground == null || ground.attachedRigidbody != null || _groundHits[i].normal.y < 0.5f ||
                        ground.transform == support.transform || ground.transform.IsChildOf(support.transform) ||
                        ground.transform.IsChildOf(transform))
                        continue;

                    if (_groundHits[i].point.y <= groundHeight)
                        continue;

                    groundHeight = _groundHits[i].point.y;
                    foundGround = true;
                }

                if (foundGround)
                    desiredPosition.y += groundHeight - bounds.min.y;

                return desiredPosition;
            }
            finally
            {
                SetCarriedCollidersEnabled(false);
            }
        }

        private Quaternion GetScaffoldingPreviewRotation()
        {
            float targetYaw = _scaffoldingPreviewBaseYaw + _scaffoldingPreviewYaw;
            _scaffoldingPreviewCurrentYaw = Mathf.SmoothDampAngle(
                _scaffoldingPreviewCurrentYaw,
                targetYaw,
                ref _scaffoldingPreviewYawVelocity,
                _scaffoldingRotationSmoothTime);
            return Quaternion.Euler(0f, _scaffoldingPreviewCurrentYaw, 0f);
        }

        private void SetCarriedCollidersEnabled(bool enabled)
        {
            if (_carriedObject == null || _savedColliderEnabled == null)
                return;

            Collider[] colliders = _carriedObject.Colliders;
            for (int i = 0; i < colliders.Length && i < _savedColliderEnabled.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = enabled && _savedColliderEnabled[i];
            }
        }

        private void RestoreCarriedColliders(GrabbableObject target)
        {
            Collider[] colliders = target.Colliders;
            for (int i = 0; i < colliders.Length && _savedColliderEnabled != null && i < _savedColliderEnabled.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = _savedColliderEnabled[i];
            }

            _savedColliderEnabled = null;
        }

        private void CreateScaffoldingPreview(ScaffoldingPart part)
        {
            _carriedScaffoldingRenderers = part.GetComponentsInChildren<Renderer>(true);
            _savedRendererEnabled = new bool[_carriedScaffoldingRenderers.Length];
            for (int i = 0; i < _carriedScaffoldingRenderers.Length; i++)
            {
                Renderer renderer = _carriedScaffoldingRenderers[i];
                if (renderer == null)
                    continue;

                _savedRendererEnabled[i] = renderer.enabled;
                renderer.enabled = false;
            }

            _scaffoldingPreviewMaterial = CreateScaffoldingPreviewMaterial();
            _scaffoldingPreview = new GameObject($"{part.name}_PlacementPreview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _scaffoldingPreview.transform.SetPositionAndRotation(part.transform.position, part.transform.rotation);
            _scaffoldingPreview.transform.localScale = part.transform.lossyScale;
            CopyPreviewMeshes(part.transform, _scaffoldingPreview.transform);

            part.BeginPlacementPreview();
            RefreshScaffoldingPreview();
        }

        private void CopyPreviewMeshes(Transform source, Transform preview)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
            if (sourceFilter != null && sourceFilter.sharedMesh != null && sourceRenderer != null)
            {
                MeshFilter previewFilter = preview.gameObject.AddComponent<MeshFilter>();
                previewFilter.sharedMesh = sourceFilter.sharedMesh;
                MeshRenderer previewRenderer = preview.gameObject.AddComponent<MeshRenderer>();
                int submeshCount = sourceFilter.sharedMesh.subMeshCount;
                Material[] materials = new Material[submeshCount];
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = _scaffoldingPreviewMaterial;
                previewRenderer.sharedMaterials = materials;
                previewRenderer.shadowCastingMode = ShadowCastingMode.Off;
                previewRenderer.receiveShadows = false;
            }

            for (int i = 0; i < source.childCount; i++)
            {
                Transform sourceChild = source.GetChild(i);
                var previewChild = new GameObject(sourceChild.name).transform;
                previewChild.SetParent(preview, false);
                previewChild.localPosition = sourceChild.localPosition;
                previewChild.localRotation = sourceChild.localRotation;
                previewChild.localScale = sourceChild.localScale;
                CopyPreviewMeshes(sourceChild, previewChild);
            }
        }

        private static Material CreateScaffoldingPreviewMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private void RefreshScaffoldingPreview()
        {
            if (_carriedScaffoldingPart == null || _scaffoldingPreviewMaterial == null)
                return;

            SetCarriedCollidersEnabled(true);
            try
            {
                _scaffoldingPreviewCanConnect = _carriedScaffoldingPart.CanConnectToTouchingParts();
            }
            finally
            {
                SetCarriedCollidersEnabled(false);
            }

            Color tint = _scaffoldingPreviewCanConnect
                ? new Color(0.2f, 1f, 0.25f, 0.45f)
                : new Color(1f, 0.2f, 0.15f, 0.45f);
            if (_scaffoldingPreviewMaterial.HasProperty(BaseColorId))
                _scaffoldingPreviewMaterial.SetColor(BaseColorId, tint);
            if (_scaffoldingPreviewMaterial.HasProperty(ColorId))
                _scaffoldingPreviewMaterial.SetColor(ColorId, tint);

            _nextScaffoldingPreviewRefresh = Time.unscaledTime + 0.1f;
        }

        private void RestoreCarriedScaffoldingRenderers()
        {
            if (_carriedScaffoldingRenderers == null)
                return;

            for (int i = 0; i < _carriedScaffoldingRenderers.Length && _savedRendererEnabled != null && i < _savedRendererEnabled.Length; i++)
            {
                if (_carriedScaffoldingRenderers[i] != null)
                    _carriedScaffoldingRenderers[i].enabled = _savedRendererEnabled[i];
            }

            _carriedScaffoldingRenderers = null;
            _savedRendererEnabled = null;
        }

        private void DestroyScaffoldingPreview()
        {
            if (_scaffoldingPreview != null)
            {
                _scaffoldingPreview.SetActive(false);
                Destroy(_scaffoldingPreview);
                _scaffoldingPreview = null;
            }

            if (_scaffoldingPreviewMaterial != null)
            {
                Destroy(_scaffoldingPreviewMaterial);
                _scaffoldingPreviewMaterial = null;
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
