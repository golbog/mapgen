using System;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const int numNeighToGenerate = 1;
    public Transform player;
    private static Transform boardHolder;

    public static Vector2 playerPos;
    static int chunkSize;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksGeneratedLastUpdate = new List<TerrainChunk>();
    static BoardManager boardManager;
    
    void Start()
    {
        boardHolder = new GameObject("BoardHolder").transform;
        GameObject bm = GameObject.Find("BoardManager");
        boardManager = bm.GetComponent(typeof(BoardManager)) as BoardManager;
        chunkSize = boardManager.startDim;
    }

    private void Update() {
        playerPos = new Vector2(player.position.x, player.position.y);
        Debug.Log("penis");
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksGeneratedLastUpdate.Count; i++) {
            terrainChunksGeneratedLastUpdate[i].SetVisible(false);
        }
        terrainChunksGeneratedLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(playerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(playerPos.y / chunkSize);
        
        for (int yOffset = -numNeighToGenerate; yOffset <= numNeighToGenerate; yOffset++) {
            for (int xOffset = -numNeighToGenerate; xOffset <= numNeighToGenerate; xOffset++) {
                Vector2 generatedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDict.ContainsKey(generatedChunkCoord)) {
                    terrainChunkDict[generatedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDict[generatedChunkCoord].IsVisible()) {
                        terrainChunksGeneratedLastUpdate.Add(terrainChunkDict[generatedChunkCoord]);
                    }
                }
                else {
                    terrainChunkDict.Add(generatedChunkCoord, new TerrainChunk(generatedChunkCoord, chunkSize, transform));
                }
            }
        }
    }

    public class TerrainChunk
    {
        Vector2 position;
        Bounds bounds;
        GameObject chunk;

        public TerrainChunk(Vector2 coord, int size, Transform parent) {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            int[,] map;
            GameObject[,] props;

            (map,props) = boardManager.createChunk(coord, 0);
            chunk = new GameObject();
            chunk.transform.SetParent(boardHolder);
            chunk.name = String.Format("Chunk ({0},{1})", coord.x, coord.y);
            MapUtilities.debugRenderMap(chunk.transform, map, boardManager.biomes, position);
            MapUtilities.debugRenderProps(chunk.transform, props, boardManager.biomes, position);
        }

        public void UpdateTerrainChunk() {
            float playerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(playerPos)) / chunkSize;
            bool visible = playerDstFromNearestEdge <= numNeighToGenerate;
            SetVisible(visible);
        }

        public void SetVisible(bool visible) {
            chunk.SetActive(visible);
        }

        public bool IsVisible() {
            return chunk.activeSelf;
        }
    }
}
