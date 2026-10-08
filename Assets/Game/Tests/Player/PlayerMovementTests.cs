using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ngecor.Player.Tests
{
    public class PlayerMovementTests : InputTestFixture
    {
        private static readonly Vector3 TestOrigin = new Vector3(1000f, 0f, 0f);
        private GameObject _player;
        private GameObject _ground;
        private GameObject _wall;
        private GameObject _ramp;
        private GameObject _pushTarget;
        private GameObject _stepProbeIgnoredColliders;
        private Transform _cameraPivot;
        private Camera _camera;
        private InputActionAsset _actionAsset;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputActionReference _moveReference;
        private InputActionReference _lookReference;

        public override void TearDown()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_player != null)
                UnityEngine.Object.DestroyImmediate(_player);

            if (_ground != null)
                UnityEngine.Object.DestroyImmediate(_ground);

            if (_wall != null)
                UnityEngine.Object.DestroyImmediate(_wall);

            if (_ramp != null)
                UnityEngine.Object.DestroyImmediate(_ramp);

            if (_pushTarget != null)
                UnityEngine.Object.DestroyImmediate(_pushTarget);

            if (_stepProbeIgnoredColliders != null)
                UnityEngine.Object.DestroyImmediate(_stepProbeIgnoredColliders);

            if (_moveReference != null)
                UnityEngine.Object.DestroyImmediate(_moveReference);

            if (_lookReference != null)
                UnityEngine.Object.DestroyImmediate(_lookReference);

            if (_actionAsset != null)
                UnityEngine.Object.DestroyImmediate(_actionAsset);

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ForwardInputMovesRelativeToPlayerYaw()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            _player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(10);

            Assert.That(_moveAction.ReadValue<Vector2>().y, Is.GreaterThan(0f), "Move action did not receive the W key.");
            Assert.That(_player.transform.position.x, Is.GreaterThan(TestOrigin.x + 0.01f));
            Assert.That(Mathf.Abs(_player.transform.position.z), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator LookInputRotatesPlayerAndCamera()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            CreatePlayer();

            Set(mouse.delta, new Vector2(20f, 10f));
            yield return null;

            Assert.That(_lookAction.ReadValue<Vector2>().sqrMagnitude, Is.GreaterThan(0f), "Look action did not receive mouse delta.");
            Assert.That(Mathf.Abs(_player.transform.eulerAngles.y), Is.GreaterThan(0.1f));
            Assert.That(Mathf.DeltaAngle(0f, _cameraPivot.localEulerAngles.x), Is.LessThan(-0.1f));
        }

        [UnityTest]
        public IEnumerator LocalPlayerCanReleaseAndRelockCursor()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            CreatePlayer();

            yield return null;
            Assert.That(GetPrivateField(typeof(Ngecor.Player.PlayerMovement),
                _player.GetComponent<Ngecor.Player.PlayerMovement>(), "_cursorLocked"), Is.True);
            Assert.That(Cursor.visible, Is.False);

            Press(keyboard.escapeKey);
            yield return null;
            Assert.That(GetPrivateField(typeof(Ngecor.Player.PlayerMovement),
                _player.GetComponent<Ngecor.Player.PlayerMovement>(), "_cursorLocked"), Is.False);
            Assert.That(Cursor.visible, Is.True);

            Release(keyboard.escapeKey);
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            Assert.That(GetPrivateField(typeof(Ngecor.Player.PlayerMovement),
                _player.GetComponent<Ngecor.Player.PlayerMovement>(), "_cursorLocked"), Is.True);
            Assert.That(Cursor.visible, Is.False);
        }

        [UnityTest]
        public IEnumerator CursorRelock_SetsCursorRelockedThisFrame_ForSingleFrame()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            CreatePlayer();
            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();

            yield return null;
            Assert.That(movement.IsCursorLocked, Is.True);
            Assert.That(movement.CursorRelockedThisFrame, Is.False);

            Press(keyboard.escapeKey);
            yield return null;
            Assert.That(movement.IsCursorLocked, Is.False);
            Assert.That(movement.CursorRelockedThisFrame, Is.False);

            Release(keyboard.escapeKey);
            yield return null;

            Press(mouse.leftButton);
            yield return null;

            Assert.That(movement.IsCursorLocked, Is.True);
            Assert.That(movement.CursorRelockedThisFrame, Is.True);

            yield return null;
            Assert.That(movement.IsCursorLocked, Is.True);
            Assert.That(movement.CursorRelockedThisFrame, Is.False);
        }

        [UnityTest]
        public IEnumerator CursorRelock_BecomesFalseImmediatelyOnNextFrame_BeforeUpdate()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            CreatePlayer();
            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();

            yield return null;
            Press(keyboard.escapeKey);
            yield return null;
            Release(keyboard.escapeKey);
            yield return null;

            // Relock on frame N
            Press(mouse.leftButton);
            yield return null;
            Assert.That(movement.IsCursorLocked, Is.True);
            Assert.That(movement.CursorRelockedThisFrame, Is.True);
            Release(mouse.leftButton);

            // Disable component so Update does not run on frame N+1
            movement.enabled = false;
            yield return null;

            // On frame N+1, CursorRelockedThisFrame must be false immediately even before Update runs
            Assert.That(movement.CursorRelockedThisFrame, Is.False,
                "CursorRelockedThisFrame must be false on subsequent frame even before Update runs.");
        }

        [UnityTest]
        public IEnumerator LocalPlayerCanReleaseCursorWithoutMoveAction()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            SetPrivateField(typeof(Ngecor.Player.PlayerMovement),
                _player.GetComponent<Ngecor.Player.PlayerMovement>(), "_moveAction", null);

            yield return null;
            Press(keyboard.escapeKey);
            yield return null;

            Assert.That(GetPrivateField(typeof(Ngecor.Player.PlayerMovement),
                _player.GetComponent<Ngecor.Player.PlayerMovement>(), "_cursorLocked"), Is.False);
        }

        [UnityTest]
        public IEnumerator LookInputIsIgnoredWhileCursorIsReleased()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            CreatePlayer();
            yield return null;

            Press(keyboard.escapeKey);
            yield return null;
            var yaw = _player.transform.rotation;
            var pitch = _cameraPivot.localRotation;
            Set(mouse.delta, new Vector2(25f, 20f));
            yield return null;

            Assert.That(Quaternion.Angle(yaw, _player.transform.rotation), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(pitch, _cameraPivot.localRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void LocalCameraIsAvailableOnlyForLocalPlayer()
        {
            CreatePlayer(false);
            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();

            Assert.That(movement.LocalCamera, Is.Null);

            movement.SetLocalPlayer(true);
            Assert.That(movement.LocalCamera, Is.SameAs(_camera));
        }

        [UnityTest]
        public IEnumerator FourWayInputMovesInEachCardinalDirection()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(8);
            Assert.That(_player.transform.position.z, Is.GreaterThan(0.01f));
            Release(keyboard.wKey);
            yield return WaitForFixedFrames(1);

            ResetPlayer();
            Press(keyboard.sKey);
            yield return WaitForFixedFrames(8);
            Assert.That(_player.transform.position.z, Is.LessThan(-0.01f));
            Release(keyboard.sKey);
            yield return WaitForFixedFrames(1);

            ResetPlayer();
            Press(keyboard.aKey);
            yield return WaitForFixedFrames(8);
            Assert.That(_player.transform.position.x, Is.LessThan(TestOrigin.x - 0.01f));
            Release(keyboard.aKey);
            yield return WaitForFixedFrames(1);

            ResetPlayer();
            Press(keyboard.dKey);
            yield return WaitForFixedFrames(8);
            Assert.That(_player.transform.position.x, Is.GreaterThan(TestOrigin.x + 0.01f));
        }

        [UnityTest]
        public IEnumerator NonLocalPlayerDoesNotMoveFromInput()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer(null);

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(10);

            Assert.That(_moveAction.ReadValue<Vector2>().y, Is.GreaterThan(0f), "The project-wide Move action should remain enabled for all players.");
            Assert.That(_player.transform.position.z, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator LocalOwnerCanMoveAfterInstantiation()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer(null);

            _player.GetComponent<Ngecor.Player.PlayerMovement>().SetLocalPlayer(true);
            Press(keyboard.wKey);
            yield return WaitForFixedFrames(8);

            Assert.That(_player.transform.position.z, Is.GreaterThan(0.01f));
        }

        [UnityTest]
        public IEnumerator CharacterControllerStopsAtWall()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            _wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _wall.transform.position = TestOrigin + new Vector3(0f, 1.5f, 1.5f);
            _wall.transform.localScale = new Vector3(4f, 3f, 1f);
            var wallPosition = _wall.transform.position;

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(60);

            Assert.That(_player.transform.position.z, Is.LessThan(1f));
            Assert.That(_wall.transform.position, Is.EqualTo(wallPosition));
        }

        [UnityTest]
        public IEnumerator ContactPushesDynamicRigidbodyWithBoundedSpeed()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var body = CreatePushTarget(size: 1.5f);
            var startingPosition = body.position;

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(30);

            Assert.That(body.position.z, Is.GreaterThan(startingPosition.z + 0.1f));
            Assert.That(body.linearVelocity.magnitude, Is.LessThanOrEqualTo(5.05f));
        }

        [UnityTest]
        public IEnumerator DisabledMovementClearsDirectionAndRejectsWheelbarrowPush()
        {
            CreatePlayer();
            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();
            var body = CreatePushTarget(size: 0.5f);
            body.useGravity = false;

            SetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_movementDirection", Vector3.forward);
            SetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_remainingPushImpulse", 100f);
            movement.enabled = false;

            Assert.That(GetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_movementDirection"),
                Is.EqualTo(Vector3.zero), "Disabling movement must clear the last input direction.");

            SetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_movementDirection", Vector3.forward);
            movement.ApplyMovementPush(body, body.worldCenterOfMass, Vector3.forward, 1f, 1f / 30f);
            yield return WaitForFixedFrames(1);

            Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero),
                "A disabled movement component must not push through a stale movement direction.");
        }

        [UnityTest]
        public IEnumerator ContactPushMoves25KgBodyMoreSlowlyThanPlayer()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var body = CreatePushTarget(size: 1.5f);
            body.mass = 25f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            var startingPosition = body.position;
            var maxSpeed = 0f;

            Press(keyboard.wKey);
            var previousCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = 60;
            try
            {
                for (var i = 0; i < 120; i++)
                {
                    yield return null;
                    maxSpeed = Mathf.Max(maxSpeed, body.linearVelocity.z);
                }
            }
            finally
            {
                Time.captureFramerate = previousCaptureFramerate;
            }

            Assert.That(body.position.z, Is.GreaterThan(startingPosition.z + 0.05f),
                "A sustained push should produce measurable movement without requiring a heavy body to move quickly.");
            Assert.That(maxSpeed, Is.LessThanOrEqualTo(2.3f),
                "A 25 kg body should stay well below the player's 5 m/s walking speed.");
        }

        [UnityTest]
        public IEnumerator PlayerPushesLowDynamicRigidbodyInsteadOfSteppingOntoIt()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var body = CreatePushTarget(size: 0.18f);
            body.mass = 25f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            Physics.SyncTransforms();
            var startingPosition = body.position;

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(120);

            Assert.That(_player.transform.position.y, Is.LessThan(0.1f),
                "The player should stay on the ground instead of stepping onto the low sack.");
            Assert.That(body.position.z, Is.GreaterThan(startingPosition.z + 0.05f),
                "The low sack should be pushed forward when it blocks the player's path.");
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator CementBagSlidesSlowlyWithoutBeingSteppedOver()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Material/CementBag.prefab");
            Assert.That(prefab, Is.Not.Null);
            var failures = new List<string>();

            var orientations = new (string name, Quaternion rotation, float centerY, float offsetX,
                float maximumPlayerY, float minimumDistance)[]
            {
                ("flat", Quaternion.identity, 0.09f, 0f, 0.15f, 0.5f),
                ("side", Quaternion.Euler(90f, 0f, 0f), 0.225f, 0f, 0.45f, 0.25f),
                ("side-corner", Quaternion.Euler(90f, 0f, 0f), 0.225f, 0.5f, 0.15f, 0.25f)
            };
            var previousCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = 60;
            try
            {
                foreach (var orientation in orientations)
                {
                    for (var repeat = 0; repeat < 5; repeat++)
                    {
                        Release(keyboard.wKey);
                        ResetPlayer();
                        yield return WaitForFixedFrames(3, 60);

                        _player.transform.rotation = Quaternion.identity;
                        var startPosition = TestOrigin + new Vector3(orientation.offsetX, orientation.centerY, 1.2f);
                        _pushTarget = UnityEngine.Object.Instantiate(prefab, startPosition, orientation.rotation);
                        var body = _pushTarget.GetComponent<Rigidbody>();
                        var initialPosition = body.position;
                        var maxPlayerHeight = _player.transform.position.y;
                        var maximumLinearSpeed = 0f;

                        Physics.SyncTransforms();
                        Press(keyboard.wKey);
                        for (var frame = 0; frame < 180; frame++)
                        {
                            yield return null;
                            maxPlayerHeight = Mathf.Max(maxPlayerHeight, _player.transform.position.y);
                            maximumLinearSpeed = Mathf.Max(maximumLinearSpeed, body.linearVelocity.magnitude);
                        }

                        var planarDistance = Vector2.Distance(
                            new Vector2(initialPosition.x, initialPosition.z),
                            new Vector2(body.position.x, body.position.z));
                        Release(keyboard.wKey);
                        var outcome = $"{orientation.name} {repeat + 1}: {planarDistance:F3} m, " +
                                      $"player height {maxPlayerHeight:F3} m, speed {maximumLinearSpeed:F3} m/s";
                        TestContext.WriteLine(outcome);
                        if (planarDistance <= orientation.minimumDistance)
                            failures.Add($"{outcome}; expected > {orientation.minimumDistance:F2} m");
                        if (maxPlayerHeight > orientation.maximumPlayerY)
                            failures.Add($"{outcome}; player stepped onto the bag");
                        if (maximumLinearSpeed > 2.3f)
                            failures.Add($"{outcome}; speed exceeded 2.3 m/s");

                        UnityEngine.Object.DestroyImmediate(_pushTarget);
                        _pushTarget = null;
                        Physics.SyncTransforms();
                    }
                }
            }
            finally
            {
                Time.captureFramerate = previousCaptureFramerate;
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }
#endif

        [UnityTest]
        public IEnumerator ContactPushAppliesAtMostOneImpulseToABodyPerMovementStep()
        {
            CreatePlayer();
            var body = CreatePushTarget(size: 0.5f);
            body.mass = 1f;
            body.useGravity = false;
            body.position = TestOrigin + new Vector3(0f, 10f, 1.2f);
            body.linearVelocity = Vector3.forward * 4f;
            Physics.SyncTransforms();

            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();
            SetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_remainingPushImpulse", 10f);
            var pushDeltaTime = 1f / 30f;
            var contactPoint = body.worldCenterOfMass + Vector3.up;
            for (var i = 0; i < 9; i++)
            {
                InvokePrivateMethod(movement, "ApplyContactPush", body, contactPoint,
                    Vector3.forward, pushDeltaTime, 1f);
            }

            yield return WaitForFixedFrames(1, 30);

            var expectedSingleContactSpeed = 4f + Ngecor.Player.PlayerMovement.CalculatePushForce(
                body.mass,
                Ngecor.Player.PlayerMovement.CalculatePushTargetSpeed(body.mass, 5f),
                4f,
                pushDeltaTime,
                0.1f,
                300f) * pushDeltaTime / body.mass;
            Assert.That(body.linearVelocity.z, Is.EqualTo(expectedSingleContactSpeed).Within(0.01f),
                "Repeated callbacks for one body must not queue repeated stale-velocity responses.");
            Assert.That(body.angularVelocity.magnitude, Is.LessThan(0.01f),
                "Contact height must not add torque that presses the body into the floor.");
            Assert.That(body.linearVelocity.z, Is.LessThanOrEqualTo(5.05f));
        }

        [UnityTest]
        public IEnumerator StepProbeDoesNotBlockForAnIgnoredColliderPair()
        {
            CreatePlayer();
            var body = CreatePushTarget(size: 0.18f);
            var controller = _player.GetComponent<CharacterController>();
            var collider = body.GetComponent<Collider>();
            Physics.IgnoreCollision(controller, collider, true);
            Physics.SyncTransforms();
            yield return WaitForFixedFrames(1);

            Assert.That(Physics.GetIgnoreCollision(controller, collider), Is.True);
            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();
            Assert.That(InvokePrivateMethod(movement, "ShouldBlockStepOverDynamicBody",
                Vector3.forward, 1f), Is.False,
                "The step probe should skip a low dynamic collider excluded from the player's collisions.");
        }

        [UnityTest]
        public IEnumerator StepProbeRetriesWhenIrrelevantCollidersFillHitBuffer()
        {
            const int irrelevantColliderCount = 12;
            CreatePlayer();
            var body = CreatePushTarget(size: 0.18f);
            var controller = _player.GetComponent<CharacterController>();
            _stepProbeIgnoredColliders = new GameObject("StepProbeIrrelevantColliders");

            for (var i = 0; i < irrelevantColliderCount; i++)
            {
                var irrelevantObject = new GameObject($"StepProbeIrrelevant{i}");
                irrelevantObject.transform.SetParent(_stepProbeIgnoredColliders.transform);
                irrelevantObject.transform.position = TestOrigin + new Vector3(
                    ((i % 3) - 1) * 0.15f,
                    0.1f + (i / 3) * 0.1f,
                    0.65f);
                var collider = irrelevantObject.AddComponent<BoxCollider>();
                collider.size = Vector3.one * 0.08f;
            }

            Physics.SyncTransforms();
            yield return WaitForFixedFrames(1);

            Assert.That(controller.isGrounded, Is.True,
                "The dynamic-body step probe is only active while the player is grounded.");

            var capsuleCenter = _player.transform.TransformPoint(controller.center) +
                                Vector3.up * 0.02f;
            var verticalSegment = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
            var lowerSphereCenter = capsuleCenter - Vector3.up * verticalSegment;
            var upperSphereCenter = capsuleCenter + Vector3.up * verticalSegment;
            var allHits = Physics.CapsuleCastAll(
                lowerSphereCenter,
                upperSphereCenter,
                controller.radius,
                Vector3.forward,
                1f + controller.skinWidth,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            Assert.That(allHits.Length, Is.GreaterThan(8),
                "The test setup must actually saturate the probe's original eight-hit buffer.");
            Assert.That(Array.Exists(allHits, hit => hit.collider == body.GetComponent<Collider>()), Is.True,
                "The same query must include the valid dynamic blocker behind the irrelevant hits.");

            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();
            Assert.That(InvokePrivateMethod(movement, "ShouldBlockStepOverDynamicBody",
                Vector3.forward, 1f), Is.True,
                "The probe should retry a saturated query so a valid dynamic blocker is not hidden by irrelevant colliders.");

            var cachedHits = (RaycastHit[])GetPrivateField(typeof(Ngecor.Player.PlayerMovement),
                movement, "_stepProbeHits");
            Assert.That(cachedHits.Length, Is.GreaterThan(8),
                "The cached result buffer should grow only after the original capacity is exhausted.");
            Assert.That(InvokePrivateMethod(movement, "ShouldBlockStepOverDynamicBody",
                Vector3.forward, 1f), Is.True);
            Assert.That(GetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_stepProbeHits"),
                Is.SameAs(cachedHits),
                "The grown result buffer should be reused once it has enough capacity.");
        }

        [UnityTest]
        public IEnumerator StepProbeDoesNotBlockForAnIgnoredLayerPair()
        {
            const int probeTestLayer = 31;
            var wasIgnored = Physics.GetIgnoreLayerCollision(0, probeTestLayer);
            try
            {
                Physics.IgnoreLayerCollision(0, probeTestLayer, true);
                CreatePlayer();
                var body = CreatePushTarget(size: 0.18f);
                body.gameObject.layer = probeTestLayer;
                Physics.SyncTransforms();
                yield return WaitForFixedFrames(1);

                Assert.That(Physics.GetIgnoreLayerCollision(0, probeTestLayer), Is.True);
                var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();
                Assert.That(InvokePrivateMethod(movement, "ShouldBlockStepOverDynamicBody",
                    Vector3.forward, 1f), Is.False,
                    "The step probe should skip layers excluded by the project collision matrix.");
            }
            finally
            {
                Physics.IgnoreLayerCollision(0, probeTestLayer, wasIgnored);
            }
        }

        [UnityTest]
        public IEnumerator StepProbeIncludesPhysicallyCollidableIgnoreRaycastObjects()
        {
            const int ignoreRaycastLayer = 2;
            var wasIgnored = Physics.GetIgnoreLayerCollision(0, ignoreRaycastLayer);
            try
            {
                Physics.IgnoreLayerCollision(0, ignoreRaycastLayer, false);
                CreatePlayer();
                var body = CreatePushTarget(size: 0.18f);
                body.gameObject.layer = ignoreRaycastLayer;
                Physics.SyncTransforms();
                yield return WaitForFixedFrames(1);

                var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();
                Assert.That(InvokePrivateMethod(movement, "ShouldBlockStepOverDynamicBody",
                    Vector3.forward, 1f), Is.True,
                    "Ignore Raycast excludes ray queries by default, but does not exclude physical collision.");
            }
            finally
            {
                Physics.IgnoreLayerCollision(0, ignoreRaycastLayer, wasIgnored);
            }
        }

        [UnityTest]
        public IEnumerator CharacterControllerCanStillStepOverAStaticBlock()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            _wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _wall.transform.position = TestOrigin + new Vector3(0f, 0.15f, 1.2f);
            _wall.transform.localScale = new Vector3(2f, 0.3f, 0.4f);
            Physics.SyncTransforms();

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(60);

            Assert.That(_player.transform.position.z, Is.GreaterThan(1.8f),
                "A low static step should remain traversable with the normal CharacterController step offset.");
        }

        [UnityTest]
        public IEnumerator ContactPushDoesNotAccelerate1KgBodyPastWalkingSpeed()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var body = CreatePushTarget(size: 1.5f);
            body.constraints = RigidbodyConstraints.FreezeRotation;
            var startingPosition = body.position;
            var maxSpeed = 0f;

            Press(keyboard.wKey);
            var previousCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = 60;
            try
            {
                for (var i = 0; i < 120; i++)
                {
                    yield return null;
                    maxSpeed = Mathf.Max(maxSpeed, body.linearVelocity.z);
                }
            }
            finally
            {
                Time.captureFramerate = previousCaptureFramerate;
            }

            Assert.That(body.position.z, Is.GreaterThan(startingPosition.z + 0.1f),
                "A light body should move under a sustained player push.");
            Assert.That(maxSpeed, Is.LessThanOrEqualTo(5.05f),
                "A light body must not outrun the player's 5 m/s walking speed.");
        }

        [Test]
        public void PushForceScalesWithMassAndRespectsTheHumanForceLimit()
        {
            var lightForce = Ngecor.Player.PlayerMovement.CalculatePushForce(1f, 5f, 0f, 0.02f, 0.2f, 300f);
            var heavyForce = Ngecor.Player.PlayerMovement.CalculatePushForce(25f, 5f, 0f, 0.02f, 0.2f, 300f);
            var nearlyAtTargetForce = Ngecor.Player.PlayerMovement.CalculatePushForce(1f, 5f, 4.9f, 0.02f, 0.2f, 300f);
            var atTargetForce = Ngecor.Player.PlayerMovement.CalculatePushForce(1f, 5f, 5f, 0.02f, 0.2f, 300f);

            Assert.That(lightForce, Is.EqualTo(23.79f).Within(0.02f));
            Assert.That(heavyForce, Is.EqualTo(300f));
            Assert.That(nearlyAtTargetForce, Is.LessThan(lightForce));
            Assert.That(atTargetForce, Is.EqualTo(0f));
        }

        [Test]
        public void PushTargetSpeedDecreasesWithMassAndNeverExceedsWalkingSpeed()
        {
            var oneKgTarget = Ngecor.Player.PlayerMovement.CalculatePushTargetSpeed(1f, 5f);
            var fiveKgTarget = Ngecor.Player.PlayerMovement.CalculatePushTargetSpeed(5f, 5f);
            var twentyFiveKgTarget = Ngecor.Player.PlayerMovement.CalculatePushTargetSpeed(25f, 5f);

            Assert.That(oneKgTarget, Is.EqualTo(5f));
            Assert.That(fiveKgTarget, Is.EqualTo(5f));
            Assert.That(twentyFiveKgTarget, Is.EqualTo(5f * Mathf.Sqrt(0.2f)).Within(0.001f));
            Assert.That(twentyFiveKgTarget, Is.LessThan(fiveKgTarget));
        }

        [UnityTest]
        public IEnumerator ContactPushHasSimilarDisplacementAtDifferentFrameRates()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var body = CreatePushTarget(size: 1.5f);
            body.mass = 5f;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            var startingPosition = body.position;

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(30, 30);
            var displacementAt30Fps = body.position.z - startingPosition.z;

            Release(keyboard.wKey);
            yield return null;
            ResetPlayer();
            body.position = startingPosition;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(120, 120);
            var displacementAt120Fps = body.position.z - startingPosition.z;

            Assert.That(displacementAt30Fps, Is.GreaterThan(0.1f));
            Assert.That(displacementAt120Fps, Is.GreaterThan(0.1f));
            var averageDisplacement = (displacementAt30Fps + displacementAt120Fps) * 0.5f;
            Assert.That(Mathf.Abs(displacementAt30Fps - displacementAt120Fps) / averageDisplacement,
                Is.LessThan(0.2f),
                $"Push displacement differed at 30 FPS ({displacementAt30Fps:F3} m) and 120 FPS ({displacementAt120Fps:F3} m).");
        }

        [UnityTest]
        public IEnumerator ContactDoesNotMoveKinematicRigidbody()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var body = CreatePushTarget(true, 1.5f);
            var startingPosition = body.position;

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(30);

            Assert.That(body.position, Is.EqualTo(startingPosition));
        }

        [UnityTest]
        public IEnumerator ContactPushesHorizontally()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var body = CreatePushTarget(size: 1.5f);
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(30);

            Assert.That(body.linearVelocity.z, Is.GreaterThan(0f));
            Assert.That(body.linearVelocity.y, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator WalkingOnDynamicRigidbodyDoesNotPushIt()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var body = CreatePushTarget(size: 1f, position: TestOrigin + new Vector3(0f, 0.5f, 0f));
            _player.transform.position = TestOrigin + Vector3.up;
            Physics.SyncTransforms();
            var startingPosition = body.position;

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(6);

            Assert.That(Vector2.Distance(
                new Vector2(body.position.x, body.position.z),
                new Vector2(startingPosition.x, startingPosition.z)), Is.LessThan(0.01f));
            Assert.That(new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude, Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator CharacterControllerClimbsSimpleRampWithoutFallingThrough()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            _ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ramp.transform.position = TestOrigin + new Vector3(0f, 1.45f, 5f);
            _ramp.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            _ramp.transform.localScale = new Vector3(4f, 0.3f, 8f);
            var controller = _player.GetComponent<CharacterController>();
            controller.enabled = false;
            _player.transform.position = TestOrigin + new Vector3(0f, 0f, 12f);
            controller.enabled = true;

            Press(keyboard.sKey);
            yield return WaitForFixedFrames(100);

            Assert.That(_player.transform.position.z, Is.LessThan(8f), $"Player stopped at {_player.transform.position}; Move={_moveAction.ReadValue<Vector2>()}; grounded={_player.GetComponent<CharacterController>().isGrounded}.");
            Assert.That(_player.transform.position.y, Is.GreaterThan(0.25f), "Player fell through or failed to climb the ramp.");
        }

        [UnityTest]
        public IEnumerator DiagonalInputDoesNotExceedCardinalSpeed()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(10);

            var cardinalOffset = _player.transform.position - TestOrigin;
            var cardinalDistance = new Vector2(cardinalOffset.x, cardinalOffset.z).magnitude;
            Release(keyboard.wKey);
            yield return null;

            var controller = _player.GetComponent<CharacterController>();
            controller.enabled = false;
            _player.transform.position = TestOrigin;
            _player.transform.rotation = Quaternion.identity;
            controller.enabled = true;

            Press(keyboard.wKey);
            Press(keyboard.dKey);
            yield return WaitForFixedFrames(10);

            var diagonalOffset = _player.transform.position - TestOrigin;
            var diagonalDistance = new Vector2(diagonalOffset.x, diagonalOffset.z).magnitude;
            Assert.That(diagonalDistance, Is.LessThanOrEqualTo(cardinalDistance + 0.05f));
        }

        [UnityTest]
        public IEnumerator CarriedMass_When25kg_ReducesEffectiveSpeed()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();

            // Baseline run: 0 kg mass
            movement.CarriedMass = 0f;
            Press(keyboard.wKey);
            yield return WaitForFixedFrames(10);
            float distanceBaseline = _player.transform.position.z - TestOrigin.z;
            Release(keyboard.wKey);

            ResetPlayer();

            // Heavy run: 25 kg mass (should be ~70% speed)
            movement.CarriedMass = 25f;
            Press(keyboard.wKey);
            yield return WaitForFixedFrames(10);
            float distanceHeavy = _player.transform.position.z - TestOrigin.z;
            Release(keyboard.wKey);

            Assert.That(distanceBaseline, Is.GreaterThan(0.5f));
            Assert.That(distanceHeavy, Is.LessThan(distanceBaseline * 0.75f));
            Assert.That(distanceHeavy, Is.GreaterThan(distanceBaseline * 0.65f));
        }

        [UnityTest]
        public IEnumerator CarriedMass_ClampedAtMaximumPenalty()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreatePlayer();
            var movement = _player.GetComponent<Ngecor.Player.PlayerMovement>();

            movement.CarriedMass = 0f;
            Press(keyboard.wKey);
            yield return WaitForFixedFrames(10);
            float distanceBaseline = _player.transform.position.z - TestOrigin.z;
            Release(keyboard.wKey);

            ResetPlayer();

            // Excessive mass: 200 kg should clamp at 0.6x speed
            movement.CarriedMass = 200f;
            Press(keyboard.wKey);
            yield return WaitForFixedFrames(10);
            float distanceCapped = _player.transform.position.z - TestOrigin.z;
            Release(keyboard.wKey);

            Assert.That(distanceCapped, Is.GreaterThan(distanceBaseline * 0.55f));
            Assert.That(distanceCapped, Is.LessThan(distanceBaseline * 0.65f));
        }

        private void CreatePlayer(bool? isLocalPlayer = true)
        {
            _actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            var playerMap = new InputActionMap("Player");
            _actionAsset.AddActionMap(playerMap);
            _moveAction = playerMap.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _lookAction = playerMap.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            _lookAction.AddBinding("<Mouse>/delta");
            playerMap.Enable();
            _moveReference = InputActionReference.Create(_moveAction);
            _lookReference = InputActionReference.Create(_lookAction);

            _player = new GameObject("PlayerMovementTest");
            _player.transform.position = TestOrigin;
            _player.SetActive(false);
            _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ground.transform.position = TestOrigin + new Vector3(0f, -0.5f, 0f);
            _ground.transform.localScale = new Vector3(100f, 1f, 100f);
            var controller = _player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.stepOffset = 0.45f;
            controller.center = Vector3.up * 0.9f;
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(_player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            _cameraPivot = pivot.transform;
            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(_cameraPivot, false);
            _camera = cameraObject.AddComponent<Camera>();

            var movement = _player.AddComponent<Ngecor.Player.PlayerMovement>();
#if UNITY_EDITOR
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player/Player.prefab");
            var prefabMovement = playerPrefab != null ? playerPrefab.GetComponent<Ngecor.Player.PlayerMovement>() : null;
            var maxPushForceField = typeof(Ngecor.Player.PlayerMovement).GetField(
                "_maxPushForce", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(prefabMovement, Is.Not.Null, "Player prefab must configure contact pushing.");
            Assert.That(maxPushForceField, Is.Not.Null);
            SetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_maxPushForce",
                maxPushForceField.GetValue(prefabMovement));
#endif
            SetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_moveAction", _moveReference);
            SetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_lookAction", _lookReference);
            SetPrivateField(typeof(Ngecor.Player.PlayerMovement), movement, "_cameraPivot", _cameraPivot);
            if (isLocalPlayer.HasValue)
                ((Ngecor.Player.PlayerMovement)movement).SetLocalPlayer(isLocalPlayer.Value);
            _player.SetActive(true);
        }

        private void ResetPlayer()
        {
            var controller = _player.GetComponent<CharacterController>();
            controller.enabled = false;
            _player.transform.position = TestOrigin;
            _player.transform.rotation = Quaternion.identity;
            controller.enabled = true;
        }

        private Rigidbody CreatePushTarget(bool isKinematic = false, float size = 0.5f, Vector3? position = null)
        {
            _pushTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _pushTarget.transform.localScale = Vector3.one * size;
            _pushTarget.transform.position = position ?? TestOrigin + new Vector3(0f, size * 0.5f, 1.2f);
            var body = _pushTarget.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.isKinematic = isKinematic;
            return body;
        }

        private static void SetPrivateField(Type type, object target, string name, object value)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing {type.Name} field '{name}'.");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(Type type, object target, string name)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing {type.Name} field '{name}'.");
            return field.GetValue(target);
        }

        private static object InvokePrivateMethod(object target, string name, params object[] arguments)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing {target.GetType().Name} method '{name}'.");
            return method.Invoke(target, arguments);
        }

        private static IEnumerator WaitForFixedFrames(int frameCount, int framerate = 60)
        {
            var previousCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = framerate;
            try
            {
                for (var i = 0; i < frameCount; i++)
                    yield return null;
            }
            finally
            {
                Time.captureFramerate = previousCaptureFramerate;
            }
        }
    }
}
