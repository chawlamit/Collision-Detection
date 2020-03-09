using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;


public struct rectangle
{
    public Vector3 minV;
    public Vector3 maxV;
        
    public rectangle(Vector3 a, Vector3 b)
    {
        minV = a;
        maxV = b;
    }


}

public class Prism : MonoBehaviour
{
    public int pointCount = 3;
    public Vector3[] points;
    public float midY, height;
    public rectangle bounds;
    public GameObject prismObject;

    private void Start()
    {
        rectangle rec = new rectangle(Vector3.zero, Vector3.zero);
    }
    // Implementation taken from Slides
    public Vector3 support(Vector3 direction)
    {
        float highest = -float.MaxValue;
        Vector3 support = Vector3.zero;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 v = points[i];
            float dot = Vector3.Dot(v, direction);
            if (dot > highest)
            {
                highest = dot;
                support = v;
            }
        }
        return support;
    }

    public void CalculateBounds()
    {
        float minX1 = Mathf.Infinity;
        float minZ1 = Mathf.Infinity;
        float maxX1 = -Mathf.Infinity;
        float maxZ1 = -Mathf.Infinity;
        for (int j = 0; j < points.Length; j++)
        {
            Vector3 pp = points[j];
            if (pp.x < minX1)
            {
                minX1 = pp.x;
                bounds.minV.x = pp.x;
            }
            if (pp.x > maxX1)
            {
                maxX1 = pp.x;
                bounds.maxV.x = pp.x;
            }
            if (pp.z < minZ1)
            {
                minZ1 = pp.z;
                bounds.minV.z = pp.z;
            }
            if (pp.z > maxX1)
            {
                maxZ1 = pp.z;
                bounds.maxV.z = pp.z;
            }

        }
        // Debug.DrawLine(bounds.minV, bounds.maxV, Color.cyan, 0.5f);
    }
    
}