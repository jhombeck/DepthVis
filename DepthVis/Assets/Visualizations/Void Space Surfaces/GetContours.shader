Shader "Hidden/GetContours"
{
    Properties
    {
        [HideInInspector]
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            uniform sampler2D _CameraDepthTexture;
            uniform float _Intensity;

            float4 _MainTex_TexelSize;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col;
                float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, i.uv));
                depth = 1-Linear01Depth(depth);
                if (depth > 0
                    &&(tex2D(_CameraDepthTexture, i.uv + float2(_MainTex_TexelSize.x, 0)).r == 0
                    || tex2D(_CameraDepthTexture, i.uv - float2(_MainTex_TexelSize.x, 0)).r == 0
                    || tex2D(_CameraDepthTexture, i.uv + float2(0, _MainTex_TexelSize.y)).r == 0
                    || tex2D(_CameraDepthTexture, i.uv - float2(0, _MainTex_TexelSize.y)).r == 0))
                {
                    col.rgb = depth;
                }
                else {
                    col.rgb = 0;
                }
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
