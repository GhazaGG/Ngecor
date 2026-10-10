using NUnit.Framework;
using UnityEngine;
using Ngecor.Player;
using Ngecor.Interaction;
using Ngecor.Multiplayer;

namespace Ngecor.Multiplayer.Tests
{
    [TestFixture]
    public class NetworkPlayerInteractionTests
    {
        private GameObject _playerObject;
        private CharacterController _characterController;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private PlayerGrab _playerGrab;
        private NetworkPlayerInteraction _interaction;
        private GameObject _targetObject;
        private GrabbableObject _grabbable;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("TestPlayer");
            _characterController = _playerObject.AddComponent<CharacterController>();
            _playerMovement = _playerObject.AddComponent<PlayerMovement>();
            _detector = _playerObject.AddComponent<InteractionDetector>();
            _playerGrab = _playerObject.AddComponent<PlayerGrab>();
            _interaction = _playerObject.AddComponent<NetworkPlayerInteraction>();

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(_playerObject.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            _targetObject = new GameObject("Target");
            _targetObject.transform.position = new Vector3(0f, 1.4f, 2f);
            var rb = _targetObject.AddComponent<Rigidbody>();
            var col = _targetObject.AddComponent<BoxCollider>();
            _grabbable = _targetObject.AddComponent<GrabbableObject>();

            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null)
                Object.DestroyImmediate(_playerObject);
            if (_targetObject != null)
                Object.DestroyImmediate(_targetObject);
        }

        [Test]
        public void ValidateGrabRequest_WithinDistance_ReturnsTrue()
        {
            _targetObject.transform.position = new Vector3(0f, 1.4f, 2f);
            bool isValid = _interaction.ValidateGrabTarget(_grabbable, _playerGrab.MaxGrabDistance);
            Assert.That(isValid, Is.True, "Grab within max distance must be valid.");
        }

        [Test]
        public void ValidateGrabRequest_ExceedingDistance_ReturnsFalse()
        {
            _targetObject.transform.position = new Vector3(0f, 1.4f, 10f);
            bool isValid = _interaction.ValidateGrabTarget(_grabbable, _playerGrab.MaxGrabDistance);
            Assert.That(isValid, Is.False, "Grab exceeding max distance must be rejected.");
        }

        [Test]
        public void ValidateGrabRequest_AlreadyHeld_ReturnsFalse()
        {
            _targetObject.transform.position = new Vector3(0f, 0f, 2f);
            var otherPlayer = new GameObject("OtherPlayer");
            try
            {
                _grabbable.OnGrab(otherPlayer);
                bool isValid = _interaction.ValidateGrabTarget(_grabbable, _playerGrab.MaxGrabDistance);
                Assert.That(isValid, Is.False, "Grab of already held object must be rejected.");
            }
            finally
            {
                Object.DestroyImmediate(otherPlayer);
            }
        }

        [Test]
        public void HandleLocalGrabRequest_WhenTargetHasNoNetworkObject_FallsBackToExecuteGrab()
        {
            _playerGrab.MaxGrabDistance = 5f;
            bool result = _interaction.HandleLocalGrabRequest(_grabbable);

            Assert.That(result, Is.True, "Offline fallback grab must succeed.");
            Assert.That(_playerGrab.IsCarrying, Is.True, "Player must carry object locally when no NetworkObject exists.");
        }

        [Test]
        public void HandleLocalDropRequest_WhenTargetHasNoNetworkObject_FallsBackToExecuteDrop()
        {
            _playerGrab.MaxGrabDistance = 5f;
            _interaction.HandleLocalGrabRequest(_grabbable);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            bool result = _interaction.HandleLocalDropRequest();
            Assert.That(result, Is.True, "Offline fallback drop must succeed.");
            Assert.That(_playerGrab.IsCarrying, Is.False, "Player must no longer carry object after drop.");
        }

        [Test]
        public void HandleLocalThrowRequest_WhenTargetHasNoNetworkObject_FallsBackToExecuteThrow()
        {
            _playerGrab.MaxGrabDistance = 5f;
            _interaction.HandleLocalGrabRequest(_grabbable);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            bool result = _interaction.HandleLocalThrowRequest(Vector3.forward);
            Assert.That(result, Is.True, "Offline fallback throw must succeed.");
            Assert.That(_playerGrab.IsCarrying, Is.False, "Player must no longer carry object after throw.");
        }

