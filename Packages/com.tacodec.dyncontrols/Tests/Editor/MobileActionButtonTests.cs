using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynControls.Tests
{
    public sealed class MobileActionButtonTests
    {
        private GameObject _eventSystemObject;
        private GameObject _buttonObject;
        private EventSystem _eventSystem;
        private MobileActionButton _button;

        [SetUp]
        public void SetUp()
        {
            _eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            _eventSystem = _eventSystemObject.GetComponent<EventSystem>();

            _buttonObject = new GameObject(
                "ActionButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(MobileActionButton));

            _button = _buttonObject.GetComponent<MobileActionButton>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_buttonObject);
            Object.DestroyImmediate(_eventSystemObject);
        }

        [Test]
        public void PointerPressAndRelease_UpdateHeldStateAndEvents()
        {
            bool pressed = false;
            bool released = false;
            _button.Pressed += _ => pressed = true;
            _button.Released += _ => released = true;

            _button.OnPointerDown(new PointerEventData(_eventSystem));

            Assert.That(_button.IsHeld, Is.True);
            Assert.That(pressed, Is.True);

            _button.OnPointerUp(new PointerEventData(_eventSystem));

            Assert.That(_button.IsHeld, Is.False);
            Assert.That(released, Is.True);
        }

        [Test]
        public void ReleaseHeldButton_ClearsStateAndRaisesEvent()
        {
            bool released = false;
            _button.Released += _ => released = true;

            _button.OnPointerDown(new PointerEventData(_eventSystem));
            _button.Release();

            Assert.That(_button.IsHeld, Is.False);
            Assert.That(released, Is.True);
        }
    }
}
