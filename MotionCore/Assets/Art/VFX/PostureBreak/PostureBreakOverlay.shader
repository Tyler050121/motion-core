Shader "MotionCore/Posture Break Overlay"
{
    Properties
    {
        [HDR] _Color ("颜色", Color) = (0.08, 0.55, 1, 0.6)
        _Fill ("内部亮度", Range(0, 1)) = 0.12
        _FresnelPower ("边缘强度", Range(0.5, 8)) = 2.5
        _ShellWidth ("包裹厚度", Range(0, 0.05)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "PostureBreakOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Fill;
                half _FresnelPower;
                half _ShellWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz + input.normalOS * _ShellWidth;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(1 - saturate(dot(normalWS, viewDirectionWS)), _FresnelPower);
                half alpha = saturate((_Fill + fresnel) * _Color.a);
                clip(alpha - 0.01);
                return half4(_Color.rgb * (1 + fresnel), alpha);
            }
            ENDHLSL
        }
    }
}
