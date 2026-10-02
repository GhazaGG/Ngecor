using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ngecor.Player;

namespace Ngecor.Interaction.Tests
{
    public class InteractionDetectorTests
    {
        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private GameObject _interactableObject;
        private GameObject _obstacleObject;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("Player");
            _playerObject.transform.position = Vector3.zero;
            _playerObject.transform.rotation = Quaternion.identity;
            _playerObject.SetActive(false);

            var controller = _playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(_playerObject.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(pivot.transform, false);
            cameraObject.AddComponent<Camera>();

            _playerMovement = _playerObject.AddComponent<PlayerMovement>();
            _detector = _playerObject.AddComponent<InteractionDetector>();
            _detector.MaxDistance = 3f;

            _playerObject.SetActive(true);
            _playerMovement.SetLocalPlayer(true);
        }

        [TearDown]
        public void TearDown()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_playerObject != null)
                Object.DestroyImmediate(_playerObject);
            if (_interactableObject != null)
                Object.DestroyImmediate(_interactableObject);
            if (_obstacleObject != null)
                Object.DestroyImmediate(_obstacleObject);
        }

        [UnityTest]
        public IEnumerator DetectsInteractableDirectlyInFront()
        {
            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 1.4f, 2f);
            var interactable = _interactableObject.AddComponent<InteractableObject>();

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.True);
            Assert.That(_detector.CurrentTarget, Is.EqualTo(interactable));
        }

        [UnityTest]
        public IEnumerator IgnoresInteractableBeyondMaxDistance()
        {
            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 1.4f, 5f);
            _interactableObject.AddComponent<InteractableObject>();

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator BlockedBySolidObstacle()
        {
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _obstacleObject.name = "SolidWall";
            _obstacleObject.transform.position = new Vector3(0f, 1.4f, 1.5f);

            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 1.4f, 2.5f);
            _interactableObject.AddComponent<InteractableObject>();

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator NullSafeWhenHittingNothing()
        {
            yield return null;
            Assert.DoesNotThrow(() => _detector.Detect());
            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator DetectsParentInteractableFromChildCollider()
        {
            _interactableObject = new GameObject("ParentInteractable");
            _interactableObject.transform.position = new Vector3(0f, 1.4f, 2f);
            var interactable = _interactableObject.AddComponent<InteractableObject>();

            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(_interactableObject.transform, false);

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.True);
            Assert.That(_detector.CurrentTarget, Is.EqualTo(interactable));
        }

        [UnityTest]
        public IEnumerator IgnoresTriggerCollidersBetweenPlayerAndInteractable()
        {
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _obstacleObject.GetComponent<Collider>().isTrigger = true;
            _obstacleObject.transform.position = new Vector3(0f, 1.4f, 1.5f);

            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 1.4f, 2.5f);
            var interactable = _interactableObject.AddComponent<InteractableObject>();

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.True);
            Assert.That(_detector.CurrentTarget, Is.EqualTo(interactable));
        }

        [UnityTest]
        public IEnumerator IgnoresInteractableWhenCanInteractIsFalse()
        {
            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 1.4f, 2f);
            var interactable = _interactableObject.AddComponent<InteractableObject>();
            interactable.SetInteractable(false);

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator NonLocalPlayerDoesNotDetect()
        {
            _interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _interactableObject.transform.position = new Vector3(0f, 1.4f, 2f);
            _interactableObject.AddComponent<InteractableObject>();

            _playerMovement.SetLocalPlayer(false);

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_detector.CurrentTarget, Is.Null);
        }
    }
}
