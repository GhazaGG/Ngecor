using NUnit.Framework;
using UnityEngine;

namespace Ngecor.Interaction.Tests
{
    public class InteractableObjectTests
    {
        private GameObject _targetObject;

        [TearDown]
        public void TearDown()
        {
            if (_targetObject != null)
                Object.DestroyImmediate(_targetObject);
        }

        [Test]
        public void InteractableObject_DefaultValues_CanInteractAndReturnsPrompt()
        {
            _targetObject = new GameObject("TestInteractable");
            var interactable = _targetObject.AddComponent<InteractableObject>();

            Assert.That(interactable.CanInteract(null), Is.True);
            Assert.That(interactable.InteractionPrompt, Is.EqualTo("Interact"));
        }

        [Test]
        public void InteractableObject_SetInteractableFalse_CanInteractReturnsFalse()
        {
            _targetObject = new GameObject("TestInteractable");
            var interactable = _targetObject.AddComponent<InteractableObject>();
            interactable.SetInteractable(false);

            Assert.That(interactable.CanInteract(null), Is.False);
        }
    }
}
