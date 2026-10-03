using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Ngecor.Player;

namespace Ngecor.Interaction.Tests
{
    public class PlayerGrabTests : InputTestFixture
    {
        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private PlayerGrab _playerGrab;
        private GameObject _targetObject1;
        private GameObject _targetObject2;
        private GameObject _obstacleObject;
        private InputActionAsset _actionAsset;
        private InputAction _interactAction;
        private InputActionReference _interactReference;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            var playerMap = new InputActionMap("Player");
            _actionAsset.AddActionMap(playerMap);
            _interactAction = playerMap.AddAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            playerMap.Enable();
            _interactReference = InputActionReference.Create(_interactAction);

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
            _playerGrab.InteractAction = _interactReference;

            _playerObject.SetActive(true);
            _playerMovement.SetLocalPlayer(true);
        }

        [TearDown]
        public override void TearDown()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_playerObject != null)
                Object.DestroyImmediate(_playerObject);
            if (_targetObject1 != null)
                Object.DestroyImmediate(_targetObject1);
            if (_targetObject2 != null)
                Object.DestroyImmediate(_targetObject2);
            if (_obstacleObject != null)
                Object.DestroyImmediate(_obstacleObject);
            if (_interactReference != null)
                Object.DestroyImmediate(_interactReference);
            if (_actionAsset != null)
                Object.DestroyImmediate(_actionAsset);

            base.TearDown();
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

            Assert.That(_playerGrab.ExecuteDrop(), Is.True);

            Assert.That(rb.isKinematic, Is.False);
            Assert.That(rb.useGravity, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(_playerGrab.CarriedObject, Is.Null);
            Assert.That(grabbable.IsHeld, Is.False);
            Assert.That(grabbable.CurrentHolder, Is.Null);
            Assert.That(_targetObject1, Is.Not.Null);
            Assert.That(Physics.GetIgnoreCollision(
                _playerObject.GetComponent<Collider>(), grabbable.Colliders[0]), Is.False);
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

        [UnityTest]
        public IEnumerator Drop_WithEmptyHandsAndAfterRelease_IsSafeAndIdempotent()
        {
            Assert.That(_playerGrab.RequestDrop(), Is.False);
            Assert.That(_playerGrab.ExecuteDrop(), Is.False);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(_playerGrab.CarriedObject, Is.Null);

            _targetObject1 = CreateGrabbable("TargetRepeatedDrop", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();
            var body = grabbable.Rigidbody;
            var playerCollider = _playerObject.GetComponent<Collider>();
            var targetCollider = grabbable.Colliders[0];

            Physics.SyncTransforms();
            yield return null;

            Assert.That(_playerGrab.ExecuteGrab(grabbable), Is.True);
            Assert.That(_playerGrab.RequestDrop(), Is.True);
            Assert.That(_playerGrab.RequestDrop(), Is.False);
            Assert.That(_playerGrab.ExecuteDrop(), Is.False);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(_playerGrab.CarriedObject, Is.Null);
            Assert.That(grabbable.IsHeld, Is.False);
            Assert.That(grabbable.CurrentHolder, Is.Null);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(body.useGravity, Is.True);
            Assert.That(Physics.GetIgnoreCollision(playerCollider, targetCollider), Is.False);
            Assert.That(_targetObject1, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator CarriedObject_ObstructedByWall_DoesNotPenetrateAndReturnsWhenWallRemoved()
        {
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = _obstacleObject;
            wall.name = "Wall";
            wall.transform.position = new Vector3(0f, 1.4f, 0.8f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.1f);

            _targetObject1 = CreateGrabbable("TargetWall", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            var camera = _playerMovement.LocalCamera;
            float distToCamera = Vector3.Distance(_targetObject1.transform.position, camera.transform.position);
            float wallDistToCamera = Vector3.Distance(wall.transform.position, camera.transform.position);

            Assert.That(distToCamera, Is.LessThan(wallDistToCamera), "Carried object penetrated wall!");

            // Remove wall and verify return to hold point
            Object.DestroyImmediate(wall);
            _obstacleObject = null;
            Physics.SyncTransforms();
            yield return null;

            var holdPoint = _playerGrab.HoldPoint;
            float distToHoldPoint = Vector3.Distance(_targetObject1.transform.position, holdPoint.position);
            Assert.That(distToHoldPoint, Is.LessThan(0.05f), "Carried object did not return to hold point after wall removal!");
        }

        [UnityTest]
        public IEnumerator ExecuteDrop_NearObstacle_ReleasesObjectAndRestoresPhysics()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "DropWall";
            _obstacleObject = wall;
            wall.transform.position = new Vector3(0f, 1.4f, 0.8f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.1f);

            _targetObject1 = CreateGrabbable("TargetDropNearWall", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();
            var body = grabbable.Rigidbody;

            Physics.SyncTransforms();
            yield return null;

            Assert.That(_playerGrab.ExecuteGrab(grabbable), Is.True);
            yield return null;

            Assert.That(_playerGrab.CarriedObject, Is.SameAs(grabbable));
            Assert.That(_playerGrab.ExecuteDrop(), Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(_playerGrab.CarriedObject, Is.Null);
            Assert.That(grabbable.IsHeld, Is.False);
            Assert.That(grabbable.CurrentHolder, Is.Null);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(body.useGravity, Is.True);
            Assert.That(_targetObject1, Is.Not.Null);

            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(_targetObject1, Is.Not.Null);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(body.useGravity, Is.True);
        }

        [UnityTest]
        public IEnumerator ExecuteDrop_WhilePlayerMoving_InheritsHorizontalVelocity()
        {
            _targetObject1 = CreateGrabbable("TargetMovingDrop", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            var controller = _playerObject.GetComponent<CharacterController>();
            controller.Move(new Vector3(5f, 0f, 0f) * Time.deltaTime);

            _playerGrab.ExecuteDrop();

            var rb = grabbable.Rigidbody;
            Assert.That(rb.linearVelocity.x, Is.GreaterThan(0.1f), $"Expected horizontal velocity > 0, got {rb.linearVelocity}");
            Assert.That(rb.linearVelocity.y, Is.EqualTo(0f), "Vertical velocity should be zero (horizontal only)");
        }

        [UnityTest]
        public IEnumerator InteractInput_TriggersGrabAndDrop()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            _targetObject1 = CreateGrabbable("TargetInput", new Vector3(0f, 1.4f, 2f)).gameObject;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Press(keyboard.eKey);
            yield return null;

            Assert.That(_playerGrab.IsCarrying, Is.True, "Pressing interact did not grab object.");

            Release(keyboard.eKey);
            yield return null;

            Press(keyboard.eKey);
            yield return null;

            Assert.That(_playerGrab.IsCarrying, Is.False, "Pressing interact did not drop object.");
        }

        [UnityTest]
        public IEnumerator Update_WithoutInteractAction_DoesNotProcessInput()
        {
            _playerGrab.InteractAction = null;
            _targetObject1 = CreateGrabbable("TargetNoInput", new Vector3(0f, 1.4f, 2f)).gameObject;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            yield return null;
            Assert.That(_playerGrab.IsCarrying, Is.False);
        }
    }
}
