using System;
using UnityEngine;

namespace GameJam.UI
{
    public enum ComicCinematicAdvanceMode
    {
        AutoOrInput,
        AutoOnly,
        InputOnly
    }

    public enum ComicCinematicEase
    {
        SmoothStep,
        SmootherStep,
        EaseInOutSine,
        EaseOutCubic,
        Linear
    }

    public enum ComicCinematicTransitionMode
    {
        SmoothPan,
        FadeThroughBlack,
        Cut
    }

    [Serializable]
    public sealed class ComicCinematicShot
    {
        [SerializeField] private Sprite _pageOverride;
        [Header("Framing")]
        [Tooltip("Zona base del comic. X/Y pueden salir del rect valido para permitir pan cuando Width/Height estan en 1.")]
        [SerializeField] private Rect _normalizedFocus = new(0f, 0f, 1f, 1f);
        [Tooltip("Desplazamiento extra en unidades de pantalla. 0.1 mueve 10% del ancho/alto visible.")]
        [SerializeField] private Vector2 _zoomOffset;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _moveDuration;
        [SerializeField, Min(0f)] private float _holdDuration;
        [SerializeField, Min(0f)] private float _zoom;
        [SerializeField, Min(0f)] private float _fadeThroughBlackDuration;
        [SerializeField] private ComicCinematicTransitionMode _transition = ComicCinematicTransitionMode.SmoothPan;
        [SerializeField] private ComicCinematicEase _ease = ComicCinematicEase.SmootherStep;
        [SerializeField] private ComicCinematicAdvanceMode _advanceMode = ComicCinematicAdvanceMode.AutoOrInput;

        public Sprite PageOverride => _pageOverride;
        public Rect NormalizedFocus => SanitizeNormalizedRect(_normalizedFocus);
        public Vector2 ZoomOffset => _zoomOffset;
        public ComicCinematicTransitionMode Transition => _transition;
        public ComicCinematicEase Ease => _ease;
        public ComicCinematicAdvanceMode AdvanceMode => _advanceMode;

        public float GetMoveDuration(float fallback)
        {
            return _moveDuration > 0.001f ? _moveDuration : fallback;
        }

        public float GetHoldDuration(float fallback)
        {
            return _holdDuration > 0.001f ? _holdDuration : fallback;
        }

        public float GetZoom(float fallback)
        {
            return _zoom > 0.001f ? _zoom : fallback;
        }

        public float GetFadeThroughBlackDuration(float fallback)
        {
            return _fadeThroughBlackDuration > 0.001f ? _fadeThroughBlackDuration : fallback;
        }

        private static Rect SanitizeNormalizedRect(Rect rect)
        {
            float width = Mathf.Clamp(rect.width, 0.01f, 1f);
            float height = Mathf.Clamp(rect.height, 0.01f, 1f);
            float x = Mathf.Clamp(rect.x, -1f, 1f);
            float y = Mathf.Clamp(rect.y, -1f, 1f);
            return new Rect(x, y, width, height);
        }
    }
}
