using UnityEngine;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BulkMaterialContainer))]
    public sealed class SandPileVisual : MonoBehaviour
    {
        [SerializeField] private Transform _mound;

        private BulkMaterialContainer _container;
        private Vector3 _fullScale;
        private Vector3 _fullPosition;
        private int _lastUnits = -1;
        private int _lastCapacity = -1;

        private void Awake()
        {
            _container = GetComponent<BulkMaterialContainer>();
            if (_mound == null)
                return;

            _fullScale = _mound.localScale;
            _fullPosition = _mound.localPosition;
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (_mound == null)
                return;

            var units = _container.TotalUnits;
            var capacity = _container.Capacity;
            if (units == _lastUnits && capacity == _lastCapacity)
                return;

            _lastUnits = units;
            _lastCapacity = capacity;
            _mound.gameObject.SetActive(units > 0);
            if (units == 0)
                return;

            // Similar mounds keep their slope; volume changes with the cube of scale.
            var scale = Mathf.Pow(Mathf.Clamp01((float)units / capacity), 1f / 3f);
            _mound.localScale = _fullScale * scale;
            var position = _fullPosition;
            position.y -= _fullScale.y * (1f - scale) * 0.5f;
            _mound.localPosition = position;
        }
    }
}
