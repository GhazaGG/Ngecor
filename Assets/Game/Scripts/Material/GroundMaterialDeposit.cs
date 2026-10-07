using UnityEngine;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BulkMaterialContainer))]
    public sealed class GroundMaterialDeposit : MonoBehaviour
    {
        [SerializeField] private GroundMaterialPile _pilePrefab;
        [SerializeField] private Transform _outlet;
        [SerializeField, Min(0.1f)] private float _surfaceDistance = 3f;
        [SerializeField, Min(0f)] private float _mergeRadius = 0.5f;

        private BulkMaterialContainer _source;
        private readonly RaycastHit[] _surfaceHits = new RaycastHit[32];
        private readonly Collider[] _pileHits = new Collider[64];

        public Vector3 DepositPoint { get; private set; }

        private void Awake()
        {
            _source = GetComponent<BulkMaterialContainer>();
        }

        public bool TryGetSurface(out RaycastHit surface)
        {
            surface = default;
            if (_pilePrefab == null)
                return false;
            var origin = _outlet != null ? _outlet.position : transform.position;
            var count = Physics.RaycastNonAlloc(origin, Vector3.down, _surfaceHits,
                _surfaceDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            // A truncated query cannot prove which surface is nearest. Keep the source intact.
            if (count == _surfaceHits.Length)
                return false;
            var nearest = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var hit = _surfaceHits[i];
                if (hit.collider.transform.IsChildOf(transform)
                    || hit.collider.GetComponentInParent<GroundMaterialPile>() != null)
                    continue;
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    surface = hit;
                }
            }
            var valid = surface.collider != null && surface.rigidbody == null
                && surface.collider.GetComponentInParent<CharacterController>() == null
                && surface.collider.GetComponentInParent<BulkMaterialContainer>() == null
                && Vector3.Dot(surface.normal, Vector3.up) >= 0.5f;
            if (valid)
                DepositPoint = surface.point;
            return valid;
        }

        public int Deposit(MaterialType type, int requestedUnits)
        {
            if (requestedUnits <= 0 || _source.GetUnits(type) == 0 || !TryGetSurface(out var surface))
                return 0;
            var count = Physics.OverlapSphereNonAlloc(surface.point, _mergeRadius, _pileHits,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            if (count == _pileHits.Length)
                return 0;
            GroundMaterialPile nearest = null;
            var distanceSquared = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var pile = _pileHits[i].GetComponentInParent<GroundMaterialPile>();
                if (pile == null || pile.Surface != surface.collider || !pile.Container.Accepts(type)
                    || Mathf.Abs(Vector3.Dot(surface.point - pile.transform.position, pile.SurfaceNormal)) > 0.05f
                    || Vector3.Dot(surface.normal, pile.SurfaceNormal) < 0.99f)
                    continue;
                var distance = (pile.transform.position - surface.point).sqrMagnitude;
                if (distance <= _mergeRadius * _mergeRadius && distance < distanceSquared)
                {
                    nearest = pile;
                    distanceSquared = distance;
                }
            }
            var created = nearest == null;
            if (created)
            {
                nearest = Instantiate(_pilePrefab);
                if (!nearest.Initialize(type, surface))
                {
                    Destroy(nearest.gameObject);
                    return 0;
                }
            }
            var moved = _source.TransferUnitsTo(nearest.Container, type, requestedUnits);
            if (moved == 0 && created)
                Destroy(nearest.gameObject);
            if (moved > 0)
            {
                // Make the new/shrunken mound discoverable by the next deposit in this frame.
                nearest.RefreshVisual();
                Physics.SyncTransforms();
            }
            return moved;
        }
    }
}
