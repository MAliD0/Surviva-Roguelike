using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using World;

[ExecuteAlways]
public class WorldMapGenerator : MonoBehaviour
{
    //World generation will be divided into few parts:
    //1. Generate canvas, consisting of basic tiles
    //  > Canvas will be moved up, to give space for boat
    //2. Get list of objects required on the map: trees, rocks, etc
    //3. Place objects on the map

    [FoldoutGroup("Settings/Blank World Map")] [SerializeField] private int width = 100;
    [FoldoutGroup("Settings/Blank World Map")] [SerializeField] private int height = 100;
    [FoldoutGroup("Settings/Blank World Map")] [SerializeField] private Vector2Int zeroCoordinates = Vector2Int.zero;
    [FoldoutGroup("Settings/Objects on Map")] [SerializeField] private List<WorldObjectToPlace> objectsToPlaceOnMap;
    [FoldoutGroup("Settings/Objects on Map")] [SerializeField] private Vector2 objectGenerationArea = new Vector2(80, 80);
    [FoldoutGroup("Settings/Objects on Map")] [SerializeField] private int gapFromTheEdge = 5;

    [FoldoutGroup("Settings")] [SerializeField] private float radius = 1;
    [FoldoutGroup("References")] [SerializeField] private WorldMapManager worldMapManager;

    [FoldoutGroup("First Step Variables")][SerializeField] MapBlockData defaultMapBlockData;

    [FoldoutGroup("Debug Buttons")] [Button("Generate Blank World Map")] public void GenerateBlankWorldMapButton(){GenerateBlankWorldMap(width, height, defaultMapBlockData);}
    [FoldoutGroup("Debug Buttons")] [Button("Generate Objects")] public void GenerateObjectsOnMapButton() {GenerateObjectsOnMap(objectsToPlaceOnMap);}
    [FoldoutGroup("Debug Buttons")] [Button("Clear All Tilemaps")] public void ClearTilemap() { worldMapManager.ClearAllLayers();}

    public void GenerateBlankWorldMap(int width, int height, MapBlockData mapBlockData = null)
    {
        worldMapManager.initiateForTesting();

        //TO DO: Generate blank world map based on width and height

        int zeroX = zeroCoordinates.x;
        int zeroY = zeroCoordinates.y;

        int minX = zeroX - (width / 2);
        int maxX = zeroX + (width / 2);

        int minY = zeroY + (height / 2);
        int maxY = zeroY + (height / 2);

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                worldMapManager.SetTile(new Vector2(x, y), mapBlockData);
            }
        }
    }

    public void GenerateObjectsOnMap(List<WorldObjectToPlace> objectsToPlace)
    {
        int numberOfObjectsToPlace = 0;
        objectsToPlace.ForEach(obj =>
        {
            numberOfObjectsToPlace += obj.quantity;
        });

        List<Vector2> points = PoissonDicsSampling.GeneratePoints(radius, objectGenerationArea, 30, numberOfObjectsToPlace);
        int pointsCount = points.Count;

        for (int i = 0; i < objectsToPlace.Count; i++)
        {
            for (var j = 0; j < objectsToPlace[i].quantity; j++)
            {
                int randomPoint = UnityEngine.Random.Range(0, points.Count);
                Vector2 position = points[randomPoint] + zeroCoordinates + (Vector2.up * (gapFromTheEdge + ((height - objectGenerationArea.y) /2)));

                worldMapManager.SetTile(position, objectsToPlace[i].objectData);

                points.RemoveAt(randomPoint);
            }
        }
    }

    [Serializable] public struct WorldObjectToPlace
    {
        public MapBlockData objectData;
        public int quantity;
    }
}
