using UnityEngine;

namespace Ngecor.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PickupWheels : MonoBehaviour
    {
        private const float MinGroundNormalY = 0.3f;
        // AddForce resets the PhysX wake counter, so impulses below solver noise are skipped or the body never sleeps.
        private const float NoiseImpulsePerKg = 5e-4f;
        // A held pickup this still (0.005 m/s) for this many fixed steps is put to sleep; its brake impulses alone would keep it awake forever.
        private const float StillSpeedSqr = 2.5e-5f;
        private const int StillStepsToSleep = 25;
        // Speeding up by more than this (m/s) while "still" means the load is over capacity and creeping: never sleep. A held pickup keeps a constant speed.
        private const float StillSpeedGain = 4e-4f;
        private const int ContactBufferSize = 16;

        [Tooltip("Sphere colliders acting as wheels. Their transform.forward is the rolling direction.")]
        [SerializeField] private SphereCollider[] _wheels;
        [Tooltip("Optional visual transforms, same order and length as Wheels. Spun around local X while rolling. Null entries are skipped.")]
        [SerializeField] private Transform[] _wheelVisuals;
        [Tooltip("Which wheels carry the handbrake (for example the rear ones). Same order and length as Wheels.")]
        [SerializeField] private bool[] _brakedWheels;
        [SerializeField] private bool _handbrakeEngaged = true;
        [Tooltip("Total holding force (N) of the handbrake across all braked wheels. Max cargo mass before rollback ~= (holdForce + free wheel rolling resistance) / (g * (sin(slope) - mu * cos(slope))) - pickup mass, mu being the effective wheel friction (about 0.09).")]
        [SerializeField, Min(0f)] private float _handbrakeHoldForce = 544f;
        [Tooltip("Sideways grip (N) of each wheel on the ground.")]
        [SerializeField, Min(0f)] private float _lateralGripForce = 4000f;
        [Tooltip("Total rolling resistance (N) of free wheels, or of all wheels while the handbrake is released.")]
        [SerializeField, Min(0f)] private float _rollingResistanceForce = 40f;

        private Rigidbody _rigidbody;
        private ContactPoint[] _contacts;
        private bool[] _grounded;
        private Vector3[] _contactPoints;
        private Vector3[] _contactNormals;
        private float[] _wheelRadii;
        private int _stillSteps;
        private float _stillStartSpeed;

        public bool HandbrakeEngaged => _handbrakeEngaged;

        public void SetHandbrake(bool engaged)
        {
            if (_handbrakeEngaged != engaged && _rigidbody != null)
                _rigidbody.WakeUp();

            _handbrakeEngaged = engaged;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _contacts = new ContactPoint[ContactBufferSize];

            var count = _wheels != null ? _wheels.Length : 0;
            _grounded = new bool[count];
            _contactPoints = new Vector3[count];
            _contactNormals = new Vector3[count];
            _wheelRadii = new float[count];
            for (var i = 0; i < count; i++)
            {
                if (_wheels[i] == null)
                    continue;

                var scale = _wheels[i].transform.lossyScale;
                _wheelRadii[i] = Mathf.Max(0.01f, _wheels[i].radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)));
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            var count = collision.GetContacts(_contacts);
            for (var c = 0; c < count; c++)
            {
                var contact = _contacts[c];
                if (contact.normal.y < MinGroundNormalY)
                    continue;

                for (var i = 0; i < _grounded.Length; i++)
                {
                    if (_grounded[i] || contact.thisCollider != _wheels[i])
                        continue;

                    _grounded[i] = true;
                    _contactPoints[i] = contact.point;
                    _contactNormals[i] = contact.normal;
                    break;
                }
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody.IsSleeping())
            {
                ClearGrounded();
                return;
            }

            var groundedCount = 0;
            var brakedGroundedCount = 0;
            for (var i = 0; i < _grounded.Length; i++)
            {
                if (!_grounded[i])
                    continue;

                groundedCount++;
                if (IsBraked(i))
                    brakedGroundedCount++;
            }

            if (groundedCount > 0)
            {
                ApplyWheelForces(groundedCount, brakedGroundedCount);
                UpdateHoldSleep(brakedGroundedCount);
            }
            else
                _stillSteps = 0;

            ClearGrounded();
        }

        private void ApplyWheelForces(int groundedCount, int brakedGroundedCount)
        {
            var dt = Time.fixedDeltaTime;
            var mass = _rigidbody.mass;
            var maxGrip = _lateralGripForce * dt;

            for (var i = 0; i < _grounded.Length; i++)
            {
                if (!_grounded[i])
                    continue;

                var point = _contactPoints[i];
                var normal = _contactNormals[i];
                var velocity = Vector3.ProjectOnPlane(_rigidbody.GetPointVelocity(point), normal);
                var forward = Vector3.ProjectOnPlane(_wheels[i].transform.forward, normal);
                if (forward.sqrMagnitude < 1e-6f)
                    continue;

                forward.Normalize();
                var forwardVelocity = forward * Vector3.Dot(velocity, forward);
                var lateralVelocity = velocity - forwardVelocity;

                var grip = Vector3.ClampMagnitude(-lateralVelocity * (mass / groundedCount), maxGrip);
                ApplyImpulse(grip, point);

                Vector3 longitudinal;
                if (_handbrakeEngaged && IsBraked(i))
                {
                    var maxHold = _handbrakeHoldForce / brakedGroundedCount * dt;
                    // Gravity is added inside the step after this impulse; cancel the velocity the body will have then, or a held pickup creeps by g*sin*dt every step.
                    var predictedVelocity = forwardVelocity + forward * (Vector3.Dot(Physics.gravity, forward) * dt);
                    longitudinal = Vector3.ClampMagnitude(-predictedVelocity * (mass / brakedGroundedCount), maxHold);
                }
                else
                {
                    var maxRolling = _rollingResistanceForce * dt / groundedCount;
                    longitudinal = Vector3.ClampMagnitude(-forwardVelocity * (mass / groundedCount), maxRolling);
                }

                ApplyImpulse(longitudinal, point);
            }
        }

        private void LateUpdate()
        {
            if (_wheelVisuals == null)
                return;

            var dt = Time.deltaTime;
            var count = Mathf.Min(_wheelVisuals.Length, _wheelRadii.Length);
            for (var i = 0; i < count; i++)
            {
                var visual = _wheelVisuals[i];
                if (visual == null || _wheels[i] == null)
                    continue;

                var wheelTransform = _wheels[i].transform;
                var forwardSpeed = Vector3.Dot(_rigidbody.GetPointVelocity(wheelTransform.position), wheelTransform.forward);
                var degrees = forwardSpeed / _wheelRadii[i] * Mathf.Rad2Deg * dt;
                visual.Rotate(Vector3.right, degrees, Space.Self);
            }
        }

        private void UpdateHoldSleep(int brakedGroundedCount)
        {
            if (_handbrakeEngaged && brakedGroundedCount > 0
                && _rigidbody.linearVelocity.sqrMagnitude < StillSpeedSqr
                && _rigidbody.angularVelocity.sqrMagnitude < StillSpeedSqr)
            {
                var speed = _rigidbody.linearVelocity.magnitude;
                if (_stillSteps == 0)
                    _stillStartSpeed = speed;
                else if (speed > _stillStartSpeed + StillSpeedGain)
                {
                    _stillSteps = 0;
                    return;
                }

                if (++_stillSteps < StillStepsToSleep)
                    return;

                _rigidbody.Sleep();
            }

            _stillSteps = 0;
        }

        private void ApplyImpulse(Vector3 impulse, Vector3 point)
        {
            var noiseFloor = _rigidbody.mass * NoiseImpulsePerKg;
            if (impulse.sqrMagnitude < noiseFloor * noiseFloor)
                return;

            _rigidbody.AddForceAtPosition(impulse, point, ForceMode.Impulse);
        }

        private bool IsBraked(int index)
        {
            return _brakedWheels != null && index < _brakedWheels.Length && _brakedWheels[index];
        }

        private void ClearGrounded()
        {
            for (var i = 0; i < _grounded.Length; i++)
                _grounded[i] = false;
        }
    }
}