        [Test]
        public void HandleLocalThrowRequest_WithOversizedDirection_NormalizesImpulse()
        {
            _playerGrab.MaxGrabDistance = 5f;
            _interaction.HandleLocalGrabRequest(_grabbable);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            var rb = _targetObject.GetComponent<Rigidbody>();
            float mass = rb.mass;
            float expectedSpeed = _playerGrab.ThrowForce / mass;

            // Send huge oversized vector (1000, 0, 0)
            bool result = _interaction.HandleLocalThrowRequest(new Vector3(1000f, 0f, 0f));
            Assert.That(result, Is.True);
            Assert.That(rb.linearVelocity.magnitude, Is.EqualTo(expectedSpeed).Within(0.01f),
                "Oversized direction vector must be normalized so throw impulse is not exploited.");
        }

        [Test]
        public void HandleLocalThrowRequest_WithNonFiniteOrZeroDirection_ReturnsFalse()
        {
            _playerGrab.MaxGrabDistance = 5f;
            _interaction.HandleLocalGrabRequest(_grabbable);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            bool nanResult = _interaction.HandleLocalThrowRequest(new Vector3(float.NaN, 0f, 0f));
            Assert.That(nanResult, Is.False, "Non-finite direction must be rejected.");
            Assert.That(_playerGrab.IsCarrying, Is.True, "Player must still carry object after rejected throw.");

            bool zeroResult = _interaction.HandleLocalThrowRequest(Vector3.zero);
            Assert.That(zeroResult, Is.False, "Zero direction must be rejected.");
            Assert.That(_playerGrab.IsCarrying, Is.True, "Player must still carry object after rejected throw.");
        }

        [Test]
        public void UpdateCarriedTransform_WhenFalse_DoesNotModifyHeldPosition()
        {
            _playerGrab.MaxGrabDistance = 5f;
            _playerGrab.UpdateCarriedTransform = false;
            _playerGrab.ExecuteGrab(_grabbable);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            Vector3 initialPos = new Vector3(10f, 10f, 10f);
            _targetObject.transform.position = initialPos;

            // Invoke LateUpdate via SendMessage
            _playerGrab.SendMessage("LateUpdate");

            Assert.That(_targetObject.transform.position, Is.EqualTo(initialPos),
                "When UpdateCarriedTransform is false, LateUpdate must not override object transform.");
        }

        [Test]
        public void ValidateGrabTarget_WhenTargetBehindWall_ReturnsFalse()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1.4f, 1f);
            Physics.SyncTransforms();

            try
            {
                bool isValid = _interaction.ValidateGrabTarget(_grabbable, _playerGrab.MaxGrabDistance);
                Assert.That(isValid, Is.False, "Grab must be rejected when target is blocked behind a wall.");
            }
            finally
            {
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void ValidateGrabTarget_WhenTargetVisible_ReturnsTrue()
        {
            _targetObject.transform.position = new Vector3(0f, 1.4f, 2f);
            Physics.SyncTransforms();

            bool isValid = _interaction.ValidateGrabTarget(_grabbable, _playerGrab.MaxGrabDistance);
            Assert.That(isValid, Is.True, "Grab must be accepted when target is directly visible.");
        }

        [Test]
        public void HandleLocalBeginInteraction_OfflineWithoutNetworkObject_BeginsLocally()
        {
            var holdObject = new GameObject("Hold");
            try
            {
                var hold = holdObject.AddComponent<FakeHoldInteractable>();

                Assert.That(_interaction.HandleLocalBeginInteraction(hold), Is.True);
                Assert.That(_playerGrab.HeldInteractable, Is.SameAs(hold));
                Assert.That(hold.BeginCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(holdObject);
            }
        }

        [Test]
        public void HandleLocalBeginInteraction_OfflineWhenTargetRefuses_StaysFree()
        {
            var holdObject = new GameObject("Hold");
            try
            {
                var hold = holdObject.AddComponent<FakeHoldInteractable>();
                hold.AcceptBegin = false;

                Assert.That(_interaction.HandleLocalBeginInteraction(hold), Is.False);
                Assert.That(_playerGrab.IsUsingInteractable, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(holdObject);
            }
        }
    }
}
