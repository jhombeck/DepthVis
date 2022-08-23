using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowGlyphSetup : MonoBehaviour
{
    Renderer _Rend;
    Material _Material;
    public List<GameObject> CenterObjects;
    public bool _SmallGlyphs = false;

    void Start()
    {
        _Rend = gameObject.GetComponent<Renderer>();
        _Material = GetComponent<Renderer>().material;
        
    }

    // Update is called once per frame
    void Update()
    {
        // Setup for multiple reference points
        Vector4[] CenterPositionArray = new Vector4[CenterObjects.Count];
        for (int i = 0; i < CenterObjects.Count; i++)
        {
            CenterPositionArray[i] = CenterObjects[i].transform.position;
        }

        // Update Uniforms 
        _Material.SetInt("_SmallGlyphsON", Convert.ToInt32(_SmallGlyphs));
        _Material.SetInt("_CenterPointAmount", CenterObjects.Count);
        _Material.SetVectorArray("_CenterPositionArray", CenterPositionArray);

    }
}
