Shader "GameJam/Map/Discovery Lit"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Main Texture", 2D) = "white" {}
        _MapTex ("Map", 2D) = "white" {}
        _DiscoveryTex ("Discovery", 2D) = "black" {}
        _HiddenColor ("Hidden Color", Color) = (0, 0, 0, 1)
        _RevealedTint ("Revealed Tint", Color) = (1, 1, 1, 1)
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0
        _MapAlphaClipThreshold ("Map Alpha Clip Threshold", Range(0, 1)) = 0.01
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.08
        _MainLightStrength ("Main Light Strength", Range(0, 2)) = 0.15
        _AdditionalLightStrength ("Flashlight Strength", Range(0, 4)) = 1.15
        _NormalInfluence ("Normal Influence", Range(0, 1)) = 0.25
        _MaxLightIntensity ("Max Light Intensity", Range(0.25, 6)) = 2.5
        _HdrMultiplier ("Flashlight HDR Multiplier", Float) = 1
        _FlashlightHaloPower ("Flashlight Halo Power", Range(0.25, 4)) = 1.35
        _FlashlightHaloSpread ("Flashlight Halo Spread", Range(0, 1)) = 0.35
        _FlashlightHaloIntensity ("Flashlight Halo Intensity", Float) = 1
        _FlashlightShadowStrength ("Flashlight Shadow Strength", Range(0, 1)) = 0
        [HDR] _FlashlightCoreColor ("Flashlight Core Color", Color) = (1, 0.96, 0.82, 1)
        _FlashlightCoreIntensity ("Flashlight Core Intensity", Float) = 2.5
        _FlashlightCoreThreshold ("Flashlight Core Threshold", Range(0, 8)) = 1.1
        _FlashlightCoreSoftness ("Flashlight Core Softness", Float) = 0.6
        _FlashlightCorePower ("Flashlight Core Power", Range(0.25, 8)) = 3
        _OutsideLightDarkness ("Outside Light Darkness", Range(0, 1)) = 0.65
        _OutsideLightTint ("Outside Light Tint", Color) = (0.04, 0.12, 0.14, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZTest LEqual
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
            };

            TEXTURE2D(_MapTex);
            SAMPLER(sampler_MapTex);
            TEXTURE2D(_DiscoveryTex);
            SAMPLER(sampler_DiscoveryTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _HiddenColor;
                half4 _RevealedTint;
                half _MapAlphaClipThreshold;
                half _AmbientStrength;
                half _MainLightStrength;
                half _AdditionalLightStrength;
                half _NormalInfluence;
                half _MaxLightIntensity;
                half _HdrMultiplier;
                half _FlashlightHaloPower;
                half _FlashlightHaloSpread;
                half _FlashlightHaloIntensity;
                half _FlashlightShadowStrength;
                half4 _FlashlightCoreColor;
                half _FlashlightCoreIntensity;
                half _FlashlightCoreThreshold;
                half _FlashlightCoreSoftness;
                half _FlashlightCorePower;
                half _OutsideLightDarkness;
                half4 _OutsideLightTint;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half3 EvaluateMapLight(Light light, half3 normalWS, half shadowStrength)
            {
                half lambert = saturate(abs(dot(normalWS, light.direction)));
                half surfaceResponse = lerp(1.0h, lambert, saturate(_NormalInfluence));
                half shadowAttenuation = lerp(1.0h, light.shadowAttenuation, shadowStrength);
                half attenuation = light.distanceAttenuation * shadowAttenuation;
                return light.color * (attenuation * surfaceResponse);
            }

            half3 CalculateBaseLighting(half3 normalWS)
            {
                half3 lighting = half3(_AmbientStrength, _AmbientStrength, _AmbientStrength);

                Light mainLight = GetMainLight();
                lighting += EvaluateMapLight(mainLight, normalWS, 1.0h) * _MainLightStrength;

                return lighting;
            }

            half3 CalculateFlashlightLighting(float4 positionHCS, float3 positionWS, half3 normalWS)
            {
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalWS = normalWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionHCS);

                half3 lighting = half3(0.0h, 0.0h, 0.0h);
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, positionWS);
                    lighting += EvaluateMapLight(light, normalWS, _FlashlightShadowStrength) * _AdditionalLightStrength;
                LIGHT_LOOP_END

                return lighting;
            }

            half MaxChannel(half3 value)
            {
                return max(max(value.r, value.g), value.b);
            }

            half3 ShapeFlashlightHalo(half3 lighting)
            {
                half energy = MaxChannel(lighting);
                if (energy <= 0.0h)
                {
                    return half3(0.0h, 0.0h, 0.0h);
                }

                half maxEnergy = _MaxLightIntensity;
                half normalizedEnergy = saturate(energy / maxEnergy);
                half focusedHalo = pow(normalizedEnergy, _FlashlightHaloPower);
                half wideHalo = sqrt(normalizedEnergy);
                half shapedEnergy = lerp(focusedHalo, wideHalo, _FlashlightHaloSpread) * maxEnergy * _FlashlightHaloIntensity;
                return lighting * (shapedEnergy / energy);
            }

            half CalculateFlashlightCore(half flashlightEnergy)
            {
                half edge0 = _FlashlightCoreThreshold;
                half edge1 = edge0 + _FlashlightCoreSoftness;
                half core = smoothstep(edge0, edge1, flashlightEnergy);
                return pow(core, _FlashlightCorePower);
            }

            half3 ApplyOutsideLightDarkness(half3 color, half3 flashlightLighting)
            {
                half lightPresence = saturate(MaxChannel(flashlightLighting) / _MaxLightIntensity);
                half darkness = _OutsideLightDarkness * (1.0h - lightPresence);
                return lerp(color, color * _OutsideLightTint.rgb, darkness);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half reveal = saturate(SAMPLE_TEXTURE2D(_DiscoveryTex, sampler_DiscoveryTex, input.uv).r);
                half4 mapColor = SAMPLE_TEXTURE2D(_MapTex, sampler_MapTex, input.uv) * _RevealedTint;
                clip(mapColor.a - _MapAlphaClipThreshold);
                half3 normalWS = normalize(input.normalWS);
                half3 baseLighting = CalculateBaseLighting(normalWS);
                half3 rawFlashlightLighting = CalculateFlashlightLighting(input.positionHCS, input.positionWS, normalWS);
                half3 flashlightLighting = ShapeFlashlightHalo(rawFlashlightLighting) * _HdrMultiplier;
                half3 lighting = min(baseLighting + flashlightLighting, half3(_MaxLightIntensity, _MaxLightIntensity, _MaxLightIntensity));
                half3 litMap = mapColor.rgb * lighting;
                litMap = ApplyOutsideLightDarkness(litMap, flashlightLighting);
                half core = CalculateFlashlightCore(MaxChannel(rawFlashlightLighting) * _HdrMultiplier);
                litMap += _FlashlightCoreColor.rgb * (core * _FlashlightCoreIntensity);
                half3 color = lerp(_HiddenColor.rgb, litMap, reveal);
                half alpha = lerp(_HiddenColor.a, mapColor.a, reveal);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
