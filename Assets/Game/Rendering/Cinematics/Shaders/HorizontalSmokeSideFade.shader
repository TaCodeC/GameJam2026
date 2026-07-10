Shader "GameJam/Cinematics/Horizontal Smoke Side Fade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Fade Color", Color) = (0, 0, 0, 1)
        _SmokeTex ("Smoke Texture", 2D) = "gray" {}

        _MaxAlpha ("Max Alpha", Range(0, 1)) = 0.92
        _FadeStart ("Fade Start From Center", Range(0, 1)) = 0.36
        _FadeEnd ("Full Fade At Sides", Range(0, 1)) = 0.92
        _SmokeScale ("Smoke Scale", Range(0.25, 12)) = 3.2
        _SmokeContrast ("Smoke Contrast", Range(0.25, 4)) = 1.35
        _SmokeStrength ("Smoke Alpha Strength", Range(0, 1)) = 0.36
        _EdgeFlutter ("Smoky Edge Flutter", Range(0, 0.35)) = 0.09
        _VerticalSmokeStretch ("Vertical Smoke Stretch", Range(0.25, 4)) = 1.15
        _SmokeSpeed ("Smoke Layer Speeds", Vector) = (0.035, 0.012, -0.022, 0.028)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "HorizontalSmokeSideFade"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SmokeTex);
            SAMPLER(sampler_SmokeTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SmokeTex_ST;
                half _MaxAlpha;
                half _FadeStart;
                half _FadeEnd;
                half _SmokeScale;
                half _SmokeContrast;
                half _SmokeStrength;
                half _EdgeFlutter;
                half _VerticalSmokeStretch;
                float4 _SmokeSpeed;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half SampleSmoke(float2 uv)
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 smokeUv = float2((uv.x - 0.5) * aspect, (uv.y - 0.5) * _VerticalSmokeStretch);
                smokeUv = smokeUv * _SmokeScale + 0.5;
                smokeUv = smokeUv * _SmokeTex_ST.xy + _SmokeTex_ST.zw;

                float time = _Time.y;
                half smokeA = SAMPLE_TEXTURE2D(_SmokeTex, sampler_SmokeTex, smokeUv + _SmokeSpeed.xy * time).r;
                half smokeB = SAMPLE_TEXTURE2D(_SmokeTex, sampler_SmokeTex, smokeUv * 1.71 + _SmokeSpeed.zw * time + 0.37).g;
                half smoke = smokeA * 0.67 + smokeB * 0.33;

                return saturate((smoke - 0.5h) * _SmokeContrast + 0.5h);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = saturate(input.uv);
                half smoke = SampleSmoke(uv);

                half sideDistance = abs((half)uv.x * 2.0h - 1.0h);
                half flutter = (smoke - 0.5h) * _EdgeFlutter;
                half sideMask = smoothstep(_FadeStart, max(_FadeStart + 0.001h, _FadeEnd), sideDistance + flutter);

                half smokyAlpha = 1.0h + (smoke - 0.5h) * _SmokeStrength;
                half edgeLock = smoothstep(0.84h, 1.0h, sideDistance);
                half alpha = saturate(sideMask * _MaxAlpha * smokyAlpha);
                alpha = lerp(alpha, saturate(_MaxAlpha), edgeLock * 0.65h);

                half spriteAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                half4 tint = _Color * input.color;
                return half4(tint.rgb, alpha * spriteAlpha * tint.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
