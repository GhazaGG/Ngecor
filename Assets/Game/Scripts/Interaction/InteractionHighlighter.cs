using System.Collections.Generic;
using UnityEngine;

namespace Ngecor.Interaction
{
    /// <summary>
    /// Renders a lightweight edge-only white silhouette outline around the focused <see cref="IInteractable"/>
    /// using an inverted-hull shader drawn via <see cref="Graphics.DrawMesh"/>.
    /// The target's original materials, textures, and shaders remain 100% untouched.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InteractionDetector))]
    public class InteractionHighlighter : MonoBehaviour
    {
        [SerializeField] private Material _outlineMaterial;

        private InteractionDetector _detector;
        private readonly List<MeshFilter> _cachedMeshFilters = new List<MeshFilter>(16);

        private Object _currentTargetObject;
        private bool _hasHighlight;

        /// <summary>True while an active target is highlighted.</summary>
        public bool HasHighlight => _hasHighlight;

        /// <summary>Number of mesh filters currently cached for outline drawing.</summary>
        public int HighlightedMeshCount => _cachedMeshFilters.Count;

        /// <summary>Alias for HighlightedMeshCount for backwards test compatibility.</summary>
        public int HighlightedRendererCount => _cachedMeshFilters.Count;

        /// <summary>The outline material assigned to draw the inverted-hull edge.</summary>
        public Material OutlineMaterial
        {
            get => _outlineMaterial;
            set => _outlineMaterial = value;
        }

        private void Awake()
        {
            _detector = GetComponent<InteractionDetector>();
        }

        private void LateUpdate()
        {
            UpdateHighlight();
            RenderOutline();
        }

        /// <summary>
        /// Evaluates detector state and updates highlight cache.
        /// Public so tests and callers can drive state updates deterministically.
        /// </summary>
        public void UpdateHighlight()
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

            // Guard against destroyed / fake-null Unity objects.
            var unityObject = target as Object;
            if (unityObject == null)
                return false;

            var behaviour = unityObject as Behaviour;
            if (behaviour != null && !behaviour.isActiveAndEnabled)
                return false;

            var component = unityObject as Component;
            if (component != null && !component.gameObject.activeInHierarchy)
                return false;

            // Immediately drop highlight if object is held by a player.
            if (target is GrabbableObject grabbable && grabbable.IsHeld)
                return false;

            return true;
        }

        private void ApplyHighlight(Object targetObject)
        {
            var component = targetObject as Component;
            if (component == null)
                return;

            component.GetComponentsInChildren(false, _cachedMeshFilters);
            _hasHighlight = _cachedMeshFilters.Count > 0;
        }

        private void RenderOutline()
        {
            if (!_hasHighlight || _cachedMeshFilters.Count == 0)
                return;

            if (_outlineMaterial == null)
            {
                var shader = Shader.Find("Ngecor/OutlineEdge");
                if (shader != null)
                {
                    _outlineMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.DontSave
                    };
                }
            }

            if (_outlineMaterial == null)
                return;

            for (var i = 0; i < _cachedMeshFilters.Count; i++)
            {
                var mf = _cachedMeshFilters[i];
                if (mf == null || !mf.gameObject.activeInHierarchy)
                    continue;

                var mesh = mf.sharedMesh;
                if (mesh == null)
                    continue;

                var matrix = mf.transform.localToWorldMatrix;
                var layer = mf.gameObject.layer;
                var subMeshCount = mesh.subMeshCount;

                for (var s = 0; s < subMeshCount; s++)
                {
                    Graphics.DrawMesh(mesh, matrix, _outlineMaterial, layer, null, s);
                }
            }
        }

        /// <summary>Clears cached mesh filters and drops highlight state.</summary>
        public void ClearHighlight()
        {
            _cachedMeshFilters.Clear();
            _currentTargetObject = null;
            _hasHighlight = false;
        }
    }
}
