using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdateableData {
    public float uniformScale = 2.5f;

    public float meshHeightMultiplier;

    public AnimationCurve meshHeightCurve;

    public bool useFalloff;

    public bool useFlatShading;

    public float minHeight {
        get {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }

    public float maxHeight {
        get {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }

}
