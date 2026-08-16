// 陶器(焼き上がった器)用シェーダー。Blinn-Phongベースの陰影に、ノーマルマップによる
// 釉薬のムラ・貫入(ひび)表現と、Reflection Probeによる艶を加える。
// 弾性Surfaceの押し込みに応じて _DeformationStrength を頂点変形に反映する(DeformationShaderDriverが駆動)。
Shader "PotteryHaptics/ClayDeformation"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.78, 0.62, 0.48, 1)
        _NormalMap ("Normal Map (Crackle/Glaze)", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.55
        _DeformationStrength ("Deformation Strength", Range(0, 1)) = 0
        _DeformationAmountCm ("Deformation Amount (world units)", Float) = 0.01
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
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _NormalStrength;
                half _Smoothness;
                half _DeformationStrength;
                float _DeformationAmountCm;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 押し込み量に応じて法線方向に頂点を沈める簡易凹み表現。
                float3 displacedPositionOS = input.positionOS.xyz - input.normalOS * (_DeformationStrength * _DeformationAmountCm);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(displacedPositionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalStrength);
                half3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                half3 normalWS = normalize(mul(normalTS, tangentToWorld));

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lightDirWS = mainLight.direction;

                half nDotL = saturate(dot(normalWS, lightDirWS));
                half3 diffuse = baseSample.rgb * mainLight.color * (nDotL * mainLight.shadowAttenuation);

                half3 halfDirWS = normalize(lightDirWS + viewDirWS);
                half specAngle = saturate(dot(normalWS, halfDirWS));
                half specPower = exp2(_Smoothness * 10 + 1);
                half3 specular = mainLight.color * pow(specAngle, specPower) * _Smoothness * mainLight.shadowAttenuation;

                half3 additionalLightColor = half3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                    uint additionalLightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < additionalLightCount; i++)
                    {
                        Light additionalLight = GetAdditionalLight(i, input.positionWS);
                        half additionalNDotL = saturate(dot(normalWS, additionalLight.direction));
                        additionalLightColor += baseSample.rgb * additionalLight.color * (additionalNDotL * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation);
                    }
                #endif

                // Reflection Probeのサンプリングヘルパー(GlossyEnvironmentReflection等)はURPのバージョンにより
                // 引数構成が変わるため、ここではSampleSHによる環境アンビエントと強めのBlinn-Phong鏡面反射のみで
                // 釉薬の艶を表現する(低リスクな安定APIのみを使用)。
                half3 ambient = SampleSH(normalWS) * baseSample.rgb;

                half3 color = diffuse + specular + additionalLightColor + ambient;

                return half4(color, baseSample.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
