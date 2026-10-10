using Ngecor.Interaction;
using UnityEngine;

namespace Ngecor.Multiplayer.Tests
{
    public sealed class FakeHoldInteractable : MonoBehaviour, IHoldInteractable
    {
        public bool AcceptBegin { get; set; } = true;
        public int BeginCount { get; private set; }
        public int EndCount { get; private set; }

        public string InteractionPrompt => "Hold";
        public bool CanInteract(GameObject interactor) => true;
        public bool CanInteractFrom(GameObject interactor, Collider collider) => true;

        public bool TryBeginInteraction(GameObject interactor)
        {
            BeginCount++;
            return AcceptBegin;
        }

        public void EndInteraction(GameObject interactor) => EndCount++;
    }
}
