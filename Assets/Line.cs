using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public string axis = "X";
    void Start()
    {
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        if (axis == "X")
        {
            lineRenderer.SetPosition(0, new Vector3(-length/2, 0, 0));
            lineRenderer.SetPosition(1, new Vector3(length/2, 0, 0));    
        }
        else
        {
            lineRenderer.SetPosition(0, new Vector3(0, -length/2, 0));
            lineRenderer.SetPosition(1, new Vector3(0, length/2,  0));    
        }
    }

    public float length = 100;

    void Update()
    {
    }
}
