using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ngecor.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float PushResponseTime = 0.1f;
        private const float FullSpeedPushMass = 5f;
        private const float StepProbeGroundClearance = 0.02f;

        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _lookAction;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private bool _isLocalPlayer;
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;
        [SerializeField, Min(0f)] private float _maxPushForce = 300f;
        [SerializeField, Min(0f)] private float _lookSensitivity = 0.1f;

        private CharacterController _characterController;
        private Camera _playerCamera;
        private bool _cursorLocked;
        private float _verticalVelocity;
        private float _cameraPitch;
        private float _remainingPushImpulse;
        private readonly HashSet<Rigidbody> _pushedBodies = new HashSet<Rigidbody>();
        private RaycastHit[] _stepProbeHits = new RaycastHit[8];

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerCamera = GetComponentInChildren<Camera>(true);
            if (_cameraPivot != null)
                _cameraPitch = Mathf.DeltaAngle(0f, _cameraPivot.localEulerAngles.x);
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
            if (_isLocalPlayer)
                SetCursorLocked(false);
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
            var direction = transform.right * input.x + transform.forward * input.y;
            direction.y = 0f;

            var deltaTime = Time.deltaTime;
            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Physics.gravity.y * deltaTime;

            var movement = direction * _moveSpeed + Vector3.up * _verticalVelocity;
            _remainingPushImpulse = _maxPushForce * deltaTime;
            _pushedBodies.Clear();

            var stepOffset = _characterController.stepOffset;
            var horizontalDistance = direction.magnitude * _moveSpeed * deltaTime;
            if (ShouldBlockStepOverDynamicBody(direction, horizontalDistance))
                _characterController.stepOffset = 0f;

            _characterController.Move(movement * deltaTime);
            _characterController.stepOffset = stepOffset;
        }

        private bool ShouldBlockStepOverDynamicBody(Vector3 movementDirection, float movementDistance)
        {
            if (!_characterController.isGrounded || _characterController.stepOffset <= 0f ||
                movementDirection.sqrMagnitude <= Mathf.Epsilon || movementDistance <= 0f)
                return false;

            var scale = transform.lossyScale;
            var radius = _characterController.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var height = _characterController.height * Mathf.Abs(scale.y);
            var capsuleCenter = transform.TransformPoint(_characterController.center) +
                                Vector3.up * StepProbeGroundClearance;
            var verticalSegment = Mathf.Max(0f, height * 0.5f - radius);
            var lowerSphereCenter = capsuleCenter - Vector3.up * verticalSegment;
            var upperSphereCenter = capsuleCenter + Vector3.up * verticalSegment;
            int hitCount;
            while (true)
            {
                hitCount = Physics.CapsuleCastNonAlloc(
                    lowerSphereCenter,
                    upperSphereCenter,
                    radius,
                    movementDirection.normalized,
                    _stepProbeHits,
                    movementDistance + _characterController.skinWidth,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore);
                if (hitCount < _stepProbeHits.Length)
                    break;

                System.Array.Resize(ref _stepProbeHits, _stepProbeHits.Length * 2);
            }

            var maximumStepHeight = _characterController.bounds.min.y + _characterController.stepOffset;

            for (var i = 0; i < hitCount; i++)
            {
                var collider = _stepProbeHits[i].collider;
                if (collider == null || collider.transform == transform || collider.transform.IsChildOf(transform))
                    continue;
                if (Physics.GetIgnoreLayerCollision(gameObject.layer, collider.gameObject.layer) ||
                    Physics.GetIgnoreCollision(_characterController, collider))
                    continue;

                var body = _stepProbeHits[i].rigidbody;
                if (body != null && !body.isKinematic && collider.bounds.max.y <= maximumStepHeight)
                    return true;
            }

            return false;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.moveDirection.y < -0.3f)
                return;

            var pushDirection = Vector3.ProjectOnPlane(hit.moveDirection, Vector3.up);
            if (pushDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            ApplyContactPush(hit.rigidbody, hit.point, pushDirection, Time.deltaTime);
        }

        private void ApplyContactPush(Rigidbody body, Vector3 point, Vector3 pushDirection, float deltaTime)
        {
            if (body == null || body.isKinematic || _remainingPushImpulse <= 0f || _pushedBodies.Contains(body))
                return;

            pushDirection = Vector3.ProjectOnPlane(pushDirection, Vector3.up);
            if (pushDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            pushDirection.Normalize();
            point.y = body.worldCenterOfMass.y;
            var currentSpeed = Vector3.Dot(body.GetPointVelocity(point), pushDirection);
            var pushForce = CalculatePushForce(
                body.mass,
                CalculatePushTargetSpeed(body.mass, _moveSpeed),
                currentSpeed,
                deltaTime,
                PushResponseTime,
                _maxPushForce);
            var impulse = Mathf.Min(pushForce * deltaTime, _remainingPushImpulse);
            if (impulse <= 0f)
                return;

            _pushedBodies.Add(body);
            body.AddForceAtPosition(pushDirection * impulse, point, ForceMode.Impulse);
            _remainingPushImpulse -= impulse;
        }

        public static float CalculatePushTargetSpeed(float mass, float moveSpeed)
        {
            if (mass <= 0f || moveSpeed <= 0f)
                return 0f;

            var speedScale = Mathf.Min(1f, Mathf.Sqrt(FullSpeedPushMass / mass));
            return moveSpeed * speedScale;
        }

        public static float CalculatePushForce(
            float mass,
            float targetSpeed,
            float currentSpeed,
            float deltaTime,
            float responseTime,
            float maxForce)
        {
            if (mass <= 0f || targetSpeed <= currentSpeed || deltaTime <= 0f || responseTime <= 0f || maxForce <= 0f)
                return 0f;

            var response = 1f - Mathf.Exp(-deltaTime / responseTime);
            var requiredForce = mass * (targetSpeed - currentSpeed) * response / deltaTime;
            return Mathf.Min(requiredForce, maxForce);
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
