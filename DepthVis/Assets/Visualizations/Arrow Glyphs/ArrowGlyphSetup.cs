using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowGlyphSetup : MonoBehaviour
{
    Renderer _Rend;
    Material _Material;
    public GameObject CenterPoint;
    public bool _SmallGlyphs = false;
    //private GameObject sphere;
    // Start is called before the first frame update
    void Start()
    {
        _Rend = gameObject.GetComponent<Renderer>();
        _Material = GetComponent<Renderer>().material;
        
    }

    // Update is called once per frame
    void Update()
    {

        //Debug.Log(CenterPoints[0].transform.position);

        //_Material.SetVector("centerPosition", CenterPoint.transform.position);
        _Material.SetInt("_SmallGlyphsON", Convert.ToInt32(_SmallGlyphs));
        Vector3 pos = CenterPoint.transform.position ;
        //Debug.Log(CenterPoint.transform.position);
    }
}
