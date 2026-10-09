using Ngecor.Interaction;
using UnityEngine;

namespace Ngecor.Material
{
    public enum MixingPhase
    {
        Empty,
        NeedsIngredients,
        DryMixing,
        DryMixed,
        WetMixing,
        HasConcrete
    }

    // Mixing bed that follows the site routine: load sand and cement, dry-mix, add water, wet-mix, then
    // collect the concrete straight from the bed. All progress is counted per stir action, never per spot.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BulkMaterialContainer), typeof(Collider))]
    public sealed class ManualMixingSpot : MonoBehaviour
    {
        [SerializeField] private Renderer _bedRenderer;
        [SerializeField] private Transform _progressVisual;
        [SerializeField] private Renderer _progressRenderer;
        [SerializeField, Min(1)] private int _cementUnitsPerSack = 25;
        [SerializeField, Min(1)] private int _cementUnitsPerBatch = 1;
        [SerializeField, Min(1)] private int _sandUnitsPerBatch = 2;
        [SerializeField, Min(1)] private int _waterUnitsPerBatch = 1;
        [Tooltip("Aksi aduk kering untuk meratakan pasir dan semen, sekali per campuran.")]
        [SerializeField, Min(1)] private int _dryActions = 5;
        [Tooltip("Aksi aduk basah per batch concrete.")]
        [SerializeField, Min(1)] private int _wetActionsPerBatch = 5;
        [SerializeField, Min(0f)] private float _labelHeight = 1.7f;

