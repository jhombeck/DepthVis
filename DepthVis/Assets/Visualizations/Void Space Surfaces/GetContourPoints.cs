using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetContourPoints : MonoBehaviour
{

    public Camera renderCam;
    Texture2D tex;
    public Material Mat;


    public int width, height;

    private void SetShader()
    {
        if (width == 0)
            width = 512;
        if (height == 0)
            height = 288;

        tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture renderTex = new RenderTexture(width, height, 24);
        renderCam.targetTexture = renderTex;
        renderCam.Render();
        RenderTexture.active = renderTex;
        Rect regionToReadFrom = new Rect(0, 0, width, height);
        tex.ReadPixels(regionToReadFrom, 0, 0, false);
        tex.Apply();

        float min = 0, max = 0;

        // Select contour Points
        List<float> contourPoints = new List<float>();
        for (int i = 0; i < tex.width; i++)
        {
            for (int k = 0; k < tex.height; k++)
            {
                float value = tex.GetPixel(i, k).r;
                if (value != 0)
                {

                    contourPoints.Add(i);
                    contourPoints.Add(k);
                    contourPoints.Add(value);
                    if (i == 0 && k == 0)
                    {
                        min = value;
                        max = value;
                    }
                    else
                    {
                        if (value < min)
                            min = value;
                        if (value > max)
                            max = value;
                    }

                }
            }
        }
        
        // Reduce contour points 
        while (contourPoints.Count > 999)
        {
            int i = Random.Range(0, ((contourPoints.Count - 1) / 3));
            contourPoints.RemoveAt(i * 3);
            contourPoints.RemoveAt(i * 3);
            contourPoints.RemoveAt(i * 3);
        }

        float[] contourPointArray = contourPoints.ToArray();
        for (int i = 0; i < contourPoints.Count; i += 3)
        {
            // Normalize Position
            contourPointArray[i] = contourPointArray[i] / width;
            contourPointArray[i + 1] = contourPointArray[i + 1] / height;
            // Normalize value
            contourPointArray[i + 2] = (contourPointArray[i + 2] - min) / (max - min);
        }

        // Update Uniforms
        Mat.SetInt("_pointCount", contourPoints.Count);
        Mat.SetFloatArray("_contourPoints", contourPointArray);
        
    }

    void Start()
    {

        SetShader();

    }


    void Update()
    {
        //SetShader(); // For calculating the shader every frame
    }
}
