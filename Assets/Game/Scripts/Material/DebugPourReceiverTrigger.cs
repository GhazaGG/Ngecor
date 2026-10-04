using System.Collections.Generic;
using UnityEngine;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DebugPourReceiverTrigger : MonoBehaviour
    {
        private readonly Dictionary<Collider, BucketPourAction> _insideColliders = new Dictionary<Collider, BucketPourAction>();
        private readonly Dictionary<BucketPourAction, int> _insideBuckets = new Dictionary<BucketPourAction, int>();

        private BulkMaterialContainer _receiver;

        private void Awake()
        {
            _receiver = GetComponentInParent<BulkMaterialContainer>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_receiver == null || other == null || _insideColliders.ContainsKey(other))
                return;

            var pourAction = other.GetComponentInParent<BucketPourAction>();
            if (pourAction == null)
                return;

            _insideColliders.Add(other, pourAction);
            if (_insideBuckets.TryGetValue(pourAction, out var colliderCount))
            {
                _insideBuckets[pourAction] = colliderCount + 1;
                return;
            }

            _insideBuckets.Add(pourAction, 1);
            pourAction.RegisterNearbyReceiver(_receiver);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null || !_insideColliders.TryGetValue(other, out var pourAction))
                return;

            _insideColliders.Remove(other);
            if (!_insideBuckets.TryGetValue(pourAction, out var colliderCount))
                return;

            if (colliderCount > 1)
            {
                _insideBuckets[pourAction] = colliderCount - 1;
                return;
            }

            _insideBuckets.Remove(pourAction);
            if (pourAction != null)
                pourAction.UnregisterNearbyReceiver(_receiver);
        }

        private void OnDisable()
        {
            foreach (var pourAction in _insideBuckets.Keys)
            {
                if (pourAction != null)
                    pourAction.UnregisterNearbyReceiver(_receiver);
            }

            _insideColliders.Clear();
            _insideBuckets.Clear();
        }
    }
}
