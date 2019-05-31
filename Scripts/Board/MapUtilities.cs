using UnityEngine;
using static BoardManager;

public class MapUtilities : MonoBehaviour
{
    public static void normalizePercentages(ref Biome[] biomes) {
        float perc = 0;
        foreach (Biome b in biomes) {
            perc += b.percentage;
        }

        foreach (Biome b in biomes) {
            b.percentage /= perc;
        }

    }

    public static float[] createCumulativeDist(Biome[] biomes) {
        float[] cumDist = new float[biomes.Length];
        for (int i = 0; i < biomes.Length; i++) {
            cumDist[i] = biomes[i].percentage;
        }
        for (int i = 1; i < biomes.Length; i++) {
            cumDist[i] += cumDist[i - 1];
        }
        return cumDist;
    }

    public static int getBiomeIndex(float[] cumDist, float percentage) {
        for (int i = 0; i < cumDist.Length; i++) {
            if (percentage <= cumDist[i]) {
                return i;
            }
        }
        return cumDist.Length - 1;
    }

    public static float[,] perlinNoiseMap(int mapWidth, int mapHeight, int octaves, float persistance, float lacunarity) {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                float amp = 1;
                float freq = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++) {
                    float sampleX = (float)x / mapWidth * freq;
                    float sampleY = (float)y / mapHeight * freq;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlin * amp;

                    amp *= persistance;
                    freq *= lacunarity;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }
        return noiseMap;
    }

    public static void debugRenderMap(Transform boardHolder, int[,] map, Biome[] biomes, Vector2 midPos) {
        if (midPos == null) {
            midPos = Vector2.zero;
        }

        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                int biome = map[x, y];
                if (biome == -1) {
                    continue;
                }
                GameObject toInstantiate = biomes[biome].floorPrefabs[Random.Range(0, biomes[biome].floorPrefabs.Length)];

                GameObject instance =
                    Instantiate(toInstantiate, new Vector3(midPos.x + x - map.GetLength(0) / 2, midPos.y + y - map.GetLength(1) / 2, 0f), Quaternion.identity, boardHolder) as GameObject;

            }
        }

    }

    public static void debugRenderProps(Transform propHolder, GameObject[,] props, Biome[] biomes, Vector2 midPos) {
         for (int x = 0; x < props.GetLength(0); x++) {
            for (int y = 0; y < props.GetLength(1); y++) {
                if (props[x, y] != null) {
                    GameObject instance =
                        Instantiate(props[x, y], new Vector3(midPos.x + x - props.GetLength(0) / 2, midPos.y + y - props.GetLength(1) / 2, 0f), Quaternion.identity, propHolder) as GameObject;
                }
            }
        }
    }
}
