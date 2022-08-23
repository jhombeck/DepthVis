using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConcentricCirclesGlpyhsSetup : MonoBehaviour
{
    public GameObject ParentObject; // Camera
    private float min, max;
    private float distanceTumor; // [0,1]

    void Start()
    {

    }
    public void SetMinMax(float min, float max)
    {
        this.min = min;
        this.max = max;
    }

    public void SetDistanceTumor(float distance)
    {
        distanceTumor = distance;
    }

    void Update()
    {
        // Update Uniforms
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.SetVector("_Center", transform.position);
        meshRenderer.material.SetFloat("_min", min);
        meshRenderer.material.SetFloat("_max", max);
        meshRenderer.material.SetFloat("_distanceTumor", distanceTumor);
        transform.rotation = Quaternion.LookRotation(ParentObject.transform.position - gameObject.transform.position) * Quaternion.Euler(new Vector3(90, 0, 0));
    }
}

