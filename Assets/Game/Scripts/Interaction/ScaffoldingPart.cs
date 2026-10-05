using UnityEngine;

namespace Ngecor.Interaction
{
    public enum ScaffoldingPartType
    {
        Catwalk,
        Support
    }

    [DisallowMultipleComponent]
    public sealed class ScaffoldingPart : MonoBehaviour
    {
        [SerializeField] private ScaffoldingPartType _partType;
        [SerializeField, Min(0f)] private float _connectionTolerance = 0.08f;
        [SerializeField, Min(1f)] private float _connectionBreakForce = 2000f;
        [SerializeField, Min(1f)] private float _connectionBreakTorque = 350f;

        private Rigidbody _body;
        private Collider[] _colliders;
        private ScaffoldingPart[] _previewCandidates;

        public bool IsSupport => _partType == ScaffoldingPartType.Support;

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            foreach (Joint joint in GetComponents<Joint>())
            {
                ScaffoldingPart other = joint.connectedBody != null
                    ? joint.connectedBody.GetComponent<ScaffoldingPart>()
                    : null;
                if (other != null)
                    SetBreakLimits(joint, other);
            }
        }

        public void BeginPlacementPreview()
        {
            _previewCandidates = FindObjectsByType<ScaffoldingPart>(FindObjectsSortMode.None);
        }

        public bool CanConnectToTouchingParts()
        {
            CacheComponents();
            if (_body == null)
                return false;

            Physics.SyncTransforms();
            foreach (ScaffoldingPart candidate in GetCandidates())
            {
                if (TryGetConnectionPoint(candidate, out _))
                    return true;
            }

            return false;
        }

        public void EndPlacementPreview()
        {
            _previewCandidates = null;
        }

        public void DetachFromOtherParts()
        {
            CacheComponents();
            if (_body == null)
                return;

            foreach (FixedJoint joint in GetComponents<FixedJoint>())
            {
                if (joint.connectedBody != null && joint.connectedBody.GetComponent<ScaffoldingPart>() != null)
                    DestroyJoint(joint);
            }

            foreach (ScaffoldingPart other in FindObjectsByType<ScaffoldingPart>(FindObjectsSortMode.None))
            {
                if (other == null || other == this)
                    continue;

                foreach (FixedJoint joint in other.GetComponents<FixedJoint>())
                {
                    if (joint.connectedBody == _body)
                        DestroyJoint(joint);
                }
            }
        }

        public int ConnectToTouchingParts()
        {
            CacheComponents();
            if (_body == null)
                return 0;

            Physics.SyncTransforms();
            int connectedCount = 0;

            foreach (ScaffoldingPart candidate in GetCandidates())
            {
                if (!TryGetConnectionPoint(candidate, out Vector3 contactPoint))
                    continue;

                ScaffoldingPart catwalk = _partType == ScaffoldingPartType.Catwalk ? this : candidate;
                ScaffoldingPart support = catwalk == this ? candidate : this;
                catwalk.ConnectTo(support, contactPoint);
                connectedCount++;
            }

            return connectedCount;
        }

        private ScaffoldingPart[] GetCandidates()
        {
            return _previewCandidates ?? FindObjectsByType<ScaffoldingPart>(FindObjectsSortMode.None);
        }

        private bool TryGetConnectionPoint(ScaffoldingPart candidate, out Vector3 contactPoint)
        {
            contactPoint = default;
            if (candidate == null || candidate == this || !AreComplementary(candidate))
                return false;

            candidate.CacheComponents();
            return candidate._body != null && candidate._body != _body && !HasConnection(candidate) &&
                   TryFindContact(candidate, out contactPoint);
        }

        private void CacheComponents()
        {
            if (_body == null)
                _body = GetComponent<Rigidbody>();
            if (_colliders == null || _colliders.Length == 0)
                _colliders = GetComponentsInChildren<Collider>();
        }

        private bool AreComplementary(ScaffoldingPart other)
        {
            return (_partType == ScaffoldingPartType.Catwalk && other._partType == ScaffoldingPartType.Support) ||
                   (_partType == ScaffoldingPartType.Support && other._partType == ScaffoldingPartType.Catwalk);
        }

        private bool HasConnection(ScaffoldingPart other)
        {
            foreach (Joint joint in GetComponents<Joint>())
            {
                if (joint != null && joint.connectedBody == other._body)
                    return true;
            }

            foreach (Joint joint in other.GetComponents<Joint>())
            {
                if (joint != null && joint.connectedBody == _body)
                    return true;
            }

            return false;
        }

        private bool TryFindContact(ScaffoldingPart other, out Vector3 contactPoint)
        {
            contactPoint = default;
            float tolerance = Mathf.Min(_connectionTolerance, other._connectionTolerance);
            float bestDistanceSqr = tolerance * tolerance;
            Vector3 bestOnThis = default;
            Vector3 bestOnOther = default;
            bool found = false;

            foreach (Collider ownCollider in _colliders)
            {
                if (!IsUsable(ownCollider))
                    continue;

                foreach (Collider otherCollider in other._colliders)
                {
                    if (!IsUsable(otherCollider) || GetBoundsGapSqr(ownCollider.bounds, otherCollider.bounds) > bestDistanceSqr)
                        continue;

                    SampleBounds(ownCollider, otherCollider, ref bestDistanceSqr, ref bestOnThis, ref bestOnOther, ref found);
                    SampleBounds(otherCollider, ownCollider, ref bestDistanceSqr, ref bestOnOther, ref bestOnThis, ref found);
                }
            }

            if (!found)
                return false;

            contactPoint = (bestOnThis + bestOnOther) * 0.5f;
            return true;
        }

        private static void SampleBounds(
            Collider source,
            Collider target,
            ref float bestDistanceSqr,
            ref Vector3 bestOnSource,
            ref Vector3 bestOnTarget,
            ref bool found)
        {
            Bounds bounds = source.bounds;
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            for (int z = 0; z < 3; z++)
            {
                Vector3 sample = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, x * 0.5f),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, y * 0.5f),
                    Mathf.Lerp(bounds.min.z, bounds.max.z, z * 0.5f));
                Vector3 onSource = source.ClosestPoint(sample);
                Vector3 onTarget = target.ClosestPoint(onSource);
                float distanceSqr = (onSource - onTarget).sqrMagnitude;
                if (distanceSqr > bestDistanceSqr)
                    continue;

                bestDistanceSqr = distanceSqr;
                bestOnSource = onSource;
                bestOnTarget = onTarget;
                found = true;
            }
        }

        private static bool IsUsable(Collider collider)
        {
            return collider != null && collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy;
        }

        private static float GetBoundsGapSqr(Bounds a, Bounds b)
        {
            float x = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
            float y = Mathf.Max(0f, Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y));
            float z = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
            return x * x + y * y + z * z;
        }

        private void ConnectTo(ScaffoldingPart support, Vector3 contactPoint)
        {
            FixedJoint joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = support._body;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = transform.InverseTransformPoint(contactPoint);
            joint.connectedAnchor = support.transform.InverseTransformPoint(contactPoint);
            joint.enableCollision = false;
            SetBreakLimits(joint, support);
        }

        private void SetBreakLimits(Joint joint, ScaffoldingPart other)
        {
            joint.breakForce = Mathf.Min(_connectionBreakForce, other._connectionBreakForce);
            joint.breakTorque = Mathf.Min(_connectionBreakTorque, other._connectionBreakTorque);
        }

        private static void DestroyJoint(FixedJoint joint)
        {
            Destroy(joint);
        }
    }
}
