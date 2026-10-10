using Ngecor.Material;
using UnityEngine;

namespace Ngecor.Construction
{
    [RequireComponent(typeof(BulkMaterialContainer), typeof(SphereCollider), typeof(DebugPourReceiverTrigger))]
    public sealed class Mixer : MonoBehaviour
    {
        public enum MixerState
        {
            Idle,
            Mixing,
            Ready
        }

        [Header("Recipe")]
        [SerializeField, Min(1)] private int _cementUnitsPerBatch = 1;
        [SerializeField, Min(1)] private int _sandUnitsPerBatch = 1;

        [Header("Mixing")]
        [SerializeField, Min(0.1f)] private float _mixingDuration = 5f;
        [SerializeField] private MixerState _state;

        private BulkMaterialContainer _container;
        private float _elapsed;
        private string _statusMessage;
        private GUIStyle _statusStyle;

        public MixerState State => _state;
        private ConcreteRecipe Recipe => new ConcreteRecipe(_cementUnitsPerBatch, _sandUnitsPerBatch);

        private void Awake()
        {
            _container = GetComponent<BulkMaterialContainer>();
            if (_container == null)
                _container = gameObject.AddComponent<BulkMaterialContainer>();

            _container.ConfigureMultipleTypesWhenEmpty(
                MaterialType.Cement, MaterialType.Sand, MaterialType.Concrete);

            var receiver = GetComponent<SphereCollider>();
            if (receiver == null)
                receiver = gameObject.AddComponent<SphereCollider>();
            receiver.isTrigger = true;
            receiver.radius = 1.5f;

            if (GetComponent<DebugPourReceiverTrigger>() == null)
                gameObject.AddComponent<DebugPourReceiverTrigger>();

            _statusMessage = BuildIdleStatus();
        }

        private void OnValidate()
        {
            _cementUnitsPerBatch = Mathf.Max(1, _cementUnitsPerBatch);
            _sandUnitsPerBatch = Mathf.Max(1, _sandUnitsPerBatch);
            _mixingDuration = Mathf.Max(0.1f, _mixingDuration);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
                return;

            if (string.IsNullOrEmpty(_statusMessage))
                _statusMessage = BuildIdleStatus();

            _statusStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(14, 14, 8, 8)
            };
            _statusStyle.normal.textColor = Color.white;

            var previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            GUI.Box(new Rect(16f, 16f, 620f, 48f), _statusMessage, _statusStyle);
            GUI.backgroundColor = previousBackgroundColor;
        }

        public bool TryStartMixing()
        {
            if (!Application.isPlaying)
                return false;

            if (_container == null)
            {
                _statusMessage = "GAGAL: container mixer tidak tersedia.";
                return false;
            }

            if (_state == MixerState.Mixing)
            {
                _statusMessage = "MIXER SEDANG MENGADUK.";
                return false;
            }

            if (_state == MixerState.Ready)
            {
                _statusMessage = $"BERHASIL: {_container.GetUnits(MaterialType.Concrete)} CONCRETE — ambil hasil dulu.";
                return false;
            }

            if (!_container.Accepts(MaterialType.Cement)
                || !_container.Accepts(MaterialType.Sand)
                || !_container.Accepts(MaterialType.Concrete))
            {
                _statusMessage = "GAGAL: mixer tidak menerima bahan resep.";
                return false;
            }

            if (!HasIngredients())
            {
                _statusMessage = BuildMissingIngredientsStatus();
                return false;
            }

            _elapsed = 0f;
            _state = MixerState.Mixing;
            _statusMessage = "MENGADUK...";
            return true;
        }

        [ContextMenu("Start Mixing")]
        private void StartMixingFromInspector()
        {
            if (!TryStartMixing())
                Debug.LogWarning("Mixer needs a complete cement-and-sand recipe. Enter Play Mode to start it.", this);
        }

        private void Update()
        {
            if (_container == null)
                return;

            if (_state == MixerState.Ready)
            {
                if (_container.GetUnits(MaterialType.Concrete) == 0)
                {
                    _state = MixerState.Idle;
                    _statusMessage = BuildIdleStatus();
                }
                return;
            }

            if (_state == MixerState.Idle)
            {
                if (HasIngredients())
                    TryStartMixing();
                else
                    _statusMessage = BuildIdleStatus();
                return;
            }

            if (_state != MixerState.Mixing)
                return;

            if (!HasIngredients())
            {
                _state = MixerState.Idle;
                _elapsed = 0f;
                _statusMessage = "GAGAL: bahan kurang saat mixer sedang mengaduk.";
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed >= _mixingDuration)
                CompleteBatch();
        }

        private bool HasIngredients() => ConcreteRecipeCalculator.Mix(
            _container.GetUnits(MaterialType.Cement),
            _container.GetUnits(MaterialType.Sand),
            Recipe).Batches > 0;

        private void CompleteBatch()
        {
            if (!_container.Accepts(MaterialType.Concrete))
            {
                _state = MixerState.Idle;
                _elapsed = 0f;
                _statusMessage = "GAGAL: mixer tidak bisa menyimpan concrete.";
                return;
            }

            int concreteUnitsPerBatch = Recipe.ConcreteUnitsPerBatch;
            int cementRemoved = _container.RemoveUnits(MaterialType.Cement, _cementUnitsPerBatch);
            int sandRemoved = _container.RemoveUnits(MaterialType.Sand, _sandUnitsPerBatch);
            int concreteAdded = _container.AddUnits(MaterialType.Concrete, concreteUnitsPerBatch);
            if (cementRemoved != _cementUnitsPerBatch || sandRemoved != _sandUnitsPerBatch
                || concreteAdded != concreteUnitsPerBatch)
            {
                if (concreteAdded > 0)
                    _container.RemoveUnits(MaterialType.Concrete, concreteAdded);
                if (cementRemoved > 0)
                    _container.AddUnits(MaterialType.Cement, cementRemoved);
                if (sandRemoved > 0)
                    _container.AddUnits(MaterialType.Sand, sandRemoved);
                _state = MixerState.Idle;
                _elapsed = 0f;
                _statusMessage = "GAGAL: jumlah bahan berubah; bahan dikembalikan.";
                return;
            }

            _state = MixerState.Ready;
            _statusMessage = $"BERHASIL: {concreteAdded} CONCRETE";
        }

        private string BuildIdleStatus()
        {
            var cementUnits = _container.GetUnits(MaterialType.Cement);
            var sandUnits = _container.GetUnits(MaterialType.Sand);
            if (cementUnits == 0 && sandUnits == 0)
                return $"SIAP: tuang {_cementUnitsPerBatch} cement + {_sandUnitsPerBatch} sand.";

            return $"BAHAN MASUK: cement {cementUnits}/{_cementUnitsPerBatch}, " +
                   $"sand {sandUnits}/{_sandUnitsPerBatch}.";
        }

        private string BuildMissingIngredientsStatus() =>
            $"GAGAL: butuh {_cementUnitsPerBatch} cement + {_sandUnitsPerBatch} sand. " +
            $"Sekarang: {_container.GetUnits(MaterialType.Cement)} + {_container.GetUnits(MaterialType.Sand)}.";
    }
}
