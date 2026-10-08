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
        private const float MaxSteeringTorque = 25f;
        private const float SteeringResponse = 10f;
        private const float ReversePushStrength = 0.65f;

        [SerializeField] private Collider[] _handleColliders;

        private Rigidbody _rigidbody;
        private PlayerMovement _interactingPlayer;
        private PlayerGrab _interactingGrab;
        private CharacterController _interactingController;
        private RaycastHit[] _gripPathHits = new RaycastHit[8];

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
                ApplySteering(_interactingPlayer.MovementInput.x);
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

            if (!TryGetGripPose(_interactingController, out var handlePoint, out var gripPosition, out _))
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

        private bool TryGetGripPose(
            CharacterController controller, out Vector3 handlePoint, out Vector3 playerPosition, out Vector3 outward)
        {
            handlePoint = Vector3.zero;
            playerPosition = Vector3.zero;
            outward = Vector3.zero;
            if (controller == null || Rigidbody == null || _handleColliders == null)
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

        private void ApplySteering(float input)
        {
            var currentYawSpeed = Vector3.Dot(Rigidbody.angularVelocity, Vector3.up);
            var torque = Mathf.Clamp(
                (Mathf.Clamp(input, -1f, 1f) * MaxSteeringSpeed - currentYawSpeed) * SteeringResponse,
                -MaxSteeringTorque,
                MaxSteeringTorque);
            Rigidbody.AddTorque(Vector3.up * torque, ForceMode.Force);
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
            if (player != null)
            {
                player.SetInteractableMovement(false);
                player.GetComponent<PlayerGrab>()?.NotifyInteractionEnded(this, feedback);
            }
        }
    }
}
