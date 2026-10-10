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
        [SerializeField, Range(0f, 0.05f)] private float _massSpeedPenaltyFactor = 0.012f;
        [SerializeField, Range(0.2f, 1f)] private float _minMassSpeedMultiplier = 0.6f;

        private CharacterController _characterController;
        private Camera _playerCamera;
        private bool _cursorLocked;
        private float _verticalVelocity;
        private float _cameraPitch;
        private float _carriedMass;
        private float _remainingPushImpulse;
        private Vector3 _movementDirection;
        private Vector2 _movementInput;
        private bool _interactableControlsMovement;
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

        private int _cursorRelockedFrame = -1;

        public Camera LocalCamera => _isLocalPlayer ? _playerCamera : null;
        public bool IsCursorLocked => _cursorLocked;
        public bool CursorRelockedThisFrame => _cursorRelockedFrame == Time.frameCount;
        public Vector2 MovementInput => _movementInput;
        public float MoveSpeed => _moveSpeed;

        // surfaceNormal (zero means world up) is the plane the push lies in; the force acts on the line through the
        // body's centre of mass inside that plane, so pushing along a ramp neither lifts nor pitches the body.
        public void ApplyMovementPush(
            Rigidbody body, Vector3 point, Vector3 pushDirection, float strength, float deltaTime, Vector3 surfaceNormal = default)
        {
            if (_isLocalPlayer && isActiveAndEnabled)
                ApplyContactPush(body, point, pushDirection, deltaTime, strength, surfaceNormal);
        }

        // Holds a pushed body back to the speed the push would reach (for example a barrow running downhill), using
        // the same speed law and force limit as the push. It stops the body outrunning the player and never pushes.
        public void ApplyMovementBrake(
            Rigidbody body, Vector3 pushDirection, float strength, float deltaTime, Vector3 surfaceNormal = default)
        {
            strength = Mathf.Clamp01(strength);
            if (!_isLocalPlayer || !isActiveAndEnabled || body == null || body.isKinematic || strength <= 0f)
                return;

            var direction = Vector3.ProjectOnPlane(pushDirection, NormalOrUp(surfaceNormal));
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            direction.Normalize();
            var centreOfMass = body.worldCenterOfMass;
            var targetSpeed = CalculatePushTargetSpeed(body.mass, _moveSpeed * strength);
            var currentSpeed = Vector3.Dot(body.GetPointVelocity(centreOfMass), direction);
            // Braking is the push law mirrored: a push against the motion toward a speed below the current one. The
            // response time is one frame so the brake holds the target instead of a proportional lag above it.
            var brakeForce = CalculatePushForce(
                body.mass, -targetSpeed, -currentSpeed, deltaTime, deltaTime, _maxPushForce * strength);
            if (brakeForce > 0f)
                body.AddForceAtPosition(-direction * (brakeForce * deltaTime), centreOfMass, ForceMode.Impulse);
        }

        private static Vector3 NormalOrUp(Vector3 surfaceNormal) =>
            surfaceNormal.sqrMagnitude > Mathf.Epsilon ? surfaceNormal.normalized : Vector3.up;

        public void SetInteractableMovement(bool controlled)
        {
            _interactableControlsMovement = controlled;
        }

        public float CarriedMass
        {
            get => _carriedMass;
            set => _carriedMass = Mathf.Max(0f, value);
        }

        public void SetLocalPlayer(bool isLocalPlayer)
        {
            if (_isLocalPlayer == isLocalPlayer)
                return;

            _isLocalPlayer = isLocalPlayer;
            SetCursorLocked(isLocalPlayer);
        }

        private void OnDisable()
        {
            _movementDirection = Vector3.zero;
            _movementInput = Vector2.zero;
            _interactableControlsMovement = false;
            if (_isLocalPlayer)
                SetCursorLocked(false);
        }

        private void Update()
        {
            if (!_isLocalPlayer || _characterController == null)
            {
                _movementDirection = Vector3.zero;
                _movementInput = Vector2.zero;
                return;
            }

            HandleCursorInput();

            if (_moveAction == null || _moveAction.action == null)
            {
                _movementDirection = Vector3.zero;
                _movementInput = Vector2.zero;
                return;
            }

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
            _movementInput = input;
            var direction = transform.right * input.x + transform.forward * input.y;
            direction.y = 0f;
            _movementDirection = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.zero;

            var deltaTime = Time.deltaTime;
            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Physics.gravity.y * deltaTime;

            var controlledDirection = _interactableControlsMovement ? Vector3.zero : direction;
            float massMultiplier = Mathf.Clamp(1f - _carriedMass * _massSpeedPenaltyFactor, _minMassSpeedMultiplier, 1f);
            var movement = controlledDirection * (_moveSpeed * massMultiplier) + Vector3.up * _verticalVelocity;
            _remainingPushImpulse = _maxPushForce * deltaTime;
            _pushedBodies.Clear();

            var stepOffset = _characterController.stepOffset;
            var horizontalDistance = controlledDirection.magnitude * (_moveSpeed * massMultiplier) * deltaTime;
            if (ShouldBlockStepOverDynamicBody(controlledDirection, horizontalDistance))
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
            if (_interactableControlsMovement)
                return;

            if (hit.moveDirection.y < -0.3f)
                return;

            var pushDirection = Vector3.ProjectOnPlane(hit.moveDirection, Vector3.up);
            if (pushDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            ApplyContactPush(hit.rigidbody, hit.point, pushDirection, Time.deltaTime);
        }

        private void ApplyContactPush(
            Rigidbody body, Vector3 point, Vector3 pushDirection, float deltaTime, float strength = 1f, Vector3 surfaceNormal = default)
        {
            strength = Mathf.Clamp01(strength);
            if (body == null || body.isKinematic || _remainingPushImpulse <= 0f || _pushedBodies.Contains(body) || strength <= 0f)
                return;

            var normal = NormalOrUp(surfaceNormal);
            pushDirection = Vector3.ProjectOnPlane(pushDirection, normal);
            if (pushDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            pushDirection.Normalize();
            point -= normal * Vector3.Dot(point - body.worldCenterOfMass, normal);
            var currentSpeed = Vector3.Dot(body.GetPointVelocity(point), pushDirection);
            var pushForce = CalculatePushForce(
                body.mass,
                CalculatePushTargetSpeed(body.mass, _moveSpeed * strength),
                currentSpeed,
                deltaTime,
                PushResponseTime,
                _maxPushForce * strength);
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
            {
                SetCursorLocked(true);
                _cursorRelockedFrame = Time.frameCount;
            }
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