        [Header("Warna")]
        [SerializeField] private Color _sandColor = new Color(0.78f, 0.66f, 0.42f);
        [SerializeField] private Color _cementColor = new Color(0.62f, 0.62f, 0.64f);
        [SerializeField] private Color _dryMixColor = new Color(0.66f, 0.6f, 0.5f);
        [SerializeField] private Color _wetColor = new Color(0.36f, 0.35f, 0.34f);
        [SerializeField] private Color _concreteColor = new Color(0.5f, 0.5f, 0.52f);
        [SerializeField] private Color _dryBarColor = new Color(0.85f, 0.7f, 0.3f);
        [SerializeField] private Color _wetBarColor = new Color(0.25f, 0.5f, 0.85f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private BulkMaterialContainer _ingredients;
        private MaterialPropertyBlock _block;
        private int _dryWork;
        private bool _dryDone;
        private int _dryUnitsAtMix;
        private int _wetWork;
        private bool _converting;
        private Vector3 _progressScale;
        private Vector3 _progressPosition;
        private int _visualKey = -1;
        private TextMesh _label;
        private Camera _camera;
        private string _notice;
        private float _noticeUntil;

        public BulkMaterialContainer Ingredients => _ingredients;
        public int DryWork => _dryWork;
        public int WetWork => _wetWork;
        public int DryActions => _dryActions;
        public int WetActionsPerBatch => _wetActionsPerBatch;
        public bool IsDryMixed => _dryDone && DryUnits <= _dryUnitsAtMix;

        private int Cement => _ingredients.GetUnits(MaterialType.Cement);
        private int Sand => _ingredients.GetUnits(MaterialType.Sand);
        private int Water => _ingredients.GetUnits(MaterialType.Water);
        private int Concrete => _ingredients.GetUnits(MaterialType.Concrete);
        private int DryUnits => Cement + Sand;
        private bool HasRecipe => Cement >= _cementUnitsPerBatch && Sand >= _sandUnitsPerBatch;
        // The mixer (MIX-001) builds the same recipe so both make concrete from one calculation.
        private ConcreteRecipe Recipe => new ConcreteRecipe(_cementUnitsPerBatch, _sandUnitsPerBatch, _waterUnitsPerBatch);

        // What the player should do next; also shown above the spot.
        public string StatusText
        {
            get
            {
                if (_notice != null && Time.time < _noticeUntil)
                    return _notice;
                switch (Phase)
                {
                    case MixingPhase.Empty:
                        return "Kosong: bawa pasir dan semen";
                    case MixingPhase.NeedsIngredients:
                        if (Cement < _cementUnitsPerBatch && Sand < _sandUnitsPerBatch)
                            return "Butuh semen dan pasir";
                        if (Cement < _cementUnitsPerBatch)
                            return "Butuh semen";
                        if (Sand < _sandUnitsPerBatch)
                            return "Butuh pasir (min " + _sandUnitsPerBatch + ")";
                        return "Siap aduk kering (tahan R)";
                    case MixingPhase.DryMixing:
                        return "Aduk kering " + _dryWork + "/" + _dryActions;
                    case MixingPhase.DryMixed:
                        return Water < _waterUnitsPerBatch ? "Sudah rata: tuang air" : "Siap aduk basah (tahan R)";
                    case MixingPhase.WetMixing:
                        return "Aduk basah " + _wetWork + "/" + _wetActionsPerBatch;
                    default:
                        return "Beton siap diambil";
                }
            }
        }

        public MixingPhase Phase
        {
            get
            {
                if (_ingredients.TotalUnits == 0)
                    return MixingPhase.Empty;
                if (!HasRecipe)
                    return Concrete > 0 ? MixingPhase.HasConcrete : MixingPhase.NeedsIngredients;
                if (!IsDryMixed)
                    return _dryWork > 0 ? MixingPhase.DryMixing : MixingPhase.NeedsIngredients;
                return _wetWork > 0 ? MixingPhase.WetMixing : MixingPhase.DryMixed;
            }
        }

        private void Awake()
        {
            _ingredients = GetComponent<BulkMaterialContainer>();
            _ingredients.AcceptFilter = AcceptsNow;
            _block = new MaterialPropertyBlock();
            if (_progressVisual != null)
            {
                _progressScale = _progressVisual.localScale;
                _progressPosition = _progressVisual.localPosition;
            }
            CreateLabel();
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            if (_label != null)
                Destroy(_label.gameObject);
        }

        private void OnValidate()
        {
            _cementUnitsPerSack = Mathf.Max(1, _cementUnitsPerSack);
            _cementUnitsPerBatch = Mathf.Max(1, _cementUnitsPerBatch);
            _sandUnitsPerBatch = Mathf.Max(1, _sandUnitsPerBatch);
            _waterUnitsPerBatch = Mathf.Max(1, _waterUnitsPerBatch);
            _dryActions = Mathf.Max(1, _dryActions);
            _wetActionsPerBatch = Mathf.Max(1, _wetActionsPerBatch);
        }

        private void Update()
        {
            if (_notice != null && Time.time >= _noticeUntil)
            {
                _notice = null;
                _visualKey = -1;
            }
            RefreshVisuals();
        }

        private void LateUpdate()
        {
            if (_label == null)
                return;
            if (_camera == null || !_camera.isActiveAndEnabled)
                _camera = Camera.main;
            _label.transform.position = transform.position + Vector3.up * _labelHeight;
            if (_camera != null)
                _label.transform.rotation = Quaternion.LookRotation(
                    _label.transform.position - _camera.transform.position);
        }

        private void CreateLabel()
        {
            if (!Application.isPlaying)
                return;
            var go = new GameObject("MixingStatusLabel");
            _label = go.AddComponent<TextMesh>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = _label.font.material;
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 64;
            _label.characterSize = 0.04f;
            _label.color = Color.white;
        }

        private void ShowNotice(string text)
        {
            _notice = text;
            _noticeUntil = Time.time + 2.5f;
            _visualKey = -1;
        }

        // Water only joins a bed that is already dry-mixed; concrete only comes from the bed itself.
        // A refused pour stays in the bucket, so the player can recover it.
        private bool AcceptsNow(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.Water:
                    if (!IsDryMixed)
                        ShowNotice("Air ditolak: aduk kering dulu");
                    return IsDryMixed;
                case MaterialType.Concrete: return _converting;
                default: return true;
            }
        }

        // Stay, not Enter: a sack released while already touching the spot must still be taken in.
        private void OnCollisionStay(Collision collision)
        {
            if (collision == null || collision.collider == null)
                return;
            TryReceiveBag(collision.collider.GetComponentInParent<CementBag>());
        }

