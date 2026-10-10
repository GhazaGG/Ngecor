using Ngecor.Interaction;
using Ngecor.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BucketPourAction), typeof(GrabbableObject))]
    public sealed class BucketPourInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference _pourAction;

        private BucketPourAction _bucketPour;
        private GrabbableObject _grabbable;
        private GameObject _currentHolder;
        private PlayerMovement _holderMovement;

        private void Awake()
        {
            _bucketPour = GetComponent<BucketPourAction>();
            _grabbable = GetComponent<GrabbableObject>();
        }

        private void OnEnable()
        {
            if (_pourAction != null && _pourAction.action != null)
                _pourAction.action.Enable();
        }

        private void Update()
        {
            if (!HasLocalHolder() || _pourAction == null || _pourAction.action == null)
            {
                CancelPour();
                return;
            }

            if (!_pourAction.action.enabled)
                _pourAction.action.Enable();

            if (_pourAction.action.IsPressed())
                RequestPour();
            else
                CancelPour();
        }

        public bool RequestPour()
        {
            return HasLocalHolder() && _bucketPour != null && _bucketPour.RequestPour();
        }

        public void CancelPour()
        {
            if (_bucketPour != null)
                _bucketPour.CancelPour();
        }

        private void OnDisable()
        {
            CancelPour();
            if (_pourAction != null && _pourAction.action != null)
                _pourAction.action.Disable();
        }

        private bool HasLocalHolder()
        {
            if (_grabbable == null)
                _grabbable = GetComponent<GrabbableObject>();

            if (_grabbable == null || !_grabbable.IsHeld)
            {
                _currentHolder = null;
                _holderMovement = null;
                return false;
            }

            var holder = _grabbable.CurrentHolder;
            if (holder != _currentHolder)
            {
                _currentHolder = holder;
                _holderMovement = holder != null ? holder.GetComponent<PlayerMovement>() : null;
            }

            return _holderMovement != null && _holderMovement.LocalCamera != null;
        }
    }
}
