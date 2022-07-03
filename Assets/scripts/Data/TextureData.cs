using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TextureData : UpdatableData
{

    public Color[] baseColors;
    
    [Range(0,1)]
    public float[] baseStartHeights;
    
    [Range(0,1)]
    public float[] baseBlends;

    float savedMinHeight;
    float savedMaxHeight; 


    public void ApplyToMaterial(Material material) {
        material.SetInt("baseColorCount", baseColors.Length);
        material.SetColorArray("baseColors", baseColors);
        material.SetFloatArray("baseStartHeights", baseStartHeights);
        material.SetFloatArray("baseBlends", values: baseBlends);
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    } 

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) {
        Debug.Log("heights updated");
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    public Color GetColor(float minHeight, float maxHeight, float height) {
        Color c = Color.black;
        float heightPercent = Mathf.InverseLerp(minHeight,maxHeight, height);
        for(int i = 0; i < baseColors.Length; i++) {
            float drawStrength = Mathf.InverseLerp(-baseBlends[i]/2-float.Epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);
            c = c * (1-drawStrength) + baseColors[i] * drawStrength;
        }
        return c;
    }
}
