using UnityEngine;
using UnityEngine.UI;
using Ngecor.Player;

namespace Ngecor.Interaction
{
    /// <summary>
    /// Displays interaction feedback UI:
    /// - An always-visible crisp '+' (plus sign) crosshair at screen center.
    /// - A centered prompt text below the crosshair showing <see cref="IInteractable.InteractionPrompt"/>.
    /// Steady-state garbage collection is zero via dirty tracking.
    /// If Canvas, crosshair, or prompt references are unassigned in inspector, they are built programmatically in Awake.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractionDetector))]
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _crosshairRoot;
        [SerializeField] private Text _promptText;
        [SerializeField] private Color _crosshairColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private int _fontSize = 18;

        private InteractionDetector _detector;
        private PlayerMovement _playerMovement;
        private bool _lastPromptVisible;
        private string _lastDisplayedPrompt;

        /// <summary>The Canvas hosting interaction UI elements.</summary>
        public Canvas Canvas => _canvas;

        /// <summary>The root transform of the '+' crosshair.</summary>
        public RectTransform CrosshairRoot => _crosshairRoot;

        /// <summary>The crosshair graphic component. Enabled and active only while local player is active.</summary>
        public Graphic Crosshair => (_crosshairRoot != null && _crosshairRoot.gameObject.activeInHierarchy) ? _crosshairRoot.GetComponentInChildren<Graphic>() : null;

        /// <summary>The text component displaying current interaction prompt.</summary>
        public Text PromptText => _promptText;

        /// <summary>True when the prompt text is visible.</summary>
        public bool IsPromptVisible => _lastPromptVisible;

        /// <summary>Currently displayed prompt string (null when hidden).</summary>
        public string DisplayedPrompt => _lastDisplayedPrompt;

        /// <summary>True if this UI is attached to the active local player or in standalone test mode without PlayerMovement.</summary>
        public bool IsLocalPlayer
        {
            get
            {
                if (_playerMovement == null)
                    _playerMovement = GetComponent<PlayerMovement>();

                return _playerMovement == null || _playerMovement.IsLocalPlayer;
            }
        }

