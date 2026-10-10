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
        public IEnumerator NearestReceiverWinsWithEitherRegistrationOrderAndFartherTie()
        {
            var source = CreateBucket();
            var nearest = CreateReceiver(MaterialType.Sand);
            var farther = CreateReceiver(MaterialType.Sand);
            var otherFarther = CreateReceiver(MaterialType.Sand);
            nearest.transform.position = Vector3.right * 0.25f;
            farther.transform.position = Vector3.right * 0.5f;
            otherFarther.transform.position = Vector3.left * 0.5f;
            var action = source.GetComponent<BucketPourAction>();

            for (var order = 0; order < 2; order++)
            {
                action.UnregisterNearbyReceiver(nearest);
                action.UnregisterNearbyReceiver(farther);
                action.UnregisterNearbyReceiver(otherFarther);
                action.RegisterNearbyReceiver(order == 0 ? farther : nearest);
                action.RegisterNearbyReceiver(otherFarther);
                action.RegisterNearbyReceiver(order == 0 ? nearest : farther);
                var previousUnits = nearest.TotalUnits;
                Assert.That(action.RequestPour(), Is.True);
                Assert.That(action.PourReceiver, Is.SameAs(nearest));
                yield return new WaitForSeconds(0.2f);

                Assert.That(nearest.TotalUnits, Is.GreaterThan(previousUnits));
                Assert.That(farther.TotalUnits, Is.Zero);
                Assert.That(otherFarther.TotalUnits, Is.Zero);
                Assert.That(source.TotalUnits + nearest.TotalUnits, Is.EqualTo(10));
            }

            // Very close unequal distances must not be treated as an exact tie.
            nearest.transform.position = Vector3.right * 0.49999997f;
            Assert.That(action.RequestPour(), Is.True);
            Assert.That(action.PourReceiver, Is.SameAs(nearest));
        }

        [UnityTest]
        public IEnumerator TiedNearestReceiversCancelTransferAndFeedbackEvenWithFartherReceiver()
        {
            var source = CreateBucket();
            var first = CreateReceiver(MaterialType.Sand);
            first.transform.position = Vector3.left * 0.5f;
            var action = source.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(first);
            Assert.That(action.RequestPour(), Is.True);
            yield return new WaitForSeconds(0.2f);
            Assert.That(action.IsPouring, Is.True);

            var second = CreateReceiver(MaterialType.Sand);
            second.transform.position = Vector3.right * 0.5f;
            action.RegisterNearbyReceiver(second);
            Assert.That(action.RequestPour(), Is.False);
            Assert.That(action.PourReceiver, Is.Null);
            Assert.That(action.IsPouring, Is.False);

            var farther = CreateReceiver(MaterialType.Sand);
            farther.transform.position = Vector3.right;
            action.RegisterNearbyReceiver(farther);
            var sourceUnits = source.TotalUnits;
            var firstUnits = first.TotalUnits;
            Assert.That(action.RequestPour(), Is.False);
            yield return new WaitForSeconds(0.3f);

            Assert.That(action.PourReceiver, Is.Null);
            Assert.That(action.IsPouring, Is.False);
            Assert.That(source.TotalUnits, Is.EqualTo(sourceUnits));
            Assert.That(first.TotalUnits, Is.EqualTo(firstUnits));
            Assert.That(second.TotalUnits, Is.Zero);
            Assert.That(farther.TotalUnits, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FullNearestReceiverDoesNotFallBackOrRemoveSourceUnits()
        {
            var source = CreateBucket();
            var nearest = CreateReceiver(MaterialType.Sand);
            nearest.transform.position = Vector3.right * 0.5f;
            nearest.AddUnits(MaterialType.Sand, nearest.Capacity);
            var farther = CreateReceiver(MaterialType.Sand);
            farther.transform.position = Vector3.right;
            var action = source.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(farther);
            action.RegisterNearbyReceiver(nearest);
            Assert.That(action.RequestPour(), Is.True);
            Assert.That(action.PourReceiver, Is.SameAs(nearest));
            yield return new WaitForSeconds(0.3f);

            Assert.That(source.TotalUnits, Is.EqualTo(10));
            Assert.That(nearest.TotalUnits, Is.EqualTo(nearest.Capacity));
            Assert.That(farther.TotalUnits, Is.Zero);
            Assert.That(action.IsPouring, Is.False);
        }

        [UnityTest]
        public IEnumerator RejectingNearestReceiverDoesNotFallBackOrRemoveSourceUnits()
        {
            var source = CreateBucket();
            var nearest = CreateReceiver(MaterialType.Cement);
            nearest.transform.position = Vector3.right * 0.5f;
            var farther = CreateReceiver(MaterialType.Sand);
            farther.transform.position = Vector3.right;
            var action = source.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(farther);
            action.RegisterNearbyReceiver(nearest);
            Assert.That(action.RequestPour(), Is.True);
            Assert.That(action.PourReceiver, Is.SameAs(nearest));
            yield return new WaitForSeconds(0.3f);

            Assert.That(source.TotalUnits, Is.EqualTo(10));
            Assert.That(nearest.TotalUnits, Is.Zero);
            Assert.That(farther.TotalUnits, Is.Zero);
            Assert.That(action.IsPouring, Is.False);
        }

        [UnityTest]
        public IEnumerator MovingBucketSelectsNewTargetAndUnregisterStopsTransfer()
        {
            var source = CreateBucket();
            source.transform.position = Vector3.left * 0.25f;
            var first = CreateReceiver(MaterialType.Sand);
            first.transform.position = Vector3.left * 0.5f;
            var second = CreateReceiver(MaterialType.Sand);
            second.transform.position = Vector3.right * 0.5f;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            var action = source.GetComponent<BucketPourAction>();
            Assert.That(action.RequestPour(), Is.True);
            Assert.That(action.PourReceiver, Is.SameAs(first));
            yield return new WaitForSeconds(0.2f);
            Assert.That(first.TotalUnits, Is.GreaterThan(0));
            Assert.That(second.TotalUnits, Is.Zero);
            Assert.That(action.IsPouring, Is.True);

            source.transform.position = Vector3.right * 0.25f;
            Physics.SyncTransforms();
            Assert.That(action.RequestPour(), Is.True);
            Assert.That(action.PourReceiver, Is.SameAs(second));
            Assert.That(action.IsPouring, Is.False);
            var firstUnits = first.TotalUnits;
            yield return new WaitForSeconds(0.2f);
            Assert.That(first.TotalUnits, Is.EqualTo(firstUnits));
            Assert.That(second.TotalUnits, Is.GreaterThan(0));
            Assert.That(source.TotalUnits + first.TotalUnits + second.TotalUnits, Is.EqualTo(10));

            action.UnregisterNearbyReceiver(second);
            Assert.That(action.PourReceiver, Is.Null);
            Assert.That(action.IsPouring, Is.False);
            var sourceUnits = source.TotalUnits;
            var secondUnits = second.TotalUnits;
            yield return new WaitForSeconds(0.3f);
            Assert.That(source.TotalUnits, Is.EqualTo(sourceUnits));
            Assert.That(first.TotalUnits, Is.EqualTo(firstUnits));
            Assert.That(second.TotalUnits, Is.EqualTo(secondUnits));
        }

        [UnityTest]
        public IEnumerator NullDestroyedAndSourceReceiversAreIgnored()
        {
            var source = CreateBucket();
            var destroyed = CreateReceiver(MaterialType.Sand);
            var receiver = CreateReceiver(MaterialType.Sand);
            receiver.transform.position = Vector3.right;
            var action = source.GetComponent<BucketPourAction>();
            action.RegisterNearbyReceiver(null);
            action.RegisterNearbyReceiver(source);
            action.RegisterNearbyReceiver(destroyed);
            action.RegisterNearbyReceiver(receiver);
            // Keep the trigger alive so it cannot unregister the destroyed component.
            Object.Destroy(destroyed);
            yield return null;
            Assert.That(destroyed == null, Is.True);

            Assert.That(action.RequestPour(), Is.True);
            Assert.That(action.PourReceiver, Is.SameAs(receiver));
            yield return new WaitForSeconds(0.2f);
            Assert.That(receiver.TotalUnits, Is.GreaterThan(0));
            Assert.That(source.TotalUnits + receiver.TotalUnits, Is.EqualTo(10));
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
        public IEnumerator DroppingHeldBucketBeforeNextPhysicsTickCancelsPendingPour()
        {
            var source = CreateBucket(withGrabbable: true);
            var receiver = CreateReceiver(MaterialType.Sand);
            receiver.transform.Find("Pour Trigger").GetComponent<BoxCollider>().size = Vector3.one * 4f;
            yield return new WaitForFixedUpdate();

            var player = CreateLocalPlayer();
            var pourInput = source.gameObject.AddComponent<BucketPourInput>();
            pourInput.enabled = false;
            var grabbable = source.GetComponent<GrabbableObject>();
            Assert.That(player.GetComponent<PlayerGrab>().ExecuteGrab(grabbable), Is.True);
            Assert.That(pourInput.RequestPour(), Is.True);

            var creditSeconds = 0.1f - Time.fixedDeltaTime * 0.5f;
            Assert.That(source.TransferForSeconds(receiver, MaterialType.Sand, creditSeconds), Is.Zero);
            Assert.That(player.GetComponent<PlayerGrab>().ExecuteDrop(), Is.True);
            Assert.That(grabbable.IsHeld, Is.False);

            yield return new WaitForFixedUpdate();

            var action = source.GetComponent<BucketPourAction>();
            Assert.That(source.TotalUnits, Is.EqualTo(10));
            Assert.That(receiver.TotalUnits, Is.Zero);
            Assert.That(action.PourReceiver, Is.Null);
            Assert.That(action.IsPouring, Is.False);
        }

        [UnityTest]
        public IEnumerator TiltedDynamicBucketWithoutDepositRetainsUnits()
        {
            var source = CreateBucket();
            var body = source.GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezePosition;
            source.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            yield return new WaitForSeconds(0.4f);

            Assert.That(source.GetUnits(MaterialType.Sand), Is.EqualTo(10));
        }

        private BulkMaterialContainer CreateBucket(bool withGrabbable = false)
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
            if (withGrabbable)
                gameObject.AddComponent<GrabbableObject>();
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
