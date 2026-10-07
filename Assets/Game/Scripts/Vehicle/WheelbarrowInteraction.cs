using Ngecor.Interaction;
using Ngecor.Player;
using UnityEngine;

namespace Ngecor.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WheelbarrowInteraction : MonoBehaviour, IHoldInteractable
    {
        [SerializeField] private Collider[] _handleColliders;

        private Rigidbody _rigidbody;
        private PlayerMovement _interactingPlayer;

        public string InteractionPrompt => "Push";

        private Rigidbody Rigidbody => _rigidbody != null ? _rigidbody : (_rigidbody = GetComponent<Rigidbody>());

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null || Rigidbody == null || Rigidbody.isKinematic || _interactingPlayer != null)
                return false;

            var grab = interactor.GetComponent<PlayerGrab>();
            return grab != null && !grab.IsCarrying && IsAnyHandleInRange(interactor.transform.position, grab.MaxGrabDistance);
        }

        public bool CanInteractFrom(GameObject interactor, Collider collider)
        {
            var grab = interactor != null ? interactor.GetComponent<PlayerGrab>() : null;
            return grab != null && IsHandleCollider(collider) &&
                   Vector3.Distance(interactor.transform.position, collider.ClosestPoint(interactor.transform.position)) <= grab.MaxGrabDistance;
        }

        public bool TryBeginInteraction(GameObject interactor)
        {
            if (!CanInteract(interactor))
                return false;

            var player = interactor.GetComponent<PlayerMovement>();
            if (player == null || player.LocalCamera == null)
                return false;

            _interactingPlayer = player;
            return true;
        }

        public void EndInteraction(GameObject interactor)
        {
            if (_interactingPlayer != null && _interactingPlayer.gameObject == interactor)
                StopInteraction();
        }

        private void LateUpdate()
        {
            if (_interactingPlayer == null)
                return;

            if (Rigidbody == null || Rigidbody.isKinematic || _interactingPlayer.LocalCamera == null)
            {
                StopInteraction();
                return;
            }

            var grab = _interactingPlayer.GetComponent<PlayerGrab>();
            if (grab == null || !IsAnyHandleInRange(_interactingPlayer.transform.position, grab.MaxGrabDistance))
            {
                StopInteraction();
                return;
            }

            _interactingPlayer.ApplyMovementPush(Rigidbody, Rigidbody.worldCenterOfMass, Time.deltaTime);
        }

        private void OnDisable() => StopInteraction();

        private bool IsHandleCollider(Collider collider)
        {
            if (collider == null || _handleColliders == null)
                return false;

            for (var i = 0; i < _handleColliders.Length; i++)
            {
                if (_handleColliders[i] == collider)
                    return true;
            }

            return false;
        }

        private bool IsAnyHandleInRange(Vector3 position, float maxDistance)
        {
            if (_handleColliders == null)
                return false;

            var maxDistanceSquared = maxDistance * maxDistance;
            for (var i = 0; i < _handleColliders.Length; i++)
            {
                var collider = _handleColliders[i];
                if (collider != null && (collider.ClosestPoint(position) - position).sqrMagnitude <= maxDistanceSquared)
                    return true;
            }

            return false;
        }

        private void StopInteraction()
        {
            var player = _interactingPlayer;
            _interactingPlayer = null;
            if (player != null)
                player.GetComponent<PlayerGrab>()?.NotifyInteractionEnded(this);
        }
    }
}
