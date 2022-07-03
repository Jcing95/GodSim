using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ProcTerrainData : UpdatableData
{

    public float uniformScale;
    public bool useFlatShading;
	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

    public float minHeight {
        get{
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }
    
    public float maxHeight {
        get{
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }

}
