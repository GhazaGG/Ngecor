using UnityEngine;

namespace Ngecor.Interaction
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(GameObject interactor);
    }

    public interface IHoldInteractable : IInteractable
    {
        bool CanInteractFrom(GameObject interactor, Collider collider);
        bool TryBeginInteraction(GameObject interactor);
        void EndInteraction(GameObject interactor);
    }
}
