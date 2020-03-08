using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class QuadTree : MonoBehaviour
{
    private int _maxAllowed = 8;
    private rectangle bounds;
    private QuadTree[] subnodes;
    private List<rectangle> QuadObjects;
    private int level;

    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;
    private List<Tuple<float, char, Prism>> _axisPointsX = new List<Tuple<float, char, Prism>>();
    private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism, bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;
    private int numofpointinsimplex = 0;
    private List<Vector3> pointList = new List<Vector3>();
    private Vector3 dir;
    private QuadTree _quad;
    private Renderer[] renderers;
    private List<rectangle> rectList;

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
        //Change the board shape
        _quad = new QuadTree(0, new rectangle(0, 0, 600, 600));
         renderers = prismParent.GetComponentsInChildren<Renderer>();
        
        _quad.clear();
        for (int i = 0; i < renderers.Length; i++)
        {
            float w = renderers[i].bounds.center.x - renderers[i].bounds.min.x;
            float h = renderers[i].bounds.center.z - renderers[i].bounds.min.z;
            _quad.insert(new rectangle(renderers[i].bounds.min.x,renderers[i].bounds.min.z,w,h));
            rectList.Add(new rectangle(renderers[i].bounds.min.x, renderers[i].bounds.min.z, w, h));
        }

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

    private IEnumerable<PrismCollision> PotentialCollisions()
    {
        /*var outputList = new List<PrismCollision>();
        for (int i = 0; i < renderers.Length; i++)
        {
            var pc = new List<rectangle>();
            pc=_quad.retrieve(rectList[i]);
            for (int j = 0; j < pc.Count; j++)
            {
                var checkPrisms = new PrismCollision();
                checkPrisms.a = rectList[i].Item3;
                checkPrisms.b = pc[j];
                outputList.Add(checkPrisms);
            }
        }
*/         /*   foreach (var checkPrisms in collisionAlongZ(olx))
        {
            yield return checkPrisms;
        }
*/
        yield break;

    }
    private bool CheckCollision(PrismCollision collision)
    {
        var prismA = collision.a;
        var prismB = collision.b;

        collision.penetrationDepthVectorAB = Vector3.zero;

        var minkowski = new List<Vector3>();
        foreach (var p in prismA.points)
        {
            foreach (var p2 in prismB.points)
            {
                minkowski.Add(p - p2);
            }
        }



        pointList = new List<Vector3>();
        // Start at a random point
        dir = new Vector3(1, 0, 1);
        pointList.Add(getSupport(prismA, prismB, dir));

        dir = -pointList[0];

        var count = 0;
        while (count < 10000)
        {
            count++;
            pointList.Add(getSupport(prismA, prismB, dir));
            if (Vector3.Dot(pointList[pointList.Count - 1], dir) < 0)
            {
                return false;
            }

            if (OinSimplex(prismA, prismB))
            {
                return true;
            }

        }
        return false;
    }

    private bool OinSimplex(Prism prismA, Prism prismB)
    {
        if (pointList.Count == 2)
        {
            if (Vector3.Dot(pointList[0] - pointList[1], -pointList[1]) > 0)
            {
                // AB x AO x AB
                // Origin is in between A and B
                dir = Vector3.Cross(Vector3.Cross(pointList[0] - pointList[1], -pointList[1]), pointList[0] - pointList[1]);
                return false;
            }
            else
            {
                pointList.RemoveAt(0);
                dir = -pointList[0];
                return false;
            }
        }
        else if (pointList.Count == 3)
        {
            return triangle(prismA, prismB, dir);
        }
        return false;
        // 3D : TODO
        // else if(numofpointinsimplex == 3) {
        //     return tetrahedron(prismA, prismB, dir);
        // }
    }


    private bool triangle(Prism prismA, Prism prismB, Vector3 dir)
    {
        var len = pointList.Count - 1;
        Vector3 AO = -pointList[len];
        Vector3 AB = pointList[len - 1] - pointList[len];
        Vector3 AC = pointList[len - 2] - pointList[len];

        Vector3 ABC = cross(AB, AC);
        Vector3 AB_ABC = cross(AB, ABC);
        Vector3 ABC_AC = cross(ABC, AC);

        if (dot(ABC_AC, AO) > 0)
        {
            if (dot(AC, AO) > 0)
            {
                dir = cross(cross(AC, AO), AC);
                pointList.RemoveAt(1);
            }
            else
            {
                if (dot(AB, AO) > 0)
                {
                    dir = cross(cross(AC, AO), AC);
                    pointList.RemoveAt(1);
                }
                else
                {
                    pointList.RemoveAt(0);
                    pointList.RemoveAt(0);
                    dir = AO;
                }
            }

        }
        else if (dot(AB_ABC, AO) > 0)
        {
            if (dot(AB, AO) > 0)
            {
                dir = cross(cross(AB, AO), AB);
                pointList.RemoveAt(0);
            }
            else
            {
                pointList.RemoveAt(0);
                pointList.RemoveAt(0);
                dir = AO;
            }
        }
        else
        {
            // TODO : Modify for 3D
            return true;
        }
        return false;
    }

    // private bool tetrahedron(Prism prismA,Prism prismB, Vector3 dir) {

    // }



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

    private Vector3 getSupport(Prism a, Prism b, Vector3 direction)
    {
        Vector3 p1 = a.support(direction);
        Vector3 p2 = b.support(-1 * direction);
        return (p1 - p2);
    }

    private float dot(Vector3 a, Vector3 b)
    {
        return (a.x * b.x + a.y * b.y + a.z * b.z);
    }

    private Vector3 cross(Vector3 a, Vector3 b)
    {
        return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
    }

    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    private class Tuple<K, V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v)
        {
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
    public struct rectangle
    {
        public float x;
        public float z;
        public float width;
        public float height;
        public rectangle(float a, float b, float c, float d)
        {
            x = a;
            z = b;
            width = c;
            height = d;
        }


    }


    #endregion

    #region QuadTree methods
    public QuadTree(int pLevel, rectangle pBounds)
    {
        level = pLevel;
        QuadObjects = new List<rectangle>();
        bounds = pBounds;
        subnodes = new QuadTree[4];
    }
    //Clear the Quadtree
    public void clear()
    {
        QuadObjects.Clear();

        for (int i = 0; i < subnodes.Length; i++)
        {
            if (subnodes[i] != null)
            {
                subnodes[i].clear();
                subnodes[i] = null;
            }
        }
    }

    private void split()
    {
        int subWidth = (int)(bounds.width / 2);
        int subHeight = (int)(bounds.height / 2);
        int x = (int)bounds.x;
        int z = (int)bounds.z;

        subnodes[0] = new QuadTree(level + 1, new rectangle(x + subWidth, z, subWidth, subHeight));
        subnodes[1] = new QuadTree(level + 1, new rectangle(x, z, subWidth, subHeight));
        subnodes[2] = new QuadTree(level + 1, new rectangle(x, z + subHeight, subWidth, subHeight));
        subnodes[3] = new QuadTree(level + 1, new rectangle(x + subWidth, z + subHeight, subWidth, subHeight));
    }

    public void insert(rectangle pRect)
    {
        if (subnodes[0] != null)
        {
            int index = getIndex(pRect);

            if (index != -1)
            {
                subnodes[index].insert(pRect);

                return;
            }
        }

        QuadObjects.Add(pRect);

        if (QuadObjects.Count > _maxAllowed)
        {
            if (subnodes[0] == null)
            {
                split();
            }

            int i = 0;
            while (i < QuadObjects.Count)
            {
                int index = getIndex(QuadObjects[i]);
                if (index != -1)
                {
                    subnodes[index].insert(QuadObjects[i]);
                    QuadObjects.Remove(QuadObjects[i]);
                }
                else
                {
                    i++;
                }
            }
        }
    }

    private int getIndex(rectangle pRect)
    {
        int index = -1;
        float verticalMidpoint = bounds.x + (bounds.width / 2);
        float horizontalMidpoint = bounds.z + (bounds.height / 2);

        // Object can completely fit within the top quadrants
        bool topQuadrant = (pRect.z < horizontalMidpoint && pRect.z + pRect.height < horizontalMidpoint);
        // Object can completely fit within the bottom quadrants
        bool bottomQuadrant = (pRect.z > horizontalMidpoint);

        // Object can completely fit within the left quadrants
        if (pRect.x < verticalMidpoint && pRect.x + pRect.width < verticalMidpoint)
        {
            if (topQuadrant)
            {
                index = 1;
            }
            else if (bottomQuadrant)
            {
                index = 2;
            }
        }
        // Object can completely fit within the right quadrants
        else if (pRect.x > verticalMidpoint)
        {
            if (topQuadrant)
            {
                index = 0;
            }
            else if (bottomQuadrant)
            {
                index = 3;
            }
        }

        return index;
    }

    public List<rectangle> retrieve(rectangle pRect)
    {
        var collisionRect = new List<rectangle>();
        int index = getIndex(pRect);
        if (index != -1 && subnodes[0] != null)
        {
            subnodes[index].retrieve(pRect);
        }

        collisionRect.AddRange(rectList);

        return collisionRect;
    }
    #endregion

}




