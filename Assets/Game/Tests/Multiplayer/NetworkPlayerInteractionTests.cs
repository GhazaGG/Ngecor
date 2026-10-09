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

            _targetObject = new GameObject("Target");
            _targetObject.transform.position = new Vector3(0f, 0f, 2f);
            var rb = _targetObject.AddComponent<Rigidbody>();
            var col = _targetObject.AddComponent<BoxCollider>();
            _grabbable = _targetObject.AddComponent<GrabbableObject>();
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
            _targetObject.transform.position = new Vector3(0f, 0f, 2f);
            bool isValid = _interaction.ValidateGrabTarget(_grabbable, _playerGrab.MaxGrabDistance);
            Assert.That(isValid, Is.True, "Grab within max distance must be valid.");
        }

        [Test]
        public void ValidateGrabRequest_ExceedingDistance_ReturnsFalse()
        {
            _targetObject.transform.position = new Vector3(0f, 0f, 10f);
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
    }
}
