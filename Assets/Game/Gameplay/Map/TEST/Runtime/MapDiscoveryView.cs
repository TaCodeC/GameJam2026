using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Gameplay.Map
{
    [DisallowMultipleComponent]
    public sealed class MapDiscoveryView : MonoBehaviour
    {
        private static readonly int MapTextureId = Shader.PropertyToID("_MapTex");
        private static readonly int DiscoveryTextureId = Shader.PropertyToID("_DiscoveryTex");
        private static readonly int HiddenColorId = Shader.PropertyToID("_HiddenColor");
        private static readonly int RevealedTintId = Shader.PropertyToID("_RevealedTint");

        [SerializeField] private MapDiscoverySystem _discovery;
        [Tooltip("Use either a world Renderer or a UI RawImage.")]
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private RawImage _targetRawImage;
        [SerializeField] private Shader _discoveryShader;
        [SerializeField] private Color _hiddenColor = Color.black;
        [SerializeField] private Color _revealedTint = Color.white;

        private Material _runtimeMaterial;
        private Material _originalRendererMaterial;
        private Material _originalUiMaterial;
        private Texture _originalUiTexture;

        public void Configure(
            MapDiscoverySystem discovery,
            RawImage targetRawImage,
            Renderer targetRenderer = null)
        {
            if (isActiveAndEnabled && _discovery != null)
            {
                _discovery.MapChanged -= BindTextures;
            }

            RestoreTargets();
            DestroyMaterial();

            _discovery = discovery;
            _targetRawImage = targetRawImage;
            _targetRenderer = targetRenderer;

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_discovery != null)
            {
                _discovery.MapChanged += BindTextures;
            }

            CreateMaterial();
            BindTextures();
        }

        private void OnEnable()
        {
            if (_discovery != null)
            {
                _discovery.MapChanged += BindTextures;
            }

            CreateMaterial();
            BindTextures();
        }

        private void OnDisable()
        {
            if (_discovery != null)
            {
                _discovery.MapChanged -= BindTextures;
            }

            RestoreTargets();
            DestroyMaterial();
        }

        private void CreateMaterial()
        {
            if (_runtimeMaterial != null)
            {
                return;
            }

            Shader shader = _discoveryShader != null
                ? _discoveryShader
                : Shader.Find("GameJam/Map/Discovery");

            if (shader == null)
            {
                Debug.LogError("Map discovery shader was not found.", this);
                return;
            }

            _runtimeMaterial = new Material(shader)
            {
                name = "MapDiscoveryView_Runtime"
            };
            _runtimeMaterial.SetColor(HiddenColorId, _hiddenColor);
            _runtimeMaterial.SetColor(RevealedTintId, _revealedTint);

            if (_targetRenderer != null)
            {
                _originalRendererMaterial = _targetRenderer.sharedMaterial;
                _targetRenderer.sharedMaterial = _runtimeMaterial;
            }

            if (_targetRawImage != null)
            {
                _originalUiMaterial = _targetRawImage.material;
                _originalUiTexture = _targetRawImage.texture;
                _targetRawImage.material = _runtimeMaterial;
            }
        }

        private void BindTextures()
        {
            if (_runtimeMaterial == null || _discovery == null || !_discovery.IsInitialized)
            {
                return;
            }

            _runtimeMaterial.SetTexture(MapTextureId, _discovery.Definition.TraversableMask);
            _runtimeMaterial.SetTexture(DiscoveryTextureId, _discovery.DiscoveryTexture);

            if (_targetRawImage != null)
            {
                _targetRawImage.texture = _discovery.Definition.TraversableMask;
            }
        }

        private void RestoreTargets()
        {
            if (_targetRenderer != null && _targetRenderer.sharedMaterial == _runtimeMaterial)
            {
                _targetRenderer.sharedMaterial = _originalRendererMaterial;
            }

            if (_targetRawImage != null && _targetRawImage.material == _runtimeMaterial)
            {
                _targetRawImage.material = _originalUiMaterial;
                _targetRawImage.texture = _originalUiTexture;
            }
        }

        private void DestroyMaterial()
        {
            if (_runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_runtimeMaterial);
            }
            else
            {
                DestroyImmediate(_runtimeMaterial);
            }

            _runtimeMaterial = null;
        }
    }
}
