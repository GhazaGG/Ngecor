using UnityEngine;

namespace Ngecor.Construction
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ScaffoldingLoadFailure : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maximumPayloadMass = 150f;
        [SerializeField, Min(0f)] private float _characterControllerMass = 75f;
        [SerializeField, Min(0f)] private float _maximumImpactImpulse = 400f;
        [SerializeField] private BoxCollider _deckCollider;
        [SerializeField] private Transform _playerLoadProbe;
        [SerializeField] private Transform[] _breakParts = new Transform[4];
        [SerializeField] private float[] _partMasses = { 20f, 20f, 12.5f, 12.5f };

        private readonly Collider[] _overlaps = new Collider[16];
        private readonly Rigidbody[] _cargoBodies = new Rigidbody[16];
        private Rigidbody _body;
        private int _cargoCount;
        private bool _broken;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        public void Configure(BoxCollider deckCollider, Transform playerLoadProbe, Transform[] breakParts)
        {
            _deckCollider = deckCollider;
            _playerLoadProbe = playerLoadProbe;
            _breakParts = breakParts;
        }

        private void FixedUpdate()
        {
            if (_broken)
                return;

            float payloadMass = 0f;
            for (int i = 0; i < _cargoCount; i++)
            {
                if (_cargoBodies[i] != null)
                    payloadMass += _cargoBodies[i].mass;
                _cargoBodies[i] = null;
            }
            _cargoCount = 0;

            if (_playerLoadProbe != null)
            {
                Vector3 halfExtents = Vector3.Scale(_playerLoadProbe.localScale, _playerLoadProbe.parent.lossyScale) * 0.5f;
                int overlapCount = Physics.OverlapBoxNonAlloc(
                    _playerLoadProbe.position,
                    halfExtents,
                    _overlaps,
                    _playerLoadProbe.rotation,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

                float deckTop = _deckCollider != null ? _deckCollider.bounds.max.y : float.NegativeInfinity;
                for (int i = 0; i < overlapCount; i++)
                {
                    if (_overlaps[i] is not CharacterController player ||
                        Mathf.Abs(player.bounds.min.y - deckTop) > 0.18f)
                    {
                        continue;
                    }

                    payloadMass += _characterControllerMass;
                    Vector3 loadPoint = new Vector3(player.bounds.center.x, deckTop, player.bounds.center.z);
                    _body.AddForceAtPosition(Physics.gravity * _characterControllerMass, loadPoint, ForceMode.Force);
                }
                for (int i = 0; i < overlapCount; i++)
                    _overlaps[i] = null;
            }

            if (payloadMass > _maximumPayloadMass)
                BreakIntoParts();
        }

        private void OnCollisionStay(Collision collision)
        {
            Rigidbody cargo = collision.rigidbody;
            if (_broken || cargo == null || cargo == _body ||
                cargo.GetComponentInParent<ScaffoldingLoadFailure>() != null ||
                _deckCollider == null || cargo.position.y < _deckCollider.bounds.center.y)
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (Mathf.Abs(contact.normal.y) < 0.5f ||
                    contact.point.y < _deckCollider.bounds.max.y - 0.12f)
                {
                    continue;
                }

                for (int j = 0; j < _cargoCount; j++)
                {
                    if (_cargoBodies[j] == cargo)
                        return;
                }

                if (_cargoCount < _cargoBodies.Length)
                    _cargoBodies[_cargoCount++] = cargo;
                return;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_broken && collision.impulse.magnitude >= _maximumImpactImpulse)
                BreakIntoParts();
            else
                OnCollisionStay(collision);
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
