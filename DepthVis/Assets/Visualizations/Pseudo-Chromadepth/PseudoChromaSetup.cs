using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PseudoChromaSetup : MonoBehaviour
{
    Renderer _Rend;
    Material _Material;
    // Start is called before the first frame update
    void Start()
    {
        _Rend = gameObject.GetComponent<Renderer>();
        _Material = GetComponent<Renderer>().material;

    }

    // Update is called once per frame
    void Update()
    {
        // A sphere that fully encloses the bounding box.
        Vector3 center = _Rend.bounds.center;
        float radius = _Rend.bounds.extents.magnitude;
        Vector3 min = _Rend.bounds.min;
        Vector3 max = _Rend.bounds.max;

        // Update Uniforms
        _Material.SetVector("bb_min", min);
        _Material.SetVector("bb_max", max);
    }


    }
