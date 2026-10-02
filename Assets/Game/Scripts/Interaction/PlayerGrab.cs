using UnityEngine;
using Ngecor.Player;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(InteractionDetector))]
    public class PlayerGrab : MonoBehaviour
    {
        [SerializeField] private Transform _holdPoint;
        [SerializeField, Min(0.1f)] private float _maxGrabDistance = 3.5f;

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

        private void Awake()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            _detector = GetComponent<InteractionDetector>();
            _playerColliders = GetComponentsInChildren<Collider>();
        }

        private void LateUpdate()
        {
            if (!IsCarrying)
            {
                if (_carriedObject != null)
                    _carriedObject = null;
                return;
            }

            var holdPoint = HoldPoint;
            if (holdPoint != null && _carriedObject != null)
            {
                var rb = _carriedObject.Rigidbody;
                if (rb != null)
                {
                    rb.position = holdPoint.position;
                    rb.rotation = holdPoint.rotation;
                }
                _carriedObject.transform.position = holdPoint.position;
                _carriedObject.transform.rotation = holdPoint.rotation;
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
                if (rb != null)
                {
                    rb.position = holdPoint.position;
                    rb.rotation = holdPoint.rotation;
                }
                target.transform.position = holdPoint.position;
                target.transform.rotation = holdPoint.rotation;
            }
        }

        private void DetachObject()
        {
            if (_carriedObject != null)
            {
                var target = _carriedObject;
                _carriedObject = null;

                SetPlayerCollisionIgnored(target, false);

                var rb = target.Rigidbody;
                if (rb != null)
                {
                    rb.isKinematic = _savedWasKinematic;
                    rb.useGravity = _savedUseGravity;
                }

                target.OnRelease();
            }
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
