using UnityEngine;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class GrabbableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Grab";
        [SerializeField] private bool _canGrab = true;

        private Rigidbody _rigidbody;
        private Collider[] _colliders;

        public string InteractionPrompt => _prompt;
        public bool IsHeld { get; private set; }
        public GameObject CurrentHolder { get; private set; }

        public Rigidbody Rigidbody => _rigidbody != null ? _rigidbody : (_rigidbody = GetComponent<Rigidbody>());
        public Collider[] Colliders => (_colliders != null && _colliders.Length > 0) ? _colliders : (_colliders = GetComponentsInChildren<Collider>());

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>();
        }

        public bool CanInteract(GameObject interactor) => _canGrab && !IsHeld;

        public void SetGrabbable(bool canGrab) => _canGrab = canGrab;

        public void OnGrab(GameObject holder)
        {
            IsHeld = true;
            CurrentHolder = holder;
        }

        public void OnRelease()
        {
            IsHeld = false;
            CurrentHolder = null;
        }
    }
}
