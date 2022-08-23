Shader "Unlit/Fog"
{
    Properties 
    {
        _Color ("Color (RGBA)", Color) = (1, 1, 1, 1) // add _Color property
        _Transparency ("Transparency Depth",Float) = 5
        _FallOff ("Trasparency Falloff",Float) = 3
    }

    SubShader 
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite On
       Blend SrcAlpha OneMinusSrcAlpha
        //Cull front 
        LOD 100

        Pass 
        {
            CGPROGRAM

            #pragma vertex vert alpha
            #pragma fragment frag alpha

            #include "UnityCG.cginc"

            struct appdata_t 
            {
                float4 vertex   : POSITION;
            };

            struct v2f 
            {
                float4 vertex  : SV_POSITION;
            };

            float4 _Color;
            float _Transparency;
            float _FallOff;

            v2f vert (appdata_t v)
            {
                v2f o;

                o.vertex     = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                // Set Alpha Value 
                _Color[3] = pow(i.vertex[2]* 100*_Transparency,_FallOff);

               return  _Color;
         
            }

            ENDCG
        }
    }
}