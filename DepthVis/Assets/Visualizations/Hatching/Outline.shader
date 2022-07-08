// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LineFalloff("Line falloff", Range(0.0, 5.0)) = 1
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Opaque"}
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; 
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 wPos :  TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LineFalloff;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = mul(unity_ObjectToWorld, v.normal).xyz;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wPos = worldPos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture;

                float3 cameraVector = normalize(_WorldSpaceCameraPos - i.wPos);
                float Dot = dot(cameraVector, i.normal);

                Dot *= _LineFalloff;
                Dot = min(1, Dot);
                fixed4 Col = fixed4(1,1,1,1);
                Col.rgb = Dot;
                return Col;
            }
            ENDCG
        }
    }
}
