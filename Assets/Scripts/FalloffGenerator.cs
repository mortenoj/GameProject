using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator {

    // This will create a map which is 0 values at the edges and higher values -> 1 towards the center
    // Used to create islands
    public static float[,] GenerateFalloffMap(int size) {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                float x = i / (float) size * 2 - 1;
                float y = j / (float) size * 2 - 1;

                // Which is closer to the edge (or closer to 1)
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i,j] = Evaluate(value);
            }
        }

        return map;
    }

    static float Evaluate(float value) {
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value,a) + Mathf.Pow(b-b*value, a));
    }
} 
