using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DynControls.Tests
{
    public sealed class PointerSurfaceTests
    {
        private GameObject _eventSystemObject;
        private GameObject _surfaceObject;
        private EventSystem _eventSystem;
        private PointerSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            _eventSystem = _eventSystemObject.GetComponent<EventSystem>();

            _surfaceObject = new GameObject("PointerSurface", typeof(RectTransform), typeof(PointerSurface));
            _surface = _surfaceObject.GetComponent<PointerSurface>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_surfaceObject);
            Object.DestroyImmediate(_eventSystemObject);
        }

        [Test]
        public void PointerPressAndRelease_UpdateState()
        {
            PointerEventData eventData = CreatePointerEvent(new Vector2(20f, 30f), 4);

            _surface.OnPointerDown(eventData);

            Assert.That(_surface.IsPressed, Is.True);
            Assert.That(_surface.PointerId, Is.EqualTo(4));
            Assert.That(_surface.Position, Is.EqualTo(new Vector2(20f, 30f)));

            _surface.OnPointerUp(eventData);

            Assert.That(_surface.IsPressed, Is.False);
            Assert.That(_surface.PointerId, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void SecondPointer_CannotTakeOverActiveSurface()
        {
            _surface.OnPointerDown(CreatePointerEvent(new Vector2(10f, 10f), 1));
            _surface.OnPointerDown(CreatePointerEvent(new Vector2(50f, 50f), 2));

            Assert.That(_surface.PointerId, Is.EqualTo(1));
            Assert.That(_surface.Position, Is.EqualTo(new Vector2(10f, 10f)));
        }

        private PointerEventData CreatePointerEvent(Vector2 position, int pointerId)
        {
            return new PointerEventData(_eventSystem)
            {
                position = position,
                pointerId = pointerId
            };
        }
    }
}
