using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace DynControls
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform _handle;

        [Header("Settings")]
        [Min(1f)]
        [SerializeField] private float _movementRange = 50f;

        [Header("Events")]
        [SerializeField] private UnityEvent<Vector2> _onValueChanged;

        public event Action<Vector2> ValueChanged;

        public Vector2 Value { get; private set; }

        private RectTransform _baseRect;
        private Camera _canvasCamera;

        private void Awake()
        {
            _baseRect = GetComponent<RectTransform>();

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _canvasCamera = parentCanvas.worldCamera;
        }

        private void OnDisable()
        {
            ResetValue();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ProcessInput(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ProcessInput(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetValue();
        }

        private void ProcessInput(PointerEventData eventData)
        {
            bool hasLocalPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _baseRect,
                eventData.position,
                _canvasCamera,
                out Vector2 localPoint);

            if (!hasLocalPoint)
                return;

            float range = Mathf.Max(1f, _movementRange);
            Vector2 clampedPoint = Vector2.ClampMagnitude(localPoint, range);

            if (_handle != null)
                _handle.anchoredPosition = clampedPoint;

            SetValue(clampedPoint / range);
        }

        private void ResetValue()
        {
            if (_handle != null)
                _handle.anchoredPosition = Vector2.zero;

            SetValue(Vector2.zero);
        }

        private void SetValue(Vector2 value)
        {
            if (Value == value)
                return;

            Value = value;
            _onValueChanged?.Invoke(value);
            ValueChanged?.Invoke(value);
        }
    }
}
