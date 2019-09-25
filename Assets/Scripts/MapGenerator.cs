using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

    // Map settings
    public enum DrawMode {
        NoiseMap,
        ColorMap,
        Mesh,
        FalloffMap
    };
    
    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;

    public bool useFalloff;
    public bool useFlatShading;

    // Perlin noise settings
    public float lacunarity;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]
    public float persistance;
    public int seed; 
    public Vector2 offset;

    // Mesh settings
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    [Range(0,6)]
    public int editorPreviewLOD;
    public bool autoUpdate;

    public TerrainType[] regions;

    float[,] falloffMap;

    static MapGenerator instance;

    // Threads
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>(); 
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();


    // Methods
    
    void Awake() {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    public static int mapChunkSize {
        get {
            if (instance == null) {
                instance = FindObjectOfType<MapGenerator>();
            }
            if (instance.useFlatShading) {
                return 95;
            }
            return 239;
        }
    }
    
    public void DrawMapInEditor() {
        MapData mapdata = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode) {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapdata.heightMap));
                break;
            case DrawMode.ColorMap:
                display.DrawTexture(TextureGenerator.TextureFromColorMap(mapdata.colorMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(
                        MeshGenerator.GenerateTerrainMesh(mapdata.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, useFlatShading),
                        TextureGenerator.TextureFromColorMap(mapdata.colorMap, mapChunkSize, mapChunkSize)
                    );
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
                break;
        }
    }

    MapData GenerateMapData(Vector2 center) {
        float [,] noiseMap = Noise.GenerateNoiseMap(
                mapChunkSize + 2, // width. Add 2 for the border
                mapChunkSize + 2, // height. Add 2 for the border
                noiseScale,
                octaves,
                persistance,
                lacunarity,
                seed,
                center + offset,
                normalizeMode
            );

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                if (useFalloff) {
                    noiseMap[x,y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap[x,y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight >= regions[i].height) {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                    } else {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    void Update() {
        if (mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback) {
        MapData mapData = GenerateMapData(center);
        lock(mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod, useFlatShading);
        lock(meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }
        if (octaves < 0) {
            octaves = 0;
        }

        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}


[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap) {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}






