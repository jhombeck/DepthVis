Shader "Unlit/SimpleGlyph"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Visibility ("Visibility", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            Cull Off
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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            uniform float3 _Center;
            uniform float _min;
            uniform float _max;

            uniform float maxRadius = 2;
            uniform float glyphScale = 1;
            uniform float backgroundOpacity = 0.2;
            uniform bool invertGlyph = true;
            uniform float radius;
            uniform float _Visibility;
            uniform float _distanceTumor;
            uniform float _Fullness; // Between 0 and 1, depending on the min and max dist
            #define M_PI 3.1415926535897932384626433832795

            float fract(float x)
            {
                
                return x - floor(x);
            }

            bool isDashedPart(float angle, float dashAngle)
            {
                if (fract(angle / dashAngle) > 0.5)
                    return true;
                return false;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                 
                float depth = distance(_Center, _WorldSpaceCameraPos);
                _Fullness = (depth - _min) / (_max - _min);
                float4 C; // output color
                float3 color = _distanceTumor * float3(217, 207, 78) / 255.0 + (1 - _distanceTumor) * float3(227, 12, 2) / 255.0;
                float3 borderColor = float3(27, 158, 119) / 255.0;
                borderColor /= 2;

                float3 backgroundColor = float3(117, 112, 179) / 255.0;

                float nRadius = clamp(radius / maxRadius, 0.0, 1.0);


                bool border = true;
                bool borderOnTop = true;
                float borderOffset = 0.06;
                float edgeThickness = 0.1;

                bool glyphOnTop = false;

                bool dashedGlyphOnTop = false;
                float dashAngle = 0.05;

                bool ticks = true;
                float tickWidth = 0.4321;
                float tickNumber = 12.;

                float circleSpacing = 0.0;
                float circleWidth = 0.2;

                float r6 = 1 - edgeThickness;
                float r5 = r6 - circleWidth;
                float r4 = r5 - circleSpacing;
                float r3 = r4 - circleWidth;
                float r2 = r3 - circleSpacing;
                float r1 = r2 - circleWidth;
                float r0 = 0.1; // center dot


                float x = (i.uv.x - 0.5) * 2.0;
                float y = (i.uv.y - 0.5) * 2.0;

                float signX = 1;
                float signY = 1;

                if (x < 0)
                    signX = -1;
                if (y < 0)
                    signY = -1;

                float dist = sqrt(x * x + y * y);

                float angle = ((atan2(x, -y) / M_PI + 1.0) / 2.0);

                float decreaseDepth = 1;

                bool drawTick = angle * tickNumber - floor(angle * tickNumber) <= tickWidth / 2. || -angle * tickNumber + ceil(angle * tickNumber) <= tickWidth / 2.;

                bool isBorder = false;

                if (dist >= r1 && dist <= r2 && angle <= _Fullness * 3.)
                {
                    if (ticks && drawTick)
                    {
                        color = color / 1.25;
                    }
                    if (border)
                    {
                        if (dist <= r1 + borderOffset)
                        {
                            isBorder = true;
                            color = borderColor;
                            if (borderOnTop)
                                depth *= decreaseDepth;
                        }
                    }
                    if (dashedGlyphOnTop && isDashedPart(angle, dashAngle))
                        depth *= decreaseDepth;

                    if (glyphOnTop && !isBorder)
                        depth *= decreaseDepth;

                    C = float4(color, _Visibility);
                }
                else if (dist >= r3 && dist <= r4 && angle <= _Fullness * 3. - 1.)
                {
                    if (ticks && drawTick)
                    {
                        color = color / 1.25;
                    }
                    if (border)
                    {
                        if (dist <= r3 + borderOffset)
                        {
                            isBorder = true;
                            color = borderColor;
                            if (borderOnTop)
                                depth *= decreaseDepth;
                        }
                    }
                    if (dashedGlyphOnTop && isDashedPart(angle, dashAngle))
                        depth *= decreaseDepth;

                    if (glyphOnTop && !isBorder)
                        depth *= decreaseDepth;

                    C = float4(color, _Visibility);
                }
                else if (dist >= r5 && dist <= r6 && angle <= _Fullness * 3. - 2.)
                {
                    if (ticks && drawTick)
                    {
                        color = color / 1.25;
                    }
                    if (border)
                    {
                        if (dist <= r5 + borderOffset)
                        {
                            isBorder = true;
                            color = borderColor;
                            if (borderOnTop)
                                depth *= decreaseDepth;
                        }
                    }
                    if (dashedGlyphOnTop && isDashedPart(angle, dashAngle))
                        depth *= decreaseDepth;

                    if (glyphOnTop && !isBorder)
                        depth *= decreaseDepth;

                    C = float4(color, _Visibility);
                }
                else
                {
                    if (dist <= 1)
                    {
                        C.xyz = backgroundColor;
                        C.a = min(backgroundOpacity, _Visibility);
                    }
                    else discard;

                    if (dist <= r0)
                    {
                        if (border)
                        {
                            if (dist >= r0 - borderOffset)
                            {
                                isBorder = true;
                                color = borderColor;
                                if (borderOnTop)
                                    depth *= decreaseDepth;
                            }
                        }
                        if (dashedGlyphOnTop && isDashedPart(angle, dashAngle))
                            depth *= decreaseDepth;

                        if (glyphOnTop && !isBorder)
                            depth *= decreaseDepth;

                        C = float4(color, 1 * _Visibility);
                    }
                }

                // draw border
                if (dist >= r6)
                {
                    C.a = min(1, _Visibility);
                }

                return C;
            }
            ENDCG
        }
    }
}

