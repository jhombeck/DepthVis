using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlyphSet : MonoBehaviour
{
    float min, max;
    
    void Start()
    {
        min = 0;
        max = 0;
        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            float distance = Vector3.Distance(transform.GetChild(i).position, Camera.main.transform.position);
            if (i == 0)
            {
                min = distance;
                max = distance;
            }
            else
            {
                if (distance < min)
                    min = distance;
                if (distance > max)
                    max = distance;
            }
        }
        for (int i = 0; i < children; ++i)
        {
            transform.GetChild(i).GetComponent<ConcentricCirclesGlpyhsSetup>().SetMinMax(min, max);
        }
    }

    void Update()
    {
        min = 0;
        max = 0;
        int children = transform.childCount;
        for (int i = 0; i < children; ++i)
        {
            float distance = Vector3.Distance(transform.GetChild(i).position, Camera.main.transform.position);
            if (i == 0)
            {
                min = distance;
                max = distance;
            }
            else
            {
                if (distance < min)
                    min = distance;
                if (distance > max)
                    max = distance;
            }
        }
        for (int i = 0; i < children; ++i)
        {
            transform.GetChild(i).GetComponent<ConcentricCirclesGlpyhsSetup>().SetMinMax(min, max);
        }
    }
}
