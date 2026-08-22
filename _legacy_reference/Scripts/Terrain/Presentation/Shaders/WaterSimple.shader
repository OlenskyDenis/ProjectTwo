Shader "ProjectTwo/Terrain/WaterSimple"
{
    Properties
    {
        _BaseColor ("Water Color", Color) = (0.15, 0.48, 0.85, 0.85)
        _Color ("Color Fallback", Color) = (0.15, 0.48, 0.85, 0.85)
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.70, 0.90, 0.75)
        _FlowSpeed ("Flow Speed", Float) = 0.5
        _WaveScale ("Wave Scale", Float) = 10.0
    }

    // SubShader 1: Universal Render Pipeline
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 _Color;
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
                float3 normal = normalize(input.normalWS);

                // Smooth bank factor: 0 at shoreline edges, 1 in deep river center
                float bankFactor = sin(saturate(input.uv.x) * 3.14159265);

                // Gentle longitudinal flow ripples (no harsh diagonal checkerboard)
                float flow1 = sin(input.uv.y * 1.2 - time * 2.5);
                float flow2 = cos(input.uv.y * 2.0 - time * 1.8);
                float ripple = (flow1 + flow2) * 0.04;

                half4 deepColor = _BaseColor.a > 0.01 ? _BaseColor : _Color;
                half4 finalWaterCol = lerp(_ShallowColor, deepColor, saturate(bankFactor * 1.1 + ripple));

                // Natural diffuse lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                half3 diffuse = mainLight.color * (NdotL * 0.7 + 0.3);

                // Subtle specular sunlight highlight on water surface
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float3 halfVector = normalize(mainLight.direction + viewDir);
                float specFactor = pow(saturate(dot(normal, halfVector)), 48.0) * 0.15;
                half3 specular = mainLight.color * specFactor;

                // Soft shoreline alpha blending: fades gently into the riverbed
                float alpha = lerp(_ShallowColor.a * 0.4, deepColor.a, bankFactor);

                half3 finalRGB = finalWaterCol.rgb * diffuse + specular;
                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }

    // SubShader 2: Built-in Render Pipeline Fallback
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 150

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
            fixed4 _Color;
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
                float bankFactor = sin(saturate(i.uv.x) * 3.14159265);
                float flow = sin(i.uv.y * 1.5 - time * 2.0) * 0.05;

                fixed4 deepCol = _BaseColor.a > 0.01 ? _BaseColor : _Color;
                fixed4 col = lerp(_ShallowColor, deepCol, saturate(bankFactor + flow));
                col.a = lerp(_ShallowColor.a * 0.35, deepCol.a, bankFactor);
                return col;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
