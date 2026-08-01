Shader "MotionCore/Perfect Dodge Afterimage"
{
    Properties
    {
        [HDR] _Color ("颜色", Color) = (0.08, 0.55, 1, 0.6)
        _Fill ("内部亮度", Range(0, 1)) = 0.2
        _FresnelPower ("边缘强度", Range(0.5, 8)) = 2.5
        _NoiseScale ("噪声密度", Range(1, 24)) = 8
        _Dissolve ("溶解强度", Range(0, 1)) = 0.45
        _Distortion ("轮廓扰动", Range(0, 0.08)) = 0.025
        [HideInInspector] _Fade ("Fade", Range(0, 1)) = 1
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
            Name "Afterimage"
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
                float _Fill;
                float _FresnelPower;
                float _NoiseScale;
                float _Dissolve;
                float _Distortion;
                float _Fade;
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

            float Hash31(float3 value)
            {
                value = frac(value * 0.1031);
                value += dot(value, value.yzx + 33.33);
                return frac((value.x + value.y) * value.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float noise = Hash31(floor(input.positionOS.xyz * _NoiseScale));
                float3 positionOS = input.positionOS.xyz
                    + input.normalOS * (noise - 0.5) * _Distortion * (1 - _Fade);
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
                half noise = Hash31(floor(input.positionWS * _NoiseScale));
                half alpha = saturate((_Fill + fresnel) * _Color.a * _Fade
                    - noise * _Dissolve * (1 - _Fade));
                clip(alpha - 0.01);
                return half4(_Color.rgb * (1 + fresnel), alpha);
            }
            ENDHLSL
        }
    }
}
