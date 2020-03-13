using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class QuadTree
{
    private int _maxAllowed = 2;
    
    private rectangle bounds;
    private QuadTree[] subnodes;
    private List<Prism> QuadObjects;
    private int level;

    private QuadTree _root;
    
    #region QuadTree methods

   

    public QuadTree(int pLevel, rectangle pBounds)
    {
        level = pLevel;
        QuadObjects = new List<Prism>();
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
        var height = bounds.maxV.z - bounds.minV.z;
        var width = bounds.maxV.x - bounds.minV.x;
        
        int subWidth = (int)(width / 2);
        int subHeight = (int)(height / 2);
        
        int x = (int)bounds.minV.x;
        int z = (int)bounds.minV.z;

        subnodes[0] = new QuadTree(level + 1, new rectangle(new Vector3(x + subWidth, 0f, z), new Vector3(x+width, 0f, z + subHeight)));
        subnodes[1] = new QuadTree(level + 1, new rectangle(new Vector3(x, 0f, z), new Vector3(x + subWidth, 0f, z + subHeight)));
        subnodes[2] = new QuadTree(level + 1, new rectangle(new Vector3(x, 0f, z + subHeight), new Vector3(x + subWidth, 0f, z + height)));
        subnodes[3] = new QuadTree(level + 1, new rectangle(new Vector3(x + subWidth, 0f, z + subHeight), new Vector3(x + width, 0f, z + height)));
    }

    public void insert(Prism prism)
    {
        var pRect = prism.bounds;
        if (subnodes[0] != null)
        {
            int index = getIndex(pRect);

            if (index != -1)
            {
                subnodes[index].insert(prism);

                return;
            }
        }

        QuadObjects.Add(prism);
        


        if (QuadObjects.Count > _maxAllowed)
        {
            if (subnodes[0] == null)
            {
                split();
            }

            int i = 0;
            while (i < QuadObjects.Count)
            {
                int index = getIndex(QuadObjects[i].bounds);
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
        
        var height = bounds.maxV.z - bounds.minV.z;
        var width = bounds.maxV.x - bounds.minV.x;
        
        float x = bounds.minV.x;
        float z = bounds.minV.z;
        
        float verticalMidpoint = x + (width / 2);
        float horizontalMidpoint = z + (height / 2);

        // Bottom
        bool bottomQuadrant = (pRect.minV.z < horizontalMidpoint && pRect.maxV.z < horizontalMidpoint);
        // Top
        bool topQuadrant = (pRect.minV.z > horizontalMidpoint);

        // Left
        if (pRect.minV.x < verticalMidpoint && pRect.maxV.x < verticalMidpoint)
        {
            if (topQuadrant)
            {
                index = 2;
            }
            else if (bottomQuadrant)
            {
                index = 1;
            }
        }
        // Right
        else if (pRect.minV.x > verticalMidpoint)
        {
            if (topQuadrant)
            {
                index = 3;
            }
            else if (bottomQuadrant)
            {
                index = 0;
            }
        }

        return index;
    }

    public List<Prism> retrieve(Prism prism)
    {
        var collisionRect = new List<rectangle>();
        int index = getIndex(prism.bounds);
        if (index != -1 && subnodes[0] != null)
        {
            return subnodes[index].retrieve(prism);
        }

        return QuadObjects;
    }
    #endregion

}




