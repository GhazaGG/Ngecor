using NUnit.Framework;
using UnityEngine;

namespace Ngecor.Multiplayer.Tests
{
    [TestFixture]
    public class NetworkPlayerPushTests
    {
        private const float Reach = 1.5f;

        [Test]
        public void IsPushRequestValid_ContactBesidePlayer_ReturnsTrue()
        {
            Assert.That(NetworkPlayerPush.IsPushRequestValid(
                Vector3.zero, new Vector3(0f, 0.9f, 0.6f), Vector3.forward, Reach), Is.True);
        }

        [Test]
        public void IsPushRequestValid_ContactBeyondReach_ReturnsFalse()
        {
            Assert.That(NetworkPlayerPush.IsPushRequestValid(
                Vector3.zero, new Vector3(0f, 0.9f, 3f), Vector3.forward, Reach), Is.False);
        }

        [Test]
        public void IsPushRequestValid_ContactAboveHead_ReturnsFalse()
        {
            Assert.That(NetworkPlayerPush.IsPushRequestValid(
                Vector3.zero, new Vector3(0f, 4f, 0.5f), Vector3.forward, Reach), Is.False);
        }

        [Test]
        public void IsPushRequestValid_VerticalDirection_ReturnsFalse()
        {
            Assert.That(NetworkPlayerPush.IsPushRequestValid(
                Vector3.zero, new Vector3(0f, 0.9f, 0.6f), Vector3.down, Reach), Is.False);
        }

        [Test]
        public void IsPushRequestValid_NonFiniteValues_ReturnsFalse()
        {
            Assert.That(NetworkPlayerPush.IsPushRequestValid(
                Vector3.zero, new Vector3(float.NaN, 0.9f, 0.6f), Vector3.forward, Reach), Is.False);
            Assert.That(NetworkPlayerPush.IsPushRequestValid(
                Vector3.zero, new Vector3(0f, 0.9f, 0.6f), new Vector3(float.PositiveInfinity, 0f, 0f), Reach), Is.False);
        }

        [Test]
        public void TryGetPushTarget_BodyWithoutNetworkObject_ReturnsFalse()
        {
            // Another player's RemoteProxy is exactly this: a kinematic Rigidbody without its own NetworkObject.
            var proxy = new GameObject("RemoteProxy");
            try
            {
                var body = proxy.AddComponent<Rigidbody>();
                body.isKinematic = true;
                Assert.That(NetworkPlayerPush.TryGetPushTarget(body, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(proxy);
            }
        }
    }
}
