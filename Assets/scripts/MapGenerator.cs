using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, Mesh};
	public DrawMode drawMode;
    public ProcTerrainData terrainData;
    public NoiseData heightData;
    public NoiseData heatData;
    public NoiseData humidityData;
    public TextureData textureData;

    public Material terrainMaterial;

	[Range(0,6)]
	public int editorPreviewLevelOfDetail;

	public bool autoUpdate;

	public BiomeData[] biomes;


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
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap,mapData.colorMap,mapData.biomeMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLevelOfDetail, terrainData.useFlatShading));
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
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, mapData.colorMap,mapData.biomeMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
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
		float[,] heightMap = Noise.GenerateNoiseMap (mapChunkSize+2, mapChunkSize+2, heightData.seed, heightData.noiseScale, heightData.octaves, heightData.persistance, heightData.lacunarity, center+heightData.offset, heightData.normalizeMode);
		float[,] heatMap = Noise.GenerateNoiseMap (mapChunkSize+2, mapChunkSize+2, heatData.seed+1, heatData.noiseScale, heatData.octaves, heatData.persistance, heatData.lacunarity, center+heatData.offset, heatData.normalizeMode);
		float[,] humidityMap = Noise.GenerateNoiseMap (mapChunkSize+2, mapChunkSize+2, humidityData.seed+2, humidityData.noiseScale, humidityData.octaves, persistance: humidityData.persistance, humidityData.lacunarity, center+humidityData.offset, humidityData.normalizeMode);
		Color[,] colorMap = CreateColorMap(heightMap, heatMap, humidityMap);
		int[,] biomeMap = CreateBiomeMap(heightMap, heatMap, humidityMap);
		return new MapData (heightMap, heatMap, humidityMap, colorMap, biomeMap);
	}

	int[,] CreateBiomeMap(float[,] heightMap, float[,] heatMap, float[,] humidityMap) {
		int[,] biomeMap = new int[heightMap.GetLength(0),heightMap.GetLength(1)];
		for(int y = 0; y < biomeMap.GetLength(0); y++) {
            for(int x = 0; x < biomeMap.GetLength(1); x++) {
				bool height = heightMap[x,y] > 0.5;
				bool heat = heatMap[x,y] > 0.5;
				bool humidity = humidityMap[x,y] > 0.5;
				biomeMap[x,y] = findBiomeIndex(height, heat, humidity);
			}
		}
		return biomeMap;
	}

	Color[,] CreateColorMap(float[,] heightMap, float[,] heatMap, float[,] humidityMap) {
		Color[,] colorMap = new Color[heightMap.GetLength(0),heightMap.GetLength(1)];
		for(int y = 0; y < colorMap.GetLength(0); y++) {
            for(int x = 0; x < colorMap.GetLength(1); x++) {
				bool height = heightMap[x,y] > 0.5;
				bool heat = heatMap[x,y] > 0.5;
				bool humidity = humidityMap[x,y] > 0.5;
				BiomeData biome = findBiome(height, heat, humidity);
				colorMap[x,y] = biome.GetColor(0, 1, heightMap[x,y]);
			}
		}
		return colorMap;
	}

	public BiomeData findBiome(bool height, bool heat, bool humidity) {
		for(int i=0; i < biomes.Length; i++) {
			BiomeData b = biomes[i];
			if(b.high == height && b.hot == heat && b.dry == humidity){
				return b;
			}
		}
		return null;
	}

	public int findBiomeIndex(bool height, bool heat, bool humidity) {
		for(int i=0; i < biomes.Length; i++) {
			BiomeData b = biomes[i];
			if(b.high == height && b.hot == heat && b.dry == humidity)
				return i;
		}
		return -1;
	}

    void OnValidate() {
        if(terrainData != null) {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if(heightData != null) {
            heightData.OnValuesUpdated -= OnValuesUpdated;
            heightData.OnValuesUpdated += OnValuesUpdated;
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

	public readonly Color[,] colorMap;
	public readonly int[,] biomeMap;

	public MapData (float[,] heightMap,float[,] heatMap,float[,] humidityMap, Color[,] colorMap, int[,] biomeMap)
	{
		this.heightMap = heightMap;
		this.heatMap = heatMap;
		this.humidityMap = humidityMap;
		this.colorMap = colorMap;
		this.biomeMap = biomeMap;
	}
}

