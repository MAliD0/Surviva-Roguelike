using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoiseGenerator
{
    public float[,] GenerateNoiseMap(PerlinNoiseParameters parameters)
        {
            float[,] noiseMap = new float[parameters.width, parameters.height];

            System.Random prng = new System.Random(parameters.seed);
            Vector2[] octavesOffsets = new Vector2[parameters.octaves];
            for (int i = 0; i < parameters.octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + parameters.offset.x;
                float offsetY = prng.Next(-100000, 100000) + parameters.offset.y;
                octavesOffsets[i] = new Vector2(offsetX, offsetY);
            }

            if (parameters. scale == 0)
            {
                parameters.scale = 0.00001f;
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;
            float halfWidth = parameters.width / 2f;
            float halfHeight = parameters.height / 2f;


            for (int y = 0; y < parameters.height; y++)
            {
                for (int x = 0; x < parameters.width; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < parameters.octaves; i++)
                    {
                        float sampleX = (x - halfWidth) / parameters.scale * frequency + octavesOffsets[i].x;
                        float sampleY = (y - halfHeight) / parameters.scale * frequency + octavesOffsets[i].y;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= parameters.persistence;
                        frequency *= parameters.lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }

                    noiseMap[x, y] = noiseHeight;
                }
            }

            for (int y = 0; y < parameters.height; y++)
            {
                for (int x = 0; x < parameters.width; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }

        return noiseMap;
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, float pow = 1.0f)
    {
        float[,] noiseMap = new PerlinNoiseGenerator().GenerateNoiseMap(new PerlinNoiseParameters
        {
            width = mapWidth,
            height = mapHeight,
            scale = scale,
            octaves = octaves,
            persistence = persistance,
            lacunarity = lacunarity,
            seed = seed,
            offset = offset
        });

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.Clamp01(Mathf.Pow(noiseMap[x, y], pow));
            }
        }

        return noiseMap;
    }
}

public class PerlinNoiseParameters
{
    public int width;
    public int height;
    public float scale;
    public int octaves;
    public float persistence;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    public float meters;
}
