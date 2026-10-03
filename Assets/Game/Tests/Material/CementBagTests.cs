using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ngecor.Material.Tests
{
    public sealed class CementBagTests
    {
        private const string PrefabPath = "Assets/Game/Prefabs/Material/CementBag.prefab";

        private readonly List<GameObject> _objects = new List<GameObject>();
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
#if UNITY_EDITOR
            _prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#endif
            Assert.That(_prefab, Is.Not.Null, PrefabPath);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            _objects.Add(ground);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in _objects)
                Object.Destroy(gameObject);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator StackOfFiveSettlesAndSleeps()
        {
            float thickness = _prefab.transform.localScale.y;
            var bags = new List<Rigidbody>();
            for (int i = 0; i < 5; i++)
                bags.Add(Spawn(new Vector3(0f, thickness * (0.5f + i) + 0.01f * i, 0f), Quaternion.identity));
            var topStart = bags[4].position;

            yield return Steps(600);

            var drift = bags[4].position - topStart;
            drift.y = 0f;
            Assert.That(drift.magnitude, Is.LessThan(0.1f), "top bag slid off the stack");
            foreach (var bag in bags)
                Assert.That(bag.IsSleeping(), Is.True, $"{bag.name} still awake");
        }

        [UnityTest]
        public IEnumerator DroppedBagDoesNotBounce()
        {
            var bag = Spawn(new Vector3(0f, 2f, 0f), Quaternion.Euler(10f, 30f, 15f));
            bool landed = false;
            float maxUpward = 0f;

            for (int i = 0; i < 400; i++)
            {
                yield return new WaitForFixedUpdate();
                if (bag.position.y < 0.4f)
                    landed = true;
                if (landed)
                    maxUpward = Mathf.Max(maxUpward, bag.linearVelocity.y);
            }

            Assert.That(landed, Is.True);
            Assert.That(maxUpward, Is.LessThan(0.5f), "bag bounced");
            Assert.That(bag.linearVelocity.magnitude, Is.LessThan(0.05f), "bag did not come to rest");
        }

        [UnityTest]
        public IEnumerator UprightBagIsBottomHeavy()
        {
            var bag = Spawn(new Vector3(0f, 0.4f, 0f), Quaternion.Euler(0f, 0f, 90f));
            yield return new WaitForFixedUpdate();

            Assert.That(bag.worldCenterOfMass.y, Is.LessThan(bag.position.y - 0.05f));
        }

        [UnityTest]
        public IEnumerator OverlappingObjectDoesNotLaunchBag()
        {
            var bag = Spawn(new Vector3(0f, _prefab.transform.localScale.y * 0.5f, 0f), Quaternion.identity);
            yield return Steps(50);

            // Seperti objek yang digeser lewat gizmo/teleport: tiba-tiba menembus sisi bag.
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(block);
            block.transform.position = new Vector3(_prefab.transform.localScale.x * 0.5f + 0.4f, 0.5f, 0f);
            // Memindah collider statis tidak membangunkan bag yang sleep; objek ber-Rigidbody yang digeser akan membangunkannya.
            bag.WakeUp();
            float maxSpeed = 0f;
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
                maxSpeed = Mathf.Max(maxSpeed, bag.linearVelocity.magnitude);
            }

            // Terukur: ±0.1 m/s dengan batas 0.5 m/s, ±0.6 m/s dengan default proyek 10 m/s.
            Assert.That(maxSpeed, Is.LessThan(0.3f), "bag pushed out too fast by depenetration");
        }

        [UnityTest]
        public IEnumerator MovingBodyCarriesBagOnImpact()
        {
            var bag = Spawn(new Vector3(0f, _prefab.transform.localScale.y * 0.5f, 0f), Quaternion.identity);
            yield return Steps(50);

            // Penabrak melayang (tanpa gravitasi/gesekan lantai) dengan massa sama: tumbukan inelastis
            // seharusnya membuat keduanya bergerak ±2 m/s. Jika bag diperlakukan seperti tembok, ±1 m/s.
            var hitter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(hitter);
            hitter.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            hitter.transform.position = new Vector3(-1f, bag.position.y, 0f);
            var hitterBody = hitter.AddComponent<Rigidbody>();
            hitterBody.useGravity = false;
            hitterBody.mass = bag.mass;
            hitterBody.linearVelocity = new Vector3(4f, 0f, 0f);

            float maxBagSpeed = 0f;
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
                maxBagSpeed = Mathf.Max(maxBagSpeed, bag.linearVelocity.x);
            }

            Assert.That(maxBagSpeed, Is.GreaterThan(1.5f), "bag behaved like a wall when hit by a moving body");
        }

        private Rigidbody Spawn(Vector3 position, Quaternion rotation)
        {
            var bag = Object.Instantiate(_prefab, position, rotation);
            _objects.Add(bag);
            return bag.GetComponent<Rigidbody>();
        }

        private static IEnumerator Steps(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }
    }
}
