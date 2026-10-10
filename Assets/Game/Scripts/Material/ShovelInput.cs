using UnityEngine;
using UnityEngine.InputSystem;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShovelAction), typeof(BulkMaterialContainer))]
    public sealed class ShovelInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference _useAction;
        [Tooltip("Di spot aduk dengan shovel kosong: tekan lebih singkat dari ini = ambil concrete, lebih lama = aduk.")]
        [SerializeField, Min(0.05f)] private float _holdToStirSeconds = 0.25f;
        [Tooltip("Detik antar aksi aduk saat R ditahan dengan shovel kosong di spot aduk.")]
        [SerializeField, Min(0.05f)] private float _stirInterval = 0.5f;

        private ShovelAction _shovel;
        private BulkMaterialContainer _container;
        private bool _spotPress;
        private bool _stirring;
        private float _pressTime;
        private float _stirTimer;

        private void Awake()
        {
            _shovel = GetComponent<ShovelAction>();
            _container = GetComponent<BulkMaterialContainer>();
        }

        private void OnEnable()
        {
            if (_useAction != null && _useAction.action != null)
                _useAction.action.Enable();
        }


        private void Update()
        {
            if (_useAction == null || _useAction.action == null || !_shovel.HasLocalHolder)
            {
                _spotPress = false;
                _stirring = false;
                return;
            }

            var action = _useAction.action;
            ProcessUse(action.WasPressedThisFrame(), action.IsPressed(), Time.deltaTime);
        }

        // Elsewhere one press scoops or dumps at once. At a mixing spot an empty shovel waits for the release:
        // a tap collects finished concrete, a hold stirs (one action now, then one per interval).
        public void ProcessUse(bool pressedThisFrame, bool isPressed, float deltaTime)
        {
            if (pressedThisFrame)
            {
                _pressTime = 0f;
                _stirring = false;
                _spotPress = _container.TotalUnits == 0 && _shovel.IsAimingAtMixingSpot();
                if (!_spotPress)
                    RequestUse();
                return;
            }

            if (!_spotPress)
                return;

            if (!isPressed)
            {
                if (!_stirring)
                    RequestUse();
                _spotPress = false;
                _stirring = false;
                return;
            }

            _pressTime += deltaTime;
            if (!_stirring)
            {
                if (_pressTime < _holdToStirSeconds)
                    return;
                _stirring = true;
                _stirTimer = 0f;
                _shovel.RequestStir();
                return;
            }

            _stirTimer += deltaTime;
            if (_stirTimer >= _stirInterval)
            {
                _stirTimer = 0f;
                _shovel.RequestStir();
            }
        }

        public int RequestUse() => _container.TotalUnits != 0 ? _shovel.RequestDump() : _shovel.RequestScoop();
    }
}
