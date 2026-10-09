using NUnit.Framework;
using UnityEngine;
using Ngecor.Player;
using Ngecor.Multiplayer;

namespace Ngecor.Multiplayer.Tests
{
    [TestFixture]
    public class NetworkPlayerTests
    {
        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private CharacterController _characterController;
        private Camera _camera;
        private AudioListener _audioListener;
        private NetworkPlayer _networkPlayer;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("TestPlayer");
            _characterController = _playerObject.AddComponent<CharacterController>();
            _playerMovement = _playerObject.AddComponent<PlayerMovement>();

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(_playerObject.transform, false);

            var camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(pivot.transform, false);
            _camera = camObj.AddComponent<Camera>();
            _audioListener = camObj.AddComponent<AudioListener>();

            _networkPlayer = _playerObject.AddComponent<NetworkPlayer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }
        }

        [Test]
        public void ApplyOwnership_WhenOwner_EnablesLocalPlayerAndComponents()
        {
            _networkPlayer.ApplyOwnership(true);

            Assert.That(_playerMovement.IsLocalPlayer, Is.True, "Local player must be active for owner.");
            Assert.That(_characterController.enabled, Is.True, "CharacterController must be enabled for owner.");
            Assert.That(_camera.enabled, Is.True, "Camera must be enabled for owner.");
            Assert.That(_audioListener.enabled, Is.True, "AudioListener must be enabled for owner.");
        }

        [Test]
        public void ApplyOwnership_WhenNotOwner_DisablesLocalPlayerAndComponents()
        {
            _networkPlayer.ApplyOwnership(false);

            Assert.That(_playerMovement.IsLocalPlayer, Is.False, "Local player must be false for non-owner.");
            Assert.That(_characterController.enabled, Is.False, "CharacterController must be disabled for non-owner.");
            Assert.That(_camera.enabled, Is.False, "Camera must be disabled for non-owner.");
            Assert.That(_audioListener.enabled, Is.False, "AudioListener must be disabled for non-owner.");
        }

        [Test]
        public void ApplyOwnership_ConfiguresRigidbody_KinematicAndCollisionsForRemoteOnly()
        {
            var rb = _playerObject.GetComponent<Rigidbody>();
            Assert.That(rb, Is.Not.Null, "NetworkPlayer should ensure Rigidbody is present.");

            _networkPlayer.ApplyOwnership(true);
            Assert.That(rb.isKinematic, Is.True, "Rigidbody must be kinematic for local player.");
            Assert.That(rb.useGravity, Is.False, "Gravity must be disabled for local player Rigidbody.");
            Assert.That(rb.detectCollisions, Is.False, "Local player must disable Rigidbody collisions so CharacterController is undisturbed.");

            _networkPlayer.ApplyOwnership(false);
            Assert.That(rb.isKinematic, Is.True, "Rigidbody must be kinematic for remote player.");
            Assert.That(rb.useGravity, Is.False, "Gravity must be disabled for remote player Rigidbody.");
            Assert.That(rb.detectCollisions, Is.True, "Remote proxy must detect collisions so physics objects treat it as a kinematic body.");
        }

        [Test]
        public void GetSafeSpawnPosition_WhenNetManagerNull_ReturnsDefaultOffset()
        {
            var spawnPos = SessionManager.GetSafeSpawnPosition(null);
            Assert.That(spawnPos.x, Is.GreaterThan(1.0f));
        }
    }
}
