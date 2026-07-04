Shader "GameJam/Minigames/InteractableStarPrompt"
{
    Properties
    {
        _Color ("Ray Color", Color) = (0.38, 0.88, 1, 1)
        _GlowColor ("Core Color", Color) = (1, 0.96, 0.45, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.95
        _Intensity ("Intensity", Range(0, 4)) = 1.8
        _PulseSpeed ("Pulse Speed", Range(0, 12)) = 3.5
        _RotationSpeed ("Rotation Speed", Range(-6, 6)) = 1.05
        _ScalePulseAmount ("Scale Pulse Amount", Range(0, 0.75)) = 0.28
        _UnevenPointAmount ("Uneven Point Amount", Range(0, 1)) = 0.42
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.35
        _CoreSize ("Core Size", Range(0.02, 0.8)) = 0.16
        _StarSize ("Star Size", Range(0.1, 1.4)) = 0.95
        _RaySharpness ("Ray Sharpness", Range(1, 24)) = 9
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "StarPrompt"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;
                float _Alpha;
                float _Intensity;
                float _PulseSpeed;
                float _RotationSpeed;
                float _ScalePulseAmount;
                float _UnevenPointAmount;
                float _TwinkleAmount;
                float _CoreSize;
                float _StarSize;
                float _RaySharpness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float2 Rotate2D(float2 value, float radians)
            {
                float sine;
                float cosine;
                sincos(radians, sine, cosine);
                return float2(
                    value.x * cosine - value.y * sine,
                    value.x * sine + value.y * cosine);
            }

            float Ray(float axisDistance, float alongDistance, float sharpness, float positiveLength, float negativeLength)
            {
                float length = lerp(negativeLength, positiveLength, step(0.0, alongDistance));
                float narrow = pow(saturate(1.0 - abs(axisDistance) * sharpness), 5.0);
                float falloff = 1.0 - smoothstep(0.0, length, abs(alongDistance));
                return narrow * falloff;
            }

            float AnimatedTipLength(float baseLength, float minScale, float maxScale, float phase, float amount, float pointTime)
            {
                float pulse = sin(pointTime + phase) * 0.5 + 0.5;
                pulse = smoothstep(0.0, 1.0, pulse);
                return baseLength * lerp(1.0, lerp(minScale, maxScale, pulse), amount);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centered = (input.uv - 0.5) * 2.0;
                float pulse01 = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float unscaledDistance = length(centered);
                float outwardPhase = frac(_Time.y * _PulseSpeed * 0.18) * 1.18;
                float outwardWave = 1.0 - smoothstep(0.0, 0.28, abs(unscaledDistance - outwardPhase));
                float scalePulse = 1.0 + _ScalePulseAmount * (pulse01 * 0.72 + outwardWave * 0.35);
                float rotation = _Time.y * _RotationSpeed + sin(_Time.y * _PulseSpeed * 0.37) * 0.22;
                float2 p = Rotate2D(centered / scalePulse, rotation);
                float distanceFromCenter = length(p);

                float core = 1.0 - smoothstep(_CoreSize * 0.25, _CoreSize, distanceFromCenter);
                float halo = 1.0 - smoothstep(_CoreSize, _StarSize, distanceFromCenter);

                float uneven = _UnevenPointAmount;
                float pointTime = _Time.y * _PulseSpeed * 1.75;
                float horizontalPositiveLength = AnimatedTipLength(_StarSize, 0.55, 1.52, 0.2, uneven, pointTime);
                float horizontalNegativeLength = AnimatedTipLength(_StarSize, 0.48, 1.18, 2.8, uneven, pointTime);
                float verticalPositiveLength = AnimatedTipLength(_StarSize, 0.42, 1.28, 4.1, uneven, pointTime);
                float verticalNegativeLength = AnimatedTipLength(_StarSize, 0.64, 1.42, 1.5, uneven, pointTime);
                float diagonalAPositiveLength = AnimatedTipLength(_StarSize * 0.72, 0.45, 1.48, 3.4, uneven, pointTime);
                float diagonalANegativeLength = AnimatedTipLength(_StarSize * 0.72, 0.36, 1.05, 5.6, uneven, pointTime);
                float diagonalBPositiveLength = AnimatedTipLength(_StarSize * 0.72, 0.38, 1.1, 0.9, uneven, pointTime);
                float diagonalBNegativeLength = AnimatedTipLength(_StarSize * 0.72, 0.5, 1.36, 4.9, uneven, pointTime);
                float horizontalRay = Ray(
                    p.y,
                    p.x,
                    _RaySharpness,
                    horizontalPositiveLength,
                    horizontalNegativeLength);
                float verticalRay = Ray(
                    p.x,
                    p.y,
                    _RaySharpness,
                    verticalPositiveLength,
                    verticalNegativeLength);

                float2 diagonalA = float2((p.x - p.y) * 0.70710678, (p.x + p.y) * 0.70710678);
                float diagonalRayA = Ray(
                    diagonalA.y,
                    diagonalA.x,
                    _RaySharpness * 1.6,
                    diagonalAPositiveLength,
                    diagonalANegativeLength);
                float diagonalRayB = Ray(
                    diagonalA.x,
                    diagonalA.y,
                    _RaySharpness * 1.6,
                    diagonalBPositiveLength,
                    diagonalBNegativeLength);

                float2 secondaryP = Rotate2D(centered / lerp(0.78, 1.08, pulse01), -rotation * 0.65 + 0.9);
                float secondaryHorizontal = Ray(
                    secondaryP.y,
                    secondaryP.x,
                    _RaySharpness * 2.4,
                    AnimatedTipLength(_StarSize * 0.46, 0.4, 1.5, 2.1, uneven, pointTime),
                    AnimatedTipLength(_StarSize * 0.28, 0.35, 1.25, 5.2, uneven, pointTime));
                float secondaryVertical = Ray(
                    secondaryP.x,
                    secondaryP.y,
                    _RaySharpness * 2.4,
                    AnimatedTipLength(_StarSize * 0.34, 0.35, 1.45, 3.7, uneven, pointTime),
                    AnimatedTipLength(_StarSize * 0.52, 0.42, 1.3, 1.1, uneven, pointTime));
                float secondaryRays = max(secondaryHorizontal, secondaryVertical) * 0.58;

                float rays = max(max(horizontalRay, verticalRay), max(max(diagonalRayA, diagonalRayB) * 0.72, secondaryRays));
                float edgeFade = 1.0 - smoothstep(0.86, 1.25, unscaledDistance);
                float pulse = lerp(0.72, 1.0, pulse01);
                float shimmerA = sin(_Time.y * _PulseSpeed * 2.7 + 1.3) * 0.5 + 0.5;
                float shimmerB = sin(_Time.y * _PulseSpeed * 4.1 + 2.6) * 0.5 + 0.5;
                float shimmer = lerp(1.0, 0.72 + max(shimmerA, shimmerB) * 0.5, _TwinkleAmount);
                float waveGlow = outwardWave * (1.0 - smoothstep(0.12, 1.14, unscaledDistance));
                float sparkle = saturate(core + halo * 0.18 + rays + waveGlow * 0.24);
                float alpha = saturate(sparkle * edgeFade * pulse * shimmer * _Alpha);

                half3 color = lerp(_Color.rgb, _GlowColor.rgb, saturate(core + rays));
                return half4(color * _Intensity * shimmer, alpha);
            }
            ENDHLSL
        }
    }
}