        public bool TryReceiveBag(CementBag bag)
        {
            if (!isActiveAndEnabled || bag == null || !bag.isActiveAndEnabled || bag.gameObject == gameObject
                || _ingredients == null || !_ingredients.Accepts(MaterialType.Cement)
                || _ingredients.Capacity - _ingredients.TotalUnits < _cementUnitsPerSack
                || (bag.TryGetComponent<GrabbableObject>(out var grabbable) && grabbable.IsHeld))
                return false;

            if (_ingredients.AddUnits(MaterialType.Cement, _cementUnitsPerSack) != _cementUnitsPerSack)
                return false;
            bag.gameObject.SetActive(false);
            Destroy(bag.gameObject);
            return true;
        }

        // One stir action. Returns false when there is nothing to stir yet (e.g. cement without sand,
        // or a dry-mixed bed that still needs water).
        public bool ExecuteStirAction()
        {
            if (!isActiveAndEnabled || _ingredients == null || !HasRecipe)
                return false;

            // Dry material added after mixing is not blended in yet.
            if (_dryDone && DryUnits > _dryUnitsAtMix)
            {
                _dryDone = false;
                _dryWork = 0;
            }

            if (!_dryDone)
            {
                if (++_dryWork >= _dryActions)
                {
                    _dryDone = true;
                    _dryUnitsAtMix = DryUnits;
                }
            }
            else if (Water >= _waterUnitsPerBatch)
            {
                if (++_wetWork >= _wetActionsPerBatch)
                    ConvertBatch();
            }
            else
                return false;

            RefreshVisuals();
            return true;
        }

        private void ConvertBatch()
        {
            var recipe = Recipe;
            _ingredients.RemoveUnits(MaterialType.Cement, recipe.CementUnits);
            _ingredients.RemoveUnits(MaterialType.Sand, recipe.SandUnits);
            _ingredients.RemoveUnits(MaterialType.Water, recipe.WaterUnits);
            _converting = true;
            _ingredients.AddUnits(MaterialType.Concrete, recipe.ConcreteUnitsPerBatch);
            _converting = false;
            _dryUnitsAtMix = Mathf.Min(_dryUnitsAtMix, DryUnits);
            _wetWork = 0;
        }

        private float Progress => !IsDryMixed ? (float)_dryWork / _dryActions
            : (float)_wetWork / _wetActionsPerBatch;

        private void RefreshVisuals()
        {
            if (_ingredients == null)
                return;
            // Cheap change check so Update does not touch the renderers every frame.
            var key = ((((Cement * 397 + Sand) * 397 + Water) * 397 + Concrete) * 397 + _dryWork) * 397
                + _wetWork * 2 + (_dryDone ? 1 : 0);
            if (key == _visualKey)
                return;
            _visualKey = key;

            var dry = Cement + Sand;
            var total = dry + Water + Concrete;
            if (_bedRenderer != null)
            {
                _bedRenderer.enabled = total > 0;
                if (total > 0)
                    SetColor(_bedRenderer, BedColor(dry, total));
            }
            UpdateProgressVisual();
            if (_label != null)
                _label.text = "Semen " + Cement + "  Pasir " + Sand + "  Air " + Water + "  Beton " + Concrete
                    + "\n" + StatusText;
        }

        private Color BedColor(int dry, int total)
        {
            var color = dry > 0 ? (Cement * _cementColor + Sand * _sandColor) / dry : _concreteColor;
            // Dry mixing evens the colors out; water darkens the mix; finished concrete is its own color.
            var mixed = IsDryMixed ? 1f : (float)_dryWork / _dryActions;
            color = Color.Lerp(color, _dryMixColor, mixed);
            if (dry + Water > 0)
                color = Color.Lerp(color, _wetColor, (float)Water / (dry + Water));
            return Color.Lerp(color, _concreteColor, (float)Concrete / total);
        }

        private void UpdateProgressVisual()
        {
            if (_progressVisual == null)
                return;
            var progress = Mathf.Clamp01(Progress);
            var scale = _progressScale;
            scale.x *= progress;
            _progressVisual.localScale = scale;
            var position = _progressPosition;
            position.x -= _progressScale.x * (1f - progress) * 0.5f;
            _progressVisual.localPosition = position;
            if (_progressRenderer != null)
                SetColor(_progressRenderer, IsDryMixed ? _wetBarColor : _dryBarColor);
        }

        private void SetColor(Renderer target, Color color)
        {
            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            target.SetPropertyBlock(_block);
        }
    }
}
