using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ngecor.Player;

namespace Ngecor.Interaction.Tests
{
    public class InteractionPromptUITests
    {
        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private InteractionPromptUI _promptUi;
        private GameObject _targetObject;

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
            _promptUi = _playerObject.AddComponent<InteractionPromptUI>();

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
            if (_targetObject != null)
                Object.DestroyImmediate(_targetObject);
        }

        private GameObject CreateInteractable(float z, string prompt, bool grabbable = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = new Vector3(0f, 1.4f, z);

            if (grabbable)
            {
                var grabbableComponent = go.AddComponent<GrabbableObject>();
                grabbableComponent.Rigidbody.isKinematic = true;
                SetPrompt(grabbableComponent, prompt);
            }
            else
            {
                var interactable = go.AddComponent<InteractableObject>();
                SetPrompt(interactable, prompt);
            }

            return go;
        }

        private static void SetPrompt(object target, string prompt)
        {
            var field = target.GetType().GetField("_prompt",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, prompt);
        }

        [UnityTest]
        public IEnumerator CrosshairIsAlwaysVisible()
        {
            yield return null;
            yield return null;

            Assert.That(_promptUi.Crosshair, Is.Not.Null);
            Assert.That(_promptUi.Crosshair.enabled, Is.True,
                "Crosshair must stay visible with no target.");

            _targetObject = CreateInteractable(2f, "Interact");
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;

            Assert.That(_promptUi.Crosshair.enabled, Is.True,
                "Crosshair must stay visible while a target is shown.");
        }

