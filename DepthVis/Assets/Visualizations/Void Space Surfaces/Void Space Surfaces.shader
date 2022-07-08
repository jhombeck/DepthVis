Shader "Hidden/VSS"
{
    Properties
    {
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
            
            sampler2D _Points;
            uniform int _pointCount;

            uniform float _contourPoints[3000];

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col;
                float2 pos = float2(i.uv.x, i.uv.y);
                float u = 0;

                float denom = 0;
                for (int j = 0; j < _pointCount; j +=3)
                {
                    float3 pt = float3(_contourPoints[j], _contourPoints[j+1], _contourPoints[j+2]);// tex2D(_Points, float2((float)j / (_pointCount)-0.5, 0)).xyz;
                    float w = 1 / pow(distance(pos, float2(pt.x, pt.y)), 4);
                    denom += w;
                    u += w * pt.z;
                }
                u = u / denom;
                u = 1*u + 0.0*(sin((u-0.5)*3.14)+0.5); // Slider for sinus-like transfer function
                
                // The following is for contour lines
                float d = frac(u*30);
                if (fmod(u*300, 10) > 1.) d = 1. - d;
                float c = d / fwidth(u*30);

                float2 normal = 500*float2(ddx(u), ddy(u)); // Factor 500 is empirically. It decodes the smoothness of the shadow
                float shadow;
                float light;

                if (dot(normalize(float2(1, -1)), normal) > 0)
                    shadow = 0.8 + 0.2 * dot(normalize(float2(1,-1)), normal);
                else
                    light =  0.4 * dot(normalize(float2(1, -1)), normal);

                u = min(u, 1);
                shadow = min(shadow, 1);
                light = min(light, 1);

                if (c >= 1)
                {
                     col.r = (1 - light)*(u)*shadow + light;
                     col.g = light;
                     col.b = (1 - light) * (1 - u) * shadow + light;
                }
                else
                {
                    col.r = 0;
                }
                    
                return  col;
            }
            ENDCG
        }
    }
}
