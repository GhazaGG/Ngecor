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
        private Transform _cameraPivot;
        private InputActionAsset _actionAsset;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputActionReference _moveReference;
        private InputActionReference _lookReference;

        public override void TearDown()
        {
            if (_player != null)
                UnityEngine.Object.DestroyImmediate(_player);

            if (_ground != null)
                UnityEngine.Object.DestroyImmediate(_ground);

            if (_wall != null)
                UnityEngine.Object.DestroyImmediate(_wall);

            if (_ramp != null)
                UnityEngine.Object.DestroyImmediate(_ramp);

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

            Press(keyboard.wKey);
            yield return WaitForFixedFrames(60);

            Assert.That(_player.transform.position.z, Is.LessThan(1f));
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

        private static void SetPrivateField(Type type, object target, string name, object value)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing {type.Name} field '{name}'.");
            field.SetValue(target, value);
        }

        private static IEnumerator WaitForFixedFrames(int frameCount)
        {
            var previousCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = 60;
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
