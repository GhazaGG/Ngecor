using UnityEngine;
using UnityEngine.InputSystem;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShovelAction), typeof(BulkMaterialContainer))]
    public sealed class ShovelInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference _useAction;
        [Tooltip("Detik antar aksi aduk saat R ditahan dengan shovel kosong di spot aduk.")]
        [SerializeField, Min(0.05f)] private float _stirInterval = 0.5f;

        private ShovelAction _shovel;
        private BulkMaterialContainer _container;
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
                _stirTimer = 0f;
                return;
            }

            var action = _useAction.action;
            if (action.WasPressedThisFrame())
            {
                _stirTimer = 0f;
                RequestUse();
            }
            else if (action.IsPressed() && _container.TotalUnits == 0)
            {
                _stirTimer += Time.deltaTime;
                if (_stirTimer >= _stirInterval)
                {
                    _stirTimer = 0f;
                    _shovel.RequestStir();
                }
            }
            else
                _stirTimer = 0f;
        }

        public int RequestUse()
        {
            if (_container.TotalUnits != 0)
                return _shovel.RequestDump();
            var stirred = _shovel.RequestStir();
            return stirred != 0 ? stirred : _shovel.RequestScoop();
        }
    }
}
