﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrismManager : MonoBehaviour
{
    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;
    private Bounds _bound;
    private List<Tuple<float, char, Prism>> _axisPointsX = new List<Tuple<float, char, Prism>>();
    private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism,bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;
    private int numofpointinsimplex=0;
    private List<Vector3> pointList = new List<Vector3>();
    private Vector3 dir;

    #region Unity Functions

    void Start()
    {
        Random.InitState(10);    //10 for no collision

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + Random.value * 7);
            var randYRot = Random.value * 360;
            var randScale = new Vector3((Random.value - 0.5f) * 2 * maxPrismScaleXZ, (Random.value - 0.5f) * 2 * maxPrismScaleY, (Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (Random.value - 0.5f) * 2 * prismRegionRadiusY, (Random.value - 0.5f) * 2 * prismRegionRadiusXZ);

            GameObject prism = null;
            Prism prismScript = null;
            if (Random.value < 0.5f)
            {
                prism = Instantiate(regularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<RegularPrism>();
            }
            else
            {
                prism = Instantiate(irregularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<IrregularPrism>();
            }
            prism.name = "Prism " + i;
            prism.transform.localScale = randScale;
            prism.transform.parent = prismParent.transform;
            prismScript.pointCount = randPointCount;
            prismScript.prismObject = prism;

            prisms.Add(prismScript);
            prismObjects.Add(prism);
            prismColliding.Add(prismScript, false);
        }
        //Variance for axis priority

        Renderer[] renderers = prismParent.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            for (int i = 0, len = renderers.Length; i < len; i++)
            {
                _bound.Encapsulate(renderers[i].bounds);

                _axisPointsX.Add(new Tuple<float, char, Prism>(renderers[i].bounds.min.x, 's', renderers[i].GetComponent<Prism>()));
                _axisPointsX.Add(new Tuple<float, char, Prism>(renderers[i].bounds.max.x, 'e', renderers[i].GetComponent<Prism>()));
            }
        }
        _axisPointsX.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        StartCoroutine(Run());
    }
    
    void Update()
    {
        #region Visualization

        DrawPrismRegion();
        DrawPrismWireFrames();

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            foreach (var prism in prisms)
            {
                prismColliding[prism] = false;
            }
            // Broad Phase
            foreach (var collision in PotentialCollisions())
            {   
                // Narrow Phase
                if (CheckCollision(collision))
                {
                    // if(prismColliding[collision.a])
                    prismColliding[collision.a] = true;
                    prismColliding[collision.b] = true;

                    ResolveCollision(collision);
                }
            }

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }

    #endregion

    #region Incomplete Functions

    private IEnumerable<PrismCollision> PotentialCollisions()
    {
        var olx = collisionAlongX();
        //Debug.Log(olx.Count);

        return collisionAlongZ(olx);

    }
    private List<PrismCollision> collisionAlongZ(List<PrismCollision> olx)
    {
        var outputListY = new List<PrismCollision>();
        for (int i = 0; i < olx.Count; i++)
        {
            var renderers = new List<Renderer>();
            var olx1 = olx[i];

            renderers.Add(olx1.a.GetComponent<Renderer>());
            renderers.Add(olx1.b.GetComponent<Renderer>());


            float az1 = renderers[0].bounds.min.z;
            float az2 = renderers[0].bounds.max.z;
            float bz1 = renderers[1].bounds.min.z;
            float bz2 = renderers[1].bounds.max.z;
            float d1z = bz1 - az2;
            float d2z = az1 - bz2;
            //Debug.Log("az1" + az1 + "bz1" + bz1 + "az2" + az2 + "bz2");
            if (d1z < 0 || d2z < 0)
            {
                outputListY.Add(olx1);
            }
        }
        //Debug.Log(outputListY.Count);
        return outputListY;
        /*   
               var temp = new List<Prism>();
               for (int i = 0; i < olx.Count; i++)
               {
                   var olx1 = olx[i];
                   temp.Add(olx1.a);
                   temp.Add(olx1.b);

               }
               var renderers = new List<Renderer>();
               var _axisPointsY = new List<Tuple<float, char, Prism>>();
               var _activeListY = new List<Prism>();
               var outputListY = new List<PrismCollision>();
               for (int i = 0; i < temp.Count; i++)
               {
                   renderers.Add(temp[i].GetComponent<Renderer>());
               }

               if (renderers.Count > 0)
               {
                   for (int i = 0, len = renderers.Count; i < len; i++)
                   {
                       bound.Encapsulate(renderers[i].bounds);
                       _axisPointsY.Add(new Tuple<float, char, Prism>(renderers[i].bounds.min.y, 's', renderers[i].GetComponent<Prism>()));
                       _axisPointsY.Add(new Tuple<float, char, Prism>(renderers[i].bounds.max.y, 'e', renderers[i].GetComponent<Prism>()));
                   }
               }
               _axisPointsY.Sort((a, b) => a.Item1.CompareTo(b.Item1));


               for (int i = 0; i < _axisPointsY.Count; i++)
               {
                   if (_axisPointsY[i].Item2 == 's')
                   {
                       _activeListY.Add(_axisPointsY[i].Item3);
                       for (int j = 0; j < _activeListY.Count - 1; j++)
                       {
                           var checkPrisms = new PrismCollision();
                           checkPrisms.a = _axisPointsY[i].Item3;
                           checkPrisms.b = _activeListY[j];
                           outputListY.Add(checkPrisms);
                       }


                   }
                   else if (_axisPointsY[i].Item2 == 'e')
                   {
                       _activeListY.Remove(_axisPointsY[i].Item3);
                   }

               }
               Debug.Log(outputListY.Count);
               return outputListY;

           }*/
    }

    private List<PrismCollision> collisionAlongX()
    {
        var outputListX = new List<PrismCollision>();
        var _activeList = new List<Prism>();

        for (int i = 0; i < _axisPointsX.Count; i++)
        {
            if (_axisPointsX[i].Item2 == 's')
            {
                _activeList.Add(_axisPointsX[i].Item3);
                for (int j = 0; j < _activeList.Count - 1; j++)
                {
                    var checkPrisms = new PrismCollision();
                    checkPrisms.a = _axisPointsX[i].Item3;
                    checkPrisms.b = _activeList[j];
                    outputListX.Add(checkPrisms);
                }


            }
            else if (_axisPointsX[i].Item2 == 'e')
            {
                _activeList.Remove(_axisPointsX[i].Item3);
            }

        }
        return outputListX;

    }




    private bool CheckCollision(PrismCollision collision)
    {
        var prismA = collision.a;
        var prismB = collision.b;
        
        collision.penetrationDepthVectorAB = Vector3.zero;
        
        var minkowski = new List<Vector3>();
        foreach(var p in prismA.points){
            foreach(var p2 in prismB.points){
                minkowski.Add( p - p2);
            }
        }

        

        pointList = new List<Vector3>();
        // Start at a random point
        dir = new Vector3(1,0,1);
        pointList.Add(getSupport(prismA, prismB, dir)); 

        dir = -pointList[0] ;

        var count = 0;
        while(count < 10000) {
            count++;
            pointList.Add(getSupport(prismA, prismB, dir));
            if (Vector3.Dot(pointList[pointList.Count - 1], dir) < 0) {
                return false;
            }

            if(OinSimplex(prismA, prismB)) {
                return true;
            }

        }
        return false;
    }

    private bool OinSimplex(Prism prismA,Prism prismB) {
        if(pointList.Count == 2) {
            if (Vector3.Dot(pointList[0]-pointList[1],-pointList[1]) > 0){
                // AB x AO x AB
                // Origin is in between A and B
                dir = Vector3.Cross(Vector3.Cross(pointList[0]-pointList[1],-pointList[1]),pointList[0]-pointList[1]);
                return false;
            }
            else {
                pointList.RemoveAt(0);
                dir = - pointList[0];
                return false;
            }
        }
        else if(pointList.Count == 3) {
            return triangle(prismA, prismB, dir);
        }
        return false;
        // 3D : TODO
        // else if(numofpointinsimplex == 3) {
        //     return tetrahedron(prismA, prismB, dir);
        // }
    }


    private bool triangle(Prism prismA,Prism prismB, Vector3 dir) {
        var len = pointList.Count - 1;
        Vector3 AO = -pointList[len];
        Vector3 AB = pointList[len-1] - pointList[len] ;
        Vector3 AC = pointList[len-2] - pointList[len] ;
        
        Vector3 ABC = cross(AB, AC);
        Vector3 AB_ABC = cross(AB, ABC);
        Vector3 ABC_AC = cross(ABC, AC);

        if(dot(ABC_AC,AO) > 0 ){
            if(dot(AC,AO) > 0) {
                dir = cross(cross(AC,AO), AC);
                pointList.RemoveAt(1);
            }
            else {
                if(dot(AB,AO) > 0) {
                dir = cross(cross(AC,AO), AC);
                pointList.RemoveAt(1);
                }
                else {
                    pointList.RemoveAt(0);
                    pointList.RemoveAt(0);
                    dir = AO;
                }
            }

        } 
        else if(dot(AB_ABC,AO) > 0 ){
            if(dot(AB,AO) > 0) {
                dir = cross(cross(AB,AO), AB);
                pointList.RemoveAt(0);
            }
            else {
                pointList.RemoveAt(0);
                pointList.RemoveAt(0);
                dir = AO;
            }
        }
        else {
            // TODO : Modify for 3D
            return true;
        }
        return false;
    }

    // private bool tetrahedron(Prism prismA,Prism prismB, Vector3 dir) {
        
    // }
    
    #endregion

    #region Private Functions
    
    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        prismObjA.transform.position += pushA;
        prismObjB.transform.position += pushB;

        Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
    }
    
    #endregion

    #region Visualization Functions

    private void DrawPrismRegion()
    {
        var points = new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(-1, 0, 1) }.Select(p => p * prismRegionRadiusXZ).ToArray();
        
        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        var wireFrameColor = Color.yellow;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismWireFrames()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var prismTransform = prismObjects[prismIndex].transform;

            var yMin = prism.midY - prism.height / 2 * prismTransform.localScale.y;
            var yMax = prism.midY + prism.height / 2 * prismTransform.localScale.y;

            var wireFrameColor = prismColliding[prisms[prismIndex]] ? Color.red : Color.green;

            foreach (var point in prism.points)
            {
                Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            }

            for (int i = 0; i < prism.pointCount; i++)
            {
                Debug.DrawLine(prism.points[i] + Vector3.up * yMin, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(prism.points[i] + Vector3.up * yMax, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    #endregion

    #region Utility Classes

    private Vector3 getSupport(Prism a, Prism b, Vector3 direction ) {
        Vector3 p1 = a.support(direction);
        Vector3 p2 = b.support(-1*direction);
        return (p1-p2);
    }

    private float dot(Vector3 a , Vector3 b) {
        return (a.x * b.x + a.y*b.y + a.z*b.z);
    }

    private Vector3 cross(Vector3 a , Vector3 b) {
        return new Vector3(a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x);
    }

    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    private class Tuple<K,V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v) {
            Item1 = k;
            Item2 = v;
        }
    }

    private class Tuple<K, V, A>
    {
        public K Item1;
        public V Item2;
        public A Item3;

        public Tuple(K k, V v, A a)
        {
            Item1 = k;
            Item2 = v;
            Item3 = a;
        }

    }

    #endregion
}