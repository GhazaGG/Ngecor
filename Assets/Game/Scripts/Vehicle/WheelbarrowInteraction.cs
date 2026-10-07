using Ngecor.Interaction;
using Ngecor.Player;
using UnityEngine;

namespace Ngecor.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WheelbarrowInteraction : MonoBehaviour, IHoldInteractable
    {
        private Rigidbody _rigidbody;
        private PlayerMovement _interactingPlayer;

        public string InteractionPrompt => "Push";

        private Rigidbody Rigidbody => _rigidbody != null ? _rigidbody : (_rigidbody = GetComponent<Rigidbody>());

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null || Rigidbody == null || Rigidbody.isKinematic || _interactingPlayer != null)
                return false;

            var grab = interactor.GetComponent<PlayerGrab>();
            return grab != null && !grab.IsCarrying &&
                   Vector3.Distance(interactor.transform.position, transform.position) <= grab.MaxGrabDistance;
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
                _interactingPlayer = null;
        }

        private void LateUpdate()
        {
            if (_interactingPlayer == null)
                return;

            if (Rigidbody == null || Rigidbody.isKinematic || _interactingPlayer.LocalCamera == null)
            {
                _interactingPlayer = null;
                return;
            }

            var grab = _interactingPlayer.GetComponent<PlayerGrab>();
            if (grab == null || Vector3.Distance(_interactingPlayer.transform.position, transform.position) > grab.MaxGrabDistance)
                return;

            _interactingPlayer.ApplyMovementPush(Rigidbody, Rigidbody.worldCenterOfMass, Time.deltaTime);
        }

        private void OnDisable() => _interactingPlayer = null;
    }
}
