using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Player.Cave
{
    public enum CavePlayerSkin
    {
        Deslizador = 0,
        Linterna = 1
    }

    [CreateAssetMenu(menuName = "Game/Player/Cave Player Texture Set")]
    public sealed class CavePlayerTextureSet : ScriptableObject
    {
        [SerializeField] private Sprite[] _deslizadorSprites = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] _linternaSprites = Array.Empty<Sprite>();

        private readonly Dictionary<Sprite, int> _deslizadorLookup = new();
        private bool _lookupReady;

        public bool TryGetSprite(Sprite animatorSprite, CavePlayerSkin skin, out Sprite sprite)
        {
            sprite = animatorSprite;

            if (animatorSprite == null || skin == CavePlayerSkin.Deslizador)
                return false;

            EnsureLookup();

            if (!_deslizadorLookup.TryGetValue(animatorSprite, out int index))
                return false;

            Sprite[] skinSprites = GetSprites(skin);
            if (index < 0 || index >= skinSprites.Length || skinSprites[index] == null)
                return false;

            sprite = skinSprites[index];
            return true;
        }

        private Sprite[] GetSprites(CavePlayerSkin skin)
        {
            return skin == CavePlayerSkin.Linterna ? _linternaSprites : _deslizadorSprites;
        }

        private void EnsureLookup()
        {
            if (_lookupReady)
                return;

            _deslizadorLookup.Clear();
            for (int i = 0; i < _deslizadorSprites.Length; i++)
            {
                Sprite sprite = _deslizadorSprites[i];
                if (sprite != null && !_deslizadorLookup.ContainsKey(sprite))
                    _deslizadorLookup.Add(sprite, i);
            }

            _lookupReady = true;
        }

        private void OnValidate()
        {
            _lookupReady = false;
        }
    }
}
