using UnityEngine;
using Ngecor.Player;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _maxDistance = 3f;
        [SerializeField] private LayerMask _layerMask = ~0;
        [SerializeField] private bool _showDebugFeedback = false;

        private PlayerMovement _playerMovement;

        public IInteractable CurrentTarget { get; private set; }
        public RaycastHit CurrentHit { get; private set; }
        public bool HasTarget => CurrentTarget != null &&
                                 (!(CurrentTarget is Object unityObject) || unityObject != null);

        public bool ShowDebugFeedback
        {
            get => _showDebugFeedback;
            set => _showDebugFeedback = value;
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = Mathf.Max(0.1f, value);
        }

        private void Awake() => _playerMovement = GetComponent<PlayerMovement>();

        private void Update() => Detect();

        public IInteractable Detect()
        {
            CurrentTarget = null;
            CurrentHit = default;

            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;
            if (camera == null)
                return null;

            var origin = camera.transform;
            if (Physics.Raycast(origin.position, origin.forward, out var hit, _maxDistance, _layerMask, QueryTriggerInteraction.Ignore))
            {
                CurrentHit = hit;
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable is IHoldInteractable holdInteractable &&
                    !holdInteractable.CanInteractFrom(gameObject, hit.collider))
                    return null;

                if (interactable != null && interactable.CanInteract(gameObject))
                    CurrentTarget = interactable;
            }
            return CurrentTarget;
        }

        private void OnGUI()
        {
            if (!_showDebugFeedback)
                return;

            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            var camera = _playerMovement != null ? _playerMovement.LocalCamera : null;
            if (camera == null)
                return;

            GUI.Label(new Rect(Screen.width / 2f - 10f, Screen.height / 2f - 10f, 20f, 20f), "+");

            if (HasTarget)
            {
                string targetName = (CurrentTarget as Component)?.gameObject.name ?? "Interactable";
                string prompt = CurrentTarget.InteractionPrompt ?? "Interact";
                GUI.Box(new Rect(Screen.width / 2f - 120f, Screen.height / 2f + 20f, 240f, 50f),
                    $"[Interaction]\nTarget: {targetName}\nPrompt: {prompt} ({CurrentHit.distance:F1}m)");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();

            if (_playerMovement == null)
                return;

            var camera = _playerMovement.LocalCamera;
            if (camera == null)
                return;

            var origin = camera.transform;
            Gizmos.color = HasTarget ? Color.green : Color.red;
            float distance = HasTarget ? CurrentHit.distance : _maxDistance;
            Gizmos.DrawRay(origin.position, origin.forward * distance);
        }
    }
}
