Shader "ProjectTwo/Terrain/TriplanarLit"
{
    Properties
    {
        _BaseColor ("Global Tint", Color) = (1,1,1,1)
        
        [Header(Flat Layer (Grass))]
        _FlatTex ("Flat Albedo (RGB)", 2D) = "white" {}
        _FlatNormal ("Flat Normal Map", 2D) = "bump" {}
        _FlatScale ("Flat UV Scale", Float) = 0.1
        
        [Header(Slope Layer (Rock))]
        _SlopeTex ("Slope Albedo (RGB)", 2D) = "white" {}
        _SlopeNormal ("Slope Normal Map", 2D) = "bump" {}
        _SlopeScale ("Slope UV Scale", Float) = 0.1
        _SlopeThreshold ("Slope Start Angle (0-1)", Range(0, 1)) = 0.4
        _SlopeBlend ("Slope Blend Softness", Range(0.01, 0.5)) = 0.1
        
        [Header(Peak Layer (Snow))]
        _PeakTex ("Peak Albedo (RGB)", 2D) = "white" {}
        _PeakNormal ("Peak Normal Map", 2D) = "bump" {}
        _PeakScale ("Peak UV Scale", Float) = 0.1
        _PeakHeightThreshold ("Peak Height (World Y)", Float) = 60.0
        _PeakBlend ("Peak Blend Softness", Float) = 10.0
        
        [Header(Surface Properties)]
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float4 color : COLOR;
            };

            TEXTURE2D(_FlatTex);
            SAMPLER(sampler_FlatTex);
            TEXTURE2D(_SlopeTex);
            SAMPLER(sampler_SlopeTex);
            TEXTURE2D(_PeakTex);
            SAMPLER(sampler_PeakTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _FlatScale;
                float _SlopeScale;
                float _SlopeThreshold;
                float _SlopeBlend;
                float _PeakScale;
                float _PeakHeightThreshold;
                float _PeakBlend;
                float _Glossiness;
                float _Metallic;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.color = input.color;
                return output;
            }

            float3 SampleTriplanar(Texture2D tex, SamplerState samp, float3 posWS, float3 blendWeights, float scale)
            {
                float3 colX = SAMPLE_TEXTURE2D(tex, samp, posWS.yz * scale).rgb;
                float3 colY = SAMPLE_TEXTURE2D(tex, samp, posWS.xz * scale).rgb;
                float3 colZ = SAMPLE_TEXTURE2D(tex, samp, posWS.xy * scale).rgb;
                return colX * blendWeights.x + colY * blendWeights.y + colZ * blendWeights.z;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 blendWeights = pow(abs(normal), 4.0);
                blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z + 0.0001);

                // Sample textures triplanarly
                float3 flatColor = SampleTriplanar(_FlatTex, sampler_FlatTex, input.positionWS, blendWeights, _FlatScale);
                float3 slopeColor = SampleTriplanar(_SlopeTex, sampler_SlopeTex, input.positionWS, blendWeights, _SlopeScale);
                float3 peakColor = SampleTriplanar(_PeakTex, sampler_PeakTex, input.positionWS, blendWeights, _PeakScale);

                // Slope weight (1.0 = vertical cliff, 0.0 = flat ground)
                float slopeFactor = 1.0 - saturate(normal.y);
                float slopeWeight = smoothstep(_SlopeThreshold - _SlopeBlend, _SlopeThreshold + _SlopeBlend, slopeFactor);

                // Height weight for snow peaks
                float heightWeight = smoothstep(_PeakHeightThreshold - _PeakBlend, _PeakHeightThreshold + _PeakBlend, input.positionWS.y);

                // Blend layers: Flat -> Slope (Rock) -> Peak (Snow)
                float3 blendedTex = lerp(flatColor, slopeColor, slopeWeight);
                blendedTex = lerp(blendedTex, peakColor, heightWeight);

                // Multiply by vertex colors and global tint
                float3 albedo = blendedTex * input.color.rgb * _BaseColor.rgb;

                // Simple URP lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                float3 ambient = SampleSH(normal) * 0.4;
                float3 diffuse = mainLight.color * (NdotL * 0.8 + 0.2);

                float3 finalColor = albedo * (diffuse + ambient);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
