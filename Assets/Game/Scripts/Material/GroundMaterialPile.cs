using UnityEngine;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BulkMaterialContainer), typeof(SandPileVisual))]
    public sealed class GroundMaterialPile : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _moundRenderer;
        [SerializeField] private UnityEngine.Material _sandMaterial;
        [SerializeField] private UnityEngine.Material _cementMaterial;
        [SerializeField] private UnityEngine.Material _concreteMaterial;

        private BulkMaterialContainer _container;
        private SandPileVisual _visual;
        private bool _initialized;

        public Collider Surface { get; private set; }
        public Vector3 SurfaceNormal { get; private set; }
        public BulkMaterialContainer Container => _container;

        private void Awake()
        {
            _container = GetComponent<BulkMaterialContainer>();
            _visual = GetComponent<SandPileVisual>();
        }

        public bool Initialize(MaterialType type, RaycastHit surface)
        {
            if (_initialized || surface.collider == null || !_container.ConfigureSingleTypeWhenEmpty(type))
                return false;
            Surface = surface.collider;
            SurfaceNormal = surface.normal;
            transform.SetPositionAndRotation(surface.point,
                Quaternion.FromToRotation(Vector3.up, surface.normal));
            if (_moundRenderer != null)
                _moundRenderer.sharedMaterial = type == MaterialType.Sand ? _sandMaterial
                    : type == MaterialType.Cement ? _cementMaterial : _concreteMaterial;
            _initialized = true;
            return true;
        }

        private void LateUpdate()
        {
            if (_initialized && _container.TotalUnits == 0)
                Destroy(gameObject);
        }

        public void RefreshVisual()
        {
            _visual.Refresh();
        }
    }
}
