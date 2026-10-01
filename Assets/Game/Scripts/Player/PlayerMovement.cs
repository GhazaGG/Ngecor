using UnityEngine;
using UnityEngine.InputSystem;

namespace Ngecor.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _lookAction;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private bool _isLocalPlayer;
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;
        [SerializeField, Min(0f)] private float _lookSensitivity = 0.1f;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private float _cameraPitch;
        private bool _controlsEnabled;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_cameraPivot != null)
                _cameraPitch = Mathf.DeltaAngle(0f, _cameraPivot.localEulerAngles.x);
        }

        private void OnEnable()
        {
            EnableControls();
        }

        private void OnDisable()
        {
            DisableControls();
        }

        public void SetLocalPlayer(bool isLocalPlayer)
        {
            if (_isLocalPlayer == isLocalPlayer)
                return;

            _isLocalPlayer = isLocalPlayer;
            if (!isActiveAndEnabled)
                return;

            if (_isLocalPlayer)
                EnableControls();
            else
                DisableControls();
        }

        private void EnableControls()
        {
            if (!_isLocalPlayer || _controlsEnabled)
                return;

            if (_moveAction != null)
                _moveAction.action.Enable();
            if (_lookAction != null)
                _lookAction.action.Enable();
            _controlsEnabled = true;
        }

        private void DisableControls()
        {
            if (!_controlsEnabled)
                return;

            if (_moveAction != null)
                _moveAction.action.Disable();
            if (_lookAction != null)
                _lookAction.action.Disable();
            _controlsEnabled = false;
        }

        private void Update()
        {
            if (!_isLocalPlayer || _moveAction == null || _characterController == null)
                return;

            if (_lookAction != null)
            {
                var look = _lookAction.action.ReadValue<Vector2>() * _lookSensitivity;
                transform.Rotate(Vector3.up, look.x, Space.World);

                if (_cameraPivot != null)
                {
                    _cameraPitch = Mathf.Clamp(_cameraPitch - look.y, -89f, 89f);
                    _cameraPivot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
                }
            }

            var input = Vector2.ClampMagnitude(_moveAction.action.ReadValue<Vector2>(), 1f);
            var direction = transform.right * input.x + transform.forward * input.y;
            direction.y = 0f;

            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Physics.gravity.y * Time.deltaTime;

            var movement = direction * _moveSpeed + Vector3.up * _verticalVelocity;
            _characterController.Move(movement * Time.deltaTime);
        }
    }
}
