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
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly Dictionary<Mesh, Mesh> s_OutlineMeshCache = new Dictionary<Mesh, Mesh>();

        [SerializeField] private Material _outlineMaterial;
        [SerializeField, Min(0.001f)] private float _outlineWidth = 0.02f;

        private InteractionDetector _detector;
        private readonly List<MeshFilter> _cachedMeshFilters = new List<MeshFilter>(16);
        private MaterialPropertyBlock _propertyBlock;

        private Object _currentTargetObject;
        private bool _hasHighlight;

        /// <summary>True while an active target is highlighted.</summary>
        public bool HasHighlight => _hasHighlight;

        /// <summary>Number of mesh filters currently cached for outline drawing.</summary>
        public int HighlightedMeshCount => _cachedMeshFilters.Count;

        /// <summary>Alias for HighlightedMeshCount for backwards test compatibility.</summary>
        public int HighlightedRendererCount => _cachedMeshFilters.Count;

        /// <summary>The outline width in meters (extrude distance along vertex normal).</summary>
        public float OutlineWidth
        {
            get => _outlineWidth;
            set => _outlineWidth = Mathf.Max(0.001f, value);
        }

        /// <summary>The outline material assigned to draw the inverted-hull edge.</summary>
        public Material OutlineMaterial
        {
            get => _outlineMaterial;
            set => _outlineMaterial = value;
        }

        private void Awake()
        {
            _detector = GetComponent<InteractionDetector>();
            _propertyBlock = new MaterialPropertyBlock();
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

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
            _propertyBlock.SetFloat(OutlineWidthId, _outlineWidth);

            for (var i = 0; i < _cachedMeshFilters.Count; i++)
            {
                var mf = _cachedMeshFilters[i];
                if (mf == null || !mf.gameObject.activeInHierarchy)
                    continue;

                var mesh = mf.sharedMesh;
                if (mesh == null)
                    continue;

                var outlineMesh = GetOrCreateOutlineMesh(mesh);
                if (outlineMesh == null)
                    continue;

                var matrix = mf.transform.localToWorldMatrix;
                var layer = mf.gameObject.layer;
                var subMeshCount = outlineMesh.subMeshCount;

                for (var s = 0; s < subMeshCount; s++)
                {
                    Graphics.DrawMesh(outlineMesh, matrix, _outlineMaterial, layer, null, s, _propertyBlock);
                }
            }
        }

        /// <summary>
        /// Returns an outline mesh with smoothed/welded normals across co-located vertices,
        /// ensuring corners and hard edges (e.g. on cubes) do not tear or break apart during extrusion.
        /// Statically cached per source Mesh for zero steady-state GC allocation.
        /// </summary>
        public static Mesh GetOrCreateOutlineMesh(Mesh source)
        {
            if (source == null)
                return null;

            if (s_OutlineMeshCache.TryGetValue(source, out var cached) && cached != null)
                return cached;

            if (!source.isReadable)
            {
                s_OutlineMeshCache[source] = source;
                return source;
            }

            var vertices = source.vertices;
            var normals = source.normals;

            if (vertices == null || normals == null || vertices.Length == 0 || vertices.Length != normals.Length)
            {
                s_OutlineMeshCache[source] = source;
                return source;
            }

            var normalMap = new Dictionary<QuantizedVertex, Vector3>(vertices.Length);
            for (var i = 0; i < vertices.Length; i++)
            {
                var key = new QuantizedVertex(vertices[i]);
                normalMap.TryGetValue(key, out var sum);
                normalMap[key] = sum + normals[i];
            }

            var smoothedNormals = new Vector3[normals.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                var key = new QuantizedVertex(vertices[i]);
                var sum = normalMap[key];
                smoothedNormals[i] = sum.sqrMagnitude > 0.0001f ? sum.normalized : normals[i];
            }

            var outlineMesh = Object.Instantiate(source);
            outlineMesh.name = source.name + "_Outline";
            outlineMesh.hideFlags = HideFlags.DontSave;
            outlineMesh.normals = smoothedNormals;

            s_OutlineMeshCache[source] = outlineMesh;
            return outlineMesh;
        }

        /// <summary>Clears the static outline mesh cache (useful for cleanup and tests).</summary>
        public static void ClearMeshCache()
        {
            s_OutlineMeshCache.Clear();
        }

        private readonly struct QuantizedVertex : System.IEquatable<QuantizedVertex>
        {
            private readonly int _x;
            private readonly int _y;
            private readonly int _z;

            public QuantizedVertex(Vector3 v)
            {
                _x = Mathf.RoundToInt(v.x * 2000f);
                _y = Mathf.RoundToInt(v.y * 2000f);
                _z = Mathf.RoundToInt(v.z * 2000f);
            }

            public bool Equals(QuantizedVertex other) => _x == other._x && _y == other._y && _z == other._z;
            public override bool Equals(object obj) => obj is QuantizedVertex other && Equals(other);
            public override int GetHashCode() => unchecked((_x * 73856093) ^ (_y * 19349663) ^ (_z * 83492791));
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
