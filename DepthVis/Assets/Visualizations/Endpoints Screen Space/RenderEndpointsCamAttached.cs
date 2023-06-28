using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderEndpointsCamAttached : MonoBehaviour
{
    public enum DisplayOptions
    {
        None,
        Thinned,
        Endpoints,
        Highlighted
    }
    public ComputeShader ComputeShader;
    public DisplayOptions DisplayOption = DisplayOptions.None;
    public bool UsePrecomputedMasks = true;
    private RenderTexture rtOut;
    private RenderTexture rtIn;

    public Texture2D EndpointTexture = null;

    public uint MaxThinningIterations = 500;
    private ComputeBuffer MaskBuffer;

    private void Start()
    {
        rtOut = new RenderTexture(Display.main.renderingWidth, Display.main.renderingHeight, 24);
        rtOut.enableRandomWrite = true;
        rtOut.Create();

        rtIn = new RenderTexture(Display.main.renderingWidth, Display.main.renderingHeight, 24);
        rtIn.enableRandomWrite = true;
        rtIn.Create();
        ComputeShader.SetInts("Size", rtOut.width, rtOut.height);
        var precomputedMasks = ComputeMask();
        MaskBuffer = new ComputeBuffer(precomputedMasks.Length, sizeof(int));
        MaskBuffer.SetData(precomputedMasks);
        ComputeShader.SetBuffer(1, "MaskValues", MaskBuffer);
        ComputeShader.SetBuffer(2, "MaskValues", MaskBuffer);
    }

    private int[] ComputeMask()
    {
        // implementing Zhang, T. Y., & Suen, C. Y. (1984). A fast parallel algorithm for thinning digital patterns
        var masks = new int[1 << 9];
        for (int i = 0; i < 1 << 8; i++)
        {
            var bits = new BitArray(new int[] { i });
            var p2 = bits[0];
            var p4 = bits[2];
            var p6 = bits[4];
            var p8 = bits[6];

            int A = 0;
            int B = 0;
            for (int j = 0; j < 8; j++)
            {
                if (!bits[j] && bits[(j + 1) % 8])
                    A++;
                if (bits[j])
                    B++;
            }

            var a = 2 <= B && B <= 6;
            var b = A == 1;
            var c1 = !(p2 && p4 && p6);
            var c2 = !(p2 && p4 && p8);
            var d1 = !(p4 && p6 && p8);
            var d2 = !(p2 && p6 && p8);

            // 1 == should be deleted
            int mask1Val = a && b && c1 && d1 ? 1 : 0;
            int mask2Val = a && b && c2 && d2 ? 1 : 0;

            masks[i] = mask1Val;
            masks[i + (1 << 8)] = mask2Val;
        }

        return masks;
    }



    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (DisplayOption == DisplayOptions.None)
            Graphics.Blit(source, destination);

        int threadGroupsX = rtOut.width / 8 + (rtOut.width % 8 > 0 ? 1 : 0);
        int threadGroupY = rtOut.height / 8 + (rtOut.height % 8 > 0 ? 1 : 0);
        
        ComputeShader.SetTextureFromGlobal(0, "Input", "_CameraDepthTexture");
        ComputeShader.SetTexture(0, "Output", rtOut);
        ComputeShader.Dispatch(0, threadGroupsX, threadGroupY, 1);

        var hasChangedBuffer = new ComputeBuffer(1, sizeof(float));
        bool isPhase1 = false;
        int thinningKernel = UsePrecomputedMasks ? 2 : 1;
        for (int i = 0; i < MaxThinningIterations; i++)
        {
            (rtIn, rtOut) = (rtOut, rtIn);
            isPhase1 = !isPhase1;

            float[] hasChanged = new float[1];
            hasChangedBuffer.SetData(hasChanged);

            ComputeShader.SetBool("IsPhase1", isPhase1);
            ComputeShader.SetTexture(thinningKernel, "Input", rtIn);
            ComputeShader.SetTexture(thinningKernel, "Output", rtOut);
            ComputeShader.SetBuffer(thinningKernel, "HasChanged", hasChangedBuffer);
            ComputeShader.Dispatch(thinningKernel, threadGroupsX, threadGroupY, 1);

            hasChangedBuffer.GetData(hasChanged);
            if (hasChanged[0] != 1)
                break;
        }
        hasChangedBuffer.Release();

        if (DisplayOption == DisplayOptions.Thinned)
            Graphics.Blit(rtOut, destination);

        (rtIn, rtOut) = (rtOut, rtIn);
        ComputeShader.SetTexture(3, "Input", rtIn);
        ComputeShader.SetTexture(3, "Output", rtOut);
        ComputeShader.Dispatch(3, threadGroupsX, threadGroupY, 1);

        SaveEndpointTexture(rtOut);


        if (DisplayOption == DisplayOptions.Endpoints)
            Graphics.Blit(rtOut, destination);

        (rtIn, rtOut) = (rtOut, rtIn);
        ComputeShader.SetTexture(4, "Input", rtIn);
        ComputeShader.SetTexture(4, "InputRGB", source);
        ComputeShader.SetTexture(4, "OutputRGB", rtOut);
        ComputeShader.Dispatch(4, threadGroupsX, threadGroupY, 1);

        if (DisplayOption == DisplayOptions.Highlighted)
            Graphics.Blit(rtOut, destination);
    }

    void SaveEndpointTexture(RenderTexture rt)
    {
        var prevActiveRenderTexture = RenderTexture.active;

        RenderTexture.active = rt;

        Texture2D texture = EndpointTexture == null ? new(rt.width, rt.height) : EndpointTexture;
        texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        
        EndpointTexture = texture;

        RenderTexture.active = prevActiveRenderTexture;
    }

    public List<Vector2Int> GetEndpointScreenCoords()
    {
        List<Vector2Int> screenCoords = new();
        for (int y = 0; y < EndpointTexture.height; y++)
        {
            for (int x = 0; x < EndpointTexture.width; x++)
            {
                if (EndpointTexture.GetPixel(x, y) == Color.white)
                    screenCoords.Add(new Vector2Int(x, y));
            }
        }
        return screenCoords;
    }


    private void OnDestroy()
    {
        MaskBuffer.Release();
    }
}
