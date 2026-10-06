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

        // AC #15: dijatuhkan dari 1,5 m, memantul tidak lebih dari 0,1 m dan diam dalam sekitar 1 detik.
        [UnityTest]
        public IEnumerator DroppedFromOneAndHalfMetersBarelyBouncesAndSettles()
        {
            var bag = Spawn(new Vector3(0f, 1.5f, 0f), Quaternion.Euler(10f, 30f, 15f));
            float previousVy = 0f;
            float landingY = 0f;
            float maxYAfterLanding = float.MinValue;
            int landingStep = -1;
            int lastMovingStep = -1;

            // Amati 3 detik penuh: bag bisa diam sesaat saat menyentuh lantai lalu terguling lagi.
            for (int i = 0; i < 150; i++)
            {
                yield return new WaitForFixedUpdate();
                float vy = bag.linearVelocity.y;
                if (landingStep < 0 && previousVy < -1f && vy - previousVy > 1f)
                {
                    landingStep = i;
                    landingY = bag.position.y;
                }
                previousVy = vy;

                if (landingStep < 0)
                    continue;
                maxYAfterLanding = Mathf.Max(maxYAfterLanding, bag.position.y);
                if (bag.linearVelocity.magnitude >= 0.05f || bag.angularVelocity.magnitude >= 0.1f)
                    lastMovingStep = i;
            }

            Assert.That(landingStep, Is.GreaterThanOrEqualTo(0), "bag never landed");
            float bounce = maxYAfterLanding - landingY;
            float settleSeconds = (lastMovingStep + 1 - landingStep) * Time.fixedDeltaTime;
            TestContext.WriteLine($"bounce={bounce:F3} m, settle={settleSeconds:F2} s");
            Assert.That(bounce, Is.LessThanOrEqualTo(0.1f), "bag bounced more than 0.1 m");
            Assert.That(settleSeconds, Is.LessThanOrEqualTo(1.5f), "bag took too long to settle");
        }

        // AC #15: bag bisa jatuh ke ramp dan berhenti secara wajar, tidak memantul seperti bola.
        [UnityTest]
        public IEnumerator ThreeBagsDroppedOnRampComeToRest()
        {
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(ramp);
            ramp.transform.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.Euler(0f, 0f, 20f));
            ramp.transform.localScale = new Vector3(4f, 0.2f, 2f);

            var bags = new List<Rigidbody>();
            for (int i = 0; i < 3; i++)
                bags.Add(Spawn(new Vector3(-1f + i, 2.3f + i * 0.35f, 0f), Quaternion.Euler(0f, 0f, 20f)));

            var maxUpward = new float[bags.Count];
            for (int step = 0; step < 300; step++)
            {
                yield return new WaitForFixedUpdate();
                for (int i = 0; i < bags.Count; i++)
                {
                    // Abaikan fase jatuh bebas di awal; ukur kecepatan naik setelah menyentuh permukaan.
                    if (step > 20)
                        maxUpward[i] = Mathf.Max(maxUpward[i], bags[i].linearVelocity.y);
                }
            }

            for (int i = 0; i < bags.Count; i++)
            {
                TestContext.WriteLine($"bag {i}: pos={bags[i].position}, maxUpward={maxUpward[i]:F2}");
                Assert.That(maxUpward[i], Is.LessThan(0.5f), $"bag {i} bounced");
                Assert.That(bags[i].linearVelocity.magnitude, Is.LessThan(0.05f), $"bag {i} still moving");
                Assert.That(bags[i].position.y, Is.GreaterThan(0f), $"bag {i} fell through");
            }
        }

        [UnityTest]
        public IEnumerator UprightBagIsBottomHeavy()
        {
            var bag = Spawn(new Vector3(0f, 0.4f, 0f), Quaternion.Euler(0f, 0f, 90f));
            yield return new WaitForFixedUpdate();

            // Pergeseran = _maxFillShift.x (0.4) × setengah panjang di world (0.65 / 2) ≈ 0.13 m.
            // Rigidbody.centerOfMass tidak ikut di-scale transform; menghitung tanpa skala akan memberi 0.2 m.
            float drop = bag.position.y - bag.worldCenterOfMass.y;
            Assert.That(drop, Is.InRange(0.1f, 0.16f));
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
