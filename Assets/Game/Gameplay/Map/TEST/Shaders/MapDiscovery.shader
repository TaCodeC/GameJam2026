Shader "GameJam/Map/Discovery"
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
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZTest LEqual
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

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

            TEXTURE2D(_MapTex);
            SAMPLER(sampler_MapTex);
            TEXTURE2D(_DiscoveryTex);
            SAMPLER(sampler_DiscoveryTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _HiddenColor;
                half4 _RevealedTint;
                half _MapAlphaClipThreshold;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half reveal = SAMPLE_TEXTURE2D(_DiscoveryTex, sampler_DiscoveryTex, input.uv).r;
                half4 mapColor = SAMPLE_TEXTURE2D(_MapTex, sampler_MapTex, input.uv) * _RevealedTint;
                clip(mapColor.a - _MapAlphaClipThreshold);
                return lerp(_HiddenColor, mapColor, saturate(reveal));
            }
            ENDHLSL
        }
    }
}
