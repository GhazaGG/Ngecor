using UnityEngine;
using System.Collections.Generic;
using Ngecor.Interaction;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BulkMaterialContainer))]
    public sealed class BucketPourAction : MonoBehaviour
    {
        [SerializeField] private MaterialType _materialType = MaterialType.Sand;

        private BulkMaterialContainer _source;
        private BulkMaterialContainer _requestedReceiver;
        private GrabbableObject _grabbable;
        private GroundMaterialDeposit _ground;
        private bool _groundRequested;
        private float _flowUntil = float.NegativeInfinity;
        private readonly HashSet<BulkMaterialContainer> _nearbyReceivers = new HashSet<BulkMaterialContainer>();

        public BulkMaterialContainer PourReceiver => _requestedReceiver;
        public Vector3 PourDestination => _requestedReceiver != null
            ? _requestedReceiver.transform.position : _ground != null ? _ground.DepositPoint : transform.position;
        // Bridge integer transfer ticks and show the final unit for a short time.
        public bool IsPouring => (_requestedReceiver != null || _groundRequested) && Time.time < _flowUntil;

        private void Awake()
        {
            _source = GetComponent<BulkMaterialContainer>();
            _grabbable = GetComponent<GrabbableObject>();
            _ground = GetComponent<GroundMaterialDeposit>();
        }

        public bool RequestPour()
        {
            var receiver = GetNearbyReceiver(out var tied);
            var ground = receiver == null && !tied && _ground != null && _ground.TryGetSurface(out _);
            if (receiver == null && !ground)
            {
                CancelPour();
                return false;
            }

            if (_requestedReceiver != receiver || _groundRequested != ground)
            {
                _flowUntil = float.NegativeInfinity;
                _source.CancelTransfer();
            }
            _requestedReceiver = receiver;
            _groundRequested = ground;
            return true;
        }

        public void CancelPour()
        {
            _requestedReceiver = null;
            _groundRequested = false;
            _flowUntil = float.NegativeInfinity;
            if (_source != null)
                _source.CancelTransfer();
        }

        public void RegisterNearbyReceiver(BulkMaterialContainer receiver)
        {
            if (receiver != null && receiver != _source)
                _nearbyReceivers.Add(receiver);
        }

        public void UnregisterNearbyReceiver(BulkMaterialContainer receiver)
        {
            if (receiver == null)
                return;

            _nearbyReceivers.Remove(receiver);
            if (_requestedReceiver == receiver)
                CancelPour();
        }

        private void OnDisable()
        {
            CancelPour();
            _nearbyReceivers.Clear();
        }

        private BulkMaterialContainer GetNearbyReceiver(out bool tied)
        {
            BulkMaterialContainer nearest = null;
            var nearestDistanceSquared = float.PositiveInfinity;
            tied = false;
            var bucketPosition = transform.position;
            foreach (var receiver in _nearbyReceivers)
            {
                if (receiver == null || receiver == _source)
                    continue;

                var distanceSquared = (receiver.transform.position - bucketPosition).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearest = receiver;
                    nearestDistanceSquared = distanceSquared;
                    tied = false;
                }
                else if (distanceSquared == nearestDistanceSquared)
                    tied = true;
            }

            // Only an exact tie at the minimum distance cancels the request.
            return tied ? null : nearest;
        }

        private void FixedUpdate()
        {
            if (_grabbable != null && !_grabbable.IsHeld)
            {
                CancelPour();
                return;
            }

            if (_source == null || (_requestedReceiver == null && !_groundRequested))
                return;

            var moved = _groundRequested ? _source.TransferToGroundForSeconds(_materialType, Time.fixedDeltaTime)
                : _source.TransferForSeconds(_requestedReceiver, _materialType, Time.fixedDeltaTime);
            if (moved > 0)
                _flowUntil = Time.time + 0.2f;
        }
    }
}
