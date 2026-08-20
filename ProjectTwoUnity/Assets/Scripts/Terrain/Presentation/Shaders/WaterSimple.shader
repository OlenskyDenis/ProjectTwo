Shader "ProjectTwo/Terrain/WaterSimple"
{
    Properties
    {
        _BaseColor ("Water Color", Color) = (0.15, 0.48, 0.85, 0.85)
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.70, 0.90, 0.75)
        _FlowSpeed ("Flow Speed", Float) = 0.5
        _WaveScale ("Wave Scale", Float) = 10.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShallowColor;
                float _FlowSpeed;
                float _WaveScale;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _FlowSpeed;
                float2 flowUV = input.uv + float2(0.0, time * 0.2);

                float wave1 = sin((flowUV.x + flowUV.y) * _WaveScale + time * 2.0);
                float wave2 = cos((flowUV.x - flowUV.y) * (_WaveScale * 1.5) - time * 1.5);
                float wave = (wave1 + wave2) * 0.5;

                half4 finalColor = lerp(_BaseColor, _ShallowColor, wave * 0.3 + 0.5);

                // Basic directional lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * (NdotL * 0.6 + 0.4);

                return half4(finalColor.rgb * diffuse, finalColor.a);
            }
            ENDHLSL
        }

        // Built-in render pipeline fallback pass
        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            fixed4 _BaseColor;
            fixed4 _ShallowColor;
            float _FlowSpeed;
            float _WaveScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y * _FlowSpeed;
                float wave = sin((i.uv.x + i.uv.y) * _WaveScale + time * 2.0) * 0.5 + 0.5;
                fixed4 col = lerp(_BaseColor, _ShallowColor, wave * 0.3);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
