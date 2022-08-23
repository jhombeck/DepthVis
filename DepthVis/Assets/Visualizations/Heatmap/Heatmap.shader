Shader "Custom/Heatmap"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _ColorCenter("Color Center", Color) = (1,0,0,1)
        _HeatmapRadius("Heatmap Radius", Float) = 50
        _HeatmapIntensity("Heatmap Intensity", Range(0,5)) = 1
        _IsolinesAmount ("Isolines Amount",Int) = 0
        _IsolineRange("Isoline Range", Range(0,1))=1
        _IsolineThickness("Isoline Thickness",Float) = 0.5
        [MaterialToggle] _IsolinesEnabled("Isoline Enabled", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };
        int _CenterPointAmount;
        float4 _CenterPositionArray[10];


        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float4 _ColorCenter;
        float _HeatmapRadius;
        float _HeatmapIntensity;
        int _IsolinesAmount;
        float _IsolineRange;
        float _IsolineThickness;
        float _IsolinesEnabled;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
         // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)



        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            //Base Color
            o.Albedo = c.rgb;

            for (int i = 0; i < _CenterPointAmount; i++)
            {

            float vertexToCenterDistance = distance(IN.worldPos, _CenterPositionArray[i].xyz);
            float3 interpolatedColor = (_Color.xyz - _ColorCenter.xyz * _HeatmapIntensity) * (vertexToCenterDistance / _HeatmapRadius) + _ColorCenter.xyz * _HeatmapIntensity;
  
            //Heatmap Color
            if (vertexToCenterDistance < _HeatmapRadius) {
                float DistanceFromOtherColors = distance(_ColorCenter.rgb * _HeatmapIntensity, o.Albedo);
                float DistanceFromThisColor = distance(_ColorCenter.rgb * _HeatmapIntensity, interpolatedColor);
                if (DistanceFromOtherColors > DistanceFromThisColor)
                    o.Albedo = interpolatedColor;
            }
            }

            //Draw Isolines 
            if (_IsolinesEnabled) {
                for (int i = 0; i < _CenterPointAmount; i++)
                {
                    float vertexToCenterDistance = distance(IN.worldPos, _CenterPositionArray[i].xyz);
                    //Isolines
                    if (_IsolinesAmount > 0) {
                        float isolinesStepSize = (_HeatmapRadius * _IsolineRange) / _IsolinesAmount;
                        for (float i = 1; i <= _IsolinesAmount; i++) {
                            // Create two different thickness level by using every other isoline
                            if (i % 2 == 0 && vertexToCenterDistance < isolinesStepSize * i && vertexToCenterDistance > isolinesStepSize * i - _IsolineThickness)
                                o.Albedo = float3(0, 0, 0);
                            if (i % 2 == 1 && vertexToCenterDistance < isolinesStepSize * i && vertexToCenterDistance > isolinesStepSize * i - _IsolineThickness/3 )
                                o.Albedo = float3(0, 0, 0);
                            
                        }
                    }
                }
            }

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
