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
        // Same human push limit as PlayerMovement's _maxPushForce on the player prefab (docs/DECISIONS.md).
        private const float MaxSteeringForce = 350f;
        private const float MaxSteeringTilt = 15f;
        private const float SteeringLeadDeadZone = 0.03f;
        private const float SteeringStiffness = 600f;
        private const float SteeringDamping = 100f;
        private const float ReversePushStrength = 0.65f;

        [SerializeField] private Collider[] _handleColliders;
        [Header("Foot friction while pushing")]
        [Tooltip("Leg colliders that rest on the ground. They keep their own material (the parking brake) until the player pushes.")]
        [SerializeField] private Collider[] _legColliders;
        [Tooltip("Low-friction material the legs use only while the barrow is held and W or S is pressed.")]
        [SerializeField] private PhysicsMaterial _movingLegMaterial;
        [Header("Steering feel")]
        [Tooltip("Maximum yaw speed (rad/s) that A/D steering may build up.")]
        [SerializeField, Min(0.1f)] private float _maxSteeringSpeed = 2f;
        [Tooltip("Sideways walking speed (m/s) of the player while steering.")]
        [SerializeField, Min(0.1f)] private float _steeringLeadSpeed = 1.5f;
        [Tooltip("How far (m) the player may step sideways ahead of the handle.")]
        [SerializeField, Range(0.1f, 1f)] private float _maxSteeringLead = 0.5f;

        private Rigidbody _rigidbody;
        private PlayerMovement _interactingPlayer;
        private PlayerGrab _interactingGrab;
        private CharacterController _interactingController;
        private RaycastHit[] _gripPathHits = new RaycastHit[8];
        private RaycastHit[] _wheelHits = new RaycastHit[4];
        private Transform _wheelTransform;
        private Vector3 _previousHeading;
        private PhysicsMaterial[] _legRestMaterials;
        private bool _legsMoving;

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

            // The player leads sideways within a short leash (CharacterController.Move, so walls and obstacles
            // stop it); the follow step only restores the lateral part beyond that leash.
            var right = Vector3.Cross(Vector3.up, -outward).normalized;
            var alignment = gripPosition - _interactingPlayer.transform.position;
            var lateralLead = -Vector3.Dot(alignment, right);
            var excessLead = Mathf.Sign(lateralLead) * Mathf.Max(0f, Mathf.Abs(lateralLead) - _maxSteeringLead);
            var followTarget = alignment - right * Vector3.Dot(alignment, right) - right * excessLead;
            var followStep = Vector3.ClampMagnitude(followTarget, _interactingPlayer.MoveSpeed * Time.deltaTime);
            var moveStep = followStep + GetSteeringLeadStep(right, lateralLead);
            if (moveStep.sqrMagnitude > Mathf.Epsilon)
                _interactingController.Move(moveStep);

            var currentHeading = -outward;
            if (_previousHeading.sqrMagnitude > Mathf.Epsilon)
            {
                var deltaYaw = Vector3.SignedAngle(_previousHeading, currentHeading, Vector3.up);
                if (Mathf.Abs(deltaYaw) > 0.001f)
                    _interactingPlayer.transform.Rotate(Vector3.up, deltaYaw, Space.World);
            }
            _previousHeading = currentHeading;

            var input = _interactingPlayer.MovementInput;
            SetLegsMoving(Mathf.Abs(input.y) > 0.01f);
            if (Mathf.Abs(input.y) > 0.01f)
                ApplyGroundPlanePush(handlePoint, -outward, input.y);
        }

        // Push along the ground surface (normal from the wheel raycast) instead of horizontally: on a ramp a horizontal
        // pull-back lifts the barrow off the ground. The brake keeps a barrow rolling downhill from outrunning the player.
        private void ApplyGroundPlanePush(Vector3 handlePoint, Vector3 forward, float input)
        {
            var groundNormal = Vector3.up;
            if (TryGetWheelGroundContact(GetWheelPosition(handlePoint), out _, out var contactNormal))
                groundNormal = contactNormal;

            var direction = forward * Mathf.Sign(input);
            var strength = Mathf.Clamp01(Mathf.Abs(input) * (input < 0f ? ReversePushStrength : 1f));
            var centreOfMass = Rigidbody.worldCenterOfMass;
            _interactingPlayer.ApplyMovementPush(Rigidbody, centreOfMass, direction, strength, Time.deltaTime, groundNormal);
            _interactingPlayer.ApplyMovementBrake(Rigidbody, direction, strength, Time.deltaTime, groundNormal);
        }

        // Held and pushing (W/S): the legs slide, so the barrow rolls on its wheel instead of dragging like a table.
        // Otherwise, including while only steering with A/D, they keep their own material: that is the parking brake that
        // holds an idle barrow on a ramp and the friction the approved A/D steering feel was tuned with.
        private void SetLegsMoving(bool moving)
        {
            if (moving == _legsMoving || _legColliders == null || _movingLegMaterial == null)
                return;

            if (_legRestMaterials == null || _legRestMaterials.Length != _legColliders.Length)
            {
                _legRestMaterials = new PhysicsMaterial[_legColliders.Length];
                for (var i = 0; i < _legColliders.Length; i++)
                    _legRestMaterials[i] = _legColliders[i] != null ? _legColliders[i].sharedMaterial : null;
            }

            for (var i = 0; i < _legColliders.Length; i++)
            {
                if (_legColliders[i] != null)
                    _legColliders[i].sharedMaterial = moving ? _movingLegMaterial : _legRestMaterials[i];
            }

            _legsMoving = moving;
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

        // A/D: the player steps sideways first (see LateUpdate); the handle then chases that position, and the front
        // wheel contact is the pivot. Both forces lie in the plane through the centre of mass that is perpendicular to the
        // barrow's own up axis, so their couple turns the barrow about that axis only (no roll), also on a ramp where
        // the barrow is pitched and a world-vertical couple would partly roll it.
        private void ApplyWheelGripAndSteering()
        {
            if (_interactingController == null ||
                !TryGetGripPose(_interactingController, out var handlePoint, out var gripPosition, out var outward))
                return;

            var right = Vector3.Cross(Vector3.up, -outward).normalized;
            var bodyUp = transform.up;
            var forceRight = Vector3.Cross(bodyUp, Vector3.ProjectOnPlane(-outward, bodyUp)).normalized;
            var centreOfMass = Rigidbody.worldCenterOfMass;
            var wheelPosition = GetWheelPosition(handlePoint);
            var wheelGrounded = TryGetWheelGroundContact(wheelPosition, out var wheelContactPoint);

            // The ground only resists the wheel sideways while it really touches something.
            if (wheelGrounded)
            {
                var wheelLateralSpeed = Vector3.Dot(Rigidbody.GetPointVelocity(wheelContactPoint), forceRight);
                if (Mathf.Abs(wheelLateralSpeed) > 0.001f)
                {
                    var desiredGripForce = -wheelLateralSpeed * Mathf.Max(1f, Rigidbody.mass) / Time.fixedDeltaTime;
                    var maxGripForce = Mathf.Max(MaxSteeringForce * 2f, Rigidbody.mass * 30f);
                    Rigidbody.AddForceAtPosition(
                        forceRight * Mathf.Clamp(desiredGripForce, -maxGripForce, maxGripForce),
                        InYawPlane(wheelContactPoint, centreOfMass, bodyUp), ForceMode.Force);
                }
            }

            var leverPoint = InYawPlane(handlePoint, centreOfMass, bodyUp);
            var lateralLead = -Vector3.Dot(gripPosition - _interactingPlayer.transform.position, right);
            if (Mathf.Abs(lateralLead) > SteeringLeadDeadZone &&
                GetRollAngle() < MaxSteeringTilt)
            {
                // The handle is pulled toward the player's actual sideways position like a leash: force grows with the
                // lead, so a loaded barrow needs a bigger lead (and answers slower) without measuring its cargo.
                // A blocked player has no lead, so nothing pushes.
                var chaseDirection = forceRight * Mathf.Sign(lateralLead);
                var handleSpeed = Vector3.Dot(Rigidbody.GetPointVelocity(leverPoint), chaseDirection);

                // Effective mass of the handle point: yaw inertia (about the wheel while it touches the ground) over the
                // lever arm squared. The spring and damper are capped to what stays stable at this physics step; the
                // light empty barrow otherwise flips its yaw back and forth every step.
                var wheelArm = wheelGrounded
                    ? Vector3.ProjectOnPlane(centreOfMass - wheelPosition, bodyUp).magnitude
                    : 0f;
                var yawInertia = Rigidbody.inertiaTensor.y + Rigidbody.mass * wheelArm * wheelArm;
                var leverArm = Mathf.Max(0.1f, Vector3.ProjectOnPlane(handlePoint - wheelPosition, bodyUp).magnitude);
                var handleMass = yawInertia / (leverArm * leverArm);
                var dt = Time.fixedDeltaTime;
                var stiffness = Mathf.Min(SteeringStiffness, 0.25f * handleMass / (dt * dt));
                var damping = Mathf.Min(SteeringDamping, 0.5f * handleMass / dt);
                var pushForce = Mathf.Clamp(
                    Mathf.Abs(lateralLead) * stiffness - handleSpeed * damping, -MaxSteeringForce, MaxSteeringForce);

                // Swinging the handle right turns the nose left (negative yaw), and the other way round. Never push
                // harder than what keeps this step's yaw below the limit; brake if it is already above it.
                var yawSpeed = -Vector3.Dot(Rigidbody.angularVelocity, bodyUp) * Mathf.Sign(lateralLead);
                var yawHeadroom = Mathf.Max(0f, _maxSteeringSpeed - yawSpeed);
                pushForce = Mathf.Min(pushForce, yawHeadroom * yawInertia / (leverArm * dt));
                if (yawSpeed > _maxSteeringSpeed)
                    pushForce = Mathf.Max(-MaxSteeringForce, Mathf.Min(pushForce, 0f) - (yawSpeed - _maxSteeringSpeed) * MaxSteeringForce);
                Rigidbody.AddForceAtPosition(chaseDirection * pushForce, leverPoint, ForceMode.Force);
            }
            else if (wheelGrounded)
            {
                var handleLateralSpeed = Vector3.Dot(Rigidbody.GetPointVelocity(leverPoint), forceRight);
                if (Mathf.Abs(handleLateralSpeed) > 0.01f)
                {
                    var maxDamping = Mathf.Max(50f, Rigidbody.mass * 10f);
                    var dampingForce = Mathf.Clamp(-handleLateralSpeed * Rigidbody.mass * 4f, -maxDamping, maxDamping);
                    Rigidbody.AddForceAtPosition(forceRight * dampingForce, leverPoint, ForceMode.Force);
                }
            }
        }

        private static Vector3 InYawPlane(Vector3 point, Vector3 centreOfMass, Vector3 bodyUp) =>
            point - bodyUp * Vector3.Dot(point - centreOfMass, bodyUp);

        // Roll only: pitching up or down a ramp keeps the right axis horizontal, so slopes do not disable steering.
        private float GetRollAngle() => Mathf.Abs(90f - Vector3.Angle(transform.right, Vector3.up));

        private Vector3 GetSteeringLeadStep(Vector3 right, float lateralLead)
        {
            // A (-1) steps the player left, D (+1) steps right; the wheel pivot swings the nose the other way.
            var input = _interactingPlayer.MovementInput.x;
            if (Mathf.Abs(input) <= 0.01f)
                return Vector3.zero;

            var distance = Mathf.Abs(input) * _steeringLeadSpeed * Time.deltaTime;
            if (input * lateralLead > 0f)
                distance = Mathf.Min(distance, Mathf.Max(0f, _maxSteeringLead - Mathf.Abs(lateralLead)));

            return right * (Mathf.Sign(input) * distance);
        }

        private Vector3 GetWheelPosition(Vector3 handlePoint)
        {
            if (_wheelTransform == null)
                _wheelTransform = transform.Find("Wheel");
            if (_wheelTransform != null)
                return _wheelTransform.position;

            var com = Rigidbody.worldCenterOfMass;
            var toHandle = Vector3.ProjectOnPlane(handlePoint - com, Vector3.up);
            var handleDist = toHandle.magnitude;
            if (handleDist > Mathf.Epsilon)
                return com - toHandle.normalized * (handleDist * 0.5f);

            return com + transform.forward * 0.5f;
        }

        private bool TryGetWheelGroundContact(Vector3 wheelPosition, out Vector3 groundContactPoint)
            => TryGetWheelGroundContact(wheelPosition, out groundContactPoint, out _);

        private bool TryGetWheelGroundContact(
            Vector3 wheelPosition, out Vector3 groundContactPoint, out Vector3 groundNormal)
        {
            groundContactPoint = wheelPosition;
            groundNormal = Vector3.up;
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
                groundNormal = _wheelHits[i].normal;
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
            SetLegsMoving(false);
            if (player != null)
            {
                player.SetInteractableMovement(false);
                player.GetComponent<PlayerGrab>()?.NotifyInteractionEnded(this, feedback);
            }
        }
    }
}
