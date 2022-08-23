using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]

public class GetContour : MonoBehaviour
{
    public Camera Cam;
    public Material Mat;
    [Range(1f, 100f)]
    public float intensity = 1;

    // Start is called before the first frame update
    void Start()
    {
        if (Cam == null)
        {
            Cam = GetComponent<Camera>();
        }
        if (Mat == null)
        {
            Mat = new Material(Shader.Find("Hidden/GetContours"));
        }

        Cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    // Update is called once per frame
    void Update()
    {
        // Update Uniform
        Mat.SetFloat("_Intensity", intensity);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, Mat);
    }
}
