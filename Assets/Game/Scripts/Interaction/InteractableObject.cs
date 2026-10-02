using UnityEngine;

namespace Ngecor.Interaction
{
    [DisallowMultipleComponent]
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Interact";
        [SerializeField] private bool _canInteract = true;

        public string InteractionPrompt => _prompt;

        public bool CanInteract(GameObject interactor) => _canInteract;

        public void SetInteractable(bool canInteract) => _canInteract = canInteract;

        public void SetPrompt(string prompt) => _prompt = prompt;
    }
}
