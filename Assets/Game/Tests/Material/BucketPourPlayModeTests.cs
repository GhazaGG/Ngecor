using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Ngecor.Interaction;
using Ngecor.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ngecor.Material.Tests
{
    public sealed class BucketPourPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var gameObject in _objects)
            {
                if (gameObject != null)
                    Object.Destroy(gameObject);
            }

            yield return null;
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator ReceiverTriggerWaitsForPourIntentThenConservesUnits()
        {
            var source = CreateBucket();
            var receiver = CreateReceiver(MaterialType.Sand);

            yield return new WaitForFixedUpdate();
            Assert.That(source.GetUnits(MaterialType.Sand), Is.EqualTo(10));
            Assert.That(source.GetComponent<BucketPourAction>().RequestPour(), Is.True);

            yield return new WaitForSeconds(0.6f);

            Assert.That(source.GetUnits(MaterialType.Sand), Is.LessThan(10));
            Assert.That(receiver.GetUnits(MaterialType.Sand), Is.GreaterThan(0));
            Assert.That(source.TotalUnits + receiver.TotalUnits, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator ReceiverTriggerStaysRegisteredUntilAllBucketCollidersExit()
        {
            var source = CreateBucket();
            var receiver = CreateReceiver(MaterialType.Sand);
            var sideCollider = new GameObject("Side Collider");
            sideCollider.transform.SetParent(source.transform, false);
            sideCollider.transform.localPosition = Vector3.left * 0.8f;
            sideCollider.AddComponent<BoxCollider>();

            yield return new WaitForFixedUpdate();
            source.transform.position = Vector3.right * 1.6f;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(source.GetComponent<BucketPourAction>().RequestPour(), Is.True);
            yield return new WaitForSeconds(0.6f);

            Assert.That(source.GetUnits(MaterialType.Sand), Is.LessThan(10));
            Assert.That(receiver.GetUnits(MaterialType.Sand), Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator ReceiverTriggerRejectsSandWithoutLosingUnits()
        {
            var source = CreateBucket();
            var receiver = CreateReceiver(MaterialType.Cement);

            yield return new WaitForFixedUpdate();
            Assert.That(source.GetComponent<BucketPourAction>().RequestPour(), Is.True);
            yield return new WaitForSeconds(0.6f);

            Assert.That(source.GetUnits(MaterialType.Sand), Is.EqualTo(10));
            Assert.That(receiver.TotalUnits, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PourIntentRequiresNearbyReceiver()
        {
            var source = CreateBucket();
            var receiver = CreateReceiver(MaterialType.Sand);
            receiver.transform.position = Vector3.right * 10f;

            Assert.That(source.GetComponent<BucketPourAction>().RequestPour(), Is.False);
            yield return new WaitForSeconds(0.4f);

            Assert.That(source.GetUnits(MaterialType.Sand), Is.EqualTo(10));
            Assert.That(receiver.TotalUnits, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PourInputRequiresCarriedBucketAndStopsWhenReleased()
        {
            var source = CreateBucket();
            var receiver = CreateReceiver(MaterialType.Sand);
            receiver.transform.Find("Pour Trigger").GetComponent<BoxCollider>().size = Vector3.one * 4f;
            var grabbable = source.gameObject.AddComponent<GrabbableObject>();
            var pourInput = source.gameObject.AddComponent<BucketPourInput>();
            pourInput.enabled = false;
            var player = CreateLocalPlayer();
            Assert.That(pourInput.RequestPour(), Is.False);

            Assert.That(player.GetComponent<PlayerGrab>().ExecuteGrab(grabbable), Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(pourInput.RequestPour(), Is.True);
            yield return new WaitForSeconds(0.6f);

            Assert.That(source.GetUnits(MaterialType.Sand), Is.LessThan(10));
            Assert.That(receiver.GetUnits(MaterialType.Sand), Is.GreaterThan(0));
            Assert.That(source.TotalUnits + receiver.TotalUnits, Is.EqualTo(10));

            pourInput.CancelPour();
            yield return new WaitForSeconds(0.2f);
            var unitsAfterRelease = receiver.TotalUnits;
            yield return new WaitForSeconds(0.3f);
            Assert.That(receiver.TotalUnits, Is.EqualTo(unitsAfterRelease));
        }

        [UnityTest]
        public IEnumerator TiltedDynamicBucketSpillsThroughContainerRule()
        {
            var source = CreateBucket();
            var body = source.GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezePosition;
            source.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            yield return new WaitForSeconds(0.4f);

            Assert.That(source.GetUnits(MaterialType.Sand), Is.LessThan(10));
        }

        private BulkMaterialContainer CreateBucket()
        {
            var gameObject = new GameObject("Bucket");
            gameObject.SetActive(false);
            _objects.Add(gameObject);

            var body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeAll;
            gameObject.AddComponent<BoxCollider>();

            var container = gameObject.AddComponent<BulkMaterialContainer>();
            SetField(container, "_capacity", 20);
            SetField(container, "_mode", ContainerMode.SingleType);
            SetField(container, "_singleType", MaterialType.Sand);
            SetField(container, "_acceptedTypes", new List<MaterialType> { MaterialType.Sand });
            SetField(container, "_contents", new List<MaterialAmount> { new MaterialAmount(MaterialType.Sand, 10) });
            SetField(container, "_transferUnitsPerSecond", 10f);
            SetField(container, "_spillUnitsPerSecond", 20f);
            gameObject.AddComponent<BucketPourAction>();
            gameObject.SetActive(true);
            return container;
        }

        private BulkMaterialContainer CreateReceiver(MaterialType acceptedType)
        {
            var gameObject = new GameObject("Receiver");
            gameObject.SetActive(false);
            _objects.Add(gameObject);

            var container = gameObject.AddComponent<BulkMaterialContainer>();
            SetField(container, "_capacity", 20);
            SetField(container, "_mode", ContainerMode.SingleType);
            SetField(container, "_singleType", acceptedType);
            SetField(container, "_acceptedTypes", new List<MaterialType> { acceptedType });
            SetField(container, "_contents", new List<MaterialAmount>());
            SetField(container, "_spillUnitsPerSecond", 0f);

            var trigger = new GameObject("Pour Trigger");
            trigger.transform.SetParent(gameObject.transform, false);
            var collider = trigger.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one * 2f;
            trigger.AddComponent<DebugPourReceiverTrigger>();
            gameObject.SetActive(true);
            return container;
        }

        private GameObject CreateLocalPlayer()
        {
            var player = new GameObject("Player");
            player.SetActive(false);
            _objects.Add(player);
            player.AddComponent<CharacterController>();
            var movement = player.AddComponent<PlayerMovement>();
            var camera = new GameObject("Camera");
            camera.transform.SetParent(player.transform, false);
            camera.AddComponent<Camera>();
            movement.SetLocalPlayer(true);
            player.AddComponent<InteractionDetector>();
            player.AddComponent<PlayerGrab>();
            player.SetActive(true);
            return player;
        }

        private static void SetField<T>(BulkMaterialContainer container, string name, T value)
        {
            typeof(BulkMaterialContainer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(container, value);
        }
    }
}
