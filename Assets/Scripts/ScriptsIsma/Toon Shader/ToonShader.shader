Shader "2XKO/StylizedToonShader"
{
    Properties
    {
        [Header(Base Texture)]
        _BaseMap("Base Texture", 2D) = "white" {}
        _BaseColor("Base Tint", Color) = (1, 1, 1, 1)

        [Header(Normal Map)]
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 5)) = 1.0

        [Header(Toon Shading)]
        _ShadowColor("Shadow Tint", Color) = (0.3, 0.3, 0.45, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0.001, 0.1)) = 0.01

        [Header(Rim Lighting)]
        _RimColor("Rim Light Color", Color) = (0.8, 0.9, 1, 1)
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.6
        _RimSmoothness("Rim Smoothness", Range(0.001, 0.1)) = 0.05

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.05)) = 0.003
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }

        // ------------------------------------------------------------------
        // PASS 1: OUTLINE PASS (Inverted Hull)
        // ------------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings vertOutline(Attributes input)
            {
                Varyings output;
                float3 inflatedPos = input.positionOS.xyz + input.normalOS * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(inflatedPos);
                return output;
            }

            float4 fragOutline(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // PASS 2: TOON LIGHTING PASS
        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float3 worldNormal  : TEXCOORD2;
                float3 worldTangent : TEXCOORD3;
                float3 worldBitangent : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float4 _ShadowColor;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float4 _RimColor;
                float _RimThreshold;
                float _RimSmoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS     = posInputs.positionCS;
                output.worldPos       = posInputs.positionWS;
                output.worldNormal    = normalInputs.normalWS;
                output.worldTangent   = normalInputs.tangentWS;
                output.worldBitangent = normalInputs.bitangentWS;
                output.uv             = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Textura base
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Mapeo de normales (Tangent Space -> World Space)
                float4 rawNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                float3 normalTS = UnpackNormalScale(rawNormal, _BumpScale);

                // Matriz TBN para convertir la normal a World Space
                float3x3 tbn = float3x3(
                    normalize(input.worldTangent),
                    normalize(input.worldBitangent),
                    normalize(input.worldNormal)
                );

                float3 N = normalize(mul(normalTS, tbn));

                // Vectores de iluminación
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(GetWorldSpaceViewDir(input.worldPos));

                // 1. Toon Shading (Sombra Cel)
                float NdotL = dot(N, L);
                float lightIntensity = smoothstep(_ShadowThreshold - _ShadowSmoothness, 
                                                  _ShadowThreshold + _ShadowSmoothness, 
                                                  NdotL);
                
                float3 toonColor = lerp(_ShadowColor.rgb, mainLight.color, lightIntensity);

                // 2. Rim Light (Brillo en los bordes)
                float NdotV = 1.0 - saturate(dot(N, V));
                float rimIntensity = smoothstep(_RimThreshold - _RimSmoothness, 
                                                _RimThreshold + _RimSmoothness, 
                                                NdotV * NdotL);
                
                float3 rimColor = rimIntensity * _RimColor.rgb;

                // Combinación final
                float3 finalRGB = albedo.rgb * toonColor + rimColor;

                return float4(finalRGB, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}