using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, Mesh};
	public DrawMode drawMode;
    public ProcTerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

	[Range(0,6)]
	public int editorPreviewLevelOfDetail;

	public bool autoUpdate;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void Awake() {
		textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
	}

    void OnValuesUpdated() {
        if(!Application.isPlaying) {
            DrawMapInEditor();
        }
    }

    void onTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public int mapChunkSize {
        get {
            if(terrainData.useFlatShading) {
                return 95;
            } else {
                return 239;
            }
        }
    }

	public void DrawMapInEditor() {
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);

		MapData mapData = GenerateMapData (Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		}  else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLevelOfDetail, terrainData.useFlatShading));
		}
	}

	public void RequestMapData(Vector2 center, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread (center, callback);
		};

		new Thread (threadStart).Start ();
	}

	void MapDataThread(Vector2 center, Action<MapData> callback) {
		MapData mapData = GenerateMapData (center);
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread (mapData, lod, callback);
		};

		new Thread (threadStart).Start ();
	}

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}

	void Update() {
		if (mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}

	MapData GenerateMapData(Vector2 center) {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize+2, mapChunkSize+2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center+noiseData.offset, noiseData.normalizeMode);
		float[,] heatMap = Noise.GenerateNoiseMap (mapChunkSize+2, mapChunkSize+2, noiseData.seed+1, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center+noiseData.offset, noiseData.normalizeMode);
		float[,] humidityMap = Noise.GenerateNoiseMap (mapChunkSize+2, mapChunkSize+2, noiseData.seed+2, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center+noiseData.offset, noiseData.normalizeMode);

		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
		return new MapData (noiseMap, heatMap, humidityMap);
	}

    void OnValidate() {
        if(terrainData != null) {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if(noiseData != null) {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if(textureData != null) {
            textureData.OnValuesUpdated -= onTextureValuesUpdated;
            textureData.OnValuesUpdated += onTextureValuesUpdated;
        }
    }

	struct MapThreadInfo<T> {
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo (Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
		
	}

}


public struct MapData {
	public readonly float[,] heightMap;
	public readonly float[,] heatMap;
	public readonly float[,] humidityMap;

	public MapData (float[,] heightMap,float[,] heatMap,float[,] humidityMap)
	{
		this.heightMap = heightMap;
		this.heatMap = heatMap;
		this.humidityMap = humidityMap;
	}
}