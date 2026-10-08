using System.Collections.Generic;
using UnityEngine;

namespace Ngecor.Interaction
{
    /// <summary>
    /// Applies a lightweight highlight tint to the <see cref="InteractionDetector"/>'s current
    /// target using a <see cref="MaterialPropertyBlock"/>. The shared material is never mutated:
    /// only <see cref="Renderer.SetPropertyBlock"/> is used, so no material instances are leaked
    /// and no asset on disk is modified.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractionDetector))]
    public class InteractionHighlighter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private bool _useEmission = false;

        private InteractionDetector _detector;
        private readonly List<Renderer> _highlightedRenderers = new List<Renderer>(16);
        private MaterialPropertyBlock _propertyBlock;

        private Object _currentTargetObject;
        private bool _hasHighlight;

        /// <summary>True while at least one renderer currently carries the highlight block.</summary>
        public bool HasHighlight => _hasHighlight;

        /// <summary>Number of renderers currently highlighted (0 when nothing is highlighted).</summary>
        public int HighlightedRendererCount => _highlightedRenderers.Count;

        /// <summary>Color applied to the target renderers.</summary>
        public Color HighlightColor
        {
            get => _highlightColor;
            set => _highlightColor = value;
        }

        private void Awake()
        {
            _detector = GetComponent<InteractionDetector>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_detector == null)
                _detector = GetComponent<InteractionDetector>();

            var target = _detector != null ? _detector.CurrentTarget : null;

            if (!IsHighlightable(target))
            {
                ClearHighlight();
                return;
            }

            var targetObject = target as Object;
            if (targetObject == _currentTargetObject)
                return;

            ClearHighlight();
            _currentTargetObject = targetObject;
            ApplyHighlight(targetObject);
        }

        private bool IsHighlightable(IInteractable target)
        {
            if (target == null)
                return false;

            // Guard against destroyed / deactivated targets (Unity's fake-null semantics).
            var unityObject = target as Object;
            if (unityObject == null)
                return false;

            var behaviour = unityObject as Behaviour;
            if (behaviour != null && !behaviour.isActiveAndEnabled)
                return false;

            var component = unityObject as Component;
            if (component != null && !component.gameObject.activeInHierarchy)
                return false;

            // A held grabbable object should never stay highlighted.
            if (target is GrabbableObject grabbable && grabbable.IsHeld)
                return false;

            return true;
        }

        private void ApplyHighlight(Object targetObject)
        {
            var component = targetObject as Component;
            if (component == null)
                return;

            component.GetComponentsInChildren(_highlightedRenderers);
            if (_highlightedRenderers.Count == 0)
                return;

            _propertyBlock.Clear();
            _propertyBlock.SetColor(BaseColorId, _highlightColor);
            _propertyBlock.SetColor(ColorId, _highlightColor);
            if (_useEmission)
                _propertyBlock.SetColor(EmissionColorId, _highlightColor);

            for (var i = 0; i < _highlightedRenderers.Count; i++)
            {
                var renderer = _highlightedRenderers[i];
                if (renderer == null)
                    continue;
                renderer.SetPropertyBlock(_propertyBlock);
            }

            _hasHighlight = true;
        }

        /// <summary>Removes the highlight from every cached renderer and resets state.</summary>
        public void ClearHighlight()
        {
            for (var i = 0; i < _highlightedRenderers.Count; i++)
            {
                var renderer = _highlightedRenderers[i];
                if (renderer == null)
                    continue;
                renderer.SetPropertyBlock(null);
            }

            _highlightedRenderers.Clear();
            _currentTargetObject = null;
            _hasHighlight = false;
        }
    }
}