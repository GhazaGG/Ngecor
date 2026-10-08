using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ngecor.Player;

namespace Ngecor.Interaction.Tests
{
    public class InteractionHighlighterTests
    {
        private GameObject _playerObject;
        private PlayerMovement _playerMovement;
        private InteractionDetector _detector;
        private InteractionHighlighter _highlighter;
        private GameObject _targetObject;
        private Material _runtimeMaterial;

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
            _highlighter = _playerObject.AddComponent<InteractionHighlighter>();

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
            if (_runtimeMaterial != null)
                Object.DestroyImmediate(_runtimeMaterial);
        }

        private GameObject CreateInteractableInFront(float z, out InteractableObject interactable)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = new Vector3(0f, 1.4f, z);
            interactable = go.AddComponent<InteractableObject>();
            return go;
        }

        [UnityTest]
        public IEnumerator HighlightsTarget_WhenInteractionDetectorAcquiresTarget()
        {
            _targetObject = CreateInteractableInFront(2f, out _);

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;

            Assert.That(_detector.HasTarget, Is.True, "Detector should have acquired the target.");
            Assert.That(_highlighter.HasHighlight, Is.True);
            Assert.That(_highlighter.HighlightedMeshCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator ClearsHighlight_WhenTargetBecomesNull()
        {
            _targetObject = CreateInteractableInFront(2f, out _);

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;
            Assert.That(_highlighter.HasHighlight, Is.True);

            // Move the target beyond max distance so the detector loses it.
            _targetObject.transform.position = new Vector3(0f, 1.4f, 10f);
            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;

            Assert.That(_detector.HasTarget, Is.False);
            Assert.That(_highlighter.HasHighlight, Is.False);
            Assert.That(_highlighter.HighlightedMeshCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ClearsHighlight_WhenTargetIsGrabbed()
        {
            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetObject.transform.position = new Vector3(0f, 1.4f, 2f);
            var grabbable = _targetObject.AddComponent<GrabbableObject>();
            grabbable.Rigidbody.isKinematic = true;

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;
            Assert.That(_highlighter.HasHighlight, Is.True);

            grabbable.OnGrab(_playerObject);
            yield return null;
            yield return null;

            Assert.That(_highlighter.HasHighlight, Is.False);
            Assert.That(_highlighter.HighlightedMeshCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ClearsHighlightSafely_WhenTargetDestroyed()
        {
            _targetObject = CreateInteractableInFront(2f, out _);

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;
            Assert.That(_highlighter.HasHighlight, Is.True);

            Object.Destroy(_targetObject);
            _targetObject = null;
            yield return null;
            yield return null;

            Assert.That(_highlighter.HasHighlight, Is.False);
            Assert.That(_highlighter.HighlightedMeshCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ClearsHighlightSafely_WhenTargetDeactivated()
        {
            _targetObject = CreateInteractableInFront(2f, out _);

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;
            Assert.That(_highlighter.HasHighlight, Is.True);

            _targetObject.SetActive(false);
            yield return null;
            yield return null;

            Assert.That(_highlighter.HasHighlight, Is.False);
            Assert.That(_highlighter.HighlightedMeshCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DoesNotMutateSharedMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            _runtimeMaterial = new Material(shader);
            _runtimeMaterial.name = "HighlightTestMaterial";
            if (_runtimeMaterial.HasProperty("_BaseColor"))
                _runtimeMaterial.SetColor("_BaseColor", new Color(0.2f, 0.4f, 0.6f, 1f));

            _targetObject = CreateInteractableInFront(2f, out _);
            var renderer = _targetObject.GetComponent<Renderer>();
            renderer.sharedMaterial = _runtimeMaterial;

            var sharedBefore = renderer.sharedMaterial;
            var colorBefore = _runtimeMaterial.HasProperty("_BaseColor")
                ? _runtimeMaterial.GetColor("_BaseColor")
                : Color.clear;

            Physics.SyncTransforms();
            yield return null;
            _detector.Detect();
            yield return null;

            Assert.That(_highlighter.HasHighlight, Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(sharedBefore),
                "Highlighting must not replace the shared material.");
            Assert.That(renderer.sharedMaterial, Is.SameAs(_runtimeMaterial));

            if (_runtimeMaterial.HasProperty("_BaseColor"))
            {
                var colorAfter = _runtimeMaterial.GetColor("_BaseColor");
                Assert.That(colorAfter.r, Is.EqualTo(colorBefore.r).Within(0.0001f));
                Assert.That(colorAfter.g, Is.EqualTo(colorBefore.g).Within(0.0001f));
                Assert.That(colorAfter.b, Is.EqualTo(colorBefore.b).Within(0.0001f));
                Assert.That(colorAfter.a, Is.EqualTo(colorBefore.a).Within(0.0001f));
            }

            _targetObject.SetActive(false);
            yield return null;
            yield return null;

            Assert.That(renderer.sharedMaterial, Is.SameAs(_runtimeMaterial));
        }

        [Test]
        public void OutlineWidth_CanBeConfiguredAndClamped()
        {
            Assert.That(_highlighter.OutlineWidth, Is.GreaterThanOrEqualTo(0.04f));
            _highlighter.OutlineWidth = 0.05f;
            Assert.That(_highlighter.OutlineWidth, Is.EqualTo(0.05f).Within(0.0001f));
            _highlighter.OutlineWidth = -1f;
            Assert.That(_highlighter.OutlineWidth, Is.EqualTo(0.001f).Within(0.0001f));
        }
    }
}
