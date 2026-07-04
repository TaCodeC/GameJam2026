using UnityEngine;

namespace GameJam.Gameplay.Cave
{
    [DisallowMultipleComponent]
    public sealed class SpriteGlowPulse : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _targetRenderer;
        [SerializeField] private Color _glowColor = new Color(1f, 0.84f, 0.2f, 1f);
        [SerializeField, Min(0f)] private float _glowStrength = 0.65f;
        [SerializeField, Min(0.1f)] private float _pulseSpeed = 2.5f;

        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private bool _glowing;

        private void Awake()
        {
            ResolveRenderer();
            CacheBaseColor();
        }

        private void Update()
        {
            if (!_glowing || _targetRenderer == null)
                return;

            float pulse = (Mathf.Sin(Time.unscaledTime * _pulseSpeed) + 1f) * 0.5f;
            float amount = Mathf.Lerp(_glowStrength * 0.35f, _glowStrength, pulse);
            _targetRenderer.color = Color.Lerp(_baseColor, _glowColor, amount);
        }

        public void SetGlowing(bool glowing)
        {
            ResolveRenderer();
            CacheBaseColor();
            _glowing = glowing;

            if (!_glowing && _targetRenderer != null)
                _targetRenderer.color = _baseColor;
        }

        private void ResolveRenderer()
        {
            if (_targetRenderer == null)
                _targetRenderer = GetComponent<SpriteRenderer>();

            if (_targetRenderer == null)
                _targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void CacheBaseColor()
        {
            if (_hasBaseColor || _targetRenderer == null)
                return;

            _baseColor = _targetRenderer.color;
            _hasBaseColor = true;
        }
    }
}
