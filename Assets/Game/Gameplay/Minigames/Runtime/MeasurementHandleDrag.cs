#pragma warning disable 0649

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameJam.Gameplay.Minigames
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class MeasurementHandleDrag : MonoBehaviour, IDragHandler
    {
        private Action _onDragged;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Configure(UIMeasurementTape tape)
        {
            Configure(tape != null ? tape.Refresh : null);
        }

        public void Configure(Action onDragged)
        {
            _onDragged = onDragged;

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform == null || _rectTransform.parent is not RectTransform parent)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPosition))
            {
                _rectTransform.anchoredPosition = localPosition;
                _onDragged?.Invoke();
            }
        }
    }
}
