using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DynControls
{
    [RequireComponent(typeof(Image))]
    public sealed class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Visual")]
        [SerializeField] private Color _pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Header("Events")]
        [SerializeField] private UnityEvent _onPressed;
        [SerializeField] private UnityEvent _onReleased;

        public event Action<MobileActionButton> Pressed;
        public event Action<MobileActionButton> Released;

        public bool IsHeld { get; private set; }

        private Image _image;
        private Color _normalColor;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_image != null)
                return;

            _image = GetComponent<Image>();
            _normalColor = _image.color;
        }

        private void OnDisable()
        {
            Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld)
                return;

            EnsureInitialized();
            IsHeld = true;
            _image.color = _pressedColor;

            _onPressed?.Invoke();
            Pressed?.Invoke(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void Release()
        {
            if (!IsHeld)
                return;

            IsHeld = false;
            _image.color = _normalColor;

            _onReleased?.Invoke();
            Released?.Invoke(this);
        }
    }
}
