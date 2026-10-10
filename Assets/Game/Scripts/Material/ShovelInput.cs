using UnityEngine;
using UnityEngine.InputSystem;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShovelAction), typeof(BulkMaterialContainer))]
    public sealed class ShovelInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference _useAction;

        private ShovelAction _shovel;
        private BulkMaterialContainer _container;

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
            if (_useAction != null && _useAction.action != null && _shovel.HasLocalHolder
                && _useAction.action.WasPressedThisFrame())
                RequestUse();
        }

        public int RequestUse()
        {
            return _container.TotalUnits == 0 ? _shovel.RequestScoop() : _shovel.RequestDump();
        }
    }
}
