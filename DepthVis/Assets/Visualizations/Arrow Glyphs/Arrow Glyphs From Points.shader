
Shader "Custom/Arrow Glyphs From Points"
{
	Properties
	{
		_LargeGlyphs("Texture", 2D) = "white" {}
		_SmallGlyphs("Texture", 2D) = "white" {}
		_MaxDistance("Maximal Glyph Distance", Float) = 0.3
		_MidDistance("Switch Glyph Distance", Float) = 0.15
		_Thickness ("Thickness", Float) = 0.8
	}
		SubShader
		{
		Tags
		{

			"RenderType" = "Transparent"
			"Queue" = "Transparent"
		}
Blend SrcAlpha OneMinusSrcAlpha

		LOD 100

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geom
				// make fog work
				#pragma multi_compile_fog


				#include "UnityCG.cginc"


				struct appdata {
					float4 vertex : POSITION;
					float3 normal : NORMAL;
				};

				struct v2g {
					float4 objPos : SV_POSITION;
					float3 normal : NORMAL;
				};


				struct g2f {
					float4 worldPos : SV_POSITION;
					float2 uv : TEXCOORD0;
					fixed4 col : COLOR;
					float distance : TEXCOORD1;
					float dotNormalGlyph : TEXCOORD2;
				};

				sampler2D _LargeGlyphs;
				sampler2D _SmallGlyphs;
				float _MaxDistance;
				float _MidDistance;
				float _MinDistance = 0.05;
				float3 centerPosition;
				int _SmallGlyphsON;
				float _Thickness;

				// Handle Multiple Spheres 
				int _CenterPointAmount;
				float4 _CenterPositionArray[10];


				v2g vert(appdata v)
				{
					v2g o;
					o.objPos = mul(unity_ObjectToWorld,v.vertex);
					o.normal = v.normal;
					UNITY_TRANSFER_FOG(o, o.vertex);
					return o;
				}



				[maxvertexcount(4)]
				void geom(point v2g input[1], inout TriangleStream<g2f> tristream) {
						g2f o;

						for (int i = 0; i < _CenterPointAmount; i++)
						{
							centerPosition = _CenterPositionArray[i].xyz;
							float distance = length(centerPosition - input[0].objPos);
							float3 SampleToCenter = normalize(centerPosition - input[0].objPos);
							float front = dot(SampleToCenter, normalize(input[0].normal));
							float thickness = _Thickness;

							// Change glyph length based on small or large glyphs
							if (_SmallGlyphsON) {
								_MaxDistance = _MidDistance;
								thickness = thickness*0.75;
							}
							else _MinDistance = _MidDistance;


							if (front >= 0) {
								if (_MaxDistance > distance && distance > _MinDistance) {
									o.distance = distance;
									o.dotNormalGlyph = front;

									//Create view alligned quad 
									float4 trans = thickness * float4(cross(SampleToCenter, mul((float3x3)unity_CameraToWorld, float3(0, 0, 1))), 0);

									o.worldPos = mul(UNITY_MATRIX_VP, (input[0].objPos + trans));
									o.uv = float2(0, 0);
									o.col = fixed4(0, 0, 0, 1);
									tristream.Append(o);

									o.worldPos = mul(UNITY_MATRIX_VP, (input[0].objPos - trans));
									o.uv = float2(0, 1);
									o.col = fixed4(1, 1, 1, 1);
									tristream.Append(o);

									o.worldPos = mul(UNITY_MATRIX_VP, float4(centerPosition, 1) + trans);
									o.uv = float2(1, 0);
									o.col = fixed4(0, 0, 0, 1);
									tristream.Append(o);

									o.worldPos = mul(UNITY_MATRIX_VP, float4(centerPosition, 1) - trans);
									o.uv = float2(1, 1);
									o.col = fixed4(1, 1, 1, 1);
									tristream.Append(o);

									tristream.RestartStrip();
								}
							}
						}



				}



				fixed4 frag(g2f i) : SV_Target
				{

					//Distance value for glyphs 

					float vmin = _MaxDistance * 0.75;
					float vmax = _MaxDistance;
					float vmin2 = _MidDistance;
					float vmax2 = _MidDistance * 0.85;

					float4 fragColor;
					float alpha_dist = 0;
					float window;

					if (_SmallGlyphsON) {
						// Calc Alpha based on distance
						float c = (_MidDistance + _MinDistance) / 2;
						float w = (_MidDistance - _MinDistance);
						window = (1 - (i.distance - (c - (w / 2))) / w);
						if (window <= 1 || window >= 0) {
							alpha_dist = 1 - 2 * abs(window - 0.5);
						}
					}
					else {
						// Calc Alpha based on distance
						float c = (_MaxDistance + _MinDistance) / 2;
						float w = (_MaxDistance - _MinDistance);
						window = (1 - (i.distance - (c - (w / 2))) / w);
						if (window <= 1 || window >= 0) {
							alpha_dist = 1 - 2 * abs(window - 0.5);
						}
					}
					// Calc Aplha based on angle
					float alpha_angle = 0;
					if (window <= 1 || window >= 0) {
						alpha_angle = i.dotNormalGlyph;
					}
					float alpha = 1.1 * min(alpha_dist, alpha_angle);

					// Sample Textures based on Distance

					//Small Glyphs
					if (i.distance < _MidDistance && _SmallGlyphsON) {
						float2 uv = float2((i.distance / _MidDistance) * i.uv.x, i.uv.y);
						fragColor = tex2D(_SmallGlyphs, uv);
					}
					//Large Glyphs
					else if (i.distance > _MidDistance) {
						float2 uv = float2((i.distance / _MaxDistance) * i.uv.x, i.uv.y);
						fragColor = tex2D(_LargeGlyphs, uv);
					}
					//Cutoff transparent part of texture
					if (fragColor.r > 0.5 && fragColor.g > 0.5 && fragColor.b > 0.5) {
						discard;
					}
					// Calculate final color of Glyph and set alpha value 
					else {
						float3 color_max = float3(0.2, 0, 1);
						float3 color_min = float3(1, 0.0, 0.2);
						fragColor = float4(float3(((i.distance / vmax) * color_max.r) + ((1 - (i.distance / vmax)) * color_min.r), ((i.distance / vmax) * color_max.g) + ((1 - (i.distance / vmax)) * color_min.g), ((i.distance / vmax) * color_max.b) + ((1 - (i.distance / vmax)) * color_min.b)), alpha);
					}

					//if (i.distance > 0){
					//	fragColor = float4 (1,0,0,1);
					//}

					UNITY_APPLY_FOG(i.fogCoord, fragColor);
					return fragColor;
				}


			ENDCG
		}



		}
}
