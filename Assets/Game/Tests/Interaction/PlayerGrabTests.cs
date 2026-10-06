using System.Collections;
using System.Reflection;
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
        private InputAction _throwAction;
        private InputActionReference _throwReference;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            var playerMap = new InputActionMap("Player");
            _actionAsset.AddActionMap(playerMap);
            _interactAction = playerMap.AddAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _throwAction = playerMap.AddAction("Attack", InputActionType.Button);
            _throwAction.AddBinding("<Mouse>/leftButton");
            playerMap.Enable();
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
            if (_throwReference != null)
                Object.DestroyImmediate(_throwReference);
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
        public IEnumerator CarriedObject_PushedTooCloseAgainstPlayer_TriggersAutoDrop()
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

            // LateUpdate should have detected the pinch and dropped the object
            Assert.That(_playerGrab.IsCarrying, Is.False, "Object pinched between player and wall should auto-drop!");
            Assert.That(grabbable.IsHeld, Is.False);
            Assert.That(grabbable.Rigidbody.isKinematic, Is.False);
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
    }
}
