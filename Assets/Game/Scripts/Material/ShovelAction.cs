using System.Collections.Generic;
using Ngecor.Interaction;
using Ngecor.Player;
using UnityEngine;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BulkMaterialContainer), typeof(GrabbableObject), typeof(GroundMaterialDeposit))]
    public sealed class ShovelAction : MonoBehaviour
    {
        [SerializeField] private Transform _blade;
        [SerializeField, Min(0.1f)] private float _reach = 1f;

        private BulkMaterialContainer _container;
        private GrabbableObject _grabbable;
        private GroundMaterialDeposit _ground;
        private GameObject _holder;
        private PlayerMovement _movement;
        private readonly Collider[] _overlaps = new Collider[32];
        private readonly RaycastHit[] _sightHits = new RaycastHit[32];
        private readonly Dictionary<BulkMaterialContainer, float> _candidates = new Dictionary<BulkMaterialContainer, float>();

        private bool HasHolder
        {
            get
            {
                if (!_grabbable.IsHeld || _grabbable.CurrentHolder == null)
                    return false;
                if (_holder != _grabbable.CurrentHolder)
                {
                    _holder = _grabbable.CurrentHolder;
                    _movement = _holder != null ? _holder.GetComponent<PlayerMovement>() : null;
                }
                return true;
            }
        }

        public bool HasLocalHolder => HasHolder && _movement != null && _movement.LocalCamera != null;

        private void Awake()
        {
            _container = GetComponent<BulkMaterialContainer>();
            _grabbable = GetComponent<GrabbableObject>();
            _ground = GetComponent<GroundMaterialDeposit>();
        }

        public int RequestScoop() => HasLocalHolder ? ExecuteScoop() : 0;
        public int RequestDump() => HasLocalHolder ? ExecuteDump() : 0;

        public int RequestStir()
        {
            var spot = FindAimedMixingSpot();
            return spot != null && spot.ExecuteStirAction() ? 1 : 0;
        }

        public bool IsAimingAtMixingSpot() => FindAimedMixingSpot() != null;

        private ManualMixingSpot FindAimedMixingSpot()
        {
            if (!HasLocalHolder)
                return null;
            var target = FindTarget(out var cancelled);
            return !cancelled && target != null ? target.GetComponent<ManualMixingSpot>() : null;
        }

        public int ExecuteScoop()
        {
            if (!isActiveAndEnabled || !HasHolder || _container.TotalUnits >= _container.Capacity)
                return 0;
            var source = FindTarget(out _);
            if (source == null || !CollectOnlyType.TryGetCollectType(source, true, out var type))
                return 0;
            if (_container.TotalUnits == 0)
                _container.ConfigureSingleTypeWhenEmpty(type);
            return source.TransferUnitsTo(_container, type, _container.Capacity - _container.TotalUnits);
        }

        public int ExecuteDump()
        {
            if (!isActiveAndEnabled || !HasHolder || !_container.TryGetMaterialType(out var type))
                return 0;
            var receiver = FindTarget(out var cancelled);
            if (cancelled)
                return 0;
            return receiver != null ? _container.TransferUnitsTo(receiver, type, _container.TotalUnits)
                : _ground.Deposit(type, _container.TotalUnits);
        }

        private BulkMaterialContainer FindTarget(out bool cancelled)
        {
            cancelled = false;
            var blade = _blade != null ? _blade : transform;
            var count = Physics.OverlapSphereNonAlloc(blade.position, _reach, _overlaps,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            _candidates.Clear();
            if (count == _overlaps.Length)
            {
                cancelled = true;
                return null;
            }
            for (var i = 0; i < count; i++)
            {
                var collider = _overlaps[i];
                var target = collider.GetComponentInParent<BulkMaterialContainer>();
                if (target == null || target == _container)
                    continue;
                if (!TryGetTargetPoint(collider, blade.position, out var point))
                    continue;
                var direction = point - blade.position;
                if (Vector3.Dot(direction.sqrMagnitude > 0f ? direction : target.transform.position - blade.position,
                    blade.forward) < 0f || direction.sqrMagnitude > _reach * _reach)
                    continue;
                if (!HasClearPath(blade.position, direction, target))
                    continue;
                var distance = direction.sqrMagnitude;
                if (!_candidates.TryGetValue(target, out var previous) || distance < previous)
                    _candidates[target] = distance;
            }
            BulkMaterialContainer nearest = null;
            var nearestDistance = float.PositiveInfinity;
            foreach (var candidate in _candidates)
            {
                if (candidate.Value < nearestDistance)
                {
                    nearest = candidate.Key;
                    nearestDistance = candidate.Value;
                    cancelled = false;
                }
                else if (candidate.Value == nearestDistance)
                    cancelled = true;
            }
            return cancelled ? null : nearest;
        }

        private bool HasClearPath(Vector3 origin, Vector3 direction, BulkMaterialContainer target)
        {
            var distance = direction.magnitude;
            if (distance == 0f)
                return true;
            var count = Physics.RaycastNonAlloc(origin, direction / distance, _sightHits,
                distance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            if (count == _sightHits.Length)
                return false;
            for (var i = 0; i < count; i++)
            {
                var collider = _sightHits[i].collider;
                if (collider.transform.IsChildOf(transform) || collider.transform.IsChildOf(_holder.transform)
                    || collider.GetComponentInParent<BulkMaterialContainer>() == target)
                    continue;
                if (_sightHits[i].distance < distance)
                    return false;
            }
            return true;
        }

        private bool TryGetTargetPoint(Collider collider, Vector3 origin, out Vector3 point)
        {
            point = origin;
            if (!(collider is MeshCollider mesh) || mesh.convex)
            {
                point = collider.ClosestPoint(origin);
                return true;
            }

            // Non-convex mounds do not support ClosestPoint. Intersect the actual mesh instead.
            var direction = collider.bounds.center - origin;
            if (direction.sqrMagnitude == 0f)
                return true;
            direction.Normalize();
            if (collider.Raycast(new Ray(origin, direction), out var hit, _reach))
            {
                point = hit.point;
                return true;
            }

            // A blade already inside a closed mound has no outward front-face hit.
            var distance = collider.bounds.size.magnitude + _reach;
            return collider.Raycast(new Ray(origin - direction * distance, direction), out _, distance);
        }
    }
}
