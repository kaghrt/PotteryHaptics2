// 粘土(こねている最中の泥)用シェーダー。Fresnelによる濡れたハイライトと
// ノイズテクスチャによる「完全に濡れた部分 / 乾いた部分」の階調グラデーションを表現する。
Shader "PotteryHaptics/MudSurface"
{
    Properties
    {
        _BaseColor ("Dry Base Color", Color) = (0.35, 0.24, 0.16, 1)
        _WetColor ("Wet Color", Color) = (0.18, 0.12, 0.08, 1)
        _NoiseMap ("Wetness Noise", 2D) = "gray" {}
        _WetGradient ("Wetness Amount", Range(0, 1)) = 0.5
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.6
        _Smoothness ("Smoothness", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseMap_ST;
                half4 _BaseColor;
                half4 _WetColor;
                half _WetGradient;
                half _FresnelPower;
                half _FresnelIntensity;
                half _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _NoiseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, input.uv).r;
                half wetness = saturate(_WetGradient - (noise - 0.5));
                half3 baseColor = lerp(_BaseColor.rgb, _WetColor.rgb, wetness);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half nDotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = baseColor * mainLight.color * (nDotL * mainLight.shadowAttenuation);

                half3 halfDirWS = normalize(mainLight.direction + viewDirWS);
                half specAngle = saturate(dot(normalWS, halfDirWS));
                half specPower = exp2(_Smoothness * 8 + 1);
                half3 specular = mainLight.color * pow(specAngle, specPower) * _Smoothness * wetness * mainLight.shadowAttenuation;

                half fresnel = pow(1 - saturate(dot(normalWS, viewDirWS)), _FresnelPower) * _FresnelIntensity * wetness;

                half3 additionalLightColor = half3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                    uint additionalLightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < additionalLightCount; i++)
                    {
                        Light additionalLight = GetAdditionalLight(i, input.positionWS);
                        half additionalNDotL = saturate(dot(normalWS, additionalLight.direction));
                        additionalLightColor += baseColor * additionalLight.color * (additionalNDotL * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation);
                    }
                #endif

                half3 ambient = SampleSH(normalWS) * baseColor;

                half3 color = diffuse + specular + additionalLightColor + ambient + fresnel;

                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
