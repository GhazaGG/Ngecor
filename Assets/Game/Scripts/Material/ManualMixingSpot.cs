using Ngecor.Interaction;
using UnityEngine;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BulkMaterialContainer), typeof(Collider))]
    public sealed class ManualMixingSpot : MonoBehaviour
    {
        [SerializeField] private GroundMaterialPile _outputPilePrefab;
        [SerializeField] private Transform _outputPoint;
        [SerializeField] private Transform _progressVisual;
        [SerializeField, Min(1)] private int _cementUnitsPerSack = 25;
        [SerializeField, Min(1)] private int _cementUnitsPerBatch = 1;
        [SerializeField, Min(1)] private int _sandUnitsPerBatch = 2;
        [SerializeField, Min(1)] private int _actionsPerBatch = 5;

        private BulkMaterialContainer _ingredients;
        private int _work;
        private int _batchNumber;
        private Vector3 _progressScale;
        private Vector3 _progressPosition;

        public int Work => _work;
        public int ActionsPerBatch => _actionsPerBatch;
        public BulkMaterialContainer Ingredients => _ingredients;

        private void Awake()
        {
            _ingredients = GetComponent<BulkMaterialContainer>();
            if (_progressVisual != null)
            {
                _progressScale = _progressVisual.localScale;
                _progressPosition = _progressVisual.localPosition;
            }
        }

        private void OnValidate()
        {
            _cementUnitsPerBatch = Mathf.Max(1, _cementUnitsPerBatch);
            _sandUnitsPerBatch = Mathf.Max(1, _sandUnitsPerBatch);
            _actionsPerBatch = Mathf.Max(1, _actionsPerBatch);
            _cementUnitsPerSack = Mathf.Max(1, _cementUnitsPerSack);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.collider == null)
                return;
            TryReceiveBag(collision.collider.GetComponentInParent<CementBag>());
        }

        public bool TryReceiveBag(CementBag bag)
        {
            if (!isActiveAndEnabled || bag == null || !bag.isActiveAndEnabled || bag.gameObject == gameObject
                || _ingredients == null || !_ingredients.Accepts(MaterialType.Cement)
                || _ingredients.Capacity - _ingredients.TotalUnits < _cementUnitsPerSack)
                return false;

            if (_ingredients.AddUnits(MaterialType.Cement, _cementUnitsPerSack) != _cementUnitsPerSack)
                return false;
            bag.gameObject.SetActive(false);
            Destroy(bag.gameObject);
            return true;
        }

        public bool ExecuteStirAction()
        {
            if (!isActiveAndEnabled || _ingredients == null || GetAvailableMix().Batches == 0)
                return false;

            _work++;
            UpdateProgressVisual();
            if (_work < _actionsPerBatch)
                return true;
            if (!TryCreateBatch())
                return true;

            _ingredients.RemoveUnits(MaterialType.Cement, _cementUnitsPerBatch);
            _ingredients.RemoveUnits(MaterialType.Sand, _sandUnitsPerBatch);
            _work = 0;
            _batchNumber++;
            UpdateProgressVisual();
            return true;
        }

        private ConcreteMixResult GetAvailableMix() => ConcreteRecipeCalculator.Mix(
            _ingredients.GetUnits(MaterialType.Cement), _ingredients.GetUnits(MaterialType.Sand),
            new ConcreteRecipe(_cementUnitsPerBatch, _sandUnitsPerBatch));

        private void UpdateProgressVisual()
        {
            if (_progressVisual == null)
                return;
            var progress = Mathf.Clamp01((float)_work / _actionsPerBatch);
            var scale = _progressScale;
            scale.x *= progress;
            _progressVisual.localScale = scale;
            var position = _progressPosition;
            position.x -= _progressScale.x * (1f - progress) * 0.5f;
            _progressVisual.localPosition = position;
        }

        private bool TryCreateBatch()
        {
            if (_outputPilePrefab == null)
                return false;

            var basePoint = _outputPoint != null ? _outputPoint.position : transform.position + transform.forward;
            var origin = basePoint + transform.right * (_batchNumber * 1.4f);
            if (!Physics.Raycast(origin + Vector3.up * 0.5f, Vector3.down, out var surface, 2f,
                    Physics.AllLayers, QueryTriggerInteraction.Ignore)
                || surface.collider.GetComponentInParent<BulkMaterialContainer>() != null)
                return false;

            var pile = Instantiate(_outputPilePrefab);
            if (!pile.Initialize(MaterialType.Concrete, surface))
            {
                Destroy(pile.gameObject);
                return false;
            }

            var batch = pile.gameObject.AddComponent<ConcreteBatch>();
            batch.Initialize(_batchNumber + 1);
            var recipeUnits = _cementUnitsPerBatch + _sandUnitsPerBatch;
            if (pile.Container.AddUnits(MaterialType.Concrete, recipeUnits) != recipeUnits)
            {
                Destroy(pile.gameObject);
                return false;
            }
            return true;
        }
    }
}
