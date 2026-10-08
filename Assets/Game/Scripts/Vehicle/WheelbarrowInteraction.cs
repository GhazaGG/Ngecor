using Ngecor.Interaction;
using Ngecor.Player;
using UnityEngine;

namespace Ngecor.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WheelbarrowInteraction : MonoBehaviour, IHoldInteractable
    {
        private const float GripAlignmentTolerance = 0.1f;
        private const float GripPathGroundClearance = 0.02f;
        private const float MaxSeparationMultiplier = 2f;
        private const float MaxSteeringSpeed = 2f;
        private const float MaxSteeringForce = 150f;
        private const float PushResponseTime = 0.08f;
        private const float ReversePushStrength = 0.65f;

        [SerializeField] private Collider[] _handleColliders;

        private Rigidbody _rigidbody;
        private PlayerMovement _interactingPlayer;
        private PlayerGrab _interactingGrab;
        private CharacterController _interactingController;
        private RaycastHit[] _gripPathHits = new RaycastHit[8];
        private RaycastHit[] _wheelHits = new RaycastHit[4];
        private Vector3 _previousHeading;

        public string InteractionPrompt => "Push";

        private Rigidbody Rigidbody => _rigidbody != null ? _rigidbody : (_rigidbody = GetComponent<Rigidbody>());

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null || Rigidbody == null || Rigidbody.isKinematic || _interactingPlayer != null)
                return false;

            var grab = interactor.GetComponent<PlayerGrab>();
            return grab != null && !grab.IsCarrying && IsAnyHandleInRange(interactor.transform.position, grab.MaxGrabDistance);
        }

        public bool CanInteractFrom(GameObject interactor, Collider collider)
        {
            var grab = interactor != null ? interactor.GetComponent<PlayerGrab>() : null;
            return grab != null && IsHandleCollider(collider) &&
                   Vector3.Distance(interactor.transform.position, collider.ClosestPoint(interactor.transform.position)) <= grab.MaxGrabDistance;
        }

        public bool TryBeginInteraction(GameObject interactor)
        {
            if (!CanInteract(interactor))
                return false;

            var player = interactor.GetComponent<PlayerMovement>();
            var grab = interactor.GetComponent<PlayerGrab>();
            var controller = interactor.GetComponent<CharacterController>();
            if (player == null || player.LocalCamera == null || grab == null || controller == null)
                return false;

            if (!TryGetGripPose(controller, out var handlePoint, out var gripPosition, out var outward))
            {
                grab.ShowInteractionFeedback("The wheelbarrow handles do not have a clear grip position.");
                return false;
            }

            var startPosition = interactor.transform.position;
            var sideOffset = Vector3.ProjectOnPlane(startPosition - handlePoint, outward);
            sideOffset.y = 0f;
            var waypoint = gripPosition + sideOffset;
            var firstStep = waypoint - startPosition;
            var secondStep = gripPosition - waypoint;
            if (firstStep.magnitude + secondStep.magnitude > grab.MaxGrabDistance)
            {
                grab.ShowInteractionFeedback("Move closer to the back of the wheelbarrow handles.");
                return false;
            }

            if (!IsGripPathClear(interactor, controller, firstStep) ||
                !IsGripPathClear(interactor, controller, secondStep, firstStep))
            {
                grab.ShowInteractionFeedback("The grip position is blocked. Clear a path behind the handles.");
                return false;
            }

            player.SetInteractableMovement(true);
            controller.Move(firstStep);
            controller.Move(secondStep);
            var remainingAlignment = Vector3.ProjectOnPlane(gripPosition - interactor.transform.position, Vector3.up).magnitude;
            if (remainingAlignment > GripAlignmentTolerance)
            {
                player.SetInteractableMovement(false);
                grab.ShowInteractionFeedback("The grip position is blocked. Clear a path behind the handles.");
                return false;
            }

            player.transform.rotation = Quaternion.LookRotation(-outward, Vector3.up);
            _previousHeading = -outward;
            _interactingPlayer = player;
            _interactingGrab = grab;
            _interactingController = controller;
            return true;
        }

        public void EndInteraction(GameObject interactor)
        {
            if (_interactingPlayer != null && _interactingPlayer.gameObject == interactor)
                StopInteraction();
        }

        private void FixedUpdate()
        {
            if (_interactingPlayer != null && _interactingPlayer.isActiveAndEnabled && Rigidbody != null && !Rigidbody.isKinematic)
                ApplyWheelGripAndSteering();
        }

        private void LateUpdate()
        {
            if (_interactingPlayer == null)
                return;

            if (Rigidbody == null || Rigidbody.isKinematic || !_interactingPlayer.isActiveAndEnabled ||
                _interactingPlayer.LocalCamera == null || _interactingGrab == null || !_interactingGrab.isActiveAndEnabled ||
                _interactingController == null || !_interactingController.enabled)
            {
                StopInteraction("The wheelbarrow interaction ended because the player or wheelbarrow is unavailable.");
                return;
            }

            if (!TryGetGripPose(_interactingController, out var handlePoint, out var gripPosition, out var outward))
            {
                StopInteraction("The wheelbarrow handle is no longer available.");
                return;
            }

            if (Vector3.Distance(_interactingPlayer.transform.position, handlePoint) >
                _interactingGrab.MaxGrabDistance * MaxSeparationMultiplier)
            {
                StopInteraction("You became too far from the wheelbarrow handles, so the interaction was released.");
                return;
            }

            var alignment = gripPosition - _interactingPlayer.transform.position;
            var followStep = Vector3.ClampMagnitude(alignment, _interactingPlayer.MoveSpeed * Time.deltaTime);
            if (followStep.sqrMagnitude > Mathf.Epsilon)
                _interactingController.Move(followStep);

            var currentHeading = -outward;
            if (_previousHeading.sqrMagnitude > Mathf.Epsilon)
            {
                var deltaYaw = Vector3.SignedAngle(_previousHeading, currentHeading, Vector3.up);
                if (Mathf.Abs(deltaYaw) > 0.001f)
                    _interactingPlayer.transform.Rotate(Vector3.up, deltaYaw, Space.World);
            }
            _previousHeading = currentHeading;

            var input = _interactingPlayer.MovementInput;
            if (Mathf.Abs(input.y) > 0.01f)
            {
                var pushStrength = Mathf.Abs(input.y) * (input.y < 0f ? ReversePushStrength : 1f);
                _interactingPlayer.ApplyMovementPush(
                    Rigidbody, handlePoint, transform.forward * Mathf.Sign(input.y), pushStrength, Time.deltaTime);
            }
        }

        private void OnDisable() => StopInteraction("The wheelbarrow interaction ended because the wheelbarrow is unavailable.");

        private bool IsHandleCollider(Collider collider)
        {
            if (collider == null || _handleColliders == null)
                return false;

            for (var i = 0; i < _handleColliders.Length; i++)
            {
                if (_handleColliders[i] == collider)
                    return true;
            }

            return false;
        }

        private bool IsAnyHandleInRange(Vector3 position, float maxDistance)
        {
            if (_handleColliders == null)
                return false;

            var maxDistanceSquared = maxDistance * maxDistance;
            for (var i = 0; i < _handleColliders.Length; i++)
            {
                var collider = _handleColliders[i];
                if (collider != null && (collider.ClosestPoint(position) - position).sqrMagnitude <= maxDistanceSquared)
                    return true;
            }

            return false;
        }

        private bool TryGetHandlePoint(out Vector3 handlePoint, out Vector3 outward)
        {
            handlePoint = Vector3.zero;
            outward = Vector3.zero;
            if (Rigidbody == null || _handleColliders == null)
                return false;

            var count = 0;
            for (var i = 0; i < _handleColliders.Length; i++)
            {
                var collider = _handleColliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;

                handlePoint += collider.bounds.center;
                count++;
            }

            if (count == 0)
                return false;

            handlePoint /= count;
            outward = Vector3.ProjectOnPlane(handlePoint - Rigidbody.worldCenterOfMass, Vector3.up);
            if (outward.sqrMagnitude <= Mathf.Epsilon)
                return false;

            outward.Normalize();
            return true;
        }

        private bool TryGetGripPose(
            CharacterController controller, out Vector3 handlePoint, out Vector3 playerPosition, out Vector3 outward)
        {
            playerPosition = Vector3.zero;
            handlePoint = Vector3.zero;
            outward = Vector3.zero;
            if (controller == null || !TryGetHandlePoint(out handlePoint, out outward))
                return false;

            var handleDepth = 0f;
            for (var i = 0; i < _handleColliders.Length; i++)
            {
                var collider = _handleColliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;

                var bounds = collider.bounds;
                var extent = Mathf.Abs(outward.x) * bounds.extents.x + Mathf.Abs(outward.z) * bounds.extents.z;
                handleDepth = Mathf.Max(handleDepth, Vector3.Dot(bounds.center - handlePoint, outward) + extent);
            }

            var scale = controller.transform.lossyScale;
            var radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            playerPosition = handlePoint + outward * (handleDepth + radius + controller.skinWidth + 0.05f);
            playerPosition.y = controller.transform.position.y;
            return true;
        }

        private void ApplyWheelGripAndSteering()
        {
            if (!TryGetHandlePoint(out var handlePoint, out var outward))
                return;

            var forward = -outward;
            var outwardRight = Vector3.Cross(Vector3.up, forward).normalized;
            var wheelPos = GetWheelPosition(handlePoint);

            // Front wheel acts as fulcrum by resisting lateral sliding
            Vector3 wheelContactPoint;
            TryGetWheelGroundContact(wheelPos, out wheelContactPoint);

            var wheelVelocity = Rigidbody.GetPointVelocity(wheelContactPoint);
            var wheelLateralSpeed = Vector3.Dot(wheelVelocity, outwardRight);
            if (Mathf.Abs(wheelLateralSpeed) > 0.001f)
            {
                var gripMass = Mathf.Max(1f, Rigidbody.mass);
                var desiredGripForce = -wheelLateralSpeed * gripMass / Time.fixedDeltaTime;
                var maxGripForce = Mathf.Max(150f, Rigidbody.mass * 30f);
                var clampedGripForce = Mathf.Clamp(desiredGripForce, -maxGripForce, maxGripForce);
                Rigidbody.AddForceAtPosition(outwardRight * clampedGripForce, wheelContactPoint, ForceMode.Force);
            }

            // Player lateral steering at handles
            var input = _interactingPlayer.MovementInput.x;
            if (Mathf.Abs(input) > 0.01f)
            {
                var swingDir = -outwardRight * Mathf.Sign(input);
                var radius = Mathf.Max(0.1f, Vector3.ProjectOnPlane(handlePoint - wheelPos, Vector3.up).magnitude);
                var targetYawSpeed = Mathf.Abs(input) * MaxSteeringSpeed;
                var currentYawSpeed = Vector3.Dot(Rigidbody.angularVelocity, Vector3.up) * Mathf.Sign(input);
                var targetSpeed = targetYawSpeed * radius;
                var currentHandleSpeed = Vector3.Dot(Rigidbody.GetPointVelocity(handlePoint), swingDir);

                if (currentYawSpeed < targetYawSpeed)
                {
                    var pushForce = PlayerMovement.CalculatePushForce(
                        Rigidbody.mass,
                        targetSpeed,
                        currentHandleSpeed,
                        Time.fixedDeltaTime,
                        PushResponseTime,
                        MaxSteeringForce);

                    if (pushForce > 0f)
                        Rigidbody.AddForceAtPosition(swingDir * pushForce, handlePoint, ForceMode.Force);
                }
                else if (currentYawSpeed > targetYawSpeed + 0.05f)
                {
                    var excessSpeed = (currentYawSpeed - targetYawSpeed) * radius;
                    var brakeForce = Mathf.Clamp(excessSpeed * Rigidbody.mass / Time.fixedDeltaTime, 0f, MaxSteeringForce);
                    Rigidbody.AddForceAtPosition(-swingDir * brakeForce, handlePoint, ForceMode.Force);
                }
            }
            else
            {
                var handleLateralSpeed = Vector3.Dot(Rigidbody.GetPointVelocity(handlePoint), outwardRight);
                if (Mathf.Abs(handleLateralSpeed) > 0.01f)
                {
                    var dampingForce = -handleLateralSpeed * Rigidbody.mass * 4f;
                    var maxDamping = Mathf.Max(50f, Rigidbody.mass * 10f);
                    dampingForce = Mathf.Clamp(dampingForce, -maxDamping, maxDamping);
                    Rigidbody.AddForceAtPosition(outwardRight * dampingForce, handlePoint, ForceMode.Force);
                }
            }
        }

        private Vector3 GetWheelPosition(Vector3 handlePoint)
        {
            var wheelTransform = transform.Find("Wheel");
            if (wheelTransform != null)
                return wheelTransform.position;

            var com = Rigidbody.worldCenterOfMass;
            var toHandle = Vector3.ProjectOnPlane(handlePoint - com, Vector3.up);
            var handleDist = toHandle.magnitude;
            if (handleDist > Mathf.Epsilon)
                return com - toHandle.normalized * (handleDist * 0.5f);

            return com + transform.forward * 0.5f;
        }

        private bool TryGetWheelGroundContact(Vector3 wheelPosition, out Vector3 groundContactPoint)
        {
            groundContactPoint = wheelPosition;
            var rayStart = wheelPosition + Vector3.up * 0.2f;
            const float rayDistance = 0.5f;
            var hitCount = Physics.RaycastNonAlloc(
                rayStart, Vector3.down, _wheelHits, rayDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hitCount; i++)
            {
                var col = _wheelHits[i].collider;
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform))
                    continue;
                if (_interactingPlayer != null &&
                    (col.transform == _interactingPlayer.transform || col.transform.IsChildOf(_interactingPlayer.transform)))
                    continue;

                groundContactPoint = _wheelHits[i].point;
                return true;
            }

            return false;
        }

        private bool IsGripPathClear(
            GameObject interactor, CharacterController controller, Vector3 movement, Vector3 startOffset = default)
        {
            if (movement.sqrMagnitude <= Mathf.Epsilon)
                return true;

            var scale = controller.transform.lossyScale;
            var radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var height = controller.height * Mathf.Abs(scale.y);
            var capsuleCenter = controller.transform.TransformPoint(controller.center) + startOffset +
                                Vector3.up * GripPathGroundClearance;
            var verticalSegment = Mathf.Max(0f, height * 0.5f - radius);
            var lowerSphereCenter = capsuleCenter - Vector3.up * verticalSegment;
            var upperSphereCenter = capsuleCenter + Vector3.up * verticalSegment;
            var direction = movement.normalized;
            int hitCount;
            while (true)
            {
                hitCount = Physics.CapsuleCastNonAlloc(
                    lowerSphereCenter, upperSphereCenter, radius, direction, _gripPathHits,
                    movement.magnitude, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                if (hitCount < _gripPathHits.Length)
                    break;

                System.Array.Resize(ref _gripPathHits, _gripPathHits.Length * 2);
            }

            for (var i = 0; i < hitCount; i++)
            {
                var collider = _gripPathHits[i].collider;
                if (collider == null || collider.transform == interactor.transform ||
                    collider.transform.IsChildOf(interactor.transform) ||
                    Physics.GetIgnoreLayerCollision(interactor.layer, collider.gameObject.layer) ||
                    Physics.GetIgnoreCollision(controller, collider))
                    continue;

                return false;
            }

            return true;
        }

        private void StopInteraction(string feedback = null)
        {
            var player = _interactingPlayer;
            _interactingPlayer = null;
            _interactingGrab = null;
            _interactingController = null;
            _previousHeading = Vector3.zero;
            if (player != null)
            {
                player.SetInteractableMovement(false);
                player.GetComponent<PlayerGrab>()?.NotifyInteractionEnded(this, feedback);
            }
        }
    }
}
