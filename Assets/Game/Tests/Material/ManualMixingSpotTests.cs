#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Ngecor.Interaction;
using Ngecor.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ngecor.Material.Tests
{
    public sealed class ManualMixingSpotTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var pile in Object.FindObjectsByType<GroundMaterialPile>(FindObjectsSortMode.None))
                Object.Destroy(pile.gameObject);
            foreach (var item in _objects)
                if (item != null)
                    Object.Destroy(item);
            yield return null;
            _objects.Clear();
        }

        [Test]
        public void FullCementSackConvertsToTunedUnitsAndIsConsumed()
        {
            var spot = CreateSpot(50);
            var bag = Prefab("CementBag");
            Set(spot, "_cementUnitsPerSack", 12);

            Assert.That(spot.TryReceiveBag(bag.GetComponent<CementBag>()), Is.True);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(12));
            Assert.That(spot.TryReceiveBag(bag.GetComponent<CementBag>()), Is.False);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(12));
        }

        [Test]
        public void SackIsPreservedWhenSpotCannotFitWholePayload()
        {
            var spot = CreateSpot(24);
            var bag = Prefab("CementBag");

            Assert.That(spot.TryReceiveBag(bag.GetComponent<CementBag>()), Is.False);
            Assert.That(bag != null, Is.True);
            Assert.That(spot.Ingredients.TotalUnits, Is.Zero);
        }

        [Test]
        public void HeldSackIsNotConsumedUntilReleased()
        {
            var spot = CreateSpot(50);
            var bag = Prefab("CementBag");
            var grabbable = bag.AddComponent<GrabbableObject>();
            var holder = new GameObject("Test Holder");
            _objects.Add(holder);
            grabbable.OnGrab(holder);

            Assert.That(spot.TryReceiveBag(bag.GetComponent<CementBag>()), Is.False);
            Assert.That(spot.Ingredients.TotalUnits, Is.Zero);
            grabbable.OnRelease();
            Assert.That(spot.TryReceiveBag(bag.GetComponent<CementBag>()), Is.True);
        }

        [UnityTest]
        public IEnumerator FullOutputSlotsPauseMixingUntilABatchIsCollected()
        {
            Ground();
            var spot = CreateSpot(100);
            Set(spot, "_actionsPerBatch", 1);
            Set(spot, "_outputSlots", 2);
            spot.Ingredients.AddUnits(MaterialType.Cement, 10);
            spot.Ingredients.AddUnits(MaterialType.Sand, 20);
            Physics.SyncTransforms();

            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(Object.FindObjectsByType<ConcreteBatch>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            Assert.That(spot.Work, Is.EqualTo(1));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(8));

            var collected = Object.FindObjectsByType<ConcreteBatch>(FindObjectsSortMode.None)[0];
            collected.GetComponent<BulkMaterialContainer>().RemoveUnits(MaterialType.Concrete, 3);
            // Empty piles destroy themselves in LateUpdate; wait until the object is gone.
            yield return null;
            yield return null;
            Physics.SyncTransforms();

            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(Object.FindObjectsByType<ConcreteBatch>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            Assert.That(spot.Work, Is.Zero);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator EachShovelActionAddsWorkAndRecipeLeavesUnmatchedIngredients()
        {
            Ground();
            var spot = CreateSpot(100);
            Set(spot, "_cementUnitsPerBatch", 2);
            Set(spot, "_sandUnitsPerBatch", 3);
            Set(spot, "_actionsPerBatch", 2);
            spot.Ingredients.AddUnits(MaterialType.Cement, 5);
            spot.Ingredients.AddUnits(MaterialType.Sand, 7);
            Physics.SyncTransforms();

            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.Work, Is.EqualTo(1));
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.Work, Is.Zero);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(3));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Sand), Is.EqualTo(4));
            var firstBatch = Object.FindObjectsByType<ConcreteBatch>(FindObjectsSortMode.None);
            Assert.That(firstBatch.Length, Is.EqualTo(1));
            Assert.That(firstBatch[0].GetComponent<BulkMaterialContainer>().GetUnits(MaterialType.Concrete), Is.EqualTo(5));

            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(1));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Sand), Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<ConcreteBatch>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            yield return null;
        }

        [Test]
        public void EmptyShovelRoutesPourInputToOneStirAction()
        {
            var spot = CreateSpot(100);
            Set(spot, "_actionsPerBatch", 5);
            spot.Ingredients.AddUnits(MaterialType.Cement, 1);
            spot.Ingredients.AddUnits(MaterialType.Sand, 2);
            var shovel = Prefab("Shovel");
            shovel.GetComponent<ShovelInput>().enabled = false;
            shovel.GetComponent<Rigidbody>().isKinematic = true;
            shovel.transform.position = new Vector3(0f, 0.12f, -0.5f);
            Set(shovel.GetComponent<ShovelAction>(), "_blade", shovel.transform);
            Set(shovel.GetComponent<GroundMaterialDeposit>(), "_outlet", shovel.transform);
            shovel.GetComponent<GrabbableObject>().OnGrab(Holder());
            Physics.SyncTransforms();

            Assert.That(shovel.GetComponent<ShovelInput>().RequestUse(), Is.EqualTo(1));
            Assert.That(spot.Work, Is.EqualTo(1));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(1));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Sand), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator EmptyBucketCollectsConcreteBatchWithExistingPourInputAction()
        {
            Ground();
            var pile = Prefab("GroundPile");
            var concrete = pile.GetComponent<BulkMaterialContainer>();
            Assert.That(concrete.ConfigureSingleTypeWhenEmpty(MaterialType.Concrete), Is.True);
            concrete.AddUnits(MaterialType.Concrete, 8);
            pile.AddComponent<ConcreteBatch>();
            pile.transform.position = new Vector3(0f, 0f, 0.4f);

            var bucket = Prefab("Bucket");
            bucket.GetComponent<Rigidbody>().isKinematic = true;
            bucket.transform.position = new Vector3(0f, 0.3f, 0.5f);
            bucket.GetComponent<BucketPourInput>().enabled = false;
            var holder = new GameObject("Test Holder");
            _objects.Add(holder);
            bucket.GetComponent<Ngecor.Interaction.GrabbableObject>().OnGrab(holder);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            var action = bucket.GetComponent<BucketPourAction>();

            Assert.That(action.RequestPour(), Is.True);
            Assert.That(Read(action, "_collectSource"), Is.SameAs(concrete));
            for (var i = 0; i < 20; i++)
            {
                Assert.That(action.RequestPour(), Is.True);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(bucket.GetComponent<BulkMaterialContainer>().GetUnits(MaterialType.Concrete), Is.GreaterThan(0));
            Assert.That(bucket.GetComponent<BulkMaterialContainer>().TotalUnits + concrete.TotalUnits, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator BucketCanPourCementIntoMultiTypeMixingSpot()
        {
            var spot = CreateSpot(100);
            var bucket = Prefab("Bucket");
            bucket.GetComponent<BucketPourInput>().enabled = false;
            var stock = bucket.GetComponent<BulkMaterialContainer>();
            Assert.That(stock.ConfigureSingleTypeWhenEmpty(MaterialType.Cement), Is.True);
            stock.AddUnits(MaterialType.Cement, 10);
            var holder = new GameObject("Test Holder");
            _objects.Add(holder);
            bucket.GetComponent<Ngecor.Interaction.GrabbableObject>().OnGrab(holder);
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(spot.Ingredients);

            Assert.That(action.RequestPour(), Is.True);
            Assert.That(Read(action, "_requestedReceiver"), Is.SameAs(spot.Ingredients));
            yield return new WaitForSeconds(1.2f);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.GreaterThan(0));
            Assert.That(stock.GetUnits(MaterialType.Cement) + spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(10));
        }

        private ManualMixingSpot CreateSpot(int capacity)
        {
            var root = new GameObject("Mixing Spot");
            root.transform.position = new Vector3(0f, 0.12f, 0f);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 0.2f, 2f);
            var contents = root.AddComponent<BulkMaterialContainer>();
            Set(contents, "_capacity", capacity);
            Set(contents, "_mode", ContainerMode.MultipleTypes);
            Set(contents, "_acceptedTypes", new List<MaterialType> { MaterialType.Cement, MaterialType.Sand });
            var spot = root.AddComponent<ManualMixingSpot>();
            Set(spot, "_outputPilePrefab", AssetDatabase.LoadAssetAtPath<GroundMaterialPile>(
                "Assets/Game/Prefabs/Material/GroundPile.prefab"));
            var output = new GameObject("Output Point");
            output.transform.position = new Vector3(2.5f, 0.12f, 0f);
            Set(spot, "_outputPoint", output.transform);
            _objects.Add(root);
            _objects.Add(output);
            return spot;
        }

        private GameObject Prefab(string name)
        {
            var path = "Assets/Game/Prefabs/Material/" + name + ".prefab";
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            _objects.Add(instance);
            return instance;
        }

        private GameObject Holder()
        {
            var holder = new GameObject("Local Holder");
            holder.SetActive(false);
            holder.AddComponent<CharacterController>();
            var movement = holder.AddComponent<PlayerMovement>();
            var camera = new GameObject("Camera");
            camera.transform.SetParent(holder.transform, false);
            camera.AddComponent<Camera>();
            movement.SetLocalPlayer(true);
            holder.SetActive(true);
            _objects.Add(holder);
            return holder;
        }

        private void Ground()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Test Ground";
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(20f, 0.5f, 20f);
            _objects.Add(ground);
        }

        private static void Set(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, field);
            info.SetValue(target, value);
        }

        private static object Read(object target, string field)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, field);
            return info.GetValue(target);
        }
    }
}
#endif
