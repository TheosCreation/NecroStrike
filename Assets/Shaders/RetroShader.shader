Shader "Custom/RetroShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Diffuse Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Float) = 0.5
        _Metallic ("Metallic", Float) = 0.0
        _RetroEmissionMap ("Emission Map", 2D) = "black" {}[Toggle] 
        _EnableEmissionMap("Enable Emission Map", Float) = 0.0
        [HDR]_RetroEmissionColor ("Emission Color", Color) = (0,0,0,1)
        [Toggle] _EnableTriplanar ("Enable Triplanar Mapping", Float) = 0.0
        _TriplanarTiling ("Triplanar Tiling", Float) = 1.0

        // ---------- New controls ----------
        [Enum(Back,2, Front,1, Off,0)] _CullMode ("Face Culling", Float) = 2
        [Toggle] _EnableTransparency ("Enable Transparency", Float) = 0.0

        // Advanced (material-controlled) state overrides for proper transparent rendering
        // Opaque defaults:   One / Zero / On
        // Transparent setup: SrcAlpha / OneMinusSrcAlpha / Off
        [Enum(One,1, SrcAlpha,5)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(Zero,0, OneMinusSrcAlpha,10)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off,0, On,1)] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
            "RenderType" = "Opaque" 
        }

        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Assets/Shaders/Triplanar.hlsl"

        TEXTURE2D(_MainTex);
        TEXTURE2D(_RetroEmissionMap);

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _MainTex_TexelSize;
        float4 _Color;
        float4 _RetroEmissionMap_ST;
        float _EnableEmissionMap;
        float4 _RetroEmissionColor;
        float _TriplanarTiling;
        float _EnableTriplanar;
        float _Smoothness;
        float _Metallic;

        // New per-material flag (used in fragment alpha)
        float _EnableTransparency;
        CBUFFER_END

        
        struct VertexInput
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        
        struct VertexOutput
        {
            float4 positionCS : SV_POSITION;
            float3 worldPos : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float3 viewDirWS : TEXCOORD2;
            float2 uv_perspective : TEXCOORD3;
            float4 shadowCoord : TEXCOORD5;
            float2 emissionUV : TEXCOORD6;
            float3 worldNormal : TEXCOORD7;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull [_CullMode]
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Required for shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
            
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
            
                output.positionCS = vertexInput.positionCS;
                
                output.worldPos = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.worldNormal = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
            
                output.uv_perspective = TRANSFORM_TEX(input.uv, _MainTex);
                output.emissionUV = TRANSFORM_TEX(input.uv, _RetroEmissionMap);
                output.shadowCoord = GetShadowCoord(vertexInput);
                
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    output.shadowCoord = ComputeScreenPos(output.positionCS);
                #endif
            
                return output;
            }
            
            float4 frag(VertexOutput input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            
                float2 uv = input.uv_perspective;

                float4 albedo;
            
                if (_EnableTriplanar > 0.5)
                {
                    albedo = SampleTriplanar(_MainTex, sampler_PointRepeat, input.worldPos, input.worldNormal, _TriplanarTiling, _MainTex_ST, _MainTex_TexelSize);
                }
                else
                {
                    albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp, uv);
                }
                
                albedo *= _Color;
            
                float3 emissionTex = SAMPLE_TEXTURE2D(_RetroEmissionMap, sampler_PointClamp, input.emissionUV).rgb;
                float3 emission = (_EnableEmissionMap > 0.5 ? emissionTex : 0.0.xxx) * _RetroEmissionColor.rgb;

                float3 normalWS = normalize(input.normalWS);
                
                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    mainLight = GetMainLight(input.shadowCoord);
                #else
                    mainLight = GetMainLight();
                #endif
                
                float3 mainDir = mainLight.direction;
                float3 mainColor = mainLight.color;
                float  distAtten = mainLight.distanceAttenuation;
                float  shadowAttenuation = mainLight.shadowAttenuation;
                
                float3 finalMainColor = mainColor * distAtten * shadowAttenuation;
                float  mainNdotL = saturate(dot(normalWS, mainDir));
                float3 diffuse = finalMainColor * mainNdotL;
                
                // Ambient lighting
                float3 ambient = SampleSH(normalWS);
                
                // Additional lights
                float3 additionalDiffuse = float3(0, 0, 0);
                
                // Standard additional lights
                #ifndef SHADERGRAPH_PREVIEW
                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.worldPos);
                    float addNdotL = saturate(dot(normalWS, light.direction));
                    float3 attenuatedLightColor = light.color * light.distanceAttenuation * light.shadowAttenuation;
                    additionalDiffuse += attenuatedLightColor * addNdotL;
                }
                #endif
                
                // Calculate specular using Blinn-Phong
                float3 viewDirWS = normalize(input.viewDirWS);
                float3 halfDir = normalize(mainDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = exp2(10 * _Smoothness + 1);
                float specular = pow(NdotH, specularPower);

                float3 specularColor = finalMainColor * specular * _Metallic;
                float3 finalColor = albedo.rgb * (diffuse + additionalDiffuse + ambient) + emission + specularColor;
            
                // Transparency from BaseMap * Color
                float outAlpha = 1.0;
                if (_EnableTransparency > 0.5)
                {
                    outAlpha = saturate(albedo.a * _Color.a);
                }
            
                return float4(finalColor, outAlpha);
            }

            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull [_CullMode]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - vertexInput.positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(vertexInput.positionWS, normalInput.normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
        
            Cull [_CullMode]
            ZWrite On
            ColorMask 0
        
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
        
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };
        
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
        
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                return output;
            }
        
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
