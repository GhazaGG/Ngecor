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
            Assert.That(_highlighter.OutlineWidth, Is.EqualTo(0.02f).Within(0.0001f));
            _highlighter.OutlineWidth = 0.05f;
            Assert.That(_highlighter.OutlineWidth, Is.EqualTo(0.05f).Within(0.0001f));
            _highlighter.OutlineWidth = -1f;
            Assert.That(_highlighter.OutlineWidth, Is.EqualTo(0.001f).Within(0.0001f));
        }

        [Test]
        public void GetOrCreateOutlineMesh_SmoothesNormalsAtHardCorners()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var mf = cube.GetComponent<MeshFilter>();
                var sourceMesh = mf.sharedMesh;
                var outlineMesh = InteractionHighlighter.GetOrCreateOutlineMesh(sourceMesh);

                Assert.That(outlineMesh, Is.Not.Null);
                Assert.That(outlineMesh, Is.Not.SameAs(sourceMesh));
                Assert.That(outlineMesh.normals.Length, Is.EqualTo(sourceMesh.normals.Length));

                // On a standard cube, all original normals are cardinal axes (e.g. (1, 0, 0)).
                // On the smoothed outline mesh, corner vertices must have non-cardinal diagonal normals.
                bool hasDiagonalNormal = false;
                foreach (var n in outlineMesh.normals)
                {
                    float maxComponent = Mathf.Max(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
                    if (maxComponent < 0.95f)
                    {
                        hasDiagonalNormal = true;
                        break;
                    }
                }

                Assert.That(hasDiagonalNormal, Is.True, "Outline mesh must have smoothed diagonal normals at corners to prevent tearing.");
            }
            finally
            {
                Object.DestroyImmediate(cube);
                InteractionHighlighter.ClearMeshCache();
            }
        }

        [Test]
        public void ClearMeshCache_DestroysClonedMeshesWithoutDestroyingSourceMesh()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var mf = cube.GetComponent<MeshFilter>();
                var sourceMesh = mf.sharedMesh;
                var outlineMesh = InteractionHighlighter.GetOrCreateOutlineMesh(sourceMesh);

                Assert.That(outlineMesh, Is.Not.Null);
                Assert.That(outlineMesh, Is.Not.SameAs(sourceMesh));

                InteractionHighlighter.ClearMeshCache();

                // Cloned mesh must be destroyed, evaluating to null via Unity's overloaded == operator
                Assert.That(outlineMesh == null, Is.True, "Cloned outline mesh must be destroyed when cache is cleared.");
                // Source mesh must remain untouched
                Assert.That(sourceMesh != null, Is.True, "Source mesh must NOT be destroyed when cache is cleared.");
            }
            finally
            {
                Object.DestroyImmediate(cube);
                InteractionHighlighter.ClearMeshCache();
            }
        }

        [Test]
        public void PruneDeadMeshes_EvictsDestroyedSourceMeshAndDestroysItsClone_WhilePreservingAliveMeshes()
        {
            InteractionHighlighter.ClearMeshCache();

            // 1. Create a persistent source mesh (e.g. shared primitive mesh used by another player)
            var persistentCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var persistentMesh = persistentCube.GetComponent<MeshFilter>().sharedMesh;
            var persistentClone = InteractionHighlighter.GetOrCreateOutlineMesh(persistentMesh);

            // 2. Create a temporary runtime mesh that will be discarded
            var tempMesh = new Mesh();
            tempMesh.name = "TempRuntimeMesh";
            tempMesh.vertices = new[] { Vector3.zero, Vector3.up, Vector3.right };
            tempMesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward };
            tempMesh.triangles = new[] { 0, 1, 2 };

            var tempClone = InteractionHighlighter.GetOrCreateOutlineMesh(tempMesh);

            try
            {
                // Verify both are in cache
                Assert.That(InteractionHighlighter.CachedMeshCount, Is.EqualTo(2));
                Assert.That(tempClone, Is.Not.Null);
                Assert.That(tempClone, Is.Not.SameAs(tempMesh));
                Assert.That(InteractionHighlighter.IsMeshCached(tempMesh), Is.True);
                Assert.That(InteractionHighlighter.IsMeshCached(persistentMesh), Is.True);

                // 3. Destroy/discard the temporary runtime mesh
                Object.DestroyImmediate(tempMesh);
                Assert.That(tempMesh == null, Is.True, "Temporary mesh must be destroyed.");

                // 4. Trigger pruning (simulating gameplay lifecycle or calling PruneDeadMeshes)
                var prunedCount = InteractionHighlighter.PruneDeadMeshes();

                // 5. Verify the cache evicted the dead mesh and destroyed its clone
                Assert.That(prunedCount, Is.EqualTo(1), "Exactly one dead mesh must be pruned.");
                Assert.That(InteractionHighlighter.CachedMeshCount, Is.EqualTo(1), "Only persistent mesh must remain in cache.");
                Assert.That(tempClone == null, Is.True, "Cloned outline mesh of destroyed source must be destroyed.");

                // 6. Verify the persistent mesh and its clone still exist intact for other players
                Assert.That(persistentMesh != null, Is.True, "Persistent source mesh must remain intact.");
                Assert.That(persistentClone != null, Is.True, "Persistent outline clone must remain intact for other players.");
                Assert.That(InteractionHighlighter.IsMeshCached(persistentMesh), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(persistentCube);
                InteractionHighlighter.ClearMeshCache();
            }
        }

        [UnityTest]
        public IEnumerator HotPath_MultiMeshTargetAndIdle_DoesNotScanCacheEveryFrameOrLookup()
        {
            InteractionHighlighter.ClearMeshCache();

            // 1. Create a composite interactable with multiple child MeshFilters
            _targetObject = new GameObject("CompositeInteractable");
            _targetObject.transform.position = new Vector3(0f, 1.4f, 2f);
            var interactable = _targetObject.AddComponent<InteractableObject>();
            var col = _targetObject.AddComponent<BoxCollider>();
            col.size = Vector3.one * 2f;

            // Child mesh 1 (Cube)
            var child1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child1.transform.SetParent(_targetObject.transform, false);
            child1.transform.localPosition = new Vector3(-0.5f, 0f, 0f);

            // Child mesh 2 (Sphere)
            var child2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            child2.transform.SetParent(_targetObject.transform, false);
            child2.transform.localPosition = new Vector3(0.5f, 0f, 0f);

            Physics.SyncTransforms();
            yield return null;

            _detector.Detect();
            Assert.That(_detector.HasTarget, Is.True);

            // Highlight target
            _highlighter.UpdateHighlight();
            Assert.That(_highlighter.HighlightedMeshCount, Is.GreaterThanOrEqualTo(2));

            // Reset prune counter
            InteractionHighlighter.ClearMeshCache();
            int initialPrunes = InteractionHighlighter.PruneInvocationCount;

            // Render across 5 consecutive frames (steady-state hot path)
            for (var frame = 0; frame < 5; frame++)
            {
                yield return null;
                _highlighter.UpdateHighlight();
            }

            // Verify that hot-path lookup & rendering did NOT trigger prune scans per-lookup or per-frame
            Assert.That(InteractionHighlighter.PruneInvocationCount, Is.LessThanOrEqualTo(1),
                "Pruning must not scan the static cache on every lookup or every frame in steady-state.");

            // Also test idle (no target)
            _targetObject.SetActive(false);
            yield return null;
            _detector.Detect();
            _highlighter.UpdateHighlight();
            Assert.That(_highlighter.HasHighlight, Is.False);

            for (var frame = 0; frame < 3; frame++)
            {
                yield return null;
                _highlighter.UpdateHighlight();
            }

            Assert.That(InteractionHighlighter.PruneInvocationCount, Is.LessThanOrEqualTo(1),
                "Idle state must not trigger redundant cache prune scans.");
        }
    }
}
