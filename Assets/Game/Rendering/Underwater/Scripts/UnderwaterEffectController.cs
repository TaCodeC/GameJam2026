using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Rendering.Underwater
{
    public enum UnderwaterQualityMode
    {
        Low,
        Medium,
        High
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UnderwaterEffectController : MonoBehaviour
    {
        static readonly List<UnderwaterEffectController> ActiveControllers = new();

        static readonly int UnderwaterColorId = Shader.PropertyToID("_UnderwaterColor");
        static readonly int TintIntensityId = Shader.PropertyToID("_TintIntensity");
        static readonly int DarknessId = Shader.PropertyToID("_Darkness");
        static readonly int VerticalGradientStrengthId = Shader.PropertyToID("_VerticalGradientStrength");
        static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");
        static readonly int DistortionTextureId = Shader.PropertyToID("_DistortionTexture");
        static readonly int DistortionStrengthId = Shader.PropertyToID("_DistortionStrength");
        static readonly int DistortionSpeedId = Shader.PropertyToID("_DistortionSpeed");
        static readonly int CausticsTextureId = Shader.PropertyToID("_CausticsTexture");
        static readonly int CausticsIntensityId = Shader.PropertyToID("_CausticsIntensity");
        static readonly int CausticsSpeedId = Shader.PropertyToID("_CausticsSpeed");
        static readonly int LightColorId = Shader.PropertyToID("_LightColor");
        static readonly int LightIntensityId = Shader.PropertyToID("_LightIntensity");
        static readonly int LightRadiusId = Shader.PropertyToID("_LightRadius");
        static readonly int LightViewportPositionId = Shader.PropertyToID("_LightViewportPosition");

        const string DistortionKeyword = "_UW_DISTORTION_ON";
        const string CausticsKeyword = "_UW_CAUSTICS_ON";
        const string LowQualityKeyword = "_UW_QUALITY_LOW";
        const string MediumQualityKeyword = "_UW_QUALITY_MEDIUM";
        const string HighQualityKeyword = "_UW_QUALITY_HIGH";

        public static bool HasActiveController
        {
            get
            {
                CleanDeadControllers();
                return ActiveControllers.Count > 0;
            }
        }

        [Header("Target")]
        [Tooltip("Material del shader fullscreen o del overlay. Es el control remoto del cenote.")]
        public Material effectMaterial;

        [Tooltip("Camara que convierte la lampara a viewport. Si esta vacio, usa la camara del mismo GameObject o Camera.main.")]
        public Camera targetCamera;

        [Header("Water Look")]
        public Color underwaterColor = new Color(0.03f, 0.48f, 0.52f, 1f);

        [Range(0f, 1f)]
        public float tintIntensity = 0.35f;

        [Range(0f, 1f)]
        public float darkness = 0.25f;

        [Range(0f, 1f)]
        public float verticalGradientStrength = 0.3f;

        [Range(0f, 1f)]
        public float vignetteStrength = 0.35f;

        [Header("Distortion")]
        public bool enableDistortion = true;

        [Tooltip("Ruido o normal map chiquito, con Wrap Mode en Repeat. Nada de textura gigante porque luego lloramos en WebGL.")]
        public Texture2D distortionTexture;

        [Range(0f, 0.05f)]
        public float distortionStrength = 0.008f;

        [Range(-1f, 1f)]
        public float distortionSpeed = 0.05f;

        [Header("Caustics")]
        public bool enableCaustics = true;

        public Texture2D causticsTexture;

        [Range(0f, 1f)]
        public float causticsIntensity = 0.08f;

        [Range(-1f, 1f)]
        public float causticsSpeed = 0.05f;

        [Header("Fake Lamp")]
        public Transform lightTransform;

        public Color lightColor = new Color(0.34f, 1f, 0.82f, 1f);

        [Range(0f, 2f)]
        public float lightIntensity = 0.5f;

        [Range(0.01f, 1.5f)]
        public float lightRadius = 0.35f;

        [Header("Quality")]
        public UnderwaterQualityMode qualityMode = UnderwaterQualityMode.Medium;

        void Reset()
        {
            targetCamera = GetComponent<Camera>();
        }

        void OnEnable()
        {
            RegisterActiveController(this);
            ApplySettings();
        }

        void OnDisable()
        {
            ActiveControllers.Remove(this);
        }

        void LateUpdate()
        {
            ApplySettings();
        }

        void OnValidate()
        {
            tintIntensity = Mathf.Clamp01(tintIntensity);
            darkness = Mathf.Clamp01(darkness);
            verticalGradientStrength = Mathf.Clamp01(verticalGradientStrength);
            vignetteStrength = Mathf.Clamp01(vignetteStrength);
            distortionStrength = Mathf.Clamp(distortionStrength, 0f, 0.05f);
            causticsIntensity = Mathf.Clamp01(causticsIntensity);
            lightIntensity = Mathf.Max(0f, lightIntensity);
            lightRadius = Mathf.Max(0.01f, lightRadius);

            ApplySettings();
        }

        public void ApplySettings()
        {
            if (effectMaterial == null)
                return;

            Camera cameraToUse = ResolveCamera();
            Vector4 lightViewportPosition = CalculateLightViewportPosition(cameraToUse);

            float qualityDistortionMultiplier = qualityMode == UnderwaterQualityMode.Low ? 0.55f : 1f;
            float qualityCausticsMultiplier = qualityMode == UnderwaterQualityMode.Low ? 0.55f : 1f;

            effectMaterial.SetColor(UnderwaterColorId, underwaterColor);
            effectMaterial.SetFloat(TintIntensityId, tintIntensity);
            effectMaterial.SetFloat(DarknessId, darkness);
            effectMaterial.SetFloat(VerticalGradientStrengthId, verticalGradientStrength);
            effectMaterial.SetFloat(VignetteStrengthId, vignetteStrength);
            effectMaterial.SetFloat(DistortionStrengthId, distortionStrength * qualityDistortionMultiplier);
            effectMaterial.SetFloat(DistortionSpeedId, distortionSpeed);
            effectMaterial.SetFloat(CausticsIntensityId, causticsIntensity * qualityCausticsMultiplier);
            effectMaterial.SetFloat(CausticsSpeedId, causticsSpeed);
            effectMaterial.SetColor(LightColorId, lightColor);
            effectMaterial.SetFloat(LightIntensityId, lightIntensity);
            effectMaterial.SetFloat(LightRadiusId, lightRadius);
            effectMaterial.SetVector(LightViewportPositionId, lightViewportPosition);

            if (distortionTexture != null)
                effectMaterial.SetTexture(DistortionTextureId, distortionTexture);

            if (causticsTexture != null)
                effectMaterial.SetTexture(CausticsTextureId, causticsTexture);

            // Keywords: los switches baratos para no cocinar muestras que nadie pidio.
            SetKeyword(effectMaterial, DistortionKeyword, enableDistortion && distortionStrength > 0f);
            SetKeyword(effectMaterial, CausticsKeyword, enableCaustics && causticsIntensity > 0f);
            SetQualityKeywords(effectMaterial, qualityMode);
        }

        Camera ResolveCamera()
        {
            if (targetCamera != null)
                return targetCamera;

            if (TryGetComponent(out Camera localCamera))
            {
                targetCamera = localCamera;
                return targetCamera;
            }

            targetCamera = Camera.main;
            return targetCamera;
        }

        Vector4 CalculateLightViewportPosition(Camera cameraToUse)
        {
            if (cameraToUse == null || lightTransform == null)
                return new Vector4(0.5f, 0.5f, 0f, 0f);

            Vector3 viewport = cameraToUse.WorldToViewportPoint(lightTransform.position);
            float isUsable = viewport.z >= 0f ? 1f : 0f;

            // No clamp: si la lampara sale de pantalla, se apaga suavecito por distancia.
            return new Vector4(viewport.x, viewport.y, isUsable, 0f);
        }

        static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        static void SetQualityKeywords(Material material, UnderwaterQualityMode mode)
        {
            material.DisableKeyword(LowQualityKeyword);
            material.DisableKeyword(MediumQualityKeyword);
            material.DisableKeyword(HighQualityKeyword);

            switch (mode)
            {
                case UnderwaterQualityMode.Low:
                    material.EnableKeyword(LowQualityKeyword);
                    break;
                case UnderwaterQualityMode.High:
                    material.EnableKeyword(HighQualityKeyword);
                    break;
                default:
                    material.EnableKeyword(MediumQualityKeyword);
                    break;
            }
        }

        static void RegisterActiveController(UnderwaterEffectController controller)
        {
            if (!ActiveControllers.Contains(controller))
                ActiveControllers.Add(controller);
        }

        static void CleanDeadControllers()
        {
            for (int i = ActiveControllers.Count - 1; i >= 0; i--)
            {
                UnderwaterEffectController controller = ActiveControllers[i];
                if (controller == null || !controller.isActiveAndEnabled)
                    ActiveControllers.RemoveAt(i);
            }
        }
    }
}
