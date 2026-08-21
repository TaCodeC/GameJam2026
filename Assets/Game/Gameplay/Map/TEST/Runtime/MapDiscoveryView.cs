using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Gameplay.Map
{
    [DisallowMultipleComponent]
    public sealed class MapDiscoveryView : MonoBehaviour
    {
        private const string DiscoveryMaterialResourcePath = "MapDiscoveryUiMaterial";

        private static readonly int MapTextureId = Shader.PropertyToID("_MapTex");
        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int DiscoveryTextureId = Shader.PropertyToID("_DiscoveryTex");
        private static readonly int HiddenColorId = Shader.PropertyToID("_HiddenColor");
        private static readonly int RevealedTintId = Shader.PropertyToID("_RevealedTint");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int MapAlphaClipThresholdId = Shader.PropertyToID("_MapAlphaClipThreshold");
        private static readonly int HdrMultiplierId = Shader.PropertyToID("_HdrMultiplier");
        private static readonly int FlashlightHaloPowerId = Shader.PropertyToID("_FlashlightHaloPower");
        private static readonly int FlashlightHaloSpreadId = Shader.PropertyToID("_FlashlightHaloSpread");
        private static readonly int FlashlightHaloIntensityId = Shader.PropertyToID("_FlashlightHaloIntensity");
        private static readonly int FlashlightShadowStrengthId = Shader.PropertyToID("_FlashlightShadowStrength");
        private static readonly int FlashlightCoreColorId = Shader.PropertyToID("_FlashlightCoreColor");
        private static readonly int FlashlightCoreIntensityId = Shader.PropertyToID("_FlashlightCoreIntensity");
        private static readonly int FlashlightCoreThresholdId = Shader.PropertyToID("_FlashlightCoreThreshold");
        private static readonly int FlashlightCoreSoftnessId = Shader.PropertyToID("_FlashlightCoreSoftness");
        private static readonly int FlashlightCorePowerId = Shader.PropertyToID("_FlashlightCorePower");
        private static readonly int OutsideLightDarknessId = Shader.PropertyToID("_OutsideLightDarkness");
        private static readonly int OutsideLightTintId = Shader.PropertyToID("_OutsideLightTint");

        [SerializeField] private MapDiscoverySystem _discovery;
        [Tooltip("Use either a world Renderer or a UI RawImage.")]
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private RawImage _targetRawImage;
        [SerializeField] private Shader _discoveryShader;
        [Tooltip("Optional visual map texture. Leave empty to reveal the traversable mask itself.")]
        [SerializeField] private Texture _mapTextureOverride;
        [SerializeField] private Color _hiddenColor = Color.black;
        [SerializeField] private Color _revealedTint = Color.white;
        [SerializeField, Range(0f, 1f)] private float _mapAlphaClipThreshold = 0.01f;
        [SerializeField, Min(0f)] private float _hdrMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float _flashlightHaloPower = 1.35f;
        [SerializeField, Range(0f, 1f)] private float _flashlightHaloSpread = 0.35f;
        [SerializeField] private float _flashlightHaloIntensity = 1f;
        [SerializeField, Range(0f, 1f)] private float _flashlightShadowStrength;
        [SerializeField, ColorUsage(true, true)] private Color _flashlightCoreColor = Color.white;
        [SerializeField, Min(0f)] private float _flashlightCoreIntensity = 2.5f;
        [SerializeField, Min(0f)] private float _flashlightCoreThreshold = 1.1f;
        [SerializeField, Min(0.001f)] private float _flashlightCoreSoftness = 0.6f;
        [SerializeField, Min(0.01f)] private float _flashlightCorePower = 3f;
        [SerializeField, Range(0f, 1f)] private float _outsideLightDarkness = 0.65f;
        [SerializeField] private Color _outsideLightTint = new Color(0.04f, 0.12f, 0.14f, 1f);

        private Material _runtimeMaterial;
        private Material _originalRendererMaterial;
        private Material _originalUiMaterial;
        private Texture _originalUiTexture;

        public void SetMapTextureOverride(Texture mapTextureOverride)
        {
            _mapTextureOverride = mapTextureOverride;
            BindTextures();
        }

        public void Configure(
            MapDiscoverySystem discovery,
            RawImage targetRawImage,
            Renderer targetRenderer = null,
            Texture mapTextureOverride = null)
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
            _mapTextureOverride = mapTextureOverride;

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
                : ResolveDiscoveryShader();

            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("Map discovery shader was not found or is not supported. Showing map texture without shader masking.", this);
                BindTextures();
                return;
            }

            _runtimeMaterial = new Material(shader)
            {
                name = "MapDiscoveryView_Runtime"
            };
            ApplyMaterialProperties();

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

        private void OnValidate()
        {
            _hdrMultiplier = Mathf.Max(0f, _hdrMultiplier);
            _flashlightHaloPower = Mathf.Max(0.01f, _flashlightHaloPower);
            _flashlightCoreIntensity = Mathf.Max(0f, _flashlightCoreIntensity);
            _flashlightCoreThreshold = Mathf.Max(0f, _flashlightCoreThreshold);
            _flashlightCoreSoftness = Mathf.Max(0.001f, _flashlightCoreSoftness);
            _flashlightCorePower = Mathf.Max(0.01f, _flashlightCorePower);
            ApplyMaterialProperties();
        }

        private void ApplyMaterialProperties()
        {
            if (_runtimeMaterial == null)
            {
                return;
            }

            _runtimeMaterial.SetColor(HiddenColorId, _hiddenColor);
            _runtimeMaterial.SetColor(RevealedTintId, _revealedTint);

            if (_runtimeMaterial.HasProperty(ZWriteId))
            {
                _runtimeMaterial.SetFloat(ZWriteId, _targetRenderer != null ? 1f : 0f);
            }

            if (_runtimeMaterial.HasProperty(MapAlphaClipThresholdId))
            {
                _runtimeMaterial.SetFloat(MapAlphaClipThresholdId, _mapAlphaClipThreshold);
            }

            if (_runtimeMaterial.HasProperty(HdrMultiplierId))
            {
                _runtimeMaterial.SetFloat(HdrMultiplierId, _hdrMultiplier);
            }

            if (_runtimeMaterial.HasProperty(FlashlightHaloPowerId))
            {
                _runtimeMaterial.SetFloat(FlashlightHaloPowerId, _flashlightHaloPower);
            }

            if (_runtimeMaterial.HasProperty(FlashlightHaloSpreadId))
            {
                _runtimeMaterial.SetFloat(FlashlightHaloSpreadId, _flashlightHaloSpread);
            }

            if (_runtimeMaterial.HasProperty(FlashlightHaloIntensityId))
            {
                _runtimeMaterial.SetFloat(FlashlightHaloIntensityId, _flashlightHaloIntensity);
            }

            if (_runtimeMaterial.HasProperty(FlashlightShadowStrengthId))
            {
                _runtimeMaterial.SetFloat(FlashlightShadowStrengthId, _flashlightShadowStrength);
            }

            if (_runtimeMaterial.HasProperty(FlashlightCoreColorId))
            {
                _runtimeMaterial.SetColor(FlashlightCoreColorId, _flashlightCoreColor);
            }

            if (_runtimeMaterial.HasProperty(FlashlightCoreIntensityId))
            {
                _runtimeMaterial.SetFloat(FlashlightCoreIntensityId, _flashlightCoreIntensity);
            }

            if (_runtimeMaterial.HasProperty(FlashlightCoreThresholdId))
            {
                _runtimeMaterial.SetFloat(FlashlightCoreThresholdId, _flashlightCoreThreshold);
            }

            if (_runtimeMaterial.HasProperty(FlashlightCoreSoftnessId))
            {
                _runtimeMaterial.SetFloat(FlashlightCoreSoftnessId, _flashlightCoreSoftness);
            }

            if (_runtimeMaterial.HasProperty(FlashlightCorePowerId))
            {
                _runtimeMaterial.SetFloat(FlashlightCorePowerId, _flashlightCorePower);
            }

            if (_runtimeMaterial.HasProperty(OutsideLightDarknessId))
            {
                _runtimeMaterial.SetFloat(OutsideLightDarknessId, _outsideLightDarkness);
            }

            if (_runtimeMaterial.HasProperty(OutsideLightTintId))
            {
                _runtimeMaterial.SetColor(OutsideLightTintId, _outsideLightTint);
            }
        }

        private void BindTextures()
        {
            if (_discovery == null || !_discovery.IsInitialized)
            {
                return;
            }

            Texture mapTexture = _mapTextureOverride != null
                ? _mapTextureOverride
                : _discovery.Definition.TraversableMask;

            if (_targetRawImage != null)
            {
                _targetRawImage.texture = mapTexture;
            }

            if (_runtimeMaterial == null)
            {
                return;
            }

            _runtimeMaterial.SetTexture(MainTextureId, mapTexture);
            _runtimeMaterial.SetTexture(MapTextureId, mapTexture);
            _runtimeMaterial.SetTexture(DiscoveryTextureId, _discovery.DiscoveryTexture);
        }

        private static Shader ResolveDiscoveryShader()
        {
            Shader shader = Shader.Find("GameJam/Map/Discovery");
            if (shader != null)
            {
                return shader;
            }

            Material material = Resources.Load<Material>(DiscoveryMaterialResourcePath);
            return material != null ? material.shader : null;
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
