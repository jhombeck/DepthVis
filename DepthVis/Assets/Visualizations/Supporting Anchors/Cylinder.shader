Shader "Unlit/Cylinder"
{
    Properties 
    {
        _Color ("Color (RGBA)", Color) = (1, 1, 1, 1) // add _Color property
        _Transparency ("Transparency Fade",Float) = 5
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

            v2f vert (appdata_t v)
            {
                v2f o;

                o.vertex     = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = i.vertex[2]* 500;
                float depthForTrans = i.vertex[2];

                float _Thickness = 0.005;
                float steps = 0.1;
                float amount = 2; 

                // Create Lines
                float linePos = 1;
                if( depth < linePos +_Thickness &&  depth > linePos -_Thickness )
                {
                        _Color = (0,0,0,1);
                }
                linePos = 0.8;
                if( depth < linePos +_Thickness &&  depth > linePos -_Thickness )
                {
                        _Color = (0,0,0,1);
                }

                linePos = 0.6;
                if( depth < linePos +_Thickness &&  depth > linePos -_Thickness )
                {
                        _Color = (0,0,0,1);
                }

                linePos = 0.4;
                if( depth < linePos +_Thickness &&  depth > linePos -_Thickness )
                {
                        _Color = (0,0,0,1);
                }

                // Set Alpha Value 
                _Color[3] = pow(i.vertex[2]* 100*_Transparency,2);

               return  _Color;
         
            }

            ENDCG
        }
    }
}