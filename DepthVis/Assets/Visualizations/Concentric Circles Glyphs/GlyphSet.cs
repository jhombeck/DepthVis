using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlyphSet : MonoBehaviour
{
    float min, max;

    public GameObject Tumor;
    float maxTumorDist, minTumorDist;

    private void updateChildren()
    {
        min = 0;
        max = 0;
        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            float distance = Vector3.Distance(transform.GetChild(i).position, Camera.main.transform.position);
            float distanceTumor = Vector3.Distance(transform.GetChild(i).position, Tumor.transform.position);

            if (i == 0)
            {
                minTumorDist = distanceTumor;
                maxTumorDist = distanceTumor;
                min = distance;
                max = distance;
            }
            else
            {
                if (distance < min)
                    min = distance;
                if (distance > max)
                    max = distance;
                if (distanceTumor < minTumorDist)
                    minTumorDist = distanceTumor;
                if (distanceTumor > maxTumorDist)
                    maxTumorDist = distanceTumor;
            }
        }
        for (int i = 0; i < children; ++i)
        {
            float distanceTumor = Vector3.Distance(transform.GetChild(i).position, Tumor.transform.position);
            transform.GetChild(i).GetComponent<ConcentricCirclesGlpyhsSetup>().SetMinMax(min, max);
            transform.GetChild(i).GetComponent<ConcentricCirclesGlpyhsSetup>().SetDistanceTumor((distanceTumor - minTumorDist) / (maxTumorDist - minTumorDist));
        }
    }

    void Start()
    {
        updateChildren();
    }

    void Update()
    {
        updateChildren();
    }
}
