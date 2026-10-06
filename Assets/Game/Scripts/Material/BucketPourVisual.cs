using Ngecor.Interaction;
using UnityEngine;

namespace Ngecor.Material
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(BucketPourAction), typeof(GrabbableObject))]
    public sealed class BucketPourVisual : MonoBehaviour
    {
        [SerializeField] private Transform _model;
        [SerializeField] private ParticleSystem _sandFlow;
        [SerializeField, Range(0f, 90f)] private float _tiltDegrees = 65f;
        [SerializeField, Min(1f)] private float _turnSpeed = 360f;

        private BucketPourAction _pour;
        private GrabbableObject _grabbable;
        private Quaternion _restRotation = Quaternion.identity;
        private BulkMaterialContainer _receiver;
        private Collider _receiverCollider;

        private void Awake()
        {
            _pour = GetComponent<BucketPourAction>();
            _grabbable = GetComponent<GrabbableObject>();
            if (_model != null)
                _restRotation = _model.localRotation;
        }

        private void LateUpdate()
        {
            if (_model == null || _sandFlow == null)
                return;

            var pouring = _grabbable.IsHeld && _pour.IsPouring;
            var targetRotation = pouring
                ? _restRotation * Quaternion.Euler(_tiltDegrees, 0f, 0f) : _restRotation;
            _model.localRotation = _grabbable.IsHeld
                ? Quaternion.RotateTowards(_model.localRotation, targetRotation, _turnSpeed * Time.deltaTime)
                : _restRotation;

            // Emit only after the lip is tipped; particles are feedback, not material state.
            var emitting = pouring && Quaternion.Angle(_restRotation, _model.localRotation) >= 30f;
            if (emitting)
            {
                var receiver = _pour.PourReceiver;
                if (_receiver != receiver)
                {
                    _receiver = receiver;
                    _receiverCollider = receiver.GetComponent<Collider>();
                }

                var destination = _receiverCollider != null
                    ? new Vector3(_receiverCollider.bounds.center.x,
                        _receiverCollider.bounds.max.y + 0.02f, _receiverCollider.bounds.center.z)
                    : receiver.transform.position;
                var main = _sandFlow.main;
                var flightTime = main.startLifetime.constant;
                var velocity = (destination - _sandFlow.transform.position) / flightTime
                    - Physics.gravity * (main.gravityModifier.constant * flightTime * 0.5f);
                if (velocity.sqrMagnitude > 0.0001f)
                    _sandFlow.transform.rotation = Quaternion.LookRotation(velocity);
                main.startSpeed = velocity.magnitude;
                if (!_sandFlow.isPlaying)
                    _sandFlow.Play(false);
            }

            var emission = _sandFlow.emission;
            emission.enabled = emitting;
        }

        private void OnDisable()
        {
            if (_model != null)
                _model.localRotation = _restRotation;
            if (_sandFlow != null)
                _sandFlow.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