        private void Awake()
        {
            _detector = GetComponent<InteractionDetector>();
            _playerMovement = GetComponent<PlayerMovement>();
            if (_playerMovement != null)
            {
                _playerMovement.LocalPlayerChanged += OnLocalPlayerChanged;
            }

            if (IsLocalPlayer)
            {
                EnsureUi();
            }
            else if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_playerMovement != null)
            {
                _playerMovement.LocalPlayerChanged -= OnLocalPlayerChanged;
            }
        }

        private void OnLocalPlayerChanged(bool isLocal)
        {
            if (isLocal)
            {
                EnsureUi();
                if (_canvas != null)
                    _canvas.gameObject.SetActive(true);
                UpdatePrompt();
            }
            else
            {
                if (_canvas != null)
                    _canvas.gameObject.SetActive(false);
                ApplyPrompt(false, null);
            }
        }

        private void LateUpdate()
        {
            if (!IsLocalPlayer)
            {
                if (_canvas != null && _canvas.gameObject.activeSelf)
                    _canvas.gameObject.SetActive(false);
                return;
            }

            if (_canvas == null || !_canvas.gameObject.activeSelf)
            {
                EnsureUi();
                if (_canvas != null)
                    _canvas.gameObject.SetActive(true);
            }

            UpdatePrompt();
        }

        /// <summary>
        /// Evaluates detector state and updates prompt visibility and text.
        /// Public so tests and external drivers can step UI deterministically.
        /// </summary>
        public void UpdatePrompt()
        {
            if (!IsLocalPlayer)
            {
                ApplyPrompt(false, null);
                return;
            }

            if (_detector == null)
                _detector = GetComponent<InteractionDetector>();

            var target = _detector != null ? _detector.CurrentTarget : null;

            var shouldShow = IsPromptable(target);
            var prompt = shouldShow ? (target.InteractionPrompt ?? "Interact") : null;

            ApplyPrompt(shouldShow, prompt);
        }

        private bool IsPromptable(IInteractable target)
        {
            if (target == null)
                return false;

            var unityObject = target as Object;
            if (unityObject == null)
                return false;

            var behaviour = unityObject as Behaviour;
            if (behaviour != null && !behaviour.isActiveAndEnabled)
                return false;

            var component = unityObject as Component;
            if (component != null && !component.gameObject.activeInHierarchy)
                return false;

            if (target is GrabbableObject grabbable && grabbable.IsHeld)
                return false;

            if (!target.CanInteract(gameObject))
                return false;

            return true;
        }

        private void ApplyPrompt(bool shouldShow, string prompt)
        {
            if (_promptText == null)
                return;

            if (_lastPromptVisible != shouldShow)
            {
                _promptText.enabled = shouldShow;
                _lastPromptVisible = shouldShow;

                if (!shouldShow)
                {
                    _promptText.text = string.Empty;
                    _lastDisplayedPrompt = null;
                    return;
                }
            }

            if (!shouldShow)
                return;

            if (!ReferenceEquals(_lastDisplayedPrompt, prompt))
            {
                _promptText.text = prompt;
                _lastDisplayedPrompt = prompt;
            }
        }

        private void EnsureUi()
        {
            if (_canvas == null)
            {
                var canvasGo = new GameObject("InteractionUI", typeof(RectTransform));
                canvasGo.transform.SetParent(transform, false);
                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (_crosshairRoot == null)
            {
                var crosshairGo = new GameObject("Crosshair", typeof(RectTransform));
                crosshairGo.transform.SetParent(_canvas.transform, false);
                _crosshairRoot = crosshairGo.GetComponent<RectTransform>();
                _crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
                _crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
                _crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
                _crosshairRoot.anchoredPosition = Vector2.zero;
                _crosshairRoot.sizeDelta = new Vector2(16f, 16f);

                // Horizontal bar of the '+'
                var hBarGo = new GameObject("HorizontalBar", typeof(RectTransform));
                hBarGo.transform.SetParent(_crosshairRoot, false);
                var hRect = hBarGo.GetComponent<RectTransform>();
                hRect.anchorMin = new Vector2(0.5f, 0.5f);
                hRect.anchorMax = new Vector2(0.5f, 0.5f);
                hRect.pivot = new Vector2(0.5f, 0.5f);
                hRect.anchoredPosition = Vector2.zero;
                hRect.sizeDelta = new Vector2(12f, 2f);
                var hImg = hBarGo.AddComponent<Image>();
                hImg.color = _crosshairColor;
                hImg.raycastTarget = false;

                // Vertical bar of the '+'
                var vBarGo = new GameObject("VerticalBar", typeof(RectTransform));
                vBarGo.transform.SetParent(_crosshairRoot, false);
                var vRect = vBarGo.GetComponent<RectTransform>();
                vRect.anchorMin = new Vector2(0.5f, 0.5f);
                vRect.anchorMax = new Vector2(0.5f, 0.5f);
                vRect.pivot = new Vector2(0.5f, 0.5f);
                vRect.anchoredPosition = Vector2.zero;
                vRect.sizeDelta = new Vector2(2f, 12f);
                var vImg = vBarGo.AddComponent<Image>();
                vImg.color = _crosshairColor;
                vImg.raycastTarget = false;
            }

            if (_promptText == null)
            {
                var promptGo = new GameObject("PromptText", typeof(RectTransform));
                promptGo.transform.SetParent(_canvas.transform, false);
                var rect = promptGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -35f);
                rect.sizeDelta = new Vector2(400f, 32f);

                _promptText = promptGo.AddComponent<Text>();
                _promptText.alignment = TextAnchor.UpperCenter;
                _promptText.fontSize = _fontSize;
                _promptText.color = Color.white;
                _promptText.raycastTarget = false;
                _promptText.font = GetDefaultFont();
                _promptText.enabled = false;
            }

            if (_crosshairRoot != null)
                _crosshairRoot.gameObject.SetActive(true);
        }

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
