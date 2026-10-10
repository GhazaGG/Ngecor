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

        [Test]
        public void CementWithoutSandCannotBeStirred()
        {
            var spot = CreateSpot(100);
            spot.Ingredients.AddUnits(MaterialType.Cement, 5);

            Assert.That(spot.ExecuteStirAction(), Is.False);
            Assert.That(spot.DryWork, Is.Zero);
            Assert.That(spot.Phase, Is.EqualTo(MixingPhase.NeedsIngredients));
        }

        [Test]
        public void DryMixThenWaterThenWetMixMakesConcreteAndKeepsLeftovers()
        {
            var spot = CreateSpot(100);
            Set(spot, "_dryActions", 2);
            Set(spot, "_wetActionsPerBatch", 2);
            spot.Ingredients.AddUnits(MaterialType.Cement, 2);
            spot.Ingredients.AddUnits(MaterialType.Sand, 5);

            Assert.That(spot.Ingredients.AddUnits(MaterialType.Water, 3), Is.Zero, "water before dry mix");
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.Phase, Is.EqualTo(MixingPhase.DryMixing));
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.IsDryMixed, Is.True);
            Assert.That(spot.ExecuteStirAction(), Is.False, "dry-mixed bed needs water");

            Assert.That(spot.Ingredients.AddUnits(MaterialType.Water, 3), Is.EqualTo(3));
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.Phase, Is.EqualTo(MixingPhase.WetMixing));
            Assert.That(spot.ExecuteStirAction(), Is.True);

            Assert.That(spot.Ingredients.GetUnits(MaterialType.Concrete), Is.EqualTo(4));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(1));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Sand), Is.EqualTo(3));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Water), Is.EqualTo(2));
            Assert.That(spot.Ingredients.AddUnits(MaterialType.Concrete, 1), Is.Zero, "concrete only comes from mixing");
        }

        [Test]
        public void DryIngredientsAddedAfterDryMixingNeedMixingAgain()
        {
            var spot = CreateSpot(100);
            Set(spot, "_dryActions", 1);
            spot.Ingredients.AddUnits(MaterialType.Cement, 1);
            spot.Ingredients.AddUnits(MaterialType.Sand, 2);
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.IsDryMixed, Is.True);

            spot.Ingredients.AddUnits(MaterialType.Sand, 2);
            Assert.That(spot.IsDryMixed, Is.False);
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.IsDryMixed, Is.True);
        }

        [Test]
        public void EmptyShovelStirsAtSpotAndNeverScoopsRawIngredients()
        {
            var spot = CreateSpot(100);
            spot.Ingredients.AddUnits(MaterialType.Cement, 3);
            var shovel = HeldShovelNextTo();
            var input = shovel.GetComponent<ShovelInput>();

            Hold(input, 1.2f);
            Assert.That(spot.DryWork, Is.Zero, "cement only: no stir");
            Tap(input);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(3), "raw cement is not scooped");

            spot.Ingredients.AddUnits(MaterialType.Sand, 6);
            Tap(input);
            Assert.That(spot.DryWork, Is.Zero, "a tap does not stir");
            Hold(input, 0.3f);
            Assert.That(spot.DryWork, Is.EqualTo(1), "hold past the threshold stirs once");
            Hold(input, 1.0f);
            Assert.That(spot.DryWork, Is.EqualTo(3), "then once per interval");
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Sand), Is.EqualTo(6));
            Assert.That(shovel.GetComponent<BulkMaterialContainer>().TotalUnits, Is.Zero);
        }

        [Test]
        public void TapCollectsConcreteWhileAnotherBatchIsReadyToStir()
        {
            var spot = CreateSpot(100);
            MakeConcrete(spot);
            spot.Ingredients.AddUnits(MaterialType.Sand, 4);
            Assert.That(spot.ExecuteStirAction(), Is.True, "re-dry-mix the added sand");
            Assert.That(spot.Ingredients.AddUnits(MaterialType.Water, 2), Is.EqualTo(2));
            var shovel = HeldShovelNextTo();
            var input = shovel.GetComponent<ShovelInput>();
            var held = shovel.GetComponent<BulkMaterialContainer>();

            Tap(input);
            Assert.That(held.GetUnits(MaterialType.Concrete), Is.EqualTo(2));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Concrete), Is.EqualTo(2));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Sand), Is.EqualTo(4));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Water), Is.EqualTo(2));
            Assert.That(spot.WetWork, Is.Zero);

            held.RemoveUnits(MaterialType.Concrete, 2);
            var concreteBefore = spot.Ingredients.GetUnits(MaterialType.Concrete);
            Hold(input, 0.3f);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Concrete), Is.EqualTo(concreteBefore + 4),
                "hold still stirs the ready batch (wet actions tuned to 1)");
            Assert.That(held.TotalUnits, Is.Zero);
        }

        [Test]
        public void ShovelScoopsConcreteFromTheBed()
        {
            var spot = CreateSpot(100);
            MakeConcrete(spot);
            spot.Ingredients.RemoveUnits(MaterialType.Cement, 99);
            var shovel = HeldShovelNextTo();

            Assert.That(shovel.GetComponent<ShovelInput>().RequestUse(), Is.EqualTo(2));
            Assert.That(shovel.GetComponent<BulkMaterialContainer>().GetUnits(MaterialType.Concrete), Is.EqualTo(2));
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Concrete), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator EmptyBucketCollectsConcreteFromTheBed()
        {
            var spot = CreateSpot(100);
            MakeConcrete(spot);
            var bucket = HeldBucket();
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(spot.Ingredients);

            Assert.That(action.RequestPour(), Is.True);
            Assert.That(Read(action, "_collectSource"), Is.SameAs(spot.Ingredients));
            for (var i = 0; i < 40; i++)
            {
                Assert.That(action.RequestPour(), Is.True);
                yield return new WaitForFixedUpdate();
            }
            var held = bucket.GetComponent<BulkMaterialContainer>();
            Assert.That(held.GetUnits(MaterialType.Concrete), Is.GreaterThan(0));
            Assert.That(held.TotalUnits + spot.Ingredients.GetUnits(MaterialType.Concrete), Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator EmptyBucketCollectsWaterFromDrumButShovelCannot()
        {
            var drum = new GameObject("Drum");
            _objects.Add(drum);
            drum.AddComponent<BoxCollider>();
            var water = drum.AddComponent<BulkMaterialContainer>();
            Set(water, "_capacity", 50);
            Set(water, "_mode", ContainerMode.SingleType);
            Set(water, "_singleType", MaterialType.Water);
            water.AddUnits(MaterialType.Water, 50);
            var rule = drum.AddComponent<CollectOnlyType>();
            Set(rule, "_type", MaterialType.Water);
            Set(rule, "_shovelCanCollect", false);

            Assert.That(CollectOnlyType.TryGetCollectType(water, true, out _), Is.False);
            Assert.That(CollectOnlyType.TryGetCollectType(water, false, out var type), Is.True);
            Assert.That(type, Is.EqualTo(MaterialType.Water));

            var bucket = HeldBucket();
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(water);
            Assert.That(action.RequestPour(), Is.True);
            for (var i = 0; i < 40; i++)
            {
                action.RequestPour();
                yield return new WaitForFixedUpdate();
            }
            Assert.That(bucket.GetComponent<BulkMaterialContainer>().GetUnits(MaterialType.Water), Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator WaterPouredBeforeDryMixingStaysInTheBucket()
        {
            var spot = CreateSpot(100);
            spot.Ingredients.AddUnits(MaterialType.Cement, 1);
            spot.Ingredients.AddUnits(MaterialType.Sand, 2);
            var bucket = HeldBucket();
            var stock = bucket.GetComponent<BulkMaterialContainer>();
            Assert.That(stock.ConfigureSingleTypeWhenEmpty(MaterialType.Water), Is.True);
            stock.AddUnits(MaterialType.Water, 10);
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(spot.Ingredients);

            Assert.That(action.RequestPour(), Is.True);
            yield return new WaitForSeconds(0.6f);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Water), Is.Zero);
            Assert.That(stock.GetUnits(MaterialType.Water), Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator BucketCanPourCementIntoMultiTypeMixingSpot()
        {
            var spot = CreateSpot(100);
            var bucket = HeldBucket();
            var stock = bucket.GetComponent<BulkMaterialContainer>();
            Assert.That(stock.ConfigureSingleTypeWhenEmpty(MaterialType.Cement), Is.True);
            stock.AddUnits(MaterialType.Cement, 10);
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(spot.Ingredients);

            Assert.That(action.RequestPour(), Is.True);
            Assert.That(Read(action, "_requestedReceiver"), Is.SameAs(spot.Ingredients));
            yield return new WaitForSeconds(1.2f);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Cement), Is.GreaterThan(0));
            Assert.That(stock.GetUnits(MaterialType.Cement) + spot.Ingredients.GetUnits(MaterialType.Cement), Is.EqualTo(10));
        }

        [Test]
        public void StatusTextTellsThePlayerWhatToDoNext()
        {
            var spot = CreateSpot(100);
            Set(spot, "_dryActions", 1);
            Assert.That(spot.StatusText, Does.Contain("Kosong"));
            spot.Ingredients.AddUnits(MaterialType.Cement, 1);
            Assert.That(spot.StatusText, Does.Contain("pasir"));
            spot.Ingredients.AddUnits(MaterialType.Sand, 2);
            Assert.That(spot.StatusText, Does.Contain("aduk kering"));
            spot.ExecuteStirAction();
            Assert.That(spot.StatusText, Does.Contain("tuang air"));
            spot.Ingredients.AddUnits(MaterialType.Water, 1);
            Assert.That(spot.StatusText, Does.Contain("aduk basah"));
        }

        [UnityTest]
        public IEnumerator HeldBucketOfSandPoursIntoTheRealSpotPrefab()
        {
            var spot = Prefab("ManualMixingSpot").GetComponent<ManualMixingSpot>();
            spot.transform.position = new Vector3(0f, 0.075f, 0f);
            var bucket = HeldBucket();
            var stock = bucket.GetComponent<BulkMaterialContainer>();
            Assert.That(stock.ConfigureSingleTypeWhenEmpty(MaterialType.Sand), Is.True);
            stock.AddUnits(MaterialType.Sand, 10);
            bucket.transform.position = new Vector3(0f, 1.0f, 0f);
            Physics.SyncTransforms();
            for (var i = 0; i < 5; i++)
                yield return new WaitForFixedUpdate();

            var action = bucket.GetComponent<BucketPourAction>();
            Assert.That(action.RequestPour(), Is.True, "no receiver registered by the spot trigger");
            yield return new WaitForSeconds(1.2f);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Sand), Is.GreaterThan(0));
        }

        [Test]
        public void TapAndReleaseAtDrumDoesNotLockTheEmptyBucketType()
        {
            var drum = CreateDrum(new Vector3(0f, 0f, 0.5f));
            var bucket = HeldBucket();
            var stock = bucket.GetComponent<BulkMaterialContainer>();
            Assert.That(stock.ConfigureSingleTypeWhenEmpty(MaterialType.Sand), Is.True);
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(drum);

            Assert.That(action.RequestPour(), Is.True);
            Assert.That(Read(action, "_collectSource"), Is.SameAs(drum));
            action.CancelPour();

            Assert.That(stock.TotalUnits, Is.Zero);
            Assert.That(stock.Accepts(MaterialType.Sand), Is.True);
            Assert.That(stock.Accepts(MaterialType.Water), Is.False);
        }

        [UnityTest]
        public IEnumerator NearerOrdinaryContainerBlocksPickupFromFartherSource()
        {
            var drum = CreateDrum(new Vector3(0f, 0f, 0.8f));
            var ordinary = new GameObject("Ordinary Container");
            _objects.Add(ordinary);
            ordinary.transform.position = new Vector3(0f, 0f, 0.2f);
            var plain = ordinary.AddComponent<BulkMaterialContainer>();
            var bucket = HeldBucket();
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(plain);
            action.RegisterNearbyReceiver(drum);

            action.RequestPour();
            Assert.That(Read(action, "_collectSource"), Is.Null);
            for (var i = 0; i < 20; i++)
            {
                action.RequestPour();
                yield return new WaitForFixedUpdate();
            }
            Assert.That(bucket.GetComponent<BulkMaterialContainer>().TotalUnits, Is.Zero);
            Assert.That(drum.GetUnits(MaterialType.Water), Is.EqualTo(50));
        }

        [Test]
        public void ExactTieBetweenSourcesCancelsPickup()
        {
            var left = CreateDrum(new Vector3(-0.5f, 0f, 0f));
            var right = CreateDrum(new Vector3(0.5f, 0f, 0f));
            var bucket = HeldBucket();
            bucket.transform.position = Vector3.zero;
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(left);
            action.RegisterNearbyReceiver(right);

            action.RequestPour();
            Assert.That(Read(action, "_collectSource"), Is.Null);
            Assert.That(Read(action, "_collectRequested"), Is.False);
        }

        [Test]
        public void HeldBucketPicksTheNextSourceAfterLeavingTheFirst()
        {
            var first = CreateDrum(new Vector3(0f, 0f, 0.5f));
            var second = CreateDrum(new Vector3(0f, 0f, 3f));
            var bucket = HeldBucket();
            var action = bucket.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(first);
            Assert.That(action.RequestPour(), Is.True);
            Assert.That(Read(action, "_collectSource"), Is.SameAs(first));

            action.UnregisterNearbyReceiver(first);
            Assert.That(Read(action, "_collectRequested"), Is.False);
            action.RegisterNearbyReceiver(second);
            Assert.That(action.RequestPour(), Is.True);
            Assert.That(Read(action, "_collectSource"), Is.SameAs(second));
        }

        private static void Tap(ShovelInput input)
        {
            input.ProcessUse(true, true, 0f);
            input.ProcessUse(false, true, 0.05f);
            input.ProcessUse(false, false, 0.05f);
        }

        // Press, hold for the given time in 0.125 s frames (exact in float), then release.
        private static void Hold(ShovelInput input, float seconds)
        {
            input.ProcessUse(true, true, 0f);
            for (var held = 0f; held < seconds - 0.001f; held += 0.125f)
                input.ProcessUse(false, true, 0.125f);
            input.ProcessUse(false, false, 0f);
        }

        private BulkMaterialContainer CreateDrum(Vector3 position)
        {
            var drum = new GameObject("Drum");
            _objects.Add(drum);
            drum.transform.position = position;
            var water = drum.AddComponent<BulkMaterialContainer>();
            Set(water, "_capacity", 50);
            Set(water, "_mode", ContainerMode.SingleType);
            Set(water, "_singleType", MaterialType.Water);
            water.AddUnits(MaterialType.Water, 50);
            var rule = drum.AddComponent<CollectOnlyType>();
            Set(rule, "_type", MaterialType.Water);
            Set(rule, "_shovelCanCollect", false);
            return water;
        }

        // Runs the real dry-mix, water, wet-mix flow; the spot refuses concrete from any other source.
        private static void MakeConcrete(ManualMixingSpot spot)
        {
            Set(spot, "_dryActions", 1);
            Set(spot, "_wetActionsPerBatch", 1);
            spot.Ingredients.AddUnits(MaterialType.Cement, 50);
            spot.Ingredients.AddUnits(MaterialType.Sand, 2);
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.Ingredients.AddUnits(MaterialType.Water, 1), Is.EqualTo(1));
            Assert.That(spot.ExecuteStirAction(), Is.True);
            Assert.That(spot.Ingredients.GetUnits(MaterialType.Concrete), Is.EqualTo(4));
        }

        private GameObject HeldShovelNextTo()
        {
            var shovel = Prefab("Shovel");
            shovel.GetComponent<ShovelInput>().enabled = false;
            shovel.GetComponent<Rigidbody>().isKinematic = true;
            shovel.transform.position = new Vector3(0f, 0.12f, -0.5f);
            Set(shovel.GetComponent<ShovelAction>(), "_blade", shovel.transform);
            Set(shovel.GetComponent<GroundMaterialDeposit>(), "_outlet", shovel.transform);
            shovel.GetComponent<GrabbableObject>().OnGrab(Holder());
            Physics.SyncTransforms();
            return shovel;
        }

        private GameObject HeldBucket()
        {
            var bucket = Prefab("Bucket");
            bucket.transform.position = Vector3.zero;
            bucket.GetComponent<Rigidbody>().isKinematic = true;
            bucket.GetComponent<BucketPourInput>().enabled = false;
            var holder = new GameObject("Test Holder");
            _objects.Add(holder);
            bucket.GetComponent<GrabbableObject>().OnGrab(holder);
            return bucket;
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
            Set(contents, "_acceptedTypes", new List<MaterialType>
                { MaterialType.Cement, MaterialType.Sand, MaterialType.Water, MaterialType.Concrete });
            root.AddComponent<CollectOnlyType>();
            var spot = root.AddComponent<ManualMixingSpot>();
            _objects.Add(root);
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
