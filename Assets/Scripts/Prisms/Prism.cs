using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prism : MonoBehaviour
{
    public int pointCount = 3;
    public Vector3[] points;
    public float midY, height;

    public GameObject prismObject;

    // Implementation taken from Slides
    public Vector3 support(Vector3 direction) {
        float highest = -float.MaxValue;
        Vector3 support = Vector3.zero;
        for(int i = 0; i < points.Length; i++) {
            Vector3 v = points[i];
            float dot = Vector3.Dot(v, direction) ;
            if (dot > highest) {
                highest = dot;
                support = v;
            }
        }
        return support;
    }
}
