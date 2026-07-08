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

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

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

            sampler2D _MapTex;
            sampler2D _DiscoveryTex;
            fixed4 _HiddenColor;
            fixed4 _RevealedTint;
            half _MapAlphaClipThreshold;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                half reveal = tex2D(_DiscoveryTex, input.uv).r;
                fixed4 mapColor = tex2D(_MapTex, input.uv) * _RevealedTint;
                clip(mapColor.a - _MapAlphaClipThreshold);
                return lerp(_HiddenColor, mapColor, saturate(reveal));
            }
            ENDCG
        }
    }
}
