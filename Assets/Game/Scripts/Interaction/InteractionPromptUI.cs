using UnityEngine;
using UnityEngine.UI;

namespace Ngecor.Interaction
{
    /// <summary>
    /// Renders the local interaction UI: an always-visible crosshair plus a prompt label that
    /// mirrors the <see cref="InteractionDetector"/>'s current target. The prompt is refreshed with
    /// dirty tracking so no string is built or assigned on frames where nothing changed, keeping
    /// steady-state garbage collection at zero. If the Canvas / crosshair / prompt references are
    /// left unassigned, matching objects are created programmatically in Awake.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractionDetector))]
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _crosshair;
        [SerializeField] private Text _promptText;
        [SerializeField] private Color _crosshairColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private int _fontSize = 18;

        private InteractionDetector _detector;

        private bool _lastPromptVisible;
        private string _lastDisplayedPrompt;

        /// <summary>The Canvas that hosts the crosshair and prompt.</summary>
        public Canvas Canvas => _canvas;

        /// <summary>The crosshair image. Always visible while the component is active.</summary>
        public Image Crosshair => _crosshair;

        /// <summary>The label that displays the current interaction prompt.</summary>
        public Text PromptText => _promptText;

        /// <summary>True when the prompt label is currently shown.</summary>
        public bool IsPromptVisible => _lastPromptVisible;

        /// <summary>The prompt string currently shown (null when hidden).</summary>
        public string DisplayedPrompt => _lastDisplayedPrompt;

        private void Awake()
        {
            _detector = GetComponent<InteractionDetector>();
            EnsureUi();
        }

        private void LateUpdate()
        {
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

            // Unity fake-null: the target may have been destroyed.
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
                    // Clear once so stale text is not left behind; reset cache to allow re-show.
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

            if (_crosshair == null)
            {
                var crosshairGo = new GameObject("Crosshair", typeof(RectTransform));
                crosshairGo.transform.SetParent(_canvas.transform, false);
                var rect = crosshairGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(8f, 8f);

                _crosshair = crosshairGo.AddComponent<Image>();
                _crosshair.color = _crosshairColor;
                _crosshair.raycastTarget = false;
            }

            if (_promptText == null)
            {
                var promptGo = new GameObject("PromptText", typeof(RectTransform));
                promptGo.transform.SetParent(_canvas.transform, false);
                var rect = promptGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -40f);
                rect.sizeDelta = new Vector2(480f, 40f);

                _promptText = promptGo.AddComponent<Text>();
                _promptText.alignment = TextAnchor.UpperCenter;
                _promptText.fontSize = _fontSize;
                _promptText.color = Color.white;
                _promptText.raycastTarget = false;
                _promptText.font = GetDefaultFont();
                _promptText.enabled = false;
            }

            // Crosshair is always visible regardless of prompt state.
            if (_crosshair != null)
                _crosshair.enabled = true;
        }

        private static Font GetDefaultFont()
        {
            // Unity 6 renamed the built-in font; fall back for older editors.
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}