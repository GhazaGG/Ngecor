using UnityEngine;

namespace Ngecor.Construction
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ScaffoldingLoadFailure : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _wobbleThreshold = 160f;
        [SerializeField, Min(1f)] private float _collapseThreshold = 190f;
        [SerializeField, Min(0f)] private float _characterControllerMass = 75f;
        [SerializeField, Min(0f)] private float _wobbleDuration = 2f;
        [SerializeField, Min(0f)] private float _wobbleTorque = 2f;
        [SerializeField, Min(0f)] private float _maximumImpactImpulse = 8f;
        [SerializeField] private Transform _playerLoadProbe;
        [SerializeField] private Transform[] _breakParts = new Transform[4];
        [SerializeField] private float[] _partMasses = { 20f, 20f, 12.5f, 12.5f };

        private Collider[] _overlaps = new Collider[16];
        private Rigidbody[] _seenBodies = new Rigidbody[16];
        private CharacterController[] _seenPlayers = new CharacterController[16];
        private Rigidbody _body;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _wobbleTime;
        private bool _wobbling;
        private bool _impactWobble;
        private bool _impactWarning;
        private bool _broken;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _restPosition = _body.position;
            _restRotation = _body.rotation;
            _body.isKinematic = true;
        }

        private void FixedUpdate()
        {
            if (_broken)
                return;

            float payloadMass = MeasurePayloadMass();
            if (payloadMass >= _collapseThreshold)
            {
                BreakIntoParts();
                return;
            }

            bool overloaded = payloadMass >= _wobbleThreshold;
            bool impactWarning = _impactWarning;
            _impactWarning = false;

            if (impactWarning)
                _impactWobble = true;

            if (!_wobbling && (overloaded || impactWarning))
                StartWobble(impactWarning);

            if (!_wobbling)
                return;

            _wobbleTime += Time.fixedDeltaTime;
            float sway = Mathf.Sin(_wobbleTime * 9f) * _wobbleTorque;
            _body.AddTorque(transform.forward * sway + transform.right * sway * 0.35f, ForceMode.Acceleration);

            if (_wobbleTime >= _wobbleDuration)
            {
                if (overloaded || _impactWobble)
                {
                    BreakIntoParts();
                    return;
                }

                CalmDown();
            }
            else if (!overloaded && !_impactWobble)
            {
                CalmDown();
            }
        }

        private float MeasurePayloadMass()
        {
            if (_playerLoadProbe == null)
                return 0f;

            int overlapCount;
            do
            {
                overlapCount = Physics.OverlapBoxNonAlloc(
                    _playerLoadProbe.position,
                    _playerLoadProbe.lossyScale * 0.5f,
                    _overlaps,
                    _playerLoadProbe.rotation,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

                if (overlapCount < _overlaps.Length)
                    break;

                int capacity = _overlaps.Length * 2;
                System.Array.Resize(ref _overlaps, capacity);
                System.Array.Resize(ref _seenBodies, capacity);
                System.Array.Resize(ref _seenPlayers, capacity);
            }
            while (true);

            float payloadMass = 0f;
            int bodyCount = 0;
            int playerCount = 0;

            for (int i = 0; i < overlapCount; i++)
            {
                Collider collider = _overlaps[i];
                if (collider is CharacterController player)
                {
                    if (!Contains(_seenPlayers, playerCount, player))
                    {
                        _seenPlayers[playerCount++] = player;
                        payloadMass += _characterControllerMass;
                    }
                    continue;
                }

                Rigidbody cargo = collider.attachedRigidbody;
                if (cargo == null || cargo == _body || cargo.isKinematic ||
                    Contains(_seenBodies, bodyCount, cargo))
                {
                    continue;
                }

                _seenBodies[bodyCount++] = cargo;
                if (cargo.GetComponentInParent<ScaffoldingLoadFailure>() != null)
                    continue;

                payloadMass += cargo.mass;
            }

            for (int i = 0; i < overlapCount; i++)
                _overlaps[i] = null;
            for (int i = 0; i < bodyCount; i++)
                _seenBodies[i] = null;
            for (int i = 0; i < playerCount; i++)
                _seenPlayers[i] = null;

            return payloadMass;
        }

        private static bool Contains(Rigidbody[] bodies, int count, Rigidbody candidate)
        {
            for (int i = 0; i < count; i++)
            {
                if (bodies[i] == candidate)
                    return true;
            }
            return false;
        }

        private static bool Contains(CharacterController[] players, int count, CharacterController candidate)
        {
            for (int i = 0; i < count; i++)
            {
                if (players[i] == candidate)
                    return true;
            }
            return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_broken && collision.impulse.magnitude >= _maximumImpactImpulse)
                _impactWarning = true;
        }

        private void StartWobble(bool impactWobble)
        {
            _wobbling = true;
            _impactWobble = impactWobble;
            _wobbleTime = 0f;
            _body.isKinematic = false;
            _body.WakeUp();
        }

        private void CalmDown()
        {
            _wobbling = false;
            _impactWobble = false;
            _wobbleTime = 0f;
            _body.isKinematic = true;
            _body.position = _restPosition;
            _body.rotation = _restRotation;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        private void BreakIntoParts()
        {
            _broken = true;
            Vector3 linearVelocity = _body.linearVelocity;
            Vector3 angularVelocity = _body.angularVelocity;
            Vector3 origin = transform.position;

            int count = Mathf.Min(_breakParts.Length, _partMasses.Length);
            for (int i = 0; i < count; i++)
            {
                Transform part = _breakParts[i];
                if (part == null)
                    continue;

                Vector3 partPosition = part.position;
                part.SetParent(null, true);

                Rigidbody partBody = part.gameObject.AddComponent<Rigidbody>();
                partBody.mass = _partMasses[i];
                partBody.interpolation = RigidbodyInterpolation.Interpolate;
                partBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                partBody.linearDamping = 0.2f;
                partBody.angularDamping = 0.5f;
                partBody.linearVelocity = linearVelocity + Vector3.Cross(angularVelocity, partPosition - origin);
                partBody.angularVelocity = angularVelocity;
                partBody.WakeUp();
            }

            Destroy(_body);
            Destroy(gameObject);
        }
    }
}
