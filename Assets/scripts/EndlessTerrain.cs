using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessTerrain : MonoBehaviour {

    const float viewerMoveThreshholdForChunkUpdate = 25f;
    const float sqrViewerMoveThreshholdForChunkUpdate = viewerMoveThreshholdForChunkUpdate * viewerMoveThreshholdForChunkUpdate;

    public LODInfo[] detailLevels;
	public static float maxViewDst;

	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	public static Vector2 viewerPositionOld;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisibleInViewDst;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();
        maxViewDst = detailLevels[detailLevels.Length-1].visibleDstThreshold;
		chunkSize = mapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        UpdateVisibleChunks ();

	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;
        if((viewerPositionOld-viewerPosition).sqrMagnitude > sqrViewerMoveThreshholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
		    UpdateVisibleChunks ();
        }
	}
		
	void UpdateVisibleChunks() {

		for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
			terrainChunksVisibleLastUpdate [i].SetVisible (false);
		}
		terrainChunksVisibleLastUpdate.Clear ();
			
		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
					terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
				} else {
					terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
				}

			}
		}
	}

	public class TerrainChunk {

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;
        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;


        public bool hasFauna;
        public bool faunaLoaded;

        GameObject[] fauna;


		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
			this.detailLevels = detailLevels;
            position = coord * size;
			bounds = new Bounds(position,Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x,0,position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
			meshRenderer.material = material;

			meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
			meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
			SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for(int i =0 ; i< detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                if(detailLevels[i].useForCollider) {
                    collisionLODMesh = lodMeshes[i];
                }
            }

			mapGenerator.RequestMapData(position, OnMapDataReceived);
		}

		void OnMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;
            UpdateTerrainChunk();
		}

		public void UpdateTerrainChunk() {
            if(!mapDataReceived)
                return;

			float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (viewerPosition));
			bool visible = viewerDstFromNearestEdge <= maxViewDst;

            if(visible) {
                int lodIndex = 0;
                for(int i=0; i< detailLevels.Length-1; i++) {
                    if(viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
                        lodIndex = i+1;
                    } else {
                        break;
                    }
                }

                if(lodIndex != previousLODIndex) {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if(lodMesh.hasMesh) {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    } else if(!lodMesh.hasRequestedMesh) {
                        lodMesh.RequestMesh(mapData);
                    }
                }

                if(lodIndex == 0) {
                    if(collisionLODMesh.hasMesh) {
                        meshCollider.sharedMesh = collisionLODMesh.mesh;
                        LoadFauna();
                    } else if(!collisionLODMesh.hasRequestedMesh) {
                        collisionLODMesh.RequestMesh(mapData);
                    }
                } else {
                    UnloadFauna();
                }

                terrainChunksVisibleLastUpdate.Add(this);
            }
			SetVisible (visible);
		}

		public void SetVisible(bool visible) {
			meshObject.SetActive (visible);
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}

       
        private void GenerateFauna(int modulo) {
            LODMesh lodMesh = lodMeshes[0];
            if(!lodMesh.hasMesh)
                return;
            Vector3 posOffset = new Vector3(position.x, 0, position.y) * mapGenerator.terrainData.uniformScale;
            int len = lodMesh.data.vertices.Length / modulo;
            fauna = new GameObject[len];

            for(int i = 0; i < fauna.Length; i++) {
                Vector3 pos = lodMesh.data.vertices[i*modulo];
                int biomeIndex = lodMesh.data.biomes[i*modulo];
                GameObject t = (GameObject) Instantiate(GetObjectForBiome(biomeIndex, i), posOffset + pos * mapGenerator.terrainData.uniformScale, Quaternion.identity);
                t.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale / 2;
                fauna[i] = t;
            }
            hasFauna = true;
            faunaLoaded = true;
        }
       

        GameObject GetObjectForBiome(int biomeIndex, int seed) {
            //pos /= mapGenerator.terrainData.uniformScale;
            Random.InitState(seed*132456);
            BiomeData data = mapGenerator.biomes[biomeIndex % 8];
            int index = Random.Range(0, data.plants.Length);
            return data.plants[index];
        }


        public void LoadFauna() {
            if(faunaLoaded)
                return;
            if(!hasFauna) {
                GenerateFauna(120);
                return;
            }
            for(int i = 0; i < fauna.Length; i++) {
                fauna[i].SetActive(true);
            }
            faunaLoaded = true;
        }

        public void UnloadFauna() {
            if(!faunaLoaded)
                return;
            for(int i = 0; i < fauna.Length; i++) {
                fauna[i].SetActive(false);
            }
            faunaLoaded = false;
        }

	}

    class LODMesh {

        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;

        int lod;

        public MeshData data;

        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }   

        void OnMeshDataReceived(MeshData meshData) {
            data = meshData;
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();

        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float visibleDstThreshold;
        public bool useForCollider;
    }

}