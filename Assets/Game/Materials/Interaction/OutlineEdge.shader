Shader "Ngecor/OutlineEdge"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 0.9)
        _OutlineWidth ("Outline Width", Float) = 0.04
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "OutlineEdge"
            Cull Front
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 normalOS = normalize(input.normalOS);
                float3 posOS = input.positionOS.xyz + normalOS * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(posOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
