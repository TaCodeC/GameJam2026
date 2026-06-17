Shader "Jaramillo/Underwater/Toon Cave Fullscreen"
{
    Properties
    {
        _UnderwaterColor ("Underwater Color", Color) = (0.03, 0.48, 0.52, 1)
        _TintIntensity ("Tint Intensity", Range(0, 1)) = 0.35
        _Darkness ("Darkness", Range(0, 1)) = 0.25
        _VerticalGradientStrength ("Vertical Gradient Strength", Range(0, 1)) = 0.3
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.35

        _DistortionTexture ("Distortion Texture", 2D) = "gray" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.05)) = 0.008
        _DistortionSpeed ("Distortion Speed", Range(-1, 1)) = 0.05

        _CausticsTexture ("Caustics Texture", 2D) = "gray" {}
        _CausticsIntensity ("Caustics Intensity", Range(0, 1)) = 0.08
        _CausticsSpeed ("Caustics Speed", Range(-1, 1)) = 0.05

        _LightColor ("Light Color", Color) = (0.34, 1.0, 0.82, 1)
        _LightIntensity ("Light Intensity", Range(0, 2)) = 0.5
        _LightRadius ("Light Radius", Range(0.01, 1.5)) = 0.35
        _LightViewportPosition ("Light Viewport Position", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Overlay"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "ToonUnderwaterFullscreen"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local_fragment _UW_DISTORTION_ON
            #pragma shader_feature_local_fragment _UW_CAUSTICS_ON
            #pragma shader_feature_local_fragment _UW_QUALITY_LOW _UW_QUALITY_MEDIUM _UW_QUALITY_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_DistortionTexture);
            SAMPLER(sampler_DistortionTexture);
            TEXTURE2D(_CausticsTexture);
            SAMPLER(sampler_CausticsTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _UnderwaterColor;
                half _TintIntensity;
                half _Darkness;
                half _VerticalGradientStrength;
                half _VignetteStrength;
                float4 _DistortionTexture_ST;
                half _DistortionStrength;
                half _DistortionSpeed;
                float4 _CausticsTexture_ST;
                half _CausticsIntensity;
                half _CausticsSpeed;
                half4 _LightColor;
                half _LightIntensity;
                half _LightRadius;
                float4 _LightViewportPosition;
            CBUFFER_END

            half2 SampleWaveOffset(float2 uv, half strength, half speed)
            {
                float time = _Time.y * speed;
                float2 waveUv = TRANSFORM_TEX(uv, _DistortionTexture);
                waveUv += float2(time, -time * 0.37);

                // Aqui la GPU hace onditas, no terapia intensiva.
                half2 wave = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, waveUv).rg * 2.0 - 1.0;

                #if defined(_UW_QUALITY_HIGH)
                    half2 tinyWave = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, waveUv * 1.73 + float2(-time * 0.23, time * 0.19)).rg * 2.0 - 1.0;
                    wave = wave * 0.68 + tinyWave * 0.32;
                #elif defined(_UW_QUALITY_LOW)
                    wave.y *= 0.45;
                #endif

                return wave * strength;
            }

            half SampleCaustics(float2 uv, half speed)
            {
                float time = _Time.y * speed;
                float2 causticsUv = TRANSFORM_TEX(uv, _CausticsTexture);
                causticsUv += float2(time * 0.31, -time * 0.19);

                half caustic = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, causticsUv).r;

                #if defined(_UW_QUALITY_HIGH)
                    half causticB = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, causticsUv * 1.41 + float2(-time * 0.17, time * 0.29)).g;
                    caustic = caustic * 0.65 + causticB * 0.35;
                #endif

                // Bajamos el "spaghetti brillante" para que parezca toon y no alberca carisima.
                return smoothstep(0.56, 0.92, caustic);
            }

            half RadialLightMask(float2 uv, half aspect)
            {
                half enabled = saturate((half)_LightViewportPosition.z);
                half radius = max(_LightRadius, 0.001);
                float2 delta = uv - _LightViewportPosition.xy;
                delta.x *= aspect;

                half light = saturate(1.0 - (half)(length(delta) / radius));
                light = light * light * (3.0 - 2.0 * light);
                return light * enabled;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUv = input.positionCS.xy / _ScreenParams.xy;
                float2 sourceUv = input.texcoord.xy;
                float2 waterUv = screenUv;
                half waterFlow = 0.5;

                #if defined(_UW_DISTORTION_ON)
                    half2 waveOffset = SampleWaveOffset(screenUv, _DistortionStrength, _DistortionSpeed);
                    half2 waveDirection = waveOffset / max(_DistortionStrength, 0.0001);

                    sourceUv += waveOffset;
                    waterUv += waveOffset * 2.0;
                    waterFlow = saturate(dot(waveDirection, half2(0.42, 0.58)) * 0.5 + 0.5);
                #endif

                sourceUv = saturate(sourceUv);
                waterUv = saturate(waterUv);
                half4 scene = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, sourceUv, _BlitMipLevel);

                half depth01 = saturate(1.0 - (half)waterUv.y);
                half3 topTint = lerp(_UnderwaterColor.rgb, half3(0.06, 0.85, 0.75), 0.42);
                half3 deepTint = lerp(_UnderwaterColor.rgb * 0.62, half3(0.00, 0.08, 0.27), 0.65);
                half gradient = saturate(depth01 * _VerticalGradientStrength);
                half3 waterTint = lerp(topTint, deepTint, gradient);
                waterTint *= 0.94 + waterFlow * 0.12;
                half tintAmount = saturate(_TintIntensity * (0.92 + waterFlow * 0.16));

                half aspect = (half)(_ScreenParams.x / max(_ScreenParams.y, 1.0));
                float2 centered = screenUv * 2.0 - 1.0;
                centered.x *= aspect;
                half vignette = smoothstep(0.28, 1.35, (half)dot(centered, centered)) * _VignetteStrength;

                half lightMask = RadialLightMask(screenUv, aspect);
                half lightEnergy = saturate(lightMask * _LightIntensity);

                half3 color = lerp(scene.rgb, waterTint, tintAmount);

                half darkness = saturate(_Darkness * (0.72 + depth01 * 0.35) + vignette);
                darkness *= 1.0 - lightEnergy * 0.72;
                color *= 1.0 - darkness;

                half3 lampTint = lerp(_LightColor.rgb, half3(0.10, 0.92, 0.80), 0.25);
                color = lerp(color, max(color, lampTint), lightEnergy * 0.42);
                color += lampTint * (lightEnergy * 0.16);

                #if defined(_UW_CAUSTICS_ON)
                    half caustics = SampleCaustics(waterUv, _CausticsSpeed);
                    #if defined(_UW_QUALITY_LOW)
                        caustics *= 0.55;
                    #endif

                    half causticsFade = smoothstep(0.08, 0.95, (half)waterUv.y);
                    half causticsEnergy = caustics * _CausticsIntensity * causticsFade * (0.45 + lightEnergy * 0.55);
                    color += lerp(topTint, lampTint, lightEnergy) * causticsEnergy;
                #endif

                return half4(saturate(color), scene.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
