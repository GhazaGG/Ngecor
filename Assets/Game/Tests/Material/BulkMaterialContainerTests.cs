using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Ngecor.Material.Tests
{
    public sealed class BulkMaterialContainerTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in _objects)
                Object.DestroyImmediate(gameObject);
            _objects.Clear();
        }

        [Test]
        public void TransferRespectsCapacityAndConservesEachType()
        {
            var source = CreateContainer(20, ContainerMode.SingleType, MaterialType.Sand);
            var target = CreateContainer(8, ContainerMode.MultipleTypes, MaterialType.Sand, MaterialType.Cement);
            source.AddUnits(MaterialType.Sand, 10);
            target.AddUnits(MaterialType.Cement, 3);

            var before = source.TotalUnits + target.TotalUnits;
            Assert.That(source.TransferForSeconds(target, MaterialType.Sand, 1f), Is.EqualTo(5));
            Assert.That(source.GetUnits(MaterialType.Sand), Is.EqualTo(5));
            Assert.That(target.GetUnits(MaterialType.Sand), Is.EqualTo(5));
            Assert.That(target.GetUnits(MaterialType.Cement), Is.EqualTo(3));
            Assert.That(source.TotalUnits + target.TotalUnits, Is.EqualTo(before));
        }

        [Test]
        public void EmptyFullAndRejectedTransfersLeaveBothContainersUnchanged()
        {
            var source = CreateContainer(10, ContainerMode.MultipleTypes, MaterialType.Sand, MaterialType.Cement);
            var sandOnly = CreateContainer(2, ContainerMode.SingleType, MaterialType.Sand);
            source.AddUnits(MaterialType.Cement, 4);

            Assert.That(source.TransferForSeconds(sandOnly, MaterialType.Cement, 1f), Is.Zero);
            Assert.That(source.TransferForSeconds(sandOnly, MaterialType.Sand, 1f), Is.Zero);
            Assert.That(source.TransferForSeconds(null, MaterialType.Cement, 1f), Is.Zero);
            Assert.That(source.TransferForSeconds(source, MaterialType.Cement, 1f), Is.Zero);
            source.AddUnits(MaterialType.Sand, 2);
            sandOnly.AddUnits(MaterialType.Sand, 2);
            Assert.That(source.TransferForSeconds(sandOnly, MaterialType.Sand, 1f), Is.Zero);
            Assert.That(source.GetUnits(MaterialType.Cement), Is.EqualTo(4));
            Assert.That(source.GetUnits(MaterialType.Sand), Is.EqualTo(2));
            Assert.That(sandOnly.GetUnits(MaterialType.Sand), Is.EqualTo(2));
        }

        [Test]
        public void InspectorContentsStayWithinCapacityAndMode()
        {
            var container = CreateContainer(10, ContainerMode.SingleType, MaterialType.Sand);
            SetField(container, "_contents", new List<MaterialAmount>
            {
                new MaterialAmount(MaterialType.Sand, 12),
                new MaterialAmount(MaterialType.Cement, 3)
            });

            InvokePrivate(container, "OnValidate");

            Assert.That(container.TotalUnits, Is.EqualTo(10));
            Assert.That(container.GetUnits(MaterialType.Sand), Is.EqualTo(10));
            Assert.That(container.GetUnits(MaterialType.Cement), Is.Zero);
        }

        [Test]
        public void RateCarriesFractionalTimeWithoutLosingUnits()
        {
            var source = CreateContainer(10, ContainerMode.SingleType, MaterialType.Sand);
            var target = CreateContainer(2, ContainerMode.SingleType, MaterialType.Sand);
            var nextTarget = CreateContainer(10, ContainerMode.SingleType, MaterialType.Sand);
            source.AddUnits(MaterialType.Sand, 5);
            SetField(source, "_transferUnitsPerSecond", 2f);

            Assert.That(source.TransferForSeconds(target, MaterialType.Sand, 0.25f), Is.Zero);
            Assert.That(source.TransferForSeconds(target, MaterialType.Sand, 0.25f), Is.EqualTo(1));
            Assert.That(source.TransferForSeconds(target, MaterialType.Sand, 0.5f), Is.EqualTo(1));
            Assert.That(source.TransferForSeconds(target, MaterialType.Sand, 1f), Is.Zero);
            Assert.That(source.TransferForSeconds(nextTarget, MaterialType.Sand, 1f), Is.EqualTo(2));
            Assert.That(source.TransferForSeconds(nextTarget, MaterialType.Sand, 1f), Is.EqualTo(1));
            Assert.That(source.TransferForSeconds(nextTarget, MaterialType.Sand, 1f), Is.Zero);
            Assert.That(source.GetUnits(MaterialType.Sand) + target.GetUnits(MaterialType.Sand)
                + nextTarget.GetUnits(MaterialType.Sand), Is.EqualTo(5));
        }

        [Test]
        public void FillVisualTracksTotalUnits()
        {
            var container = CreateContainer(10, ContainerMode.SingleType, MaterialType.Sand);
            var fill = new GameObject("Fill").transform;
            fill.SetParent(container.transform);
            fill.localScale = new Vector3(1f, 2f, 1f);
            SetField(container, "_fillVisual", fill);

            container.AddUnits(MaterialType.Sand, 5);
            Assert.That(fill.localScale.y, Is.EqualTo(1f).Within(0.001f));
            container.RemoveUnits(MaterialType.Sand, 5);
            Assert.That(fill.localScale.y, Is.Zero.Within(0.001f));
        }

        [Test]
        public void TiltSpillsOnlyFromContainerWithRigidbody()
        {
            var bodyObject = new GameObject("Container With Rigidbody");
            _objects.Add(bodyObject);
            bodyObject.AddComponent<Rigidbody>().isKinematic = true;
            var withBody = bodyObject.AddComponent<BulkMaterialContainer>();
            var withoutBody = CreateContainer(20, ContainerMode.SingleType, MaterialType.Sand);
            SetField(withBody, "_spillUnitsPerSecond", 100f);
            SetField(withoutBody, "_spillUnitsPerSecond", 100f);
            InvokePrivate(withBody, "Awake");
            InvokePrivate(withoutBody, "Awake");
            withBody.AddUnits(MaterialType.Sand, 10);
            withoutBody.AddUnits(MaterialType.Sand, 10);
            InvokePrivate(withBody, "FixedUpdate");
            Assert.That(withBody.TotalUnits, Is.EqualTo(10));
            withBody.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            withoutBody.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            InvokePrivate(withBody, "FixedUpdate");
            InvokePrivate(withoutBody, "FixedUpdate");

            var spilled = (int)System.Math.Min(10, System.Math.Floor(100d * Time.fixedDeltaTime));
            Assert.That(withBody.TotalUnits, Is.EqualTo(10 - spilled));
            Assert.That(withoutBody.TotalUnits, Is.EqualTo(10));
        }

        private BulkMaterialContainer CreateContainer(int capacity, ContainerMode mode, params MaterialType[] accepted)
        {
            var gameObject = new GameObject("Container");
            _objects.Add(gameObject);
            var container = gameObject.AddComponent<BulkMaterialContainer>();
            SetField(container, "_capacity", capacity);
            SetField(container, "_mode", mode);
            SetField(container, "_singleType", accepted[0]);
            SetField(container, "_acceptedTypes", new List<MaterialType>(accepted));
            return container;
        }

        private static void SetField<T>(BulkMaterialContainer container, string name, T value)
        {
            typeof(BulkMaterialContainer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(container, value);
        }

        private static void InvokePrivate(BulkMaterialContainer container, string name)
        {
            typeof(BulkMaterialContainer).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(container, null);
        }
    }
}
