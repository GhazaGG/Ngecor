using UnityEngine;
using Ngecor.Player;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _originTransform;
        [SerializeField, Min(0.1f)] private float _maxDistance = 3f;
        [SerializeField] private LayerMask _layerMask = ~0;
        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private bool _isLocalPlayer = true;
        [SerializeField] private bool _showDebugFeedback = true;

        private PlayerMovement _playerMovement;

        public IInteractable CurrentTarget { get; private set; }
        public RaycastHit CurrentHit { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = Mathf.Max(0.1f, value);
        }

        public void SetLocalPlayer(bool isLocalPlayer)
        {
            _isLocalPlayer = isLocalPlayer;
            if (!_isLocalPlayer)
            {
                CurrentTarget = null;
                CurrentHit = default;
            }
        }

        public void SetOrigin(Transform originTransform)
        {
            _originTransform = originTransform;
        }

        public void SetCamera(Camera camera)
        {
            _camera = camera;
        }

        private void Awake()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            ResolveOrigin();
        }

        private void Start()
        {
            ResolveOrigin();
        }

        private void ResolveOrigin()
        {
            if (_camera == null && _playerMovement != null && _playerMovement.LocalCamera != null)
            {
                _camera = _playerMovement.LocalCamera;
            }

            if (_camera == null && _originTransform == null)
            {
                _camera = GetComponentInChildren<Camera>(true);
            }
        }

        private void Update()
        {
            if (!_isLocalPlayer)
                return;

            Detect();
        }

        public IInteractable Detect()
        {
            if (!_isLocalPlayer)
            {
                CurrentTarget = null;
                CurrentHit = default;
                return null;
            }

            Transform origin = _camera != null ? _camera.transform : (_originTransform != null ? _originTransform : transform);
            Ray ray = new Ray(origin.position, origin.forward);

            CurrentTarget = null;
            CurrentHit = default;

            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _layerMask, _triggerInteraction))
            {
                CurrentHit = hit;
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    CurrentTarget = interactable;
                }
            }

            return CurrentTarget;
        }

        private void OnGUI()
        {
            if (!_showDebugFeedback || !_isLocalPlayer)
                return;

            if (HasTarget)
            {
                string targetName = (CurrentTarget as Component)?.gameObject.name ?? "Interactable";
                string prompt = CurrentTarget.InteractionPrompt ?? "Interact";
                GUI.Box(new Rect(10, 10, 240, 50), $"[Interaction]\nTarget: {targetName}\nPrompt: {prompt} (Distance: {CurrentHit.distance:F1}m)");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = _camera != null ? _camera.transform : (_originTransform != null ? _originTransform : transform);
            if (origin == null)
                return;

            Gizmos.color = HasTarget ? Color.green : Color.red;
            float distance = HasTarget ? CurrentHit.distance : _maxDistance;
            Gizmos.DrawRay(origin.position, origin.forward * distance);
        }
    }
}