        [UnityTest]
        public IEnumerator PromptTextDisplaysPrompt_WhenTargetAcquired()
        {
            _targetObject = CreateInteractable(2f, "Open Crate");
            var interactable = _targetObject.GetComponent<InteractableObject>();

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;

            Assert.That(_detector.HasTarget, Is.True);
            Assert.That(_promptUi.PromptText, Is.Not.Null);
            Assert.That(_promptUi.PromptText.enabled, Is.True);
            Assert.That(_promptUi.PromptText.text, Is.EqualTo(interactable.InteractionPrompt));
            Assert.That(_promptUi.IsPromptVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator PromptTextHides_WhenTargetLost()
        {
            _targetObject = CreateInteractable(2f, "Interact");

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;
            Assert.That(_promptUi.PromptText.enabled, Is.True);

            // Move the target out of range so the detector loses it.
            _targetObject.transform.position = new Vector3(0f, 1.4f, 10f);
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_promptUi.PromptText.enabled, Is.False);
            Assert.That(_promptUi.IsPromptVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator PromptTextUpdates_WhenTargetChanges()
        {
            _targetObject = CreateInteractable(2f, "First");
            var first = _targetObject.GetComponent<InteractableObject>();

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;
            Assert.That(_promptUi.PromptText.text, Is.EqualTo(first.InteractionPrompt));

            Object.DestroyImmediate(_targetObject);

            _targetObject = CreateInteractable(2f, "Second");
            var second = _targetObject.GetComponent<InteractableObject>();
            Assert.That(second.InteractionPrompt, Is.Not.EqualTo(first.InteractionPrompt));

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;

            Assert.That(_detector.HasTarget, Is.True);
            Assert.That(_promptUi.PromptText.enabled, Is.True);
            Assert.That(_promptUi.PromptText.text, Is.EqualTo(second.InteractionPrompt));
        }

        [UnityTest]
        public IEnumerator PromptTextHidesSafely_WhenTargetDestroyed()
        {
            _targetObject = CreateInteractable(2f, "Interact");

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;
            Assert.That(_promptUi.PromptText.enabled, Is.True);

            Object.Destroy(_targetObject);
            _targetObject = null;
            yield return null;
            yield return null;

            Assert.That(_promptUi.PromptText.enabled, Is.False);
            Assert.That(_promptUi.IsPromptVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator PromptTextHides_WhenTargetIsGrabbed()
        {
            _targetObject = CreateInteractable(2f, "Interact", grabbable: true);
            var grabbable = _targetObject.GetComponent<GrabbableObject>();

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;
            Assert.That(_promptUi.PromptText.enabled, Is.True);

            grabbable.OnGrab(_playerObject);
            yield return null;
            yield return null;

            Assert.That(_promptUi.PromptText.enabled, Is.False);
            Assert.That(_promptUi.IsPromptVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator TwoPlayers_NonLocalPlayerHasNoCrosshairOrPrompt_LocalPlayerHasExactlyOne()
        {
            // 1. Setup a remote (non-local) player. Player.prefab sets _isLocalPlayer to false by default.
            var remotePlayerObject = new GameObject("RemotePlayer");
            remotePlayerObject.transform.position = new Vector3(5f, 0f, 0f);
            remotePlayerObject.transform.rotation = Quaternion.identity;
            remotePlayerObject.SetActive(false);

            var remoteController = remotePlayerObject.AddComponent<CharacterController>();
            var remotePivot = new GameObject("CameraPivot");
            remotePivot.transform.SetParent(remotePlayerObject.transform, false);
            remotePivot.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            var remoteCameraObject = new GameObject("PlayerCamera");
            remoteCameraObject.transform.SetParent(remotePivot.transform, false);
            remoteCameraObject.AddComponent<Camera>();

            var remoteMovement = remotePlayerObject.AddComponent<PlayerMovement>();
            var remoteDetector = remotePlayerObject.AddComponent<InteractionDetector>();
            var remotePromptUi = remotePlayerObject.AddComponent<InteractionPromptUI>();

            // When activated, remoteMovement remains non-local (default _isLocalPlayer is false)
            remotePlayerObject.SetActive(true);
            yield return null;

            // Target in front of both players
            _targetObject = CreateInteractable(2f, "Grab", grabbable: true);
            var remoteTarget = CreateInteractable(2f, "Grab", grabbable: true);
            remoteTarget.transform.position = new Vector3(5f, 1.4f, 2f);
            Physics.SyncTransforms();
            yield return null;

            _detector.Detect();
            remoteDetector.Detect();
            _promptUi.UpdatePrompt();
            remotePromptUi.UpdatePrompt();
            yield return null;

            // Non-local player checks: no active canvas, no crosshair, no prompt
            Assert.That(remoteMovement.IsLocalPlayer, Is.False, "Remote player must be non-local.");
            Assert.That(remotePromptUi.Canvas == null || !remotePromptUi.Canvas.gameObject.activeInHierarchy, Is.True,
                "Non-local player must not have an active canvas.");
            Assert.That(remotePromptUi.Crosshair, Is.Null,
                "Non-local player must not display a crosshair.");
            Assert.That(remotePromptUi.IsPromptVisible, Is.False,
                "Non-local player must not display prompt.");

            // Local player checks: exactly one active canvas, crosshair visible, prompt visible
            Assert.That(_playerMovement.IsLocalPlayer, Is.True, "Local player must be local.");
            Assert.That(_promptUi.Canvas, Is.Not.Null);
            Assert.That(_promptUi.Canvas.gameObject.activeInHierarchy, Is.True,
                "Local player must have exactly one active canvas.");
            Assert.That(_promptUi.Crosshair, Is.Not.Null);
            Assert.That(_promptUi.Crosshair.enabled, Is.True);
            Assert.That(_promptUi.IsPromptVisible, Is.True);
            Assert.That(_promptUi.DisplayedPrompt, Is.EqualTo("Grab"));

            // Transition test: switch remote player to local via SetLocalPlayer
            remoteMovement.SetLocalPlayer(true);
            yield return null;
            remoteDetector.Detect();
            remotePromptUi.UpdatePrompt();
            yield return null;

            Assert.That(remoteMovement.IsLocalPlayer, Is.True);
            Assert.That(remotePromptUi.Canvas, Is.Not.Null);
            Assert.That(remotePromptUi.Canvas.gameObject.activeInHierarchy, Is.True);
            Assert.That(remotePromptUi.Crosshair, Is.Not.Null);
            Assert.That(remotePromptUi.Crosshair.enabled, Is.True);
            Assert.That(remotePromptUi.IsPromptVisible, Is.True);

            // Clean up
            Object.DestroyImmediate(remotePlayerObject);
            Object.DestroyImmediate(remoteTarget);
        }
    }
}
