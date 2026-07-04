using UnityEngine;

namespace GameJam.Player.Cave
{
    [DisallowMultipleComponent]
    public sealed class CavePlayerSkinController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private CavePlayerTextureSet _textureSet;
        [SerializeField] private CavePlayerSkin _defaultSkin = CavePlayerSkin.Deslizador;
        [SerializeField] private CavePlayerSkin _currentSkin = CavePlayerSkin.Deslizador;

        public CavePlayerSkin CurrentSkin => _currentSkin;

        private void Reset()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _currentSkin = _defaultSkin;
            ApplyCurrentSkin();
        }

        private void LateUpdate()
        {
            ApplyCurrentSkin();
        }

        public void SetSkin(CavePlayerSkin skin)
        {
            _currentSkin = skin;
            ApplyCurrentSkin();
        }

        public void SetDeslizador()
        {
            SetSkin(CavePlayerSkin.Deslizador);
        }

        public void SetLinterna()
        {
            SetSkin(CavePlayerSkin.Linterna);
        }

        public void ToggleSkin()
        {
            SetSkin(_currentSkin == CavePlayerSkin.Deslizador
                ? CavePlayerSkin.Linterna
                : CavePlayerSkin.Deslizador);
        }

        private void ApplyCurrentSkin()
        {
            if (_spriteRenderer == null || _textureSet == null)
                return;

            if (_textureSet.TryGetSprite(_spriteRenderer.sprite, _currentSkin, out Sprite replacement))
                _spriteRenderer.sprite = replacement;
        }
    }
}
