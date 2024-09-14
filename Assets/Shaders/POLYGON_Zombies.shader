Shader "SyntyStudios/ZombiesURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Blood ("Blood", 2D) = "white" {}
        _BloodColor ("BloodColor", Color) = (0.6470588,0.2569204,0.2569204,1)
        _BloodAmount ("BloodAmount", Range(0,1)) = 0.5
        _Emissive ("Emissive", 2D) = "black" {}
        _EmissiveColor ("EmissiveColor", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Blood;
            float4 _BloodColor;
            float _BloodAmount;
            sampler2D _Emissive;
            float4 _EmissiveColor;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                // Transform object space to homogenous clip space (URP replacement for UnityObjectToClipPos)
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS); 
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Sample the main texture
                float4 baseColor = tex2D(_MainTex, IN.uv);
                
                // Blend blood texture based on BloodAmount
                float4 bloodColor = tex2D(_Blood, IN.uv);
                float4 finalColor = lerp(baseColor, _BloodColor, bloodColor.a * _BloodAmount);

                // Add emissive effect
                float4 emissive = tex2D(_Emissive, IN.uv) * _EmissiveColor;
                
                return float4(finalColor.rgb + emissive.rgb, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}