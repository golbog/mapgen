﻿using UnityEngine;
using System;
using Random = UnityEngine.Random;
//using Mirror;
using static MapUtilities;
using static BoardManager;

public class GrowTerrain : MonoBehaviour {
    [Serializable]
    public class Count {
        public int minimum;
        public int maximum;

        public Count(int min, int max) {
            minimum = min;
            maximum = max;
        }
    }

    //[SyncVar]
    public int seed = 42;
    public int startDim = 5;
    public int numIter = 5;
    public bool smooth = true;

    public GameObject middleProp;

    public Biome[] biomesGT;
    private Transform boardHolder;
    private Transform propHolder;

    private int[,] map;
    private GameObject[,] props;

    private void Start() {
        //if (isServer) { seed = (int) Time.time; } //ODKOMENTEREJ ZA MP
        MapUtilities.normalizePercentages(ref biomesGT);
    }

    public (int[,], GameObject[,]) createChunk(Vector2 coord, int seed) {
        Random.InitState(seed);
        int[,] map;
        GameObject[,] props;

        map = growBiomes(biomesGT, startDim, coord);
        int dim = startDim;
        for (int i = 0; i < numIter; i++) {
            (map, dim) = zoom(map, dim, coord);
        }

        if (smooth) {
            map = smoothen(map);
        }
        props = populate(map);

        if (middleProp != null && coord.x == 0 && coord.y == 0) {
            (map, props) = placeMiddleProp(map, props);
        }
        return (map, props);
    }
    private int[,] growBiomes(Biome[] biomes, int dim, Vector2 coord) {
        float[] cumDist = createCumulativeDist(biomes);
        int[,] biomeMap = new int[dim, dim];

        coord *= dim;

        // First step initialize "zoomed out" image
        for (int x = 0; x < dim; x++) {
            for (int y = 0; y < dim; y++) {
                Random.InitState(seed + ((int)coord.x + x) + ((int)coord.y + y));

                biomeMap[x, y] = getBiomeIndex(cumDist, Random.Range(0.0f, 1.0f));
            }
        }

        return biomeMap;
    }

    private (int[,], int) zoom(int[,] prevBiomeMap, int prevDim, Vector2 coord) {
        int dim = prevDim * 2 - 1;
        int[,] biomeMap = new int[dim, dim];

        Random.InitState(seed * prevDim + (int)coord.x + (int)coord.y);

        for (int x = 0; x < dim; x++) {
            for (int y = 0; y < dim; y++) {
                if (x % 2 == 0 && y % 2 == 0) {
                    biomeMap[x, y] = prevBiomeMap[x / 2, y / 2];
                }
                else if (x % 2 == 0) {
                    biomeMap[x, y] = Random.Range(0, 2) == 1 ? prevBiomeMap[x / 2, y / 2] : prevBiomeMap[x / 2, y / 2 + 1];
                }
                else if (y % 2 == 0) {
                    biomeMap[x, y] = Random.Range(0, 2) == 1 ? prevBiomeMap[x / 2, y / 2] : prevBiomeMap[x / 2 + 1, y / 2];
                }
                else {
                    if (Random.Range(0, 2) == 0) {
                        biomeMap[x, y] = Random.Range(0, 2) == 1 ? prevBiomeMap[x / 2, y / 2] : prevBiomeMap[x / 2, y / 2 + 1];
                    }
                    else {
                        biomeMap[x, y] = Random.Range(0, 2) == 1 ? prevBiomeMap[x / 2 + 1, y / 2] : prevBiomeMap[x / 2 + 1, y / 2 + 1];
                    }
                }
            }
        }
        return (biomeMap, dim);
    }

    private int[,] smoothen(int[,] map) {
        for (int x = 1; x < map.GetLength(0) - 1; x++) {
            for (int y = 1; y < map.GetLength(1) - 1; y++) {
                if (map[x - 1, y] == map[x + 1, y] && map[x, y - 1] == map[x, y + 1]) {
                    map[x, y] = Random.Range(0, 2) == 0 ? map[x - 1, y] : map[x, y - 1];
                }
                else if (map[x - 1, y] == map[x + 1, y]) {
                    map[x, y] = map[x - 1, y];
                }
                else if (map[x, y - 1] == map[x, y + 1]) {
                    map[x, y] = map[x, y - 1];
                }
            }
        }
        return map;
    }

    private GameObject[,] populate(int[,] map) {
        props = new GameObject[map.GetLength(0), map.GetLength(1)];
        int octaves = 5;
        float persistance = 0.5f;
        float lacunarity = 2f;

        float[,] noiseMap = perlinNoiseMap(map.GetLength(0), map.GetLength(1), octaves, persistance, lacunarity);


        for (int x = 0; x < map.GetLength(0) - 1; x++) {
            for (int y = 0; y < map.GetLength(1) - 1; y++) {
                int b = map[x, y];
                if (x > 0 && y > 0 && (props[x - 1, y] != null || props[x, y - 1] != null)) {
                    continue;
                }
                if (biomesGT[b].propsPrefabs.Length < 1) {
                    continue;
                }
                if (Random.Range(0.0f, 1.0f) > 0.9f || noiseMap[x, y] > 1.5f) { // PLACEHOLDER PERCENTAGES
                    props[x, y] = biomesGT[b].propsPrefabs[Random.Range(0, biomesGT[b].propsPrefabs.Length)];
                }
                else {
                    props[x, y] = null;
                }
            }
        }
        return props;
    }

    private (int[,], GameObject[,]) placeMiddleProp(int[,] map, GameObject[,] props) {
        float width = middleProp.GetComponent<SpriteRenderer>().bounds.size.x; // recheck this!
        float height = middleProp.GetComponent<SpriteRenderer>().bounds.size.y;

        int startLocX = map.GetLength(0) / 2;
        int startLocY = map.GetLength(1) / 2;

        for (int x = Mathf.RoundToInt(startLocX - width / 2); x < width + Mathf.RoundToInt(startLocX - width / 2); x++) {
            for (int y = Mathf.RoundToInt(startLocY - height / 2); y < height + Mathf.RoundToInt(startLocY - height / 2); y++) {
                map[x, y] = -1;
                props[x, y] = null;
            }
        }
        props[startLocX, startLocY] = middleProp;

        return (map, props);
    }

    /*
    void Update() {
        if (Input.GetKeyDown("up")) { 
            Destroy(boardHolder.gameObject);
            (map, dim) = zoom(map, dim);
            debugRenderMap();
        }
        if (Input.GetKeyDown("down")) {
            Destroy(boardHolder.gameObject);
            smoothen();
            debugRenderMap();
        }
        if (Input.GetKeyDown("right")) {
            populate();
            debugRenderProps();
        }
        if (Input.GetKeyDown("left")) {
            Destroy(propHolder.gameObject);
        }

    }

    private void OnGUI() {
        if (Debug.isDebugBuild) {
            GUILayout.BeginArea(new Rect(100, 400, 215, 9999));
            GUILayout.Label("Seed for map=" + seed);
            GUILayout.EndArea();
        }
    }
    */

    public int[,] cutMap(int[,] map) {
        int lenX = map.GetLength(0) / 2 + 1;
        int lenY = map.GetLength(1) / 2 + 1;
        int[,] subMap = new int[lenX, lenY];
        for (int x = 0; x < lenX; x++) {
            for (int y = 0; y < lenY; y++) {
                subMap[x, y] = map[lenX - lenX / 2 + x, lenY - lenY / 2 + y];
            }
        }
        return subMap;
    }

}



