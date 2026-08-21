using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace DynControls
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PointerSurface : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IDragHandler,
        IPointerMoveHandler
    {
        [Header("Events")]
        [SerializeField] private UnityEvent<Vector2> _onPositionChanged;
        [SerializeField] private UnityEvent _onPressed;
        [SerializeField] private UnityEvent _onReleased;

        public event Action<Vector2> PositionChanged;
        public event Action<PointerSurface> Pressed;
        public event Action<PointerSurface> Released;

        public Vector2 Position { get; private set; }
        public Vector2 Delta { get; private set; }
        public bool IsPressed { get; private set; }
        public int PointerId { get; private set; } = int.MinValue;

        private void OnDisable()
        {
            ReleasePointer();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Un dedo a la vez; tampoco estamos operando un reactor.
            if (IsPressed && eventData.pointerId != PointerId)
                return;

            UpdatePointer(eventData);

            if (IsPressed)
                return;

            IsPressed = true;
            PointerId = eventData.pointerId;

            _onPressed?.Invoke();
            Pressed?.Invoke(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsCurrentPointer(eventData))
                return;

            UpdatePointer(eventData);
            ReleasePointer();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsCurrentPointer(eventData))
                UpdatePointer(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!IsPressed || IsCurrentPointer(eventData))
                UpdatePointer(eventData);
        }

        private bool IsCurrentPointer(PointerEventData eventData)
        {
            return IsPressed && eventData.pointerId == PointerId;
        }

        private void UpdatePointer(PointerEventData eventData)
        {
            Position = eventData.position;
            Delta = eventData.delta;

            _onPositionChanged?.Invoke(Position);
            PositionChanged?.Invoke(Position);
        }

        private void ReleasePointer()
        {
            if (!IsPressed)
                return;

            IsPressed = false;
            PointerId = int.MinValue;
            Delta = Vector2.zero;

            _onReleased?.Invoke();
            Released?.Invoke(this);
        }
    }
}
