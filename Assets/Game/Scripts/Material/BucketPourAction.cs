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
        private BulkMaterialContainer _collectSource;
        private bool _collectRequested;
        private MaterialType _collectType;
        private bool _restoreTypeOnCancel;
        private MaterialType _typeBeforeCollect;
        private GrabbableObject _grabbable;
        private GroundMaterialDeposit _ground;
        private bool _groundRequested;
        private float _flowUntil = float.NegativeInfinity;
        private readonly HashSet<BulkMaterialContainer> _nearbyReceivers = new HashSet<BulkMaterialContainer>();

        private MaterialType CurrentMaterialType => _source != null && _source.TryGetMaterialType(out var type)
            ? type : _materialType;

        public BulkMaterialContainer PourReceiver => _requestedReceiver;
        public Vector3 PourDestination => _collectSource != null
            ? _collectSource.transform.position : _requestedReceiver != null
            ? _requestedReceiver.transform.position : _ground != null ? _ground.DepositPoint : transform.position;
        // Bridge integer transfer ticks and show the final unit for a short time.
        public bool IsPouring => (_collectRequested || _requestedReceiver != null || _groundRequested)
            && Time.time < _flowUntil;

        private void Awake()
        {
            _source = GetComponent<BulkMaterialContainer>();
            _grabbable = GetComponent<GrabbableObject>();
            _ground = GetComponent<GroundMaterialDeposit>();
        }

        public bool RequestPour()
        {
            if (_collectRequested)
                return true;
            if (_source.TotalUnits == 0 && RequestCollect())
                return true;

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
            _collectSource = null;
            _collectRequested = false;
            _requestedReceiver = receiver;
            _groundRequested = ground;
            return true;
        }

        public void CancelPour()
        {
            _requestedReceiver = null;
            if (_collectSource != null)
                _collectSource.CancelTransfer();
            _collectSource = null;
            _collectRequested = false;
            _groundRequested = false;
            _flowUntil = float.NegativeInfinity;
            if (_source != null)
                _source.CancelTransfer();
            // A pickup cancelled before its first unit leaves the empty bucket as it was.
            if (_restoreTypeOnCancel && _source != null && _source.TotalUnits == 0)
                _source.ConfigureSingleTypeWhenEmpty(_typeBeforeCollect);
            _restoreTypeOnCancel = false;
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
            // Reset the whole request so a held R can pick the next source.
            if (_collectSource == receiver || _requestedReceiver == receiver)
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

            if (_source == null || (!_collectRequested && _requestedReceiver == null && !_groundRequested))
                return;

            if (_collectRequested && _collectSource == null)
                return;
            var moved = _collectRequested
                ? _collectSource.TransferForSeconds(_source, _collectType, Time.fixedDeltaTime)
                : _groundRequested ? _source.TransferToGroundForSeconds(CurrentMaterialType, Time.fixedDeltaTime)
                : _source.TransferForSeconds(_requestedReceiver, CurrentMaterialType, Time.fixedDeltaTime);
            if (moved > 0)
            {
                _flowUntil = Time.time + 0.2f;
                if (_collectRequested)
                    _restoreTypeOnCancel = false;
            }
        }

        private bool RequestCollect()
        {
            if (_source == null || _source.TotalUnits != 0)
                return false;

            // Same unique-nearest, no-fallback rule as pouring: a nearer ordinary container blocks pickup.
            var nearest = GetNearbyReceiver(out var tied);
            if (nearest == null || tied || !nearest.TryGetComponent<CollectOnlyType>(out _)
                || !CollectOnlyType.TryGetCollectType(nearest, false, out var nearestType))
                return false;

            CancelPour();
            var hadType = _source.TryGetSingleType(out var previous);
            if (!_source.ConfigureSingleTypeWhenEmpty(nearestType))
                return false;
            _restoreTypeOnCancel = hadType && previous != nearestType;
            _typeBeforeCollect = previous;
            _collectSource = nearest;
            _collectType = nearestType;
            _collectRequested = true;
            return true;
        }
    }
}
