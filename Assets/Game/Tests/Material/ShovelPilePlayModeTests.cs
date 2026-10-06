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
    public sealed class ShovelPilePlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var pile in Object.FindObjectsByType<GroundMaterialPile>(FindObjectsSortMode.None))
                Object.Destroy(pile.gameObject);
            foreach (var instance in _objects)
                if (instance != null)
                    Object.Destroy(instance);
            yield return null;
            _objects.Clear();
        }

        [TestCase(MaterialType.Sand)]
        [TestCase(MaterialType.Cement)]
        [TestCase(MaterialType.Concrete)]
        public void ScoopFollowsTypeAndDumpConservesPartialCapacity(MaterialType type)
        {
            var shovel = Shovel();
            var source = Container(new Vector3(0f, 1f, 0.5f), type, 10, 7);
            Physics.SyncTransforms();
            Assert.That(shovel.RequestScoop(), Is.EqualTo(2));
            Assert.That(source.TotalUnits, Is.EqualTo(5));
            var contents = shovel.GetComponent<BulkMaterialContainer>();
            Assert.That(contents.GetUnits(type), Is.EqualTo(2));
            Assert.That(contents.ConfigureSingleTypeWhenEmpty(type == MaterialType.Sand
                ? MaterialType.Cement : MaterialType.Sand), Is.False);
            source.transform.position = Vector3.forward * 10f;
            var receiver = Container(new Vector3(0f, 1f, 0.5f), type, 1);
            Physics.SyncTransforms();
            Assert.That(shovel.RequestDump(), Is.EqualTo(1));
            Assert.That(contents.TotalUnits, Is.EqualTo(1));
            Assert.That(source.TotalUnits + receiver.TotalUnits + contents.TotalUnits, Is.EqualTo(7));
            receiver.transform.position = Vector3.forward * 10f;
            source.transform.position = new Vector3(0f, 1f, 0.5f);
            Physics.SyncTransforms();
            Assert.That(shovel.RequestDump(), Is.EqualTo(1));
            Assert.That(contents.TotalUnits, Is.Zero);
            Assert.That(contents.ConfigureSingleTypeWhenEmpty(MaterialType.Cement), Is.True);
        }

        [Test]
        public void ScoopIgnoresSelfBehindAndOccludedTargets()
        {
            var shovel = Shovel();
            Container(new Vector3(0f, 1f, -0.5f), MaterialType.Sand, 10, 7);
            var front = Container(new Vector3(0f, 1f, 0.75f), MaterialType.Sand, 10, 7);
            var wall = Cube(new Vector3(0f, 1f, 0.35f), new Vector3(0.5f, 0.5f, 0.05f));
            Physics.SyncTransforms();
            Assert.That(shovel.RequestScoop(), Is.Zero);
            Assert.That(front.TotalUnits, Is.EqualTo(7));
            wall.SetActive(false);
            Physics.SyncTransforms();
            Assert.That(shovel.RequestScoop(), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RealMoundCanBeScoopedIntoBucketAndRecoveredPileDisappears()
        {
            Ground();
            var shovel = Shovel();
            var sourcePile = Prefab("SandPile");
            sourcePile.transform.position = new Vector3(0f, 0f, 0.65f);
            shovel.transform.position = new Vector3(0f, 0.25f, -0.25f);
            yield return null;
            Physics.SyncTransforms();
            var stock = sourcePile.GetComponent<BulkMaterialContainer>();
            var initial = stock.TotalUnits;
            Assert.That(ScoopWithoutWarnings(shovel), Is.EqualTo(2));
            Assert.That(stock.TotalUnits, Is.EqualTo(initial - 2));
            sourcePile.SetActive(false);
            var bucket = Prefab("Bucket");
            bucket.GetComponent<Rigidbody>().isKinematic = true;
            bucket.transform.position = new Vector3(0f, 0.32f, 0.5f);
            yield return null;
            Physics.SyncTransforms();
            Assert.That(shovel.RequestDump(), Is.EqualTo(2));
            var bucketContents = bucket.GetComponent<BulkMaterialContainer>();
            Assert.That(bucketContents.TotalUnits, Is.EqualTo(2));
            Assert.That(stock.TotalUnits + bucketContents.TotalUnits, Is.EqualTo(initial));
            bucket.transform.position = Vector3.right * 5f + Vector3.up;
            Assert.That(bucket.GetComponent<GroundMaterialDeposit>().Deposit(MaterialType.Sand, 2), Is.EqualTo(2));
            var pile = Piles()[0];
            shovel.transform.position = pile.transform.position + new Vector3(0f, 0.15f, -0.5f);
            Physics.SyncTransforms();
            Assert.That(ScoopWithoutWarnings(shovel), Is.EqualTo(2));
            Assert.That(shovel.GetComponent<BulkMaterialContainer>().TotalUnits + stock.TotalUnits, Is.EqualTo(initial));
            yield return null;
            yield return null;
            Assert.That(pile == null, Is.True);
        }

        [Test]
        public void ExactNearestTieCancelsScoopAndDumpIncludingGroundFallback()
        {
            Ground();
            var shovel = Shovel();
            var first = Container(new Vector3(-0.3f, 1f, 0.5f), MaterialType.Sand, 10, 7);
            var second = Container(new Vector3(0.3f, 1f, 0.5f), MaterialType.Sand, 10, 7);
            var farther = Container(new Vector3(0f, 1f, 0.9f), MaterialType.Sand, 10);
            Physics.SyncTransforms();
            Assert.That(shovel.RequestScoop(), Is.Zero);
            var contents = shovel.GetComponent<BulkMaterialContainer>();
            contents.AddUnits(MaterialType.Sand, 2);
            Assert.That(shovel.RequestDump(), Is.Zero);
            Assert.That(contents.TotalUnits, Is.EqualTo(2));
            Assert.That(first.TotalUnits + second.TotalUnits, Is.EqualTo(14));
            Assert.That(farther.TotalUnits, Is.Zero);
            Assert.That(Piles().Length, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FullOrRejectingNearestDoesNotFallBack(bool rejecting)
        {
            Ground();
            var shovel = Shovel();
            var contents = shovel.GetComponent<BulkMaterialContainer>();
            contents.AddUnits(MaterialType.Sand, 2);
            var nearest = Container(new Vector3(0f, 1f, 0.4f), rejecting ? MaterialType.Cement : MaterialType.Sand,
                2, rejecting ? 0 : 2);
            var farther = Container(new Vector3(0.4f, 1f, 0.7f), MaterialType.Sand, 10);
            Physics.SyncTransforms();
            Assert.That(shovel.RequestDump(), Is.Zero);
            Assert.That(contents.TotalUnits, Is.EqualTo(2));
            Assert.That(nearest.TotalUnits, Is.EqualTo(rejecting ? 0 : 2));
            Assert.That(farther.TotalUnits, Is.Zero);
            Assert.That(Piles().Length, Is.Zero);
        }

        [Test]
        public void ExecutionRejectsReleasedOrRemoteHolder()
        {
            var shovel = Shovel();
            var source = Container(new Vector3(0f, 1f, 0.5f), MaterialType.Sand, 10, 7);
            Physics.SyncTransforms();
            var grabbable = shovel.GetComponent<GrabbableObject>();
            var movement = grabbable.CurrentHolder.GetComponent<PlayerMovement>();
            movement.SetLocalPlayer(false);
            Assert.That(shovel.ExecuteScoop(), Is.Zero);
            movement.SetLocalPlayer(true);
            Assert.That(shovel.ExecuteScoop(), Is.EqualTo(2));
            grabbable.OnRelease();
            Assert.That(shovel.ExecuteDump(), Is.Zero);
            Assert.That(source.TotalUnits, Is.EqualTo(5));
        }

        [Test]
        public void UseRequestScoopsWhenEmptyAndDumpsWhenLoaded()
        {
            Ground();
            var shovel = Shovel();
            var source = Container(new Vector3(0f, 1f, 0.5f), MaterialType.Sand, 10, 7);
            var input = shovel.GetComponent<ShovelInput>();
            Physics.SyncTransforms();
            Assert.That(input.RequestUse(), Is.EqualTo(2));
            Assert.That(source.TotalUnits, Is.EqualTo(5));
            source.gameObject.SetActive(false);
            Physics.SyncTransforms();
            Assert.That(input.RequestUse(), Is.EqualTo(2));
            Assert.That(shovel.GetComponent<BulkMaterialContainer>().TotalUnits, Is.Zero);
            Assert.That(Piles().Length, Is.EqualTo(1));
            Assert.That(Piles()[0].Container.TotalUnits + source.TotalUnits, Is.EqualTo(7));
        }

        [Test]
        public void MissingGroundAndDynamicSupportKeepAllUnits()
        {
            var shovel = Shovel();
            var contents = shovel.GetComponent<BulkMaterialContainer>();
            contents.AddUnits(MaterialType.Sand, 2);
            Assert.That(shovel.ExecuteDump(), Is.Zero);
            var floor = Ground();
            floor.gameObject.AddComponent<Rigidbody>().isKinematic = true;
            Physics.SyncTransforms();
            Assert.That(shovel.ExecuteDump(), Is.Zero);
            Assert.That(contents.TotalUnits, Is.EqualTo(2));
            Assert.That(Piles().Length, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DepositsMergeImmediatelyGrowPastReferenceAndEmptyPileIsRemoved()
        {
            Ground();
            var shovel = Shovel();
            var contents = shovel.GetComponent<BulkMaterialContainer>();
            contents.AddUnits(MaterialType.Sand, 2);
            var deposit = shovel.GetComponent<GroundMaterialDeposit>();
            Assert.That(deposit.Deposit(MaterialType.Sand, 2), Is.EqualTo(2));
            contents.AddUnits(MaterialType.Sand, 2);
            shovel.transform.position += Vector3.right * 0.2f;
            Assert.That(deposit.Deposit(MaterialType.Sand, 2), Is.EqualTo(2));
            Assert.That(Piles().Length, Is.EqualTo(1));
            var pile = Piles()[0];
            Assert.That(pile.Container.TotalUnits, Is.EqualTo(4));
            Assert.That(pile.Container.Capacity, Is.EqualTo(int.MaxValue));
            Assert.That(pile.GetComponent<Rigidbody>(), Is.Null);
            var mound = pile.GetComponentInChildren<MeshCollider>();
            var before = mound.bounds.size;
            var stock = Container(Vector3.right * 10f, MaterialType.Sand, 200, 150);
            Assert.That(stock.TransferUnitsTo(pile.Container, MaterialType.Sand, 150), Is.EqualTo(150));
            yield return null;
            Physics.SyncTransforms();
            Assert.That(mound.bounds.size.y, Is.GreaterThan(before.y));
            Assert.That(mound.sharedMesh, Is.SameAs(mound.GetComponent<MeshFilter>().sharedMesh));
            Assert.That(pile.Container.TotalUnits + stock.TotalUnits, Is.EqualTo(154));
            Assert.That(pile.Container.TransferUnitsTo(stock, MaterialType.Sand, 154), Is.EqualTo(154));
            yield return null;
            yield return null;
            Assert.That(pile == null, Is.True);
        }

        [Test]
        public void DifferentTypesAndDifferentFloorsNeverMerge()
        {
            Ground();
            var shovel = Shovel();
            var contents = shovel.GetComponent<BulkMaterialContainer>();
            var deposit = shovel.GetComponent<GroundMaterialDeposit>();
            contents.AddUnits(MaterialType.Sand, 2);
            Assert.That(deposit.Deposit(MaterialType.Sand, 2), Is.EqualTo(2));
            contents.ConfigureSingleTypeWhenEmpty(MaterialType.Cement);
            contents.AddUnits(MaterialType.Cement, 2);
            Assert.That(deposit.Deposit(MaterialType.Cement, 2), Is.EqualTo(2));
            Cube(new Vector3(0.2f, 0.3f, 0f), new Vector3(0.1f, 0.1f, 0.1f));
            shovel.transform.position += Vector3.right * 0.2f;
            Physics.SyncTransforms();
            contents.ConfigureSingleTypeWhenEmpty(MaterialType.Sand);
            contents.AddUnits(MaterialType.Sand, 2);
            Assert.That(deposit.Deposit(MaterialType.Sand, 2), Is.EqualTo(2));
            Assert.That(Piles().Length, Is.EqualTo(3));
            var total = 0;
            foreach (var pile in Piles())
                total += pile.Container.TotalUnits;
            Assert.That(total, Is.EqualTo(6));
        }

        [TestCase("Bucket")]
        [TestCase("Shovel")]
        public void PassiveSpillHasExactlyMatchingPileUnits(string name)
        {
            Ground();
            var instance = Prefab(name);
            instance.transform.SetPositionAndRotation(Vector3.up, Quaternion.Euler(90f, 0f, 0f));
            instance.GetComponent<Rigidbody>().isKinematic = true;
            var contents = instance.GetComponent<BulkMaterialContainer>();
            var units = name == "Bucket" ? 12 : 2;
            contents.AddUnits(MaterialType.Sand, units);
            Set(contents, "_spillUnitsPerSecond", 100f);
            Physics.SyncTransforms();
            Invoke(contents, "FixedUpdate");
            Assert.That(contents.TotalUnits, Is.LessThan(units));
            Assert.That(Piles().Length, Is.EqualTo(1));
            Assert.That(contents.TotalUnits + Piles()[0].Container.TotalUnits, Is.EqualTo(units));
            foreach (var pile in Piles())
                Object.DestroyImmediate(pile.gameObject);
            foreach (var item in _objects)
                if (item != null && item.name == "Ground")
                    item.SetActive(false);
            contents.AddUnits(MaterialType.Sand, units - contents.TotalUnits);
            Physics.SyncTransforms();
            Invoke(contents, "FixedUpdate");
            Assert.That(contents.TotalUnits, Is.EqualTo(units));
        }

        [UnityTest]
        public IEnumerator BucketPourToGroundConservesAndStopsImmediatelyAfterRelease()
        {
            Ground();
            var bucket = Prefab("Bucket");
            bucket.transform.position = Vector3.up;
            bucket.GetComponent<Rigidbody>().isKinematic = true;
            bucket.GetComponent<BucketPourInput>().enabled = false;
            bucket.GetComponent<GrabbableObject>().OnGrab(Holder());
            var source = bucket.GetComponent<BulkMaterialContainer>();
            source.AddUnits(MaterialType.Sand, 12);
            var action = bucket.GetComponent<BucketPourAction>();
            Physics.SyncTransforms();
            Assert.That(action.RequestPour(), Is.True);
            yield return new WaitForSeconds(0.35f);
            Assert.That(action.IsPouring, Is.True);
            Assert.That(source.TotalUnits, Is.LessThan(12));
            Assert.That(Piles().Length, Is.EqualTo(1));
            Assert.That(source.TotalUnits + Piles()[0].Container.TotalUnits, Is.EqualTo(12));
            Assert.That(bucket.GetComponentInChildren<ParticleSystem>().emission.enabled, Is.True);
            bucket.GetComponent<GrabbableObject>().OnRelease();
            var units = source.TotalUnits;
            yield return new WaitForFixedUpdate();
            Assert.That(source.TotalUnits, Is.EqualTo(units));
            Assert.That(action.IsPouring, Is.False);
        }

        [Test]
        public void BucketGroundFallbackIsCancelledByTieOrRejectingReceiver()
        {
            Ground();
            var bucket = Prefab("Bucket");
            bucket.GetComponent<Rigidbody>().isKinematic = true;
            bucket.transform.position = Vector3.up;
            bucket.GetComponent<BucketPourInput>().enabled = false;
            bucket.GetComponent<GrabbableObject>().OnGrab(Holder());
            var source = bucket.GetComponent<BulkMaterialContainer>();
            source.AddUnits(MaterialType.Sand, 12);
            var action = bucket.GetComponent<BucketPourAction>();
            var first = Container(new Vector3(-0.5f, 1f, 0f), MaterialType.Sand, 10);
            var second = Container(new Vector3(0.5f, 1f, 0f), MaterialType.Cement, 10);
            action.RegisterNearbyReceiver(first);
            action.RegisterNearbyReceiver(second);
            Physics.SyncTransforms();
            Assert.That(action.RequestPour(), Is.False);
            Invoke(action, "FixedUpdate");
            action.UnregisterNearbyReceiver(first);
            Assert.That(action.RequestPour(), Is.True);
            Invoke(action, "FixedUpdate");
            Assert.That(source.TotalUnits, Is.EqualTo(12));
            Assert.That(Piles().Length, Is.Zero);
        }

        private ShovelAction Shovel()
        {
            var instance = Prefab("Shovel");
            instance.GetComponent<ShovelInput>().enabled = false;
            instance.GetComponent<Rigidbody>().isKinematic = true;
            instance.transform.position = Vector3.up;
            Set(instance.GetComponent<ShovelAction>(), "_blade", instance.transform);
            Set(instance.GetComponent<GroundMaterialDeposit>(), "_outlet", instance.transform);
            instance.GetComponent<GrabbableObject>().OnGrab(Holder());
            return instance.GetComponent<ShovelAction>();
        }

        private GameObject Holder()
        {
            var holder = new GameObject("Local Holder");
            _objects.Add(holder);
            holder.SetActive(false);
            holder.transform.position = Vector3.left * 20f;
            holder.AddComponent<CharacterController>();
            var movement = holder.AddComponent<PlayerMovement>();
            var camera = new GameObject("Camera");
            camera.transform.SetParent(holder.transform, false);
            camera.AddComponent<Camera>();
            movement.SetLocalPlayer(true);
            holder.SetActive(true);
            return holder;
        }

        private GameObject Prefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Material/" + name + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            _objects.Add(instance);
            return instance;
        }

        private BulkMaterialContainer Container(Vector3 position, MaterialType type, int capacity, int units = 0)
        {
            var instance = Cube(position, Vector3.one * 0.12f);
            instance.SetActive(false);
            var container = instance.AddComponent<BulkMaterialContainer>();
            Set(container, "_capacity", capacity);
            Set(container, "_singleType", type);
            instance.SetActive(true);
            container.AddUnits(type, units);
            return container;
        }

        private Collider Ground()
        {
            var instance = Cube(new Vector3(0f, -0.05f, 0f), new Vector3(10f, 0.1f, 10f));
            instance.name = "Ground";
            Physics.SyncTransforms();
            return instance.GetComponent<Collider>();
        }

        private GameObject Cube(Vector3 position, Vector3 scale)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            _objects.Add(instance);
            return instance;
        }

        private static GroundMaterialPile[] Piles() => Object.FindObjectsByType<GroundMaterialPile>(FindObjectsSortMode.None);

        private static void Set(object instance, string field, object value)
        {
            instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
        }

        private static int ScoopWithoutWarnings(ShovelAction shovel)
        {
            var warnings = 0;
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    warnings++;
            };
            Application.logMessageReceived += capture;
            try
            {
                var moved = shovel.RequestScoop();
                Assert.That(warnings, Is.Zero, "Mound queries must not emit physics warnings or errors.");
                return moved;
            }
            finally { Application.logMessageReceived -= capture; }
        }

        private static void Invoke(object instance, string method)
        {
            instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
        }
    }
}
#endif
