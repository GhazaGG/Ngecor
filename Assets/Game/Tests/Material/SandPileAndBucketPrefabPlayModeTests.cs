#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ngecor.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Ngecor.Material.Tests
{
    public sealed class SandPileAndBucketPrefabPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var instance in _objects)
                Object.Destroy(instance);
            yield return null;
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator SandPileShrinksInVolumeWithFixedBaseAndMatchingCollider()
        {
            var pile = InstantiatePrefab("SandPile");
            var container = pile.GetComponent<BulkMaterialContainer>();
            var mound = pile.transform.Find("Mound");
            yield return null;

            Assert.That(pile.GetComponent<Rigidbody>(), Is.Null);
            Assert.That(mound.GetComponent<MeshCollider>().sharedMesh,
                Is.SameAs(mound.GetComponent<MeshFilter>().sharedMesh));
            var initialScale = mound.localScale;
            var initialBase = mound.localPosition.y - initialScale.y * 0.5f;
            var initialUnits = container.TotalUnits;
            container.RemoveUnits(MaterialType.Sand, initialUnits / 2);
            yield return null;

            var ratio = (float)container.TotalUnits / initialUnits;
            var factor = Mathf.Pow(ratio, 1f / 3f);
            Assert.That(Vector3.Distance(mound.localScale, initialScale * factor), Is.LessThan(0.0001f));
            Assert.That(mound.localPosition.y - mound.localScale.y * 0.5f,
                Is.EqualTo(initialBase).Within(0.0001f));
            Physics.SyncTransforms();
            var collider = mound.GetComponent<MeshCollider>();
            Assert.That(collider.bounds.size.y,
                Is.EqualTo(mound.localScale.y).Within(0.001f));
            Assert.That(collider.Raycast(new Ray(mound.position + Vector3.up * 2f,
                Vector3.down), out var hit, 5f), Is.True);
            Assert.That(hit.normal.y, Is.GreaterThan(0.5f));
            Assert.That(hit.point.y, Is.EqualTo(collider.bounds.max.y).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator EmptyPileHasNoVisibleMoundOrCollisionAndCanBeRefilled()
        {
            var pile = InstantiatePrefab("SandPile");
            var container = pile.GetComponent<BulkMaterialContainer>();
            var mound = pile.transform.Find("Mound");
            container.RemoveUnits(MaterialType.Sand, container.TotalUnits);
            yield return null;
            Physics.SyncTransforms();

            Assert.That(mound.gameObject.activeInHierarchy, Is.False);
            Assert.That(pile.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(mound.localScale.y, Is.GreaterThan(0f));

            container.AddUnits(MaterialType.Sand, container.Capacity);
            yield return null;
            Assert.That(mound.gameObject.activeInHierarchy, Is.True);
            Assert.That(mound.localScale, Is.EqualTo(new Vector3(1.15f, 0.5f, 0.9f)));
            Assert.That(pile.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BucketCavityLetsSmallRigidBodyLandOnItsBottom()
        {
            var bucket = InstantiatePrefab("Bucket");
            bucket.GetComponent<Rigidbody>().isKinematic = true;
            Assert.That(bucket.GetComponent<Collider>(), Is.Null);
            Assert.That(bucket.GetComponentsInChildren<BoxCollider>().Length, Is.EqualTo(5));

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _objects.Add(ball);
            ball.transform.position = Vector3.up;
            ball.transform.localScale = Vector3.one * 0.08f;
            var body = ball.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            yield return new WaitForSeconds(1f);

            // Bottom top = -0.27; sphere radius = 0.04. It must enter the cavity.
            Assert.That(ball.transform.position.y, Is.EqualTo(-0.23f).Within(0.03f));
            Assert.That(Mathf.Abs(body.linearVelocity.y), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator BucketStaysFilledWithoutValidGroundEvenWhenTipped()
        {
            var bucket = InstantiatePrefab("Bucket");
            var body = bucket.GetComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            var container = bucket.GetComponent<BulkMaterialContainer>();
            container.AddUnits(MaterialType.Sand, 12);

            yield return new WaitForSeconds(0.4f);
            Assert.That(container.TotalUnits, Is.EqualTo(12));

            bucket.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.4f);
            Assert.That(container.TotalUnits, Is.EqualTo(12));
        }

        [UnityTest]
        public IEnumerator PourAnimationTiltsAndEmitsThenResetsWithoutLosingUnits()
        {
            var bucket = CreateHeldPourBucket();
            var source = bucket.GetComponent<BulkMaterialContainer>();
            var receiver = CreatePourReceiver(MaterialType.Sand, 100);
            var action = bucket.GetComponent<BucketPourAction>();
            var model = bucket.transform.Find("Pour Model");
            var flow = model.GetComponentInChildren<ParticleSystem>();
            action.RegisterNearbyReceiver(receiver);
            Assert.That(action.RequestPour(), Is.True);

            yield return new WaitForSeconds(0.4f);
            Assert.That(Quaternion.Angle(Quaternion.identity, model.localRotation), Is.GreaterThan(50f));
            Assert.That(flow.emission.enabled, Is.True);
            Assert.That(flow.particleCount, Is.GreaterThan(0));
            Assert.That(source.TotalUnits, Is.LessThan(12));
            Assert.That(source.TotalUnits + receiver.TotalUnits, Is.EqualTo(12));
            CapturePourPreview("BucketPourAnimation-Pouring");

            action.CancelPour();
            yield return new WaitForSeconds(0.4f);
            Assert.That(Quaternion.Angle(Quaternion.identity, model.localRotation), Is.LessThan(0.01f));
            Assert.That(flow.emission.enabled, Is.False);
            Assert.That(flow.particleCount, Is.Zero);
            CapturePourPreview("BucketPourAnimation-Stopped");
            var stoppedUnits = receiver.TotalUnits;
            yield return new WaitForSeconds(0.2f);
            Assert.That(receiver.TotalUnits, Is.EqualTo(stoppedUnits));

            action.RequestPour();
            yield return new WaitForSeconds(1.5f);
            Assert.That(source.TotalUnits, Is.Zero);
            Assert.That(receiver.TotalUnits, Is.EqualTo(12));
            Assert.That(flow.emission.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator PourAnimationStaysOffForRejectedFullOrEmptyTransfer()
        {
            var bucket = CreateHeldPourBucket();
            var source = bucket.GetComponent<BulkMaterialContainer>();
            var action = bucket.GetComponent<BucketPourAction>();
            var model = bucket.transform.Find("Pour Model");
            var flow = model.GetComponentInChildren<ParticleSystem>();
            var rejected = CreatePourReceiver(MaterialType.Cement, 100);
            action.RegisterNearbyReceiver(rejected);
            action.RequestPour();
            yield return new WaitForSeconds(0.4f);
            Assert.That(source.TotalUnits, Is.EqualTo(12));
            Assert.That(flow.emission.enabled, Is.False);
            Assert.That(flow.particleCount, Is.Zero);
            Assert.That(model.localRotation, Is.EqualTo(Quaternion.identity));

            action.UnregisterNearbyReceiver(rejected);
            var full = CreatePourReceiver(MaterialType.Sand, 2);
            full.AddUnits(MaterialType.Sand, 2);
            action.RegisterNearbyReceiver(full);
            action.RequestPour();
            yield return new WaitForSeconds(0.4f);
            Assert.That(source.TotalUnits, Is.EqualTo(12));
            Assert.That(full.TotalUnits, Is.EqualTo(2));
            Assert.That(flow.emission.enabled, Is.False);
            Assert.That(flow.particleCount, Is.Zero);

            action.UnregisterNearbyReceiver(full);
            var receiver = CreatePourReceiver(MaterialType.Sand, 100);
            source.RemoveUnits(MaterialType.Sand, 12);
            action.RegisterNearbyReceiver(receiver);
            action.RequestPour();
            yield return new WaitForSeconds(0.4f);
            Assert.That(flow.emission.enabled, Is.False);
            Assert.That(flow.particleCount, Is.Zero);
            Assert.That(model.localRotation, Is.EqualTo(Quaternion.identity));
        }

        [UnityTest]
        public IEnumerator LastSandUnitStillHasVisiblePourFeedback()
        {
            var bucket = CreateHeldPourBucket();
            var source = bucket.GetComponent<BulkMaterialContainer>();
            source.RemoveUnits(MaterialType.Sand, 11);
            var receiver = CreatePourReceiver(MaterialType.Sand, 1);
            var action = bucket.GetComponent<BucketPourAction>();
            var flow = bucket.GetComponentInChildren<ParticleSystem>();
            action.RegisterNearbyReceiver(receiver);
            action.RequestPour();
            yield return new WaitForSeconds(0.25f);

            Assert.That(source.TotalUnits, Is.Zero);
            Assert.That(receiver.TotalUnits, Is.EqualTo(1));
            Assert.That(flow.particleCount, Is.GreaterThan(0));
            yield return new WaitForSeconds(0.6f);
            Assert.That(flow.emission.enabled, Is.False);
            Assert.That(flow.particleCount, Is.Zero);
        }

        private GameObject CreateHeldPourBucket()
        {
            var bucket = InstantiatePrefab("Bucket");
            bucket.transform.position = Vector3.up * 1.4f;
            bucket.GetComponent<BucketPourInput>().enabled = false;
            var body = bucket.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            bucket.GetComponent<BulkMaterialContainer>().AddUnits(MaterialType.Sand, 12);
            var holder = new GameObject("Animation Holder");
            _objects.Add(holder);
            bucket.GetComponent<GrabbableObject>().OnGrab(holder);
            return bucket;
        }

        private BulkMaterialContainer CreatePourReceiver(MaterialType type, int capacity)
        {
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Animation Receiver";
            _objects.Add(receiver);
            receiver.SetActive(false);
            receiver.transform.position = new Vector3(0f, 0.25f, 0.6f);
            receiver.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
            var container = receiver.AddComponent<BulkMaterialContainer>();
            var settings = new SerializedObject(container);
            settings.FindProperty("_capacity").intValue = capacity;
            settings.FindProperty("_singleType").enumValueIndex = (int)type;
            settings.ApplyModifiedPropertiesWithoutUndo();
            receiver.SetActive(true);
            return container;
        }

        [UnityTest]
        public IEnumerator PourPreviewCreatesMissingDirectoryAndCanCaptureAgain()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Screenshot regression requires an active graphics device.");

            CreateHeldPourBucket();
            CreatePourReceiver(MaterialType.Sand, 100);
            yield return null;

            var directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs",
                "PourPreview-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                Assert.That(Directory.Exists(directory), Is.False);
                CapturePourPreview("First", directory);
                Assert.That(Directory.Exists(directory), Is.True);
                var first = File.ReadAllBytes(Path.Combine(directory, "First.png"));
                Assert.That(first.Length, Is.GreaterThan(8));
                Assert.That(first[0], Is.EqualTo(137));
                Assert.That(System.Text.Encoding.ASCII.GetString(first, 1, 3), Is.EqualTo("PNG"));

                CapturePourPreview("Second", directory);
                var second = File.ReadAllBytes(Path.Combine(directory, "Second.png"));
                Assert.That(second.Length, Is.GreaterThan(8));
                Assert.That(second[0], Is.EqualTo(137));
                Assert.That(System.Text.Encoding.ASCII.GetString(second, 1, 3), Is.EqualTo("PNG"));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private void CapturePourPreview(string name, string directory = null)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;

            directory = directory ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs");
            Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("Preview Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(2.5f, 1.7f, 0.9f);
            camera.transform.LookAt(new Vector3(0f, 0.95f, 0.3f));
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.13f, 0.16f, 0.2f);
            var lightObject = new GameObject("Preview Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            var target = new RenderTexture(960, 720, 24);
            target.Create();
            var request = new RenderPipeline.StandardRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var pixels = new Texture2D(960, 720, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            RenderTexture.active = previous;
            target.Release();
            Object.Destroy(target);
            Object.Destroy(pixels);
            Object.Destroy(cameraObject);
            Object.Destroy(lightObject);
        }

        private GameObject InstantiatePrefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/Material/" + name + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            _objects.Add(instance);
            return instance;
        }
    }
}
#endif
