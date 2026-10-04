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
        [SerializeField, Min(0f)] private float _climbSpeed = 2.5f;

        private CharacterController _characterController;
        private Camera _playerCamera;
        private LadderClimbable _activeLadder;
        private bool _cursorLocked;
        private float _verticalVelocity;
        private float _cameraPitch;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerCamera = GetComponentInChildren<Camera>(true);
            if (_cameraPivot != null)
                _cameraPitch = Mathf.DeltaAngle(0f, _cameraPivot.localEulerAngles.x);
        }

        private void OnEnable()
        {
            if (_moveAction != null)
                _moveAction.action.Enable();
            if (_lookAction != null)
                _lookAction.action.Enable();
        }

        private void Start()
        {
            if (_isLocalPlayer)
                SetCursorLocked(true);
        }

        public Camera LocalCamera => _isLocalPlayer ? _playerCamera : null;

        public void SetLocalPlayer(bool isLocalPlayer)
        {
            if (_isLocalPlayer == isLocalPlayer)
                return;

            _isLocalPlayer = isLocalPlayer;
            SetCursorLocked(isLocalPlayer);
        }

        private void OnDisable()
        {
            _activeLadder = null;
            if (_isLocalPlayer)
                SetCursorLocked(false);
        }

        private void OnTriggerEnter(Collider other) => SetActiveLadder(other);

        private void OnTriggerStay(Collider other) => SetActiveLadder(other);

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out LadderClimbable ladder) && ladder == _activeLadder)
                _activeLadder = null;
        }

        private void SetActiveLadder(Collider other)
        {
            if (other.TryGetComponent(out LadderClimbable ladder))
                _activeLadder = ladder;
        }

        private void Update()
        {
            if (!_isLocalPlayer || _characterController == null)
                return;

            HandleCursorInput();

            if (_moveAction == null)
                return;

            if (_lookAction != null && _cursorLocked)
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

            if (_activeLadder != null && input.y > 0f &&
                transform.position.y >= _activeLadder.TopExitHeight - _characterController.stepOffset)
                _activeLadder = null;

            if (_activeLadder != null)
            {
                _verticalVelocity = 0f;
                var ladderMovement = Vector3.up * (input.y * _climbSpeed) + transform.right * (input.x * _moveSpeed);
                _characterController.Move(ladderMovement * Time.deltaTime);
                return;
            }

            var direction = transform.right * input.x + transform.forward * input.y;
            direction.y = 0f;

            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Physics.gravity.y * Time.deltaTime;

            var movement = direction * _moveSpeed + Vector3.up * _verticalVelocity;
            _characterController.Move(movement * Time.deltaTime);
        }

        private void HandleCursorInput()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
                return;
            }

            if (!_cursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _isLocalPlayer && _cursorLocked)
                SetCursorLocked(true);
        }

        private void SetCursorLocked(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
