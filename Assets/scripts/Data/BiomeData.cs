using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BiomeData : UpdatableData
{
    public bool hot;
    public bool dry;
    public bool high;

    public GameObject[] plants;
    public TextureData colors;
    
    public Color GetColor(float minHeight, float maxHeight, float height) { 
        float midHeight = (minHeight + maxHeight) / 2;
        minHeight = high ? midHeight : minHeight;
        maxHeight = high ? maxHeight : midHeight;
        return colors.GetColor(minHeight,maxHeight,height);
    }
}
