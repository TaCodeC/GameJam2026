Shader "Jaramillo/Underwater/Toon Cave Overlay"
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
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ToonUnderwaterOverlay"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local_fragment _UW_DISTORTION_ON
            #pragma shader_feature_local_fragment _UW_CAUSTICS_ON
            #pragma shader_feature_local_fragment _UW_QUALITY_LOW _UW_QUALITY_MEDIUM _UW_QUALITY_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct VaryingsOverlay
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            VaryingsOverlay Vert(Attributes input)
            {
                VaryingsOverlay output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
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

            half2 SampleOverlayFlow(float2 uv)
            {
                float time = _Time.y * _DistortionSpeed;
                float2 waveUv = TRANSFORM_TEX(uv, _DistortionTexture);
                waveUv += float2(time, -time * 0.37);

                // El overlay no dobla el mundo, pero si puede menear el color como agua honesta.
                half2 wave = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, waveUv).rg * 2.0 - 1.0;

                #if defined(_UW_QUALITY_HIGH)
                    half2 tinyWave = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, waveUv * 1.73 + float2(-time * 0.23, time * 0.19)).rg * 2.0 - 1.0;
                    wave = wave * 0.68 + tinyWave * 0.32;
                #elif defined(_UW_QUALITY_LOW)
                    wave.y *= 0.45;
                #endif

                return wave * _DistortionStrength;
            }

            half SampleCaustics(float2 uv)
            {
                float time = _Time.y * _CausticsSpeed;
                float2 causticsUv = TRANSFORM_TEX(uv, _CausticsTexture);
                causticsUv += float2(time * 0.31, -time * 0.19);

                half caustic = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, causticsUv).r;

                #if defined(_UW_QUALITY_HIGH)
                    caustic = caustic * 0.65 + SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, causticsUv * 1.41 + time * 0.11).g * 0.35;
                #endif

                return smoothstep(0.56, 0.92, caustic);
            }

            half4 Frag(VaryingsOverlay input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                float2 waterUv = uv;
                half waterFlow = 0.5;

                #if defined(_UW_DISTORTION_ON)
                    half2 waveOffset = SampleOverlayFlow(uv);
                    half2 waveDirection = waveOffset / max(_DistortionStrength, 0.0001);
                    waterUv = saturate(uv + waveOffset * 2.0);
                    waterFlow = saturate(dot(waveDirection, half2(0.42, 0.58)) * 0.5 + 0.5);
                #endif

                half depth01 = saturate(1.0 - (half)waterUv.y);

                half3 topTint = lerp(_UnderwaterColor.rgb, half3(0.06, 0.85, 0.75), 0.42);
                half3 deepTint = lerp(_UnderwaterColor.rgb * 0.62, half3(0.00, 0.08, 0.27), 0.65);
                half3 waterTint = lerp(topTint, deepTint, saturate(depth01 * _VerticalGradientStrength));
                waterTint *= 0.94 + waterFlow * 0.12;
                half tintAmount = saturate(_TintIntensity * (0.92 + waterFlow * 0.16));

                half aspect = (half)(_ScreenParams.x / max(_ScreenParams.y, 1.0));
                float2 centered = uv * 2.0 - 1.0;
                centered.x *= aspect;
                half vignette = smoothstep(0.28, 1.35, (half)dot(centered, centered)) * _VignetteStrength;

                half lightEnergy = saturate(RadialLightMask(uv, aspect) * _LightIntensity);
                half overlayAlpha = saturate(tintAmount * 0.42 + _Darkness * (0.35 + depth01 * 0.35) + vignette * 0.55);
                overlayAlpha *= 1.0 - lightEnergy * 0.55;

                half3 color = waterTint;
                color = lerp(color, _LightColor.rgb, lightEnergy * 0.42);

                #if defined(_UW_CAUSTICS_ON)
                    half caustics = SampleCaustics(waterUv) * _CausticsIntensity * (0.45 + lightEnergy * 0.55);
                    #if defined(_UW_QUALITY_LOW)
                        caustics *= 0.55;
                    #endif
                    color += _LightColor.rgb * caustics;
                    overlayAlpha = saturate(overlayAlpha + caustics * 0.22);
                #endif

                // Version barata: no distorsiona la escena, pero tampoco pide otra ronda de cafe a la GPU.
                return half4(saturate(color) * input.color.rgb, overlayAlpha * input.color.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
