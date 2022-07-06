using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConcentricCirclesGlpyhsSetup : MonoBehaviour
{
    private GameObject ParentObject; // Camera
    private float min, max;

    void Start()
    {
        ParentObject = Camera.main.gameObject;
    }
    public void SetMinMax(float min, float max)
    {
        this.min = min;
        this.max = max;
    }

    void Update()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.SetVector("_Center", transform.position);
        meshRenderer.material.SetFloat("_min", min);
        meshRenderer.material.SetFloat("_max", max);

        transform.rotation = Quaternion.LookRotation(ParentObject.transform.position - gameObject.transform.position) * Quaternion.Euler(new Vector3(90, 0, 0));
    }
}
