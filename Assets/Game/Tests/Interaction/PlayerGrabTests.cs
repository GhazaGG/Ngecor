using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ngecor.Player;

namespace Ngecor.Interaction.Tests
{
    public class PlayerGrabTests
    {
        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private PlayerGrab _playerGrab;
        private GameObject _targetObject1;
        private GameObject _targetObject2;

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
            _playerGrab = _playerObject.AddComponent<PlayerGrab>();

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
            if (_targetObject1 != null)
                Object.DestroyImmediate(_targetObject1);
            if (_targetObject2 != null)
                Object.DestroyImmediate(_targetObject2);
        }

        private GrabbableObject CreateGrabbable(string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            return go.AddComponent<GrabbableObject>();
        }

        [UnityTest]
        public IEnumerator RequestGrab_GrabsValidTargetInFront()
        {
            _targetObject1 = CreateGrabbable("Target1", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            bool grabbed = _playerGrab.RequestGrab();

            Assert.That(grabbed, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.True);
            Assert.That(_playerGrab.CarriedObject, Is.EqualTo(grabbable));
            Assert.That(grabbable.IsHeld, Is.True);
            Assert.That(grabbable.CurrentHolder, Is.EqualTo(_playerObject));
        }

        [UnityTest]
        public IEnumerator RequestGrab_RejectsSecondGrabWhenAlreadyCarrying()
        {
            _targetObject1 = CreateGrabbable("Target1", new Vector3(0f, 1.4f, 2f)).gameObject;
            _targetObject2 = CreateGrabbable("Target2", new Vector3(0f, 1.4f, 2.5f)).gameObject;
            var grabbable1 = _targetObject1.GetComponent<GrabbableObject>();
            var grabbable2 = _targetObject2.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            bool firstGrab = _playerGrab.ExecuteGrab(grabbable1);
            Assert.That(firstGrab, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            bool secondGrab = _playerGrab.RequestGrab(grabbable2);
            Assert.That(secondGrab, Is.False);
            Assert.That(_playerGrab.CarriedObject, Is.EqualTo(grabbable1));
            Assert.That(grabbable2.IsHeld, Is.False);
        }

        [UnityTest]
        public IEnumerator RequestGrab_RejectsTargetBeyondMaxDistance()
        {
            _targetObject1 = CreateGrabbable("TargetFar", new Vector3(0f, 1.4f, 10f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            bool grabbed = _playerGrab.RequestGrab(grabbable);

            Assert.That(grabbed, Is.False);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(grabbable.IsHeld, Is.False);
        }

        [UnityTest]
        public IEnumerator RequestGrab_RejectsTargetAlreadyHeld()
        {
            _targetObject1 = CreateGrabbable("TargetHeld", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            var otherHolder = new GameObject("OtherPlayer");
            grabbable.OnGrab(otherHolder);

            Physics.SyncTransforms();
            yield return null;

            bool grabbed = _playerGrab.RequestGrab(grabbable);

            Assert.That(grabbed, Is.False);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(grabbable.CurrentHolder, Is.EqualTo(otherHolder));

            Object.DestroyImmediate(otherHolder);
        }

        [UnityTest]
        public IEnumerator RequestGrab_ReturnsFalseWhenNoGrabbableDetected()
        {
            yield return null;
            _detector.Detect();

            bool grabbed = _playerGrab.RequestGrab();

            Assert.That(grabbed, Is.False);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(_playerGrab.CarriedObject, Is.Null);
        }

        [UnityTest]
        public IEnumerator CarriedObject_FollowsHoldPointPositionAndRotation()
        {
            _targetObject1 = CreateGrabbable("TargetFollow", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            var holdPoint = _playerGrab.HoldPoint;
            Assert.That(Vector3.Distance(_targetObject1.transform.position, holdPoint.position), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(_targetObject1.transform.rotation, holdPoint.rotation), Is.LessThan(1f));

            // Move player
            _playerObject.transform.position = new Vector3(5f, 0f, 5f);
            Physics.SyncTransforms();
            yield return null;

            Assert.That(Vector3.Distance(_targetObject1.transform.position, holdPoint.position), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator CarriedObject_CollisionsWithPlayerAreIgnoredWhileHeld()
        {
            _targetObject1 = CreateGrabbable("TargetCollision", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();
            var playerCollider = _playerObject.GetComponent<Collider>();
            var targetCollider = _targetObject1.GetComponent<Collider>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);

            Assert.That(Physics.GetIgnoreCollision(playerCollider, targetCollider), Is.True);

            _playerGrab.ExecuteDrop();

            Assert.That(Physics.GetIgnoreCollision(playerCollider, targetCollider), Is.False);
        }

        [UnityTest]
        public IEnumerator ExecuteDrop_RestoresOriginalPhysicsState()
        {
            _targetObject1 = CreateGrabbable("TargetPhysics", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();
            var rb = grabbable.Rigidbody;
            rb.isKinematic = false;
            rb.useGravity = true;

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);

            Assert.That(rb.isKinematic, Is.True);
            Assert.That(rb.useGravity, Is.False);

            _playerGrab.ExecuteDrop();

            Assert.That(rb.isKinematic, Is.False);
            Assert.That(rb.useGravity, Is.True);
        }

        [UnityTest]
        public IEnumerator CarriedObject_HandlesExternalDestructionGracefully()
        {
            _targetObject1 = CreateGrabbable("TargetDestroy", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            Object.DestroyImmediate(_targetObject1);
            _targetObject1 = null;

            yield return null;

            Assert.DoesNotThrow(() => { bool carrying = _playerGrab.IsCarrying; });
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(_playerGrab.CarriedObject, Is.Null);
        }

        [UnityTest]
        public IEnumerator RequestDrop_DropsCarriedObjectAndAllowsNewGrab()
        {
            _targetObject1 = CreateGrabbable("TargetDrop1", new Vector3(0f, 1.4f, 2f)).gameObject;
            _targetObject2 = CreateGrabbable("TargetDrop2", new Vector3(0f, 1.4f, 2.5f)).gameObject;
            var grabbable1 = _targetObject1.GetComponent<GrabbableObject>();
            var grabbable2 = _targetObject2.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            bool firstGrab = _playerGrab.ExecuteGrab(grabbable1);
            Assert.That(firstGrab, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            bool dropped = _playerGrab.RequestDrop();
            Assert.That(dropped, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(grabbable1.IsHeld, Is.False);

            bool secondGrab = _playerGrab.RequestGrab(grabbable2);
            Assert.That(secondGrab, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.True);
            Assert.That(_playerGrab.CarriedObject, Is.EqualTo(grabbable2));
        }
    }
}

