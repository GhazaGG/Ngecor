using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

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
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(3f));
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
