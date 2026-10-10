using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Ngecor.Player;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ngecor.Interaction.Tests
{
    public class PlayerGrabTests : InputTestFixture
    {
        // Headless runs are unthrottled (sub-millisecond frames), so one sideways step falls below the
        // CharacterController's minimum move distance. Pin steering tests to a realistic frame rate.
        private const int SteeringTestFrameRate = 60;

        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private PlayerGrab _playerGrab;
        private GameObject _targetObject1;
        private GameObject _targetObject2;
        private GameObject _obstacleObject;
        private InputActionAsset _actionAsset;
        private InputAction _moveAction;
        private InputActionReference _moveReference;
        private InputAction _interactAction;
        private InputActionReference _interactReference;
        private InputAction _throwAction;
        private InputActionReference _throwReference;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            if (InputSystem.actions != null)
                InputSystem.actions.Disable();

            _actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            var playerMap = new InputActionMap("Player");
            _actionAsset.AddActionMap(playerMap);
            _moveAction = playerMap.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _interactAction = playerMap.AddAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _throwAction = playerMap.AddAction("Attack", InputActionType.Button);
            _throwAction.AddBinding("<Mouse>/leftButton");
            playerMap.Enable();
            _moveReference = InputActionReference.Create(_moveAction);
            _interactReference = InputActionReference.Create(_interactAction);
            _throwReference = InputActionReference.Create(_throwAction);

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
            _playerGrab.ThrowAction = _throwReference;

            _playerObject.SetActive(true);
            _playerMovement.SetLocalPlayer(true);
        }

        [TearDown]
        public override void TearDown()
        {
            Time.captureFramerate = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_actionAsset != null)
                _actionAsset.Disable();

            if (_playerObject != null)
                Object.DestroyImmediate(_playerObject);
            if (_targetObject1 != null)
                Object.DestroyImmediate(_targetObject1);
            if (_targetObject2 != null)
                Object.DestroyImmediate(_targetObject2);
            if (_obstacleObject != null)
                Object.DestroyImmediate(_obstacleObject);
            if (_interactAction != null)
            {
                _interactAction.Disable();
                _interactAction = null;
            }
            if (_throwAction != null)
            {
                _throwAction.Disable();
                _throwAction = null;
            }
            if (_moveAction != null)
            {
                _moveAction.Disable();
                _moveAction = null;
            }
            if (_actionAsset != null)
            {
                Object.DestroyImmediate(_actionAsset);
                _actionAsset = null;
            }
            if (_interactReference != null)
            {
                Object.DestroyImmediate(_interactReference);
                _interactReference = null;
            }
            if (_moveReference != null)
            {
                Object.DestroyImmediate(_moveReference);
                _moveReference = null;
            }
            if (_throwReference != null)
            {
                Object.DestroyImmediate(_throwReference);
                _throwReference = null;
            }

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

        private Component CreateWheelbarrow(string name, Vector3 position, out BoxCollider tray, out BoxCollider handle)
        {
            var wheelbarrow = new GameObject(name);
            wheelbarrow.transform.position = position;
            var body = wheelbarrow.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 4f;
            body.centerOfMass = Vector3.zero;

            var interactionType = FindWheelbarrowInteractionType();
            var interaction = wheelbarrow.AddComponent(interactionType);
            tray = CreateWheelbarrowCollider(wheelbarrow.transform, "Tray", new Vector3(0f, 0f, 0.25f), new Vector3(0.5f, 0.5f, 0.1f));
            handle = CreateWheelbarrowCollider(wheelbarrow.transform, "Handle", new Vector3(0f, 0f, -0.25f), Vector3.one * 0.2f);

            var handleField = interactionType.GetField("_handleColliders", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(handleField, Is.Not.Null, "Wheelbarrow interaction must expose its serialized handle colliders.");
            handleField.SetValue(interaction, new Collider[] { handle });
            return interaction;
        }

        private static BoxCollider CreateWheelbarrowCollider(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            var collider = child.AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }

        private Component CreateWheelbarrowForMovement(out BoxCollider handle)
        {
            EnableMoveInput();
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _obstacleObject.transform.position = new Vector3(0f, -0.5f, 0f);
            _obstacleObject.transform.localScale = new Vector3(20f, 1f, 20f);
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out handle);
            _targetObject1 = interaction.gameObject;
            _playerObject.transform.position = new Vector3(0.6f, 0f, 0f);
            _playerMovement.LocalCamera.transform.rotation = Quaternion.LookRotation(
                handle.bounds.center - _playerMovement.LocalCamera.transform.position, Vector3.up);
            Physics.SyncTransforms();
            return interaction;
        }

        private void EnableMoveInput()
        {
            typeof(PlayerMovement).GetField("_moveAction", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_playerMovement, _moveReference);
        }

        private static System.Type FindWheelbarrowInteractionType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Ngecor.Vehicle.WheelbarrowInteraction");
                if (type != null)
                    return type;
            }

            Assert.Fail("WheelbarrowInteraction must be available in the loaded Unity assemblies.");
            return null;
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
            grabbable.Rigidbody.mass = 25f;

            Physics.SyncTransforms();
            yield return null;

            _detector.Detect();
            Assert.That(_detector.HasTarget, Is.True);

            _playerGrab.ExecuteGrab(grabbable);
            Assert.That(_playerGrab.IsCarrying, Is.True);
            Assert.That(_playerMovement.CarriedMass, Is.EqualTo(25f));

            Object.DestroyImmediate(_targetObject1);
            _targetObject1 = null;

            Assert.That(_detector.HasTarget, Is.False,
                "A destroyed Unity component must no longer be exposed as an interaction target.");

            yield return null;

            Assert.DoesNotThrow(() => { bool carrying = _playerGrab.IsCarrying; });
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(_playerGrab.CarriedObject, Is.Null);
            Assert.That(_playerMovement.CarriedMass, Is.EqualTo(0f));
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

            Physics.SyncTransforms();
            yield return null;

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
        public IEnumerator CarriedObject_ObstructedByRamp_DoesNotPenetrateRamp()
        {
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var ramp = _obstacleObject;
            ramp.name = "TestRamp";
            ramp.transform.position = new Vector3(0f, 1.15f, 1.2f);
            ramp.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            ramp.transform.localScale = new Vector3(4f, 0.3f, 4f);

            _targetObject1 = CreateGrabbable("TargetRampBox", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            var boxCollider = _targetObject1.GetComponent<Collider>();
            var rampCollider = ramp.GetComponent<Collider>();

            bool penetrating = Physics.ComputePenetration(
                boxCollider, _targetObject1.transform.position, _targetObject1.transform.rotation,
                rampCollider, ramp.transform.position, ramp.transform.rotation,
                out Vector3 depenDir, out float depenDist);

            Assert.That(penetrating && depenDist > 0.01f, Is.False,
                $"Carried object penetrated ramp by {depenDist}m!");

            var camera = _playerMovement.LocalCamera;
            float forwardDist = Vector3.Dot(_targetObject1.transform.position - camera.transform.position, camera.transform.forward);
            Assert.That(forwardDist, Is.GreaterThan(camera.nearClipPlane),
                "Carried object fell behind camera or near-clip plane!");
        }

        [UnityTest]
        public IEnumerator CarriedObject_WithCompoundChildColliders_ObstructedByWall_DoesNotPenetrate()
        {
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = _obstacleObject;
            wall.name = "CompoundWall";
            wall.transform.position = new Vector3(0f, 1.4f, 0.8f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.1f);

            // Objek majemuk: root tanpa collider, 2 anak dengan BoxCollider ber-offset
            var compoundRoot = new GameObject("CompoundGrabbable");
            compoundRoot.transform.position = new Vector3(0f, 1.4f, 2f);
            var rb = compoundRoot.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;

            var child1 = new GameObject("ChildCol1");
            child1.transform.SetParent(compoundRoot.transform, false);
            child1.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            var col1 = child1.AddComponent<BoxCollider>();
            col1.size = new Vector3(0.4f, 0.4f, 0.4f);

            var child2 = new GameObject("ChildCol2");
            child2.transform.SetParent(compoundRoot.transform, false);
            child2.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            var col2 = child2.AddComponent<BoxCollider>();
            col2.size = new Vector3(0.4f, 0.4f, 0.4f);

            var grabbable = compoundRoot.AddComponent<GrabbableObject>();

            _targetObject1 = compoundRoot;

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            var wallCollider = wall.GetComponent<Collider>();

            bool pen1 = Physics.ComputePenetration(
                col1, col1.transform.position, col1.transform.rotation,
                wallCollider, wall.transform.position, wall.transform.rotation,
                out _, out float dist1);
            Assert.That(pen1 && dist1 > 0.01f, Is.False, $"Child collider 1 penetrated wall by {dist1}m!");

            bool pen2 = Physics.ComputePenetration(
                col2, col2.transform.position, col2.transform.rotation,
                wallCollider, wall.transform.position, wall.transform.rotation,
                out _, out float dist2);
            Assert.That(pen2 && dist2 > 0.01f, Is.False, $"Child collider 2 penetrated wall by {dist2}m!");
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
            _targetObject1.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
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
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            var objectCollider = _targetObject1.GetComponent<Collider>();
            var wallCollider = wall.GetComponent<Collider>();
            var playerCollider = _playerObject.GetComponent<Collider>();

            // 1. Tidak menembus dinding lebih dari 0.01
            bool penetratesWall = Physics.ComputePenetration(
                objectCollider, _targetObject1.transform.position, _targetObject1.transform.rotation,
                wallCollider, wall.transform.position, wall.transform.rotation,
                out _, out float wallPenDist);
            Assert.That(penetratesWall && wallPenDist > 0.01f, Is.False,
                $"Dropped object penetrated wall by {wallPenDist}m!");

            // 2. Kecepatan body.linearVelocity.magnitude < 3f
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(3f),
                $"Dropped object launched with excessive velocity: {body.linearVelocity.magnitude} m/s ({body.linearVelocity})");

            // 3. Collision with player: if overlapping in tight space, collision must be ignored until separated (anti-launch)
            bool insidePlayer = Physics.ComputePenetration(
                objectCollider, _targetObject1.transform.position, _targetObject1.transform.rotation,
                playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
                out _, out float playerPenDist);
            if (insidePlayer && playerPenDist > 0.001f)
            {
                Assert.That(Physics.GetIgnoreCollision(playerCollider, objectCollider), Is.True,
                    "Collision with player must remain ignored while overlapping to prevent launching!");

                // Step player back to clear the overlap
                _playerObject.transform.position += new Vector3(0f, 0f, -1f);
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();

                Assert.That(Physics.GetIgnoreCollision(playerCollider, objectCollider), Is.False,
                    "Collision with player should be restored after separating!");
            }
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
        public IEnumerator WheelbarrowInteraction_StartsOnlyFromConfiguredHandle()
        {
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out var tray, out var handle);
            _targetObject1 = interaction.gameObject;

            _playerObject.transform.position = new Vector3(0f, 0f, 3f);
            _playerMovement.LocalCamera.transform.rotation = Quaternion.LookRotation(
                tray.bounds.center - _playerMovement.LocalCamera.transform.position, Vector3.up);
            Physics.SyncTransforms();
            yield return null;
            Assert.That(((IHoldInteractable)interaction).CanInteractFrom(_playerObject, tray), Is.False,
                "The tray must not start a handle interaction.");
            _detector.Detect();
            Assert.That(_detector.CurrentHit.collider, Is.SameAs(tray));
            Assert.That(_detector.CurrentTarget, Is.Null, "A ray hit on the tray must not start a handle interaction.");

            _playerObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _playerMovement.LocalCamera.transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();

            Assert.That(_detector.CurrentHit.collider, Is.SameAs(handle));
            Assert.That(_playerGrab.RequestGrab(), Is.True);
            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_StaysHeldAtNormalSeparationAndSecondPressReleases()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var first = CreateWheelbarrow("FirstWheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out _);
            _targetObject1 = first.gameObject;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            first.transform.position = new Vector3(0f, 1.4f, 6f);
            Physics.SyncTransforms();
            yield return null;
            Assert.That(_playerGrab.IsUsingInteractable, Is.True,
                "Normal movement or a lagging wheelbarrow must not end the hold before E is pressed again.");

            Press(keyboard.eKey);
            yield return null;
            Assert.That(_playerGrab.IsUsingInteractable, Is.False,
                "The second Interact press must release the wheelbarrow.");
            Release(keyboard.eKey);

            var secondPosition = _playerObject.transform.position + _playerObject.transform.forward * 2f;
            secondPosition.y = 1.4f;
            var second = CreateWheelbarrow("SecondWheelbarrow", secondPosition, out _, out _);
            _targetObject2 = second.gameObject;
            Physics.SyncTransforms();
            yield return null;

            _detector.Detect();
            Assert.That((_detector.CurrentTarget as Component)?.gameObject, Is.SameAs(second.gameObject));
            Assert.That(_playerGrab.RequestGrab(), Is.True,
                $"A new wheelbarrow must be usable immediately after the previous one is released. Feedback: {_playerGrab.InteractionFeedback ?? "none"}.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_DisableAndReenableClearsAndRestoresHold()
        {
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out _);
            _targetObject1 = interaction.gameObject;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            ((Behaviour)interaction).enabled = false;
            Assert.That(_playerGrab.IsUsingInteractable, Is.False,
                "Disabling the interactable must clear PlayerGrab's held-interactable state.");
            Assert.That(_playerGrab.InteractionFeedback, Does.Contain("unavailable"));

            ((Behaviour)interaction).enabled = true;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True,
                "The wheelbarrow must be usable again after its interaction component is re-enabled.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_AlignsPlayerFromEitherSideOfTheHandles()
        {
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out var handle);
            _targetObject1 = interaction.gameObject;
            var hold = (IHoldInteractable)interaction;
            var controller = _playerObject.GetComponent<CharacterController>();

            foreach (var side in new[] { -1.2f, 1.2f })
            {
                _playerObject.transform.SetPositionAndRotation(
                    new Vector3(side, 0f, handle.bounds.center.z), Quaternion.identity);
                _playerMovement.LocalCamera.transform.rotation = Quaternion.LookRotation(
                    handle.bounds.center - _playerMovement.LocalCamera.transform.position, Vector3.up);
                Physics.SyncTransforms();
                yield return null;
                _detector.Detect();

                Assert.That(_detector.CurrentHit.collider, Is.SameAs(handle));
                var expectedRootPosition = handle.bounds.center +
                    Vector3.ProjectOnPlane(handle.bounds.center - interaction.GetComponent<Rigidbody>().worldCenterOfMass, Vector3.up).normalized *
                    (handle.bounds.extents.z + controller.radius + controller.skinWidth + 0.05f);
                expectedRootPosition.y = _playerObject.transform.position.y;

                Assert.That(_playerGrab.RequestGrab(), Is.True);
                Assert.That(Vector3.Distance(_playerObject.transform.position, expectedRootPosition), Is.LessThan(0.1f),
                    "Starting beside either handle must route the player behind the handles without moving the wheelbarrow.");
                Assert.That(interaction.GetComponent<Rigidbody>().linearVelocity, Is.EqualTo(Vector3.zero));

                hold.EndInteraction(_playerObject);
            }
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator WheelbarrowPrefab_AlignsPlayerFromBothConfiguredHandles()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Vehicle/Wheelbarrow.prefab");
            Assert.That(prefab, Is.Not.Null, "The original wheelbarrow prefab must be available to Play Mode tests.");

            var wheelbarrow = Object.Instantiate(prefab);
            wheelbarrow.name = "WheelbarrowPrefabTest";
            wheelbarrow.transform.position = new Vector3(0f, 0.5f, 1.9f);
            _targetObject1 = wheelbarrow;
            var body = wheelbarrow.GetComponent<Rigidbody>();
            body.useGravity = false;
            var hold = wheelbarrow.GetComponent<IHoldInteractable>();
            Assert.That(hold, Is.Not.Null);
            Physics.SyncTransforms();

            foreach (var handleName in new[] { "Handle_Left", "Handle_Right" })
            {
                var handle = wheelbarrow.transform.Find(handleName).GetComponent<Collider>();
                var side = handleName == "Handle_Left" ? -1.5f : 1.5f;
                _playerObject.transform.SetPositionAndRotation(
                    new Vector3(side, 0f, handle.bounds.center.z), Quaternion.identity);
                _playerMovement.LocalCamera.transform.rotation = Quaternion.LookRotation(
                    handle.bounds.center - _playerMovement.LocalCamera.transform.position, Vector3.up);
                Physics.SyncTransforms();
                yield return null;
                _detector.Detect();

                Assert.That(_detector.CurrentHit.collider, Is.SameAs(handle),
                    $"The original {handleName} must be targetable from its side.");
                Assert.That(_playerGrab.RequestGrab(), Is.True);
                Assert.That(_playerGrab.IsUsingInteractable, Is.True);
                hold.EndInteraction(_playerObject);
            }
        }
#endif

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_RejectsBlockedGripPositionWithFeedback()
        {
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out _);
            _targetObject1 = interaction.gameObject;
            _playerObject.transform.position = new Vector3(0.6f, 0f, 0f);
            _obstacleObject = new GameObject("BlockedGrip");
            _obstacleObject.transform.position = new Vector3(0.3f, 0.9f, 0.3f);
            _obstacleObject.AddComponent<BoxCollider>().size = new Vector3(0.2f, 1.8f, 0.2f);
            Physics.SyncTransforms();
            yield return null;

            var startPosition = _playerObject.transform.position;
            var began = ((IHoldInteractable)interaction).TryBeginInteraction(_playerObject);

            Assert.That(began, Is.False);
            Assert.That(_playerGrab.IsUsingInteractable, Is.False);
            Assert.That(Vector3.Distance(_playerObject.transform.position, startPosition), Is.LessThan(0.05f),
                "A blocked alignment must not leave the player at a partial position.");
            Assert.That(_playerGrab.InteractionFeedback, Does.Contain("blocked"));
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_ReleasesAfterExtremeSeparation()
        {
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out _);
            _targetObject1 = interaction.gameObject;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            interaction.transform.position = new Vector3(0f, 1.4f, 20f);
            Physics.SyncTransforms();
            yield return null;

            Assert.That(_playerGrab.IsUsingInteractable, Is.False);
            Assert.That(_playerGrab.InteractionFeedback, Does.Contain("too far"));
        }

        [UnityTest]
        public IEnumerator RequestGrab_WithBeginInteractionHandler_RoutesTheHoldThroughTheHandler()
        {
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out var handle);
            _targetObject1 = interaction.gameObject;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            Assert.That(_detector.CurrentHit.collider, Is.SameAs(handle));

            IHoldInteractable routed = null;
            _playerGrab.BeginInteractionRequestHandler = target =>
            {
                routed = target;
                return true;
            };

            Assert.That(_playerGrab.RequestGrab(), Is.True);
            Assert.That(routed, Is.SameAs(interaction), "The hold request must reach the handler.");
            Assert.That(_playerGrab.IsUsingInteractable, Is.False,
                "With a handler, only the handler (the host) decides whether the hold starts.");
        }

        [UnityTest]
        public IEnumerator ExecuteEndInteraction_ReleasesTheHoldAndRaisesInteractionEndedOnce()
        {
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out _);
            _targetObject1 = interaction.gameObject;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);
            Assert.That(_playerGrab.HeldInteractable, Is.SameAs(interaction));

            var ended = 0;
            _playerGrab.InteractionEnded += () => ended++;

            Assert.That(_playerGrab.ExecuteEndInteraction(), Is.True);
            Assert.That(_playerGrab.IsUsingInteractable, Is.False);
            Assert.That(_playerGrab.HeldInteractable, Is.Null);
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(_playerGrab.ExecuteEndInteraction(), Is.False);
            Assert.That(ended, Is.EqualTo(1), "Ending twice must not report a second end.");
        }

        [UnityTest]
        public IEnumerator InteractionEnded_FiresWhenTheWheelbarrowReleasesTheHoldItself()
        {
            var interaction = CreateWheelbarrow("Wheelbarrow", new Vector3(0f, 1.4f, 1.4f), out _, out _);
            _targetObject1 = interaction.gameObject;
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            var ended = 0;
            _playerGrab.InteractionEnded += () => ended++;
            interaction.transform.position = new Vector3(0f, 1.4f, 20f);
            Physics.SyncTransforms();
            yield return null;

            Assert.That(_playerGrab.IsUsingInteractable, Is.False);
            Assert.That(ended, Is.EqualTo(1), "The network layer must hear about holds the interactable ends.");
        }

        [UnityTest]
        public IEnumerator ExecuteReplicatedGrab_AttachesBeyondGrabDistance()
        {
            var grabbable = CreateGrabbable("Far", new Vector3(0f, 1.4f, 10f));
            _targetObject1 = grabbable.gameObject;
            yield return null;

            Assert.That(_playerGrab.ExecuteGrab(grabbable), Is.False, "A local grab still respects the distance limit.");
            Assert.That(_playerGrab.ExecuteReplicatedGrab(grabbable), Is.True,
                "The host already validated this grab; a lagging copy of the holder must not refuse it.");
            Assert.That(_playerGrab.CarriedObject, Is.SameAs(grabbable));
            Assert.That(grabbable.IsHeld, Is.True);
        }

        [UnityTest]
        public IEnumerator ExecuteReplicatedGrab_RefusesObjectHeldBySomeoneElse()
        {
            var grabbable = CreateGrabbable("Held", new Vector3(0f, 1.4f, 2f));
            _targetObject1 = grabbable.gameObject;
            var otherHolder = new GameObject("OtherHolder");
            try
            {
                grabbable.OnGrab(otherHolder);
                yield return null;

                Assert.That(_playerGrab.ExecuteReplicatedGrab(grabbable), Is.False,
                    "An object must never have two holders, even when updates arrive out of order.");
                Assert.That(_playerGrab.IsCarrying, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(otherHolder);
            }
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_WPushesForwardWhileHeld()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var interaction = CreateWheelbarrowForMovement(out var handle);
            var body = interaction.GetComponent<Rigidbody>();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            Press(keyboard.wKey);
            yield return null;
            for (var i = 0; i < 4; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(_playerGrab.IsUsingInteractable, Is.True,
                "Pushing the wheelbarrow must not break the hold during normal motion.");
            Assert.That(_playerMovement.MovementInput.y, Is.EqualTo(1f));
            Assert.That(Vector3.Dot(body.linearVelocity, interaction.transform.forward), Is.GreaterThan(0.1f),
                "W must push in the wheelbarrow's forward direction.");
            Assert.That(Vector3.Dot(body.linearVelocity, interaction.transform.forward), Is.LessThanOrEqualTo(5.05f),
                "The existing mass-aware push speed limit must still apply.");
            Assert.That(body.isKinematic, Is.False);
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_RemoteHolderOnSimulatingPeer_PushesWithoutMovingThePlayer()
        {
            var interaction = CreateWheelbarrowForMovement(out _);
            var body = interaction.GetComponent<Rigidbody>();
            _playerObject.transform.position = Vector3.zero;
            _playerMovement.SetLocalPlayer(false);
            _playerObject.GetComponent<CharacterController>().enabled = false;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(((IHoldInteractable)interaction).TryBeginInteraction(_playerObject), Is.True,
                "The host must accept a remote holder it simulates the barrow for.");
            var playerStart = _playerObject.transform.position;

            _playerMovement.SetRemoteMovementInput(Vector2.up);
            yield return null;
            for (var i = 0; i < 4; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Dot(body.linearVelocity, interaction.transform.forward), Is.GreaterThan(0.1f),
                "The replicated W must push the barrow on the host.");
            Assert.That(_playerObject.transform.position, Is.EqualTo(playerStart),
                "The host must never move a client's player; the owner does.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_RemoteHolderOnSimulatingPeer_IgnoresHolderCollisionsUntilReleased()
        {
            var interaction = CreateWheelbarrowForMovement(out var handle);
            _playerObject.transform.position = Vector3.zero;
            _playerMovement.SetLocalPlayer(false);
            _playerObject.GetComponent<CharacterController>().enabled = false;
            var proxy = new GameObject("RemoteProxy");
            proxy.transform.SetParent(_playerObject.transform, false);
            var proxyCollider = proxy.AddComponent<CapsuleCollider>();
            proxyCollider.height = 1.8f;
            proxyCollider.radius = 0.35f;
            proxyCollider.center = new Vector3(0f, 0.9f, 0f);
            proxy.AddComponent<Rigidbody>().isKinematic = true;
            Physics.SyncTransforms();
            yield return null;

            var hold = (IHoldInteractable)interaction;
            Assert.That(hold.TryBeginInteraction(_playerObject), Is.True);
            Assert.That(Physics.GetIgnoreCollision(proxyCollider, handle), Is.True,
                "The holder's replica trails the real player; pulling the barrow back must not hit it on the host.");

            hold.EndInteraction(_playerObject);
            Assert.That(Physics.GetIgnoreCollision(proxyCollider, handle), Is.False,
                "Collisions must come back once the hold ends.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_KinematicCopyWithLocalPlayer_FollowsTheHandleWithoutForces()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var interaction = CreateWheelbarrowForMovement(out _);
            var body = interaction.GetComponent<Rigidbody>();
            body.isKinematic = true;
            yield return null;
            _detector.Detect();

            Assert.That(_playerGrab.RequestGrab(), Is.True, "A network client must be able to hold its kinematic copy.");
            Assert.That(_playerObject.transform.position.x, Is.EqualTo(0f).Within(0.1f), "The owner snaps to the grip.");
            var bodyStart = body.position;

            Press(keyboard.wKey);
            for (var i = 0; i < 4; i++)
                yield return null;
            Assert.That(_playerGrab.IsUsingInteractable, Is.True, "A kinematic copy must not end the hold as unavailable.");
            Assert.That(body.position, Is.EqualTo(bodyStart), "Only the host moves the barrow.");
            Release(keyboard.wKey);

            var playerZ = _playerObject.transform.position.z;
            interaction.transform.position += new Vector3(0f, 0f, 1f);
            Physics.SyncTransforms();
            Time.captureFramerate = SteeringTestFrameRate; // the follow step is speed * deltaTime; reset in TearDown
            for (var i = 0; i < 30; i++)
                yield return null;
            Assert.That(_playerObject.transform.position.z, Is.GreaterThan(playerZ + 0.5f),
                "The owner's player must follow the replicated handle.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_KinematicCopyWithRemotePlayer_IsRefused()
        {
            var interaction = CreateWheelbarrowForMovement(out _);
            interaction.GetComponent<Rigidbody>().isKinematic = true;
            _playerObject.transform.position = Vector3.zero;
            _playerMovement.SetLocalPlayer(false);
            Physics.SyncTransforms();
            yield return null;

            Assert.That(((IHoldInteractable)interaction).TryBeginInteraction(_playerObject), Is.False,
                "A peer that neither simulates the barrow nor controls the player has nothing to do.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_SecondPlayerIsRejectedWithoutLockingMovement()
        {
            var interaction = CreateWheelbarrowForMovement(out _);
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            var second = new GameObject("SecondPlayer");
            try
            {
                second.SetActive(false);
                var controller = second.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.35f;
                controller.center = new Vector3(0f, 0.9f, 0f);
                var secondMovement = second.AddComponent<PlayerMovement>();
                second.AddComponent<InteractionDetector>();
                second.AddComponent<PlayerGrab>();
                second.transform.position = new Vector3(-0.6f, 0f, 0f);
                second.SetActive(true);
                Physics.SyncTransforms();

                Assert.That(((IHoldInteractable)interaction).TryBeginInteraction(second), Is.False,
                    "Only one player may hold the handles.");
                var locked = (bool)typeof(PlayerMovement)
                    .GetField("_interactableControlsMovement", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(secondMovement);
                Assert.That(locked, Is.False, "A rejected player must keep normal movement.");
            }
            finally
            {
                Object.DestroyImmediate(second);
            }
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_DSteersHandleRightAndNoseLeftWhileHeld()
        {
            yield return SteerSyntheticWheelbarrow(true);
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_ASteersHandleLeftAndNoseRightWhileHeld()
        {
            yield return SteerSyntheticWheelbarrow(false);
        }

        private IEnumerator SteerSyntheticWheelbarrow(bool right)
        {
            Time.captureFramerate = SteeringTestFrameRate;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var interaction = CreateWheelbarrowForMovement(out var handle);
            var body = interaction.GetComponent<Rigidbody>();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            var side = right ? 1f : -1f;
            var cartRight = interaction.transform.right;
            var startHandle = handle.bounds.center;
            var startPlayer = _playerObject.transform.position;

            Press(right ? keyboard.dKey : keyboard.aKey);
            yield return new WaitForSeconds(0.2f);

            var handleShift = Vector3.Dot(handle.bounds.center - startHandle, cartRight);
            var playerShift = Vector3.Dot(_playerObject.transform.position - startPlayer, cartRight);
            var noseYaw = Vector3.SignedAngle(Vector3.forward, interaction.transform.forward, Vector3.up);

            Assert.That(_playerGrab.IsUsingInteractable, Is.True,
                "Steering the wheelbarrow must not break the hold during normal motion.");
            Assert.That(_playerMovement.MovementInput.x, Is.EqualTo(side), "A/D input must reach the held interaction.");
            Assert.That(playerShift * side, Is.GreaterThan(0.005f),
                "A steps the player left and D steps the player right.");
            Assert.That(handleShift * side, Is.GreaterThan(0.005f),
                "The handle must follow the player's sideways step in the same direction.");
            Assert.That(body.angularVelocity.y * side, Is.LessThan(-0.05f),
                "With the wheel as pivot the nose swings opposite to the handle: A turns it right, D turns it left.");
            Assert.That(noseYaw * side, Is.LessThan(0f), "The nose heading must turn opposite to the player's step.");
            Assert.That(Mathf.Abs(body.angularVelocity.y), Is.LessThanOrEqualTo(2.1f), "Steering speed must remain bounded.");
            Assert.That(Vector3.Angle(interaction.transform.up, Vector3.up), Is.LessThan(3f),
                "Steering force must yaw the wheelbarrow without rolling it.");
            Assert.That(body.isKinematic, Is.False);
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_BlockedPlayerSteeringDoesNotPushTheWheelbarrow()
        {
            Time.captureFramerate = SteeringTestFrameRate;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var interaction = CreateWheelbarrowForMovement(out var handle);
            var body = interaction.GetComponent<Rigidbody>();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            var playerPosition = _playerObject.transform.position;
            _targetObject2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetObject2.name = "SteeringWall";
            _targetObject2.transform.localScale = new Vector3(1f, 2f, 1f);
            _targetObject2.transform.position = playerPosition + Vector3.left * (0.35f + 0.02f + 0.5f) + Vector3.up;
            Physics.SyncTransforms();
            var startHandle = handle.bounds.center;

            Press(keyboard.aKey);
            yield return null;
            for (var i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
            Assert.That(Mathf.Abs(_playerObject.transform.position.x - playerPosition.x), Is.LessThan(0.05f),
                "The wall must stop the player's sideways step.");
            Assert.That(Mathf.Abs(handle.bounds.center.x - startHandle.x), Is.LessThan(0.05f),
                "A blocked player must not keep pushing the wheelbarrow away from the grip.");
            Assert.That(Mathf.Abs(body.angularVelocity.y), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_SteeringStopsWhenTheBarrowIsRolledOnItsSide()
        {
            Time.captureFramerate = SteeringTestFrameRate;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var interaction = CreateWheelbarrowForMovement(out var handle);
            var body = interaction.GetComponent<Rigidbody>();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            Press(keyboard.aKey);
            body.rotation = Quaternion.Euler(0f, 0f, 25f);
            body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            var startHandle = handle.bounds.center;
            yield return new WaitForSeconds(0.2f);

            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
            Assert.That(Mathf.Abs(body.angularVelocity.y), Is.LessThan(0.05f),
                "A barrow rolled past the limit must not be steered further.");
            Assert.That(Mathf.Abs(handle.bounds.center.x - startHandle.x), Is.LessThan(0.02f),
                "Handle force must stay off while the barrow is on its side.");
        }

#if UNITY_EDITOR
        private const string WheelbarrowPrefabPath = "Assets/Game/Prefabs/Vehicle/Wheelbarrow.prefab";

        private Rigidbody _prefabBody;
        private Vector3 _prefabStart;

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_DSteersHandleRightAndNoseLeftAroundGroundedWheel()
        {
            yield return SteerGroundedPrefab(true, 0);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_ASteersHandleLeftAndNoseRightAroundGroundedWheel()
        {
            yield return SteerGroundedPrefab(false, 0);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_ASteersHandleLeftAndNoseRightWithThreeCargo()
        {
            yield return SteerGroundedPrefab(false, 3);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_DSteersHandleRightAndNoseLeftWithThreeCargo()
        {
            yield return SteerGroundedPrefab(true, 3);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_ASteersHandleLeftAndNoseRightOnTwentyDegreeSlope()
        {
            yield return SteerGroundedPrefab(false, 0, 20f);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_DSteersHandleRightAndNoseLeftOnTwentyDegreeSlope()
        {
            yield return SteerGroundedPrefab(true, 0, 20f);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_LiftedWheelHasNoArtificialGroundGrip()
        {
            yield return SpawnAndHoldPrefab(0);
            var body = _prefabBody;
            var cartRight = body.transform.right;

            body.useGravity = false;
            body.position += Vector3.up * 2f;
            body.linearVelocity = cartRight;
            body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            for (var i = 0; i < 12; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
            // The held handle may still hold the barrow back a little, but a ground grip would cancel the sideways
            // speed within a couple of physics steps.
            Assert.That(Vector3.Dot(body.linearVelocity, cartRight), Is.GreaterThan(0.6f),
                "With the wheel off the ground nothing may artificially brake the barrow sideways.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_WPushesForwardKeepingFrontWheelDown()
        {
            yield return PushGroundedPrefab(true, 0);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_WPushesForwardWithThreeCargoKeepingFrontWheelDown()
        {
            yield return PushGroundedPrefab(true, 3);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_SReversesKeepingFrontWheelDown()
        {
            yield return PushGroundedPrefab(false, 0);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_SReversesWithThreeCargoKeepingFrontWheelDown()
        {
            yield return PushGroundedPrefab(false, 3);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_WThenSOnTwentyDegreeSlopeKeepsBarrowOnTheGround()
        {
            yield return PushThenReverseOnSlope(20f);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_WThenSOnTenDegreeSlopeKeepsBarrowOnTheGround()
        {
            yield return PushThenReverseOnSlope(10f);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_WPushesThreeTwentyFiveKgLoadsOnFlatGround()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SpawnAndHoldPrefab(3, cargoMass: 25f);

            var body = _prefabBody;
            var forward = Vector3.ProjectOnPlane(body.transform.forward, Vector3.up).normalized;
            var start = body.position;
            Press(keyboard.wKey);
            yield return null;
            for (var i = 0; i < 50; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
            Assert.That(Vector3.Dot(body.position - start, forward), Is.GreaterThan(0.3f),
                "Three 25 kg loads in the tray must be movable by pushing W on flat ground.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_HeldWithoutInputOnTwentyDegreeSlopeStaysPut()
        {
            yield return SpawnAndHoldPrefab(0, 20f);
            yield return AssertBarrowStaysPut(100, "A held barrow with no input must act as a parked one on a ramp.");
            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_NotHeldOnTwentyDegreeSlopeStaysPut()
        {
            yield return SpawnAndHoldPrefab(0, 20f, hold: false);
            yield return AssertBarrowStaysPut(100, "A parked barrow must keep its original foot friction on a ramp.");
        }

        private IEnumerator AssertBarrowStaysPut(int fixedSteps, string message)
        {
            var start = _prefabBody.position;
            for (var i = 0; i < fixedSteps; i++)
                yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Distance(_prefabBody.position, start), Is.LessThan(0.05f), message);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_WDownTwentyDegreeSlopeDoesNotOutrunThePlayer()
        {
            yield return WalkBarrowDownTwentyDegreeSlope(0);
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_WDownTwentyDegreeSlopeWithThreeCargoDoesNotOutrunThePlayer()
        {
            yield return WalkBarrowDownTwentyDegreeSlope(3);
        }

        private IEnumerator WalkBarrowDownTwentyDegreeSlope(int cargoCount)
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SpawnAndHoldPrefab(cargoCount, 20f, downhill: true);

            var body = _prefabBody;
            var targetSpeed = PlayerMovement.CalculatePushTargetSpeed(body.mass, _playerMovement.MoveSpeed);
            var downhill = Vector3.ProjectOnPlane(body.transform.forward, Vector3.up).normalized;
            Press(keyboard.wKey);
            yield return null;

            var peakSpeed = 0f;
            for (var i = 0; i < 125; i++)
            {
                yield return new WaitForFixedUpdate();
                peakSpeed = Mathf.Max(peakSpeed, body.linearVelocity.magnitude);
            }

            Debug.Log($"[VEH-002] downhill cargo={cargoCount} peakSpeed={peakSpeed:F2} target={targetSpeed:F2} " +
                      $"travel={Vector3.Dot(body.position - _prefabStart, downhill):F2}");
            Assert.That(_playerGrab.IsUsingInteractable, Is.True,
                $"Walking downhill must not release the barrow. Feedback: {_playerGrab.InteractionFeedback ?? "none"}.");
            Assert.That(Vector3.Dot(body.position - _prefabStart, downhill), Is.GreaterThan(1f),
                "W must walk the barrow down the slope.");
            Assert.That(peakSpeed, Is.LessThan(targetSpeed * 1.1f),
                "The barrow must not run away from the player faster than the push speed.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_LegsSlideOnlyWhileHeldAndPushing()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var movingMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                "Assets/Game/Settings/WheelLowFriction.physicMaterial");
            Assert.That(movingMaterial, Is.Not.Null);
            yield return SpawnAndHoldPrefab(0);

            var legs = new[]
            {
                _targetObject1.transform.Find("Leg_Left").GetComponent<Collider>(),
                _targetObject1.transform.Find("Leg_Right").GetComponent<Collider>(),
            };
            var rest = new[] { legs[0].sharedMaterial, legs[1].sharedMaterial };
            Assert.That(rest[0], Is.Not.Null.And.Not.SameAs(movingMaterial), "Legs rest on the high-friction material.");

            yield return null;
            AssertLegMaterials(legs, rest, "Holding without input keeps the original foot friction.");

            Press(keyboard.wKey);
            yield return null;
            yield return null;
            AssertLegMaterials(legs, new[] { movingMaterial, movingMaterial }, "W while held lowers the foot friction.");

            Release(keyboard.wKey);
            yield return null;
            yield return null;
            AssertLegMaterials(legs, rest, "Releasing the keys restores the original foot friction.");

            Press(keyboard.aKey);
            yield return null;
            yield return null;
            AssertLegMaterials(legs, rest, "Steering with A keeps the original foot friction the A/D feel was tuned with.");

            Release(keyboard.aKey);
            yield return null;
            Press(keyboard.sKey);
            yield return null;
            yield return null;
            AssertLegMaterials(legs, new[] { movingMaterial, movingMaterial }, "S while held lowers the foot friction.");

            Release(keyboard.sKey);
            yield return null;
            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            Assert.That(_playerGrab.IsUsingInteractable, Is.False, "The second E press releases the barrow.");
            AssertLegMaterials(legs, rest, "Releasing the barrow with E restores the original foot friction.");
        }

        [UnityTest]
        public IEnumerator WheelbarrowPrefab_LegsKeepOriginalFrictionWhenInteractionIsDisabledWhilePushing()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var movingMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                "Assets/Game/Settings/WheelLowFriction.physicMaterial");
            yield return SpawnAndHoldPrefab(0);

            var legs = new[]
            {
                _targetObject1.transform.Find("Leg_Left").GetComponent<Collider>(),
                _targetObject1.transform.Find("Leg_Right").GetComponent<Collider>(),
            };
            var rest = new[] { legs[0].sharedMaterial, legs[1].sharedMaterial };
            Press(keyboard.wKey);
            yield return null;
            yield return null;
            AssertLegMaterials(legs, new[] { movingMaterial, movingMaterial }, "W while held lowers the foot friction.");

            ((Behaviour)_targetObject1.GetComponent(FindWheelbarrowInteractionType())).enabled = false;
            AssertLegMaterials(legs, rest, "Disabling the interaction while pushing restores the original foot friction.");
        }

        private static void AssertLegMaterials(Collider[] legs, PhysicsMaterial[] expected, string message)
        {
            for (var i = 0; i < legs.Length; i++)
                Assert.That(legs[i].sharedMaterial, Is.SameAs(expected[i]), message);
        }

        private IEnumerator PushThenReverseOnSlope(float slopeDegrees)
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SpawnAndHoldPrefab(0, slopeDegrees);

            var body = _prefabBody;
            var wheel = _targetObject1.transform.Find("Wheel");
            var legLeft = _targetObject1.transform.Find("Leg_Left");
            var legRight = _targetObject1.transform.Find("Leg_Right");
            var ground = _obstacleObject.GetComponent<Collider>();
            var startWheelGap = GetGroundGap(wheel, ground);
            var startLegLeftGap = GetGroundGap(legLeft, ground);
            var startLegRightGap = GetGroundGap(legRight, ground);
            var uphill = Vector3.ProjectOnPlane(body.transform.forward, Vector3.up).normalized;

            Press(keyboard.wKey);
            for (var i = 0; i < 75; i++)
                yield return new WaitForFixedUpdate();
            var travelUp = Vector3.Dot(body.position - _prefabStart, uphill);
            // Input must be changed from the Update phase; after WaitForFixedUpdate the keyboard has no state buffer.
            yield return null;
            Release(keyboard.wKey);
            yield return null;
            yield return null;
            var reverseStart = body.position;
            Press(keyboard.sKey);

            var maxLift = 0f;
            for (var i = 0; i < 75; i++)
            {
                yield return new WaitForFixedUpdate();
                var wheelGap = GetGroundGap(wheel, ground);
                var leftGap = GetGroundGap(legLeft, ground);
                var rightGap = GetGroundGap(legRight, ground);
                maxLift = Mathf.Max(
                    maxLift,
                    float.IsNaN(wheelGap) ? 1f : wheelGap - startWheelGap,
                    float.IsNaN(leftGap) ? 1f : leftGap - startLegLeftGap,
                    float.IsNaN(rightGap) ? 1f : rightGap - startLegRightGap);
            }

            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
            Assert.That(travelUp, Is.GreaterThan(1f), "W must push the barrow up the slope.");
            Assert.That(Vector3.Dot(body.position - reverseStart, uphill), Is.LessThan(-0.3f),
                "S must pull the barrow back down the slope.");
            Assert.That(maxLift, Is.LessThan(0.03f),
                "Reversing on a slope must not lift the wheel or legs off the ground.");
        }

        private IEnumerator PushGroundedPrefab(bool forwardPush, int cargoCount)
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SpawnAndHoldPrefab(cargoCount);

            var body = _prefabBody;
            var wheel = _targetObject1.transform.Find("Wheel");
            var ground = _obstacleObject.GetComponent<Collider>();
            var forward = Vector3.ProjectOnPlane(body.transform.forward, Vector3.up).normalized;
            var start = body.position;
            var startPitch = Mathf.Asin(body.transform.forward.y) * Mathf.Rad2Deg;
            var startGap = GetWheelGroundGap(wheel, ground);
            Assert.That(startGap, Is.Not.NaN, "The front wheel of the prefab must touch the ground before pushing.");

            var maxPitchChange = 0f;
            var maxWheelLift = 0f;
            var peakSpeed = 0f;
            Press(forwardPush ? keyboard.wKey : keyboard.sKey);
            yield return null;
            for (var i = 0; i < 50; i++)
            {
                yield return new WaitForFixedUpdate();
                peakSpeed = Mathf.Max(peakSpeed, Vector3.Dot(body.linearVelocity, forward) * (forwardPush ? 1f : -1f));
                maxPitchChange = Mathf.Max(
                    maxPitchChange, Mathf.Abs(Mathf.Asin(body.transform.forward.y) * Mathf.Rad2Deg - startPitch));
                var gap = GetWheelGroundGap(wheel, ground);
                maxWheelLift = Mathf.Max(maxWheelLift, float.IsNaN(gap) ? 1f : gap - startGap);
            }

            var travel = Vector3.Dot(body.position - start, forward);
            Debug.Log($"[VEH-002] {(forwardPush ? "W" : "S")} cargo={cargoCount} peakSpeed={peakSpeed:F2} travel={travel:F2}");
            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
            Assert.That(travel * (forwardPush ? 1f : -1f), Is.GreaterThan(0.3f),
                forwardPush ? "W must push the barrow forward." : "S must pull the barrow backward.");
            Assert.That(maxPitchChange, Is.LessThan(5f), "Pushing or pulling must not pitch the barrow.");
            Assert.That(maxWheelLift, Is.LessThan(0.03f), "The front wheel must stay on the ground.");
            if (!forwardPush)
            {
                // Reverse target is 0.65 of the walking push speed (about 3.2 m/s). With the legs dragging at full
                // friction the same barrow only reached about 2.3 m/s empty and 1.0 m/s with three cargo cubes.
                Assert.That(peakSpeed, Is.GreaterThan(cargoCount > 0 ? 2.5f : 2.7f),
                    "Reversing must roll smoothly toward the push speed instead of dragging the legs.");
            }
        }

        private static float GetGroundGap(Transform part, Collider ground)
        {
            foreach (var hit in Physics.RaycastAll(part.position + Vector3.up * 0.3f, Vector3.down, 1f))
            {
                if (hit.collider == ground)
                    return hit.distance;
            }

            return float.NaN;
        }

        // Distance from the wheel axle down to the ground collider, or NaN when the wheel does not see the ground.
        private static float GetWheelGroundGap(Transform wheel, Collider ground)
        {
            foreach (var hit in Physics.RaycastAll(wheel.position + Vector3.up * 0.2f, Vector3.down, 0.6f))
            {
                if (hit.collider == ground)
                    return hit.distance;
            }

            return float.NaN;
        }

        private IEnumerator SteerGroundedPrefab(bool right, int cargoCount, float slopeDegrees = 0f)
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return SpawnAndHoldPrefab(cargoCount, slopeDegrees);

            var body = _prefabBody;
            var wheel = _targetObject1.transform.Find("Wheel");
            var handle = _targetObject1.transform.Find("Handle_Left").GetComponent<Collider>();
            var ground = _obstacleObject.GetComponent<Collider>();
            var grounded = false;
            foreach (var hit in Physics.RaycastAll(wheel.position + Vector3.up * 0.2f, Vector3.down, 0.5f))
                grounded |= hit.collider == ground;
            Assert.That(grounded, Is.True, "The front wheel of the prefab must touch the ground before steering.");

            var side = right ? 1f : -1f;
            var cartRight = body.transform.right;
            var legLeft = _targetObject1.transform.Find("Leg_Left");
            var legRight = _targetObject1.transform.Find("Leg_Right");
            var groundNormal = Quaternion.Euler(-slopeDegrees, 0f, 0f) * Vector3.up;
            var startLegLeftGap = GetGroundGap(legLeft, ground);
            var startLegRightGap = GetGroundGap(legRight, ground);
            var startRoll = Mathf.Asin(Vector3.Dot(body.transform.right, groundNormal)) * Mathf.Rad2Deg;
            var maxLegLift = 0f;
            var maxRollChange = 0f;
            var startForward = Vector3.ProjectOnPlane(body.transform.forward, Vector3.up);
            var startTilt = Vector3.Angle(body.transform.up, Vector3.up);
            var startHandle = handle.bounds.center;
            var startPlayer = _playerObject.transform.position;
            var startWheel = wheel.position;

            var peakYawSpeed = 0f;
            Press(right ? keyboard.dKey : keyboard.aKey);
            yield return null;
            for (var i = 0; i < 25; i++)
            {
                yield return new WaitForFixedUpdate();
                peakYawSpeed = Mathf.Max(peakYawSpeed, Mathf.Abs(body.angularVelocity.y));
                var leftGap = GetGroundGap(legLeft, ground);
                var rightGap = GetGroundGap(legRight, ground);
                maxLegLift = Mathf.Max(
                    maxLegLift,
                    float.IsNaN(leftGap) ? 1f : leftGap - startLegLeftGap,
                    float.IsNaN(rightGap) ? 1f : rightGap - startLegRightGap);
                maxRollChange = Mathf.Max(
                    maxRollChange,
                    Mathf.Abs(Mathf.Asin(Vector3.Dot(body.transform.right, groundNormal)) * Mathf.Rad2Deg - startRoll));
            }

            var handleShift = Vector3.Dot(handle.bounds.center - startHandle, cartRight);
            var playerShift = Vector3.Dot(_playerObject.transform.position - startPlayer, cartRight);
            var wheelTravel = Vector3.ProjectOnPlane(wheel.position - startWheel, Vector3.up).magnitude;
            var handleTravel = Vector3.ProjectOnPlane(handle.bounds.center - startHandle, Vector3.up).magnitude;
            var noseYaw = Vector3.SignedAngle(
                startForward, Vector3.ProjectOnPlane(body.transform.forward, Vector3.up), Vector3.up);
            var tilt = Vector3.Angle(body.transform.up, Vector3.up);

            Assert.That(_playerGrab.IsUsingInteractable, Is.True, "Steering must not break the hold.");
            Assert.That(playerShift * side, Is.GreaterThan(0.03f), "A steps the player left and D steps the player right.");
            Assert.That(handleShift * side, Is.GreaterThan(0.03f), "The handle must swing the same way as the player.");
            Assert.That(noseYaw * side, Is.LessThan(-2f), "A must turn the nose right and D must turn it left.");
            Assert.That(tilt - startTilt, Is.LessThan(8f), "Steering must not roll one side of the wheelbarrow up.");
            Assert.That(peakYawSpeed, Is.LessThan(2.3f), "Steering speed must stay bounded.");
            Assert.That(maxRollChange, Is.LessThan(5f), "Steering must not roll the barrow relative to the ground.");
            Assert.That(maxLegLift, Is.LessThan(0.03f), "Both legs must stay on the ground while steering.");
            Assert.That(wheelTravel, Is.LessThan(handleTravel * 0.5f),
                "The front wheel contact must stay a relatively fixed pivot while the handle swings.");
            Assert.That(body.isKinematic, Is.False);
        }

        private IEnumerator SpawnAndHoldPrefab(
            int cargoCount, float slopeDegrees = 0f, float cargoMass = 1f, bool downhill = false, bool hold = true)
        {
            Time.captureFramerate = SteeringTestFrameRate;
            // The player prefab ships with a 350 N push limit; the bare test player defaults to 300 N.
            typeof(PlayerMovement).GetField("_maxPushForce", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_playerMovement, 350f);
            EnableMoveInput();
            // Ground whose top surface passes through the origin, pitched up along +Z by slopeDegrees.
            var slope = Quaternion.Euler(-slopeDegrees, 0f, 0f);
            var slopeRise = Mathf.Tan(slopeDegrees * Mathf.Deg2Rad);
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _obstacleObject.transform.SetPositionAndRotation(slope * Vector3.down * 0.5f, slope);
            _obstacleObject.transform.localScale = new Vector3(40f, 1f, 40f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WheelbarrowPrefabPath);
            Assert.That(prefab, Is.Not.Null, "The original wheelbarrow prefab must be available to Play Mode tests.");
            var wheelbarrow = Object.Instantiate(prefab);
            wheelbarrow.name = "WheelbarrowPrefabSteeringTest";
            // downhill turns the barrow around so that its nose (and W) points down the slope.
            wheelbarrow.transform.SetPositionAndRotation(
                new Vector3(0f, 0.3f + slopeRise * 3f, 3f), downhill ? slope * Quaternion.Euler(0f, 180f, 0f) : slope);
            _targetObject1 = wheelbarrow;
            _prefabBody = wheelbarrow.GetComponent<Rigidbody>();
            _prefabStart = _prefabBody.position;

            if (cargoCount > 0)
            {
                // Same as the Playground's Cargo_1..3: loose 1 kg cubes resting in the tray.
                _targetObject2 = new GameObject("Cargo");
                for (var i = 0; i < cargoCount; i++)
                {
                    var cargo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cargo.transform.SetParent(_targetObject2.transform, true);
                    cargo.transform.localScale = Vector3.one * 0.4f;
                    cargo.transform.position = wheelbarrow.transform.TransformPoint(new Vector3(0f, 0.9f, -0.2f + 0.4f * i));
                    cargo.AddComponent<Rigidbody>().mass = cargoMass;
                }
            }

            Physics.SyncTransforms();
            for (var i = 0; i < 50; i++)
                yield return new WaitForFixedUpdate();

            if (!hold)
                yield break;

            var handle = wheelbarrow.transform.Find("Handle_Left").GetComponent<Collider>();
            // Facing downhill the handles are uphill of the barrow; the grip path would climb into the slope from the
            // handle's own height, so start a little further up and let the grip step walk down to the handles.
            var playerZ = handle.bounds.center.z + (downhill ? 0.8f : 0f);
            _playerObject.transform.SetPositionAndRotation(
                new Vector3(handle.bounds.center.x - 1.5f, slopeRise * playerZ, playerZ), Quaternion.identity);
            _playerMovement.LocalCamera.transform.rotation = Quaternion.LookRotation(
                handle.bounds.center - _playerMovement.LocalCamera.transform.position, Vector3.up);
            Physics.SyncTransforms();
            yield return null;
            // The controller may settle on a slope during that frame; aim again at the thin handle bar.
            _playerMovement.LocalCamera.transform.rotation = Quaternion.LookRotation(
                handle.bounds.center - _playerMovement.LocalCamera.transform.position, Vector3.up);
            _detector.Detect();

            Assert.That(_detector.CurrentHit.collider, Is.SameAs(handle));
            Assert.That(_playerGrab.RequestGrab(), Is.True,
                $"Feedback: {_playerGrab.InteractionFeedback ?? "none"}.");
            Assert.That(_playerGrab.IsUsingInteractable, Is.True);
        }
#endif

        [UnityTest]
        public IEnumerator WheelbarrowInteraction_SReversesAlongOrientationWhileHeld()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var interaction = CreateWheelbarrowForMovement(out _);
            var body = interaction.GetComponent<Rigidbody>();
            yield return null;
            _detector.Detect();
            Assert.That(_playerGrab.RequestGrab(), Is.True);

            Press(keyboard.sKey);
            for (var i = 0; i < 4; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(_playerGrab.IsUsingInteractable, Is.True,
                "The wheelbarrow must remain held while reversing.");
            Assert.That(_playerMovement.MovementInput.y, Is.EqualTo(-1f), "S input must reach the held interaction.");
            Assert.That(Vector3.Dot(body.linearVelocity, interaction.transform.forward), Is.LessThan(-0.1f),
                "S must pull the wheelbarrow backward along its orientation.");
            Assert.That(Vector3.Dot(body.linearVelocity, interaction.transform.forward), Is.GreaterThanOrEqualTo(-3.4f),
                "Reverse speed must respect its gentler push strength.");
            Assert.That(body.isKinematic, Is.False);
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

        [UnityTest]
        public IEnumerator ExecuteThrow_WithCarriedObject_AppliesImpulseInLookDirection()
        {
            _targetObject1 = CreateGrabbable("LightProp", new Vector3(0f, 1.4f, 2f)).gameObject;
            var target = _targetObject1.GetComponent<GrabbableObject>();
            target.Rigidbody.mass = 1f;

            _playerGrab.ExecuteGrab(target);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            bool thrown = _playerGrab.ExecuteThrow();
            Assert.That(thrown, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.False);
            Assert.That(target.Rigidbody.isKinematic, Is.False);
            Assert.That(target.Rigidbody.useGravity, Is.True);

            // Default throw force is 10N·s, so 1kg object should achieve forward velocity ~10m/s
            Assert.That(target.Rigidbody.linearVelocity.z, Is.GreaterThan(5f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExecuteThrow_HeavyObjectVsLightObject_HeavyObjectTravelsFarShorter()
        {
            _targetObject1 = CreateGrabbable("Light1kg", new Vector3(0f, 1.4f, 2f)).gameObject;
            var light = _targetObject1.GetComponent<GrabbableObject>();
            light.Rigidbody.mass = 1f;

            _playerGrab.ExecuteGrab(light);
            _playerGrab.ExecuteThrow();
            float lightSpeed = light.Rigidbody.linearVelocity.magnitude;

            _targetObject2 = CreateGrabbable("Heavy25kg", new Vector3(0f, 1.4f, 2f)).gameObject;
            var heavy = _targetObject2.GetComponent<GrabbableObject>();
            heavy.Rigidbody.mass = 25f;

            _playerGrab.ExecuteGrab(heavy);
            _playerGrab.ExecuteThrow();
            float heavySpeed = heavy.Rigidbody.linearVelocity.magnitude;

            // 25kg cement bag should receive ~1/25 of the velocity change
            Assert.That(heavySpeed, Is.GreaterThan(0.01f), "Heavy object should still receive some velocity");
            Assert.That(lightSpeed, Is.GreaterThan(heavySpeed * 15f),
                $"Light speed ({lightSpeed}) should be substantially greater than heavy speed ({heavySpeed})");
            yield return null;
        }

        [Test]
        public void ExecuteThrow_WithEmptyHands_ReturnsFalseWithoutErrors()
        {
            Assert.That(_playerGrab.IsCarrying, Is.False);
            bool result = _playerGrab.ExecuteThrow();
            Assert.That(result, Is.False);
        }

        [UnityTest]
        public IEnumerator ExecuteThrow_WhileMoving_InheritsPlayerHorizontalVelocity()
        {
            _targetObject1 = CreateGrabbable("MovingThrowTarget", new Vector3(0f, 1.4f, 2f)).gameObject;
            var target = _targetObject1.GetComponent<GrabbableObject>();
            target.Rigidbody.mass = 1f;

            _playerGrab.ExecuteGrab(target);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            // Simulate character moving horizontally along X
            var controller = _playerObject.GetComponent<CharacterController>();
            controller.Move(new Vector3(4f, 0f, 0f) * Time.deltaTime);
            yield return null;

            _playerGrab.ExecuteThrow();

            var rb = target.Rigidbody;
            // Should have positive Z velocity from throw impulse and inherited positive X velocity from player motion
            Assert.That(rb.linearVelocity.z, Is.GreaterThan(5f), "Should have forward throw velocity");
            Assert.That(rb.linearVelocity.x, Is.GreaterThan(0.1f), "Should inherit player horizontal X velocity");
        }

        [UnityTest]
        public IEnumerator ThrowInput_WhenCursorAlreadyLocked_TriggersThrow()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            _targetObject1 = CreateGrabbable("TargetThrowInput", new Vector3(0f, 1.4f, 2f)).gameObject;
            var target = _targetObject1.GetComponent<GrabbableObject>();

            _playerGrab.ExecuteGrab(target);
            Assert.That(_playerGrab.IsCarrying, Is.True);
            Assert.That(_playerMovement.IsCursorLocked, Is.True);

            Press(mouse.leftButton);
            yield return null;

            Assert.That(_playerGrab.IsCarrying, Is.False, "Pressing LMB with cursor locked should throw carried object");
        }

        [UnityTest]
        public IEnumerator ThrowInput_WhenClickReLocksCursor_DoesNotTriggerThrow()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            _targetObject1 = CreateGrabbable("TargetRelockGuard", new Vector3(0f, 1.4f, 2f)).gameObject;
            var target = _targetObject1.GetComponent<GrabbableObject>();

            _playerGrab.ExecuteGrab(target);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            // Unlock cursor via Escape
            Press(keyboard.escapeKey);
            yield return null;
            Assert.That(_playerMovement.IsCursorLocked, Is.False);

            Release(keyboard.escapeKey);
            yield return null;

            // Click LMB to re-lock cursor: MUST NOT THROW
            Press(mouse.leftButton);
            yield return null;

            Assert.That(_playerMovement.IsCursorLocked, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.True, "LMB click that relocked cursor must NOT throw carried object");

            Release(mouse.leftButton);
            yield return null;

            // Next click when cursor was ALREADY locked: SHOULD THROW
            Press(mouse.leftButton);
            yield return null;

            Assert.That(_playerGrab.IsCarrying, Is.False, "LMB click after cursor was locked should throw carried object");
        }

        [UnityTest]
        public IEnumerator Update_WithoutThrowAction_DoesNotProcessThrowInput()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            _playerGrab.ThrowAction = null;
            _targetObject1 = CreateGrabbable("TargetNoThrowInput", new Vector3(0f, 1.4f, 2f)).gameObject;
            var target = _targetObject1.GetComponent<GrabbableObject>();

            _playerGrab.ExecuteGrab(target);
            Assert.That(_playerGrab.IsCarrying, Is.True);
            Assert.That(_playerMovement.IsCursorLocked, Is.True);

            Press(mouse.leftButton);
            yield return null;

            Assert.That(_playerGrab.IsCarrying, Is.True, "Without ThrowAction, pressing LMB must not throw carried object (no hardcoded fallback)");
        }

        [UnityTest]
        public IEnumerator ThrowInput_AfterRelock_WhenMovementUpdatesBeforeGrab_ThrowsCarriedObject()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            _targetObject1 = CreateGrabbable("TargetOrderMoveFirst", new Vector3(0f, 1.4f, 2f)).gameObject;
            var target = _targetObject1.GetComponent<GrabbableObject>();

            // Disable components before grabbing so OnDisable does not trigger ExecuteDrop
            _playerMovement.enabled = false;
            _playerGrab.enabled = false;

            _playerGrab.ExecuteGrab(target);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            var moveUpdate = typeof(PlayerMovement).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            var grabUpdate = typeof(PlayerGrab).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

            // Unlock cursor via Escape
            Press(keyboard.escapeKey);
            yield return null;
            moveUpdate.Invoke(_playerMovement, null);
            grabUpdate.Invoke(_playerGrab, null);
            Assert.That(_playerMovement.IsCursorLocked, Is.False);
            Release(keyboard.escapeKey);

            // Frame N: Click LMB to relock cursor
            Press(mouse.leftButton);
            yield return null;
            moveUpdate.Invoke(_playerMovement, null);
            grabUpdate.Invoke(_playerGrab, null);
            Assert.That(_playerMovement.IsCursorLocked, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.True, "Relock click must NOT throw carried object");
            Release(mouse.leftButton);

            // Frame N+1: Click LMB to throw, running Movement before Grab
            Press(mouse.leftButton);
            yield return null;
            moveUpdate.Invoke(_playerMovement, null);
            grabUpdate.Invoke(_playerGrab, null);

            Assert.That(_playerGrab.IsCarrying, Is.False, "Throw click must throw object when Movement updates before Grab");
        }

        [UnityTest]
        public IEnumerator ThrowInput_AfterRelock_WhenGrabUpdatesBeforeMovement_ThrowsCarriedObject()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            _targetObject1 = CreateGrabbable("TargetOrderGrabFirst", new Vector3(0f, 1.4f, 2f)).gameObject;
            var target = _targetObject1.GetComponent<GrabbableObject>();

            // Disable components before grabbing so OnDisable does not trigger ExecuteDrop
            _playerMovement.enabled = false;
            _playerGrab.enabled = false;

            _playerGrab.ExecuteGrab(target);
            Assert.That(_playerGrab.IsCarrying, Is.True);

            var moveUpdate = typeof(PlayerMovement).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            var grabUpdate = typeof(PlayerGrab).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

            // Unlock cursor via Escape
            Press(keyboard.escapeKey);
            yield return null;
            moveUpdate.Invoke(_playerMovement, null);
            grabUpdate.Invoke(_playerGrab, null);
            Assert.That(_playerMovement.IsCursorLocked, Is.False);
            Release(keyboard.escapeKey);

            // Frame N: Click LMB to relock cursor (test with Grab first during relock)
            Press(mouse.leftButton);
            yield return null;
            grabUpdate.Invoke(_playerGrab, null);
            moveUpdate.Invoke(_playerMovement, null);
            Assert.That(_playerMovement.IsCursorLocked, Is.True);
            Assert.That(_playerGrab.IsCarrying, Is.True, "Relock click must NOT throw carried object even if Grab runs first");
            Release(mouse.leftButton);

            // Frame N+1: Click LMB to throw, running Grab BEFORE Movement
            Press(mouse.leftButton);
            yield return null;
            grabUpdate.Invoke(_playerGrab, null);
            moveUpdate.Invoke(_playerMovement, null);

            Assert.That(_playerGrab.IsCarrying, Is.False, "Throw click must throw object even when Grab updates before Movement");
        }

        [UnityTest]
        public IEnumerator CarriedObject_CarryingHeavyObject_AppliesCarriedMassToMovement()
        {
            _targetObject1 = CreateGrabbable("HeavyTarget", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();
            grabbable.Rigidbody.mass = 25f;

            Physics.SyncTransforms();
            yield return null;

            Assert.That(_playerMovement.CarriedMass, Is.EqualTo(0f));
            _playerGrab.ExecuteGrab(grabbable);
            Assert.That(_playerMovement.CarriedMass, Is.EqualTo(25f));

            _playerGrab.ExecuteDrop();
            Assert.That(_playerMovement.CarriedMass, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator CarriedObject_PressedAgainstWall_PrioritizesWallOverCameraNearClip()
        {
            // Wall placed at 0.55m from player center (camera at Z=0)
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = _obstacleObject;
            wall.name = "CloseWall";
            wall.transform.position = new Vector3(0f, 1.4f, 0.55f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.1f);

            _targetObject1 = CreateGrabbable("SmallBox", new Vector3(0f, 1.4f, 2f)).gameObject;
            _targetObject1.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            var boxCol = _targetObject1.GetComponent<Collider>();
            var wallCol = wall.GetComponent<Collider>();

            bool pen = Physics.ComputePenetration(
                boxCol, _targetObject1.transform.position, _targetObject1.transform.rotation,
                wallCol, wall.transform.position, wall.transform.rotation,
                out _, out float dist);

            Assert.That(pen && dist > 0.01f, Is.False,
                $"Carried object penetrated wall by {dist}m when pressed close!");
        }

        [UnityTest]
        public IEnumerator CarriedObject_PushedTooCloseAgainstPlayer_ExceedingPinchDelay_TriggersAutoDrop()
        {
            // Wall placed right at player capsule face (Z=0.25m), leaving no space for 0.4m carried box
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = _obstacleObject;
            wall.name = "PinchingWall";
            wall.transform.position = new Vector3(0f, 1.4f, 0.25f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.1f);

            _targetObject1 = CreateGrabbable("PinchBox", new Vector3(0f, 1.4f, 2f)).gameObject;
            _targetObject1.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            // Immediately after 1 frame (< 0.3s delay), object should still be carried
            Assert.That(_playerGrab.IsCarrying, Is.True, "Object should not immediately drop before pinch delay expires!");

            // Wait until after _pinchDropDelay (0.3s)
            yield return new WaitForSeconds(_playerGrab.PinchDropDelay + 0.1f);
            yield return null;

            // After exceeding pinch delay, LateUpdate should drop the object
            Assert.That(_playerGrab.IsCarrying, Is.False, "Object pinched past delay should auto-drop!");
            Assert.That(grabbable.IsHeld, Is.False);
            Assert.That(grabbable.Rigidbody.isKinematic, Is.False);
        }

        [UnityTest]
        public IEnumerator CarriedObject_PushedTooCloseAgainstPlayer_BrieflyPinchedThenReleased_DoesNotDrop()
        {
            // Wall placed right at player capsule face (Z=0.25m)
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = _obstacleObject;
            wall.name = "TemporaryPinchingWall";
            wall.transform.position = new Vector3(0f, 1.4f, 0.25f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.1f);

            _targetObject1 = CreateGrabbable("BriefPinchBox", new Vector3(0f, 1.4f, 2f)).gameObject;
            _targetObject1.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            // Terhimpit selama < jeda (mis. 0.1s saat delay 0.3s)
            yield return new WaitForSeconds(0.1f);
            Assert.That(_playerGrab.IsCarrying, Is.True, "Object pinched briefly should not drop!");

            // Dilepas dari dinding (dinding dipindahkan jauh ke belakang)
            wall.transform.position = new Vector3(0f, 1.4f, 10f);
            Physics.SyncTransforms();

            // Tunggu melewati durasi jeda awal (0.35s) saat sudah tidak terhimpit
            yield return new WaitForSeconds(0.35f);

            // Objek harus tetap dibawa karena timer ter-reset saat tidak terhimpit
            Assert.That(_playerGrab.IsCarrying, Is.True, "Object should remain held after being released from pinch before delay expired!");
            Assert.That(grabbable.IsHeld, Is.True);
        }

        [UnityTest]
        public IEnumerator CarriedObject_WhenAutoDropDisabled_PushedTooCloseAgainstPlayer_ExceedingPinchDelay_DoesNotDrop()
        {
            // Wall placed right at player capsule face (Z=0.25m), leaving no space for 0.4m carried box
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = _obstacleObject;
            wall.name = "PinchingWall";
            wall.transform.position = new Vector3(0f, 1.4f, 0.25f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.1f);

            _targetObject1 = CreateGrabbable("PinchBoxDisabled", new Vector3(0f, 1.4f, 2f)).gameObject;
            _targetObject1.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            _playerGrab.AutoDropEnabled = false;
            yield return null;

            // Wait until after _pinchDropDelay (0.3s)
            yield return new WaitForSeconds(_playerGrab.PinchDropDelay + 0.1f);
            yield return null;

            // With AutoDropEnabled = false, carried object must remain held even when pinched
            Assert.That(_playerGrab.IsCarrying, Is.True, "Carried object must remain held when AutoDropEnabled is false!");
            Assert.That(grabbable.IsHeld, Is.True);
        }

        [UnityTest]
        public IEnumerator CarriedObject_WhenAutoDropHandlerConfigured_PushedTooCloseAgainstPlayer_ExceedingPinchDelay_InvokesHandlerExactlyOnce()
        {
            // Wall placed right at player capsule face (Z=0.25m), leaving no space for 0.4m carried box
            _obstacleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var wall = _obstacleObject;
            wall.name = "PinchingWall";
            wall.transform.position = new Vector3(0f, 1.4f, 0.25f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.1f);

            _targetObject1 = CreateGrabbable("PinchBoxHandler", new Vector3(0f, 1.4f, 2f)).gameObject;
            _targetObject1.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            int handlerCallCount = 0;
            _playerGrab.AutoDropHandler = () =>
            {
                handlerCallCount++;
                _playerGrab.ExecuteDrop();
            };
            yield return null;

            // Wait until after _pinchDropDelay (0.3s)
            yield return new WaitForSeconds(_playerGrab.PinchDropDelay + 0.1f);
            yield return null;

            Assert.That(handlerCallCount, Is.EqualTo(1), "AutoDropHandler must be invoked exactly once when pinched past delay!");
            Assert.That(_playerGrab.IsCarrying, Is.False, "Carried object must be dropped after handler executes!");
            Assert.That(grabbable.IsHeld, Is.False);
        }

        [UnityTest]
        public IEnumerator OnDisable_WhileCarryingHeavyObject_ResetsCarriedMassToZero()
        {
            _targetObject1 = CreateGrabbable("HeavyDisableTarget", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();
            grabbable.Rigidbody.mass = 25f;

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            Assert.That(_playerMovement.CarriedMass, Is.EqualTo(25f));

            _playerGrab.enabled = false;
            yield return null;

            Assert.That(_playerMovement.CarriedMass, Is.EqualTo(0f),
                "Disabling PlayerGrab should reset CarriedMass on PlayerMovement to 0!");
            Assert.That(_playerGrab.IsCarrying, Is.False);
        }

        [UnityTest]
        public IEnumerator ExecuteDrop_WhenOverlappingPlayerCapsule_MaintainsIgnoreCollisionUntilSeparated()
        {
            _targetObject1 = CreateGrabbable("OverlapDropTarget", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();
            var playerCol = _playerObject.GetComponent<Collider>();
            var targetCol = _targetObject1.GetComponent<Collider>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            // Position target overlapping player capsule (capsule is at origin, radius 0.35, height 1.8)
            _targetObject1.transform.position = _playerObject.transform.position + new Vector3(0f, 0.9f, 0.2f);
            Physics.SyncTransforms();

            _playerGrab.ExecuteDrop();

            // Collision must remain ignored while overlapping to prevent launching impulse
            Assert.That(Physics.GetIgnoreCollision(playerCol, targetCol), Is.True,
                "Ignore collision must remain active immediately after drop while overlapping capsule!");

            // Move player far away so they are completely separated
            _playerObject.transform.position += new Vector3(10f, 0f, 0f);
            Physics.SyncTransforms();

            yield return new WaitForFixedUpdate();

            // Once separated, collision must be restored
            Assert.That(Physics.GetIgnoreCollision(playerCol, targetCol), Is.False,
                "Ignore collision should be restored once player and object have separated!");
        }

        [UnityTest]
        public IEnumerator SeparationTracking_HandlesDestroyedObjectGracefully()
        {
            _targetObject1 = CreateGrabbable("DestroyedDuringSeparation", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            _targetObject1.transform.position = _playerObject.transform.position + new Vector3(0f, 0.9f, 0.2f);
            Physics.SyncTransforms();

            _playerGrab.ExecuteDrop();

            // Destroy object immediately while in separation tracking queue
            Object.DestroyImmediate(_targetObject1);
            _targetObject1 = null;

            Assert.DoesNotThrow(() =>
            {
                Physics.SyncTransforms();
            });

            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator SeparationTracking_ExceedingInitialQueueCapacity_MonitorsAllAndRestoresCollisionOnceSeparated()
        {
            var playerCol = _playerObject.GetComponent<Collider>();
            var targets = new List<GameObject>();
            var targetCols = new List<Collider>();

            try
            {
                // Create 10 grabbable objects (initial queue capacity is 8)
                const int count = 10;
                for (int i = 0; i < count; i++)
                {
                    var obj = CreateGrabbable($"OverflowTarget_{i}", new Vector3(0f, 1.4f, 2f)).gameObject;
                    targets.Add(obj);
                    var grabbable = obj.GetComponent<GrabbableObject>();
                    var col = obj.GetComponent<Collider>();
                    targetCols.Add(col);

                    Physics.SyncTransforms();
                    yield return null;

                    _playerGrab.ExecuteGrab(grabbable);
                    yield return null;

                    // Position overlapping player capsule
                    obj.transform.position = _playerObject.transform.position + new Vector3(0f, 0.9f, 0.2f);
                    Physics.SyncTransforms();

                    _playerGrab.ExecuteDrop();

                    Assert.That(Physics.GetIgnoreCollision(playerCol, col), Is.True,
                        $"Object {i} must maintain ignored collision while overlapping player capsule!");
                }

                // Move player far away so all objects are completely separated
                _playerObject.transform.position += new Vector3(10f, 0f, 0f);
                Physics.SyncTransforms();

                yield return new WaitForFixedUpdate();

                // All objects, including those exceeding initial queue capacity (8), must have collision restored
                for (int i = 0; i < count; i++)
                {
                    Assert.That(Physics.GetIgnoreCollision(playerCol, targetCols[i]), Is.False,
                        $"Collision for object {i} must be restored once separated, even when initial capacity was exceeded!");
                }
            }
            finally
            {
                foreach (var obj in targets)
                {
                    if (obj != null)
                        Object.DestroyImmediate(obj);
                }
            }
        }

        [UnityTest]
        public IEnumerator OnDisable_WhenCarriedObjectOverlapsPlayer_PreservesIgnoreUntilSeparatedAndPreventsLaunch()
        {
            _targetObject1 = CreateGrabbable("OverlapOnDisableTarget", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();
            var rb = grabbable.Rigidbody;
            var playerCol = _playerObject.GetComponent<Collider>();
            var targetCol = _targetObject1.GetComponent<Collider>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            // Position held object deeply overlapping player capsule
            _targetObject1.transform.position = _playerObject.transform.position + new Vector3(0f, 0.9f, 0.1f);
            Physics.SyncTransforms();

            // Disable PlayerGrab while object is overlapping
            _playerGrab.enabled = false;
            yield return null;

            Assert.That(_playerGrab.IsCarrying, Is.False, "Object should be dropped when component is disabled!");
            Assert.That(grabbable.IsHeld, Is.False);
            Assert.That(Physics.GetIgnoreCollision(playerCol, targetCol), Is.True,
                "Collision must remain ignored while overlapping capsule, even after PlayerGrab is disabled!");

            // Run physics step while overlapping: solver must NOT launch object
            yield return new WaitForFixedUpdate();
            Assert.That(rb.linearVelocity.magnitude, Is.LessThan(2f),
                $"Object launched with excessive velocity on disable: {rb.linearVelocity.magnitude} m/s ({rb.linearVelocity})");

            // Move player far away to separate
            _playerObject.transform.position += new Vector3(10f, 0f, 0f);
            Physics.SyncTransforms();

            yield return new WaitForFixedUpdate();

            // Once separated, collision must be restored
            Assert.That(Physics.GetIgnoreCollision(playerCol, targetCol), Is.False,
                "Collision should be restored once player and object have separated after disable!");
        }

        [UnityTest]
        public IEnumerator CarriedObject_CompoundCollider_IgnoresTriggersWhenCalculatingCarriedRadius()
        {
            var compoundRoot = new GameObject("CompoundWithTrigger");
            compoundRoot.transform.position = new Vector3(0f, 1.4f, 2f);
            var rb = compoundRoot.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;

            // Solid collider: size 0.4 -> extents 0.2
            var childSolid = new GameObject("SolidPart");
            childSolid.transform.SetParent(compoundRoot.transform, false);
            var solidCol = childSolid.AddComponent<BoxCollider>();
            solidCol.size = new Vector3(0.4f, 0.4f, 0.4f);

            // Trigger collider: 10m detector trigger
            var childTrigger = new GameObject("TriggerPart");
            childTrigger.transform.SetParent(compoundRoot.transform, false);
            var triggerCol = childTrigger.AddComponent<BoxCollider>();
            triggerCol.size = new Vector3(10f, 10f, 10f);
            triggerCol.isTrigger = true;

            var grabbable = compoundRoot.AddComponent<GrabbableObject>();
            _targetObject1 = compoundRoot;

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            // Carried radius should be computed from solid collider (0.2m), ignoring the 10m trigger
            Assert.That(_playerGrab.CarriedRadius, Is.EqualTo(0.2f).Within(0.01f),
                "CarriedRadius must ignore trigger colliders and only use solid collider bounds!");
        }

        [UnityTest]
        public IEnumerator CarriedObject_CollidingWithDynamicRigidbody_DoesNotImpartExcessiveImpulse()
        {
            // Dynamic prop in front of player
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = "DynamicProp";
            prop.transform.position = new Vector3(0f, 1.4f, 1.0f);
            prop.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            var propRb = prop.AddComponent<Rigidbody>();
            propRb.mass = 1f;
            propRb.useGravity = false;
            _obstacleObject = prop;

            _targetObject1 = CreateGrabbable("HeldTarget", new Vector3(0f, 1.4f, 2f)).gameObject;
            _targetObject1.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;

            _playerGrab.ExecuteGrab(grabbable);
            yield return null;

            // Simulate physics for 15 frames while held object is near/touching the dynamic prop
            for (int i = 0; i < 15; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            // Prop should not receive infinite/wild kinematic impulse
            Assert.That(propRb.linearVelocity.magnitude, Is.LessThan(3f),
                $"Dynamic prop launched with excessive velocity: {propRb.linearVelocity.magnitude} m/s ({propRb.linearVelocity})");
        }

        [UnityTest]
        public IEnumerator TwoPlayers_WhenPlayerADisablesWhileOverlapping_AndPlayerBGrabsAndMovesAway_PlayerACollisionRestored()
        {
            var playerB = new GameObject("PlayerB");
            try
            {
                playerB.transform.position = new Vector3(0f, 0f, 0.5f);
                var bCol = playerB.AddComponent<CapsuleCollider>();
                bCol.height = 1.8f;
                bCol.radius = 0.35f;
                bCol.center = new Vector3(0f, 0.9f, 0f);

                var pivot = new GameObject("CameraPivot");
                pivot.transform.SetParent(playerB.transform, false);
                pivot.transform.localPosition = new Vector3(0f, 1.4f, 0f);

                var cameraObject = new GameObject("PlayerCamera");
                cameraObject.transform.SetParent(pivot.transform, false);
                cameraObject.AddComponent<Camera>();

                var bMovement = playerB.AddComponent<PlayerMovement>();
                var playerBGrab = playerB.AddComponent<PlayerGrab>();
                playerBGrab.InteractAction = _interactReference;
                playerBGrab.ThrowAction = _throwReference;
                bMovement.SetLocalPlayer(false);

                _targetObject1 = CreateGrabbable("SharedTarget", new Vector3(0f, 1.4f, 2f)).gameObject;
                var grabbable = _targetObject1.GetComponent<GrabbableObject>();
                var playerACol = _playerObject.GetComponent<Collider>();
                var targetCol = _targetObject1.GetComponent<Collider>();

                Physics.SyncTransforms();
                yield return null;

                // Player A grabs target
                _playerGrab.ExecuteGrab(grabbable);
                yield return null;

                // Deeply overlap target with Player A capsule
                _targetObject1.transform.position = _playerObject.transform.position + new Vector3(0f, 0.9f, 0.1f);
                Physics.SyncTransforms();

                // Player A is disabled while overlapping
                _playerGrab.enabled = false;
                yield return null;

                Assert.That(_playerGrab.IsCarrying, Is.False, "Player A must release object when disabled");
                Assert.That(Physics.GetIgnoreCollision(playerACol, targetCol), Is.True,
                    "Player A must ignore collision while overlapping after disable!");

                // Player B grabs the object while it still overlaps Player A
                bool grabBSuccess = playerBGrab.ExecuteGrab(grabbable);
                yield return null;

                Assert.That(grabBSuccess, Is.True, "Player B must successfully grab the object");
                Assert.That(playerBGrab.IsCarrying, Is.True);
                Assert.That(grabbable.IsHeld, Is.True);
                Assert.That(grabbable.CurrentHolder, Is.EqualTo(playerB));

                // Player B carries object away from Player A
                playerB.transform.position += new Vector3(10f, 0f, 0f);
                _targetObject1.transform.position += new Vector3(10f, 0f, 0f);
                Physics.SyncTransforms();

                yield return new WaitForFixedUpdate();

                // Player A collision with object must be restored once separated, even though Player B re-grabbed it!
                Assert.That(Physics.GetIgnoreCollision(playerACol, targetCol), Is.False,
                    "Player A's collision with object must be restored once separated after Player B re-grabs it!");

                // Player B must still have collision ignored with the object it is carrying
                Assert.That(Physics.GetIgnoreCollision(bCol, targetCol), Is.True,
                    "Player B must maintain ignored collision while carrying the object!");
            }
            finally
            {
                if (playerB != null)
                    Object.DestroyImmediate(playerB);
            }
        }

        [Test]
        public void RequestGrab_WithGrabRequestHandler_InvokesHandlerAndReturnsResult()
        {
            _targetObject1 = CreateGrabbable("TargetForHandler", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            bool handlerCalled = false;
            GrabbableObject passedTarget = null;
            _playerGrab.GrabRequestHandler = target =>
            {
                handlerCalled = true;
                passedTarget = target;
                return true;
            };

            bool result = _playerGrab.RequestGrab(grabbable);

            Assert.That(handlerCalled, Is.True, "GrabRequestHandler must be invoked when assigned.");
            Assert.That(passedTarget, Is.SameAs(grabbable), "Target must match passed GrabbableObject.");
            Assert.That(result, Is.True, "Result must match handler return value.");
            Assert.That(_playerGrab.IsCarrying, Is.False, "Local ExecuteGrab must not be called when handler intercepts.");
        }

        [Test]
        public void RequestDrop_WithDropRequestHandler_InvokesHandlerAndReturnsResult()
        {
            bool handlerCalled = false;
            _playerGrab.DropRequestHandler = () =>
            {
                handlerCalled = true;
                return true;
            };

            bool result = _playerGrab.RequestDrop();

            Assert.That(handlerCalled, Is.True, "DropRequestHandler must be invoked when assigned.");
            Assert.That(result, Is.True, "Result must match handler return value.");
        }

        [Test]
        public void RequestGrab_WithoutGrabRequestHandler_ExecutesDirectly()
        {
            _targetObject1 = CreateGrabbable("TargetDirect", new Vector3(0f, 1.4f, 2f)).gameObject;
            var grabbable = _targetObject1.GetComponent<GrabbableObject>();

            _playerGrab.GrabRequestHandler = null;
            bool result = _playerGrab.RequestGrab(grabbable);

            Assert.That(result, Is.True, "Direct RequestGrab must succeed.");
            Assert.That(_playerGrab.IsCarrying, Is.True, "Local ExecuteGrab must be executed.");
        }
    }
}
