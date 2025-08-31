Shader "Custom/RetroShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Diffuse Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Float) = 0.5
        _Metallic ("Metallic", Float) = 0.0
        _RetroEmissionMap ("Emission Map", 2D) = "black" {}
        _EnableEmissionMap("Enable Emission Map", Float) = 0.0
        [HDR]_RetroEmissionColor ("Emission Color", Color) = (0,0,0,1)
        [Toggle] _EnableTriplanar ("Enable Triplanar Mapping", Float) = 0.0
        _TriplanarTiling ("Triplanar Tiling", Float) = 1.0

        [Enum(Back,2, Front,1, Off,0)] _CullMode ("Face Culling", Float) = 2
        [Toggle] _EnableTransparency ("Enable Transparency", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

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
        SAMPLER(sampler_MainTex);

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
        float _EnableTransparency;
        float _Cutoff;
        CBUFFER_END

        struct VertexInput
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct VertexOutput
        {
            float4 positionCS   : SV_POSITION;
            float3 worldPos     : TEXCOORD0;
            float3 normalWS     : TEXCOORD1;
            float3 viewDirWS    : TEXCOORD2;
            float2 uv_perspective : TEXCOORD3;
            float4 shadowCoord  : TEXCOORD5;
            float2 emissionUV   : TEXCOORD6;
            float3 worldNormal  : TEXCOORD7;
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
        
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog
        
            struct VOut
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float2 uvMain     : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
                float4 shadowCoord: TEXCOORD5;
                float2 uvEmiss    : TEXCOORD6;
                float3 worldNormal: TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
        
            VOut vert(VertexInput input)
            {
                VOut o;
        
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        
                VertexPositionInputs vpos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vnorm = GetVertexNormalInputs(input.normalOS);
        
                o.positionCS   = vpos.positionCS;
                o.worldPos     = vpos.positionWS;
                o.normalWS     = vnorm.normalWS;
                o.worldNormal  = vnorm.normalWS;
                o.viewDirWS    = GetWorldSpaceViewDir(vpos.positionWS);
                o.uvMain       = TRANSFORM_TEX(input.uv, _MainTex);
                o.uvEmiss      = TRANSFORM_TEX(input.uv, _RetroEmissionMap);
                
                // Use GetShadowCoord for proper cascade handling
                o.shadowCoord = GetShadowCoord(vpos);
        
                #if !defined(_FOG_FRAGMENT)
                    o.fogFactor = ComputeFogFactor(o.positionCS.z);
                #else
                    o.fogFactor = 0.0;
                #endif
        
                return o;
            }
        
            float4 frag(VOut i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        
                float4 albedo;
                if (_EnableTriplanar > 0.5)
                {
                    albedo = SampleTriplanar(_MainTex, sampler_MainTex, i.worldPos, i.worldNormal, _TriplanarTiling, _MainTex_ST, _MainTex_TexelSize);
                }
                else
                {
                    albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvMain);
                }
        
                albedo *= _Color;
        
                // Alpha testing for proper shadow casting
                if (_EnableTransparency > 0.5)
                {
                    float alpha = saturate(albedo.a * _Color.a);
                    clip(alpha - _Cutoff);
                }
        
                float3 emissTex = SAMPLE_TEXTURE2D(_RetroEmissionMap, sampler_MainTex, i.uvEmiss).rgb;
                float3 emission = ((_EnableEmissionMap > 0.5) ? emissTex : 0.0.xxx) * _RetroEmissionColor.rgb;
        
                float outAlpha = 1.0;
                if (_EnableTransparency > 0.5)
                {
                    outAlpha = saturate(albedo.a * _Color.a);
                }
        
                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);
        
                SurfaceData surf = (SurfaceData)0;
                surf.albedo     = albedo.rgb;
                surf.metallic   = _Metallic;
                surf.specular   = 0.0.xxx;
                surf.smoothness = _Smoothness;
                surf.occlusion  = 1.0;
                surf.emission   = emission;
                surf.alpha      = outAlpha;
        
                BRDFData brdf;
                InitializeBRDFData(surf, brdf);
        
                float3 color = 0.0.xxx;
        
                // Proper shadow sampling for cascade transitions
                Light mainLight = GetMainLight(i.shadowCoord);
                color += LightingPhysicallyBased(brdf, mainLight, N, V);
        
                #ifndef SHADERGRAPH_PREVIEW
                {
                    int count = GetAdditionalLightsCount();
                    for (int idx = 0; idx < count; ++idx)
                    {
                        Light li = GetAdditionalLight(idx, i.worldPos);
                        color += LightingPhysicallyBased(brdf, li, N, V);
                    }
                }
                #endif
        
                float3 bakedGI = SampleSH(N);
                color += bakedGI * brdf.diffuse;
        
                color = MixFog(color, i.fogFactor);
        
                return float4(color, outAlpha);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_CullMode]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull[_CullMode]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthNormalsVaryings DepthNormalsVertex(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output = (DepthNormalsVaryings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half4 DepthNormalsFragment(DepthNormalsVaryings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Handle alpha testing
                if (_EnableTransparency > 0.5)
                {
                    float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                    clip(albedo.a - _Cutoff);
                }

                float3 normalWS = normalize(input.normalWS);
                return float4(PackNormalOctRectEncode(TransformWorldToViewDir(normalWS, true)), 0.0, 0.0);
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
                float3 normalOS   : NORMAL;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs v = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = v.positionCS;
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Handle alpha testing for depth
                if (_EnableTransparency > 0.5)
                {
                    float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                    clip(albedo.a - _Cutoff);
                }

                return 0;
            }
            ENDHLSL
        }
    }
}