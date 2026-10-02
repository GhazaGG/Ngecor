using NUnit.Framework;
using UnityEngine;

namespace Ngecor.Interaction.Tests
{
    public class GrabbableObjectTests
    {
        private GameObject _targetObject;
        private GrabbableObject _grabbable;
        private GameObject _interactor;

        [SetUp]
        public void SetUp()
        {
            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _grabbable = _targetObject.AddComponent<GrabbableObject>();
            _interactor = new GameObject("Player");
        }

        [TearDown]
        public void TearDown()
        {
            if (_targetObject != null)
                Object.DestroyImmediate(_targetObject);
            if (_interactor != null)
                Object.DestroyImmediate(_interactor);
        }

        [Test]
        public void ImplementsIInteractableWithDefaultGrabPrompt()
        {
            Assert.That(_grabbable, Is.InstanceOf<IInteractable>());
            IInteractable interactable = _grabbable;
            Assert.That(interactable.InteractionPrompt, Is.EqualTo("Grab"));
        }

        [Test]
        public void CanInteractReturnsTrueByDefault()
        {
            Assert.That(_grabbable.CanInteract(_interactor), Is.True);
            Assert.That(_grabbable.IsHeld, Is.False);
            Assert.That(_grabbable.CurrentHolder, Is.Null);
        }

        [Test]
        public void CanInteractReturnsFalseWhenHeld()
        {
            _grabbable.OnGrab(_interactor);

            Assert.That(_grabbable.IsHeld, Is.True);
            Assert.That(_grabbable.CurrentHolder, Is.EqualTo(_interactor));
            Assert.That(_grabbable.CanInteract(_interactor), Is.False);
        }

        [Test]
        public void CanInteractReturnsFalseWhenDisabledBySetGrabbable()
        {
            _grabbable.SetGrabbable(false);

            Assert.That(_grabbable.CanInteract(_interactor), Is.False);
        }

        [Test]
        public void OnReleaseResetsHeldState()
        {
            _grabbable.OnGrab(_interactor);
            _grabbable.OnRelease();

            Assert.That(_grabbable.IsHeld, Is.False);
            Assert.That(_grabbable.CurrentHolder, Is.Null);
            Assert.That(_grabbable.CanInteract(_interactor), Is.True);
        }
    }
}
