using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class RuntimeAlchemyCirclesManager : MonoBehaviour
{
    [SerializeField]
    private List<RuntimeAlchemyCircle> runtimeAlchemyCircles = new List<RuntimeAlchemyCircle>();

    [SerializeField]
    private SerializedDictionary<Vector2Int, RuntimeAlchemyCircle> circleByTile = new SerializedDictionary<Vector2Int, RuntimeAlchemyCircle>();

    [SerializeField]
    private Vector2 testCoordinate;

    private readonly AlchemyCircleScanner alchemyCircleScanner = new AlchemyCircleScanner();

    private MapLayerLogic mediumLayer;

    private void Awake()
    {
        runtimeAlchemyCircles ??= new List<RuntimeAlchemyCircle>();

        circleByTile ??=
            new SerializedDictionary<Vector2Int, RuntimeAlchemyCircle>();
    }

    private void Start()
    {
        if (WorldMapManager.Instance == null)
        {
            Debug.LogError(
                "[RuntimeAlchemyCirclesManager] WorldMapManager is missing."
            );

            return;
        }

        mediumLayer = WorldMapManager.Instance.GetLayer(
            MapLayerType.mediumLayer
        );

        if (mediumLayer == null)
        {
            Debug.LogError(
                "[RuntimeAlchemyCirclesManager] Medium layer is missing."
            );

            return;
        }

        mediumLayer.onMapTilePlaced += OnMediumTileSet;
        mediumLayer.onMapTileRemoved += OnMediumTileRemoved;

        WorldMapManager.Instance.onObjectCreated += OnCircleElementCreated;
        WorldMapManager.Instance.onObjectDestroyed += OnCircleElementDestroyed;

        RebuildAllCircles();
    }

    private void OnCircleElementDestroyed(MapLayerType type, Vector2Int int1, Vector2Int int2, GameObject @object)
    {
        print("Enter");

        if(type != MapLayerType.circleElementLayer || @object == null) return;

        if(circleByTile.TryGetValue(int1, out RuntimeAlchemyCircle runtimeAlchemyCircle))
        {
            runtimeAlchemyCircle.RemoveElement(int1);
        }
    }

    private void OnCircleElementCreated(MapLayerType type, Vector2Int int1, Vector2Int int2, GameObject @object)
    {
        print("Enter");

        if(type != MapLayerType.circleElementLayer || @object == null) return;

        if(circleByTile.TryGetValue(int1, out RuntimeAlchemyCircle runtimeAlchemyCircle))
        {
            if(!@object.TryGetComponent(out AlchemyArrayComponentInstance component)) return;

            runtimeAlchemyCircle.TryAddElement(int1, component);
        }
    }

    private void OnDestroy()
    {
        if (mediumLayer == null)
            return;

        mediumLayer.onMapTilePlaced -= OnMediumTileSet;
        mediumLayer.onMapTileRemoved -= OnMediumTileRemoved;
    }

    private void OnMediumTileSet(
        Dictionary<Vector2Int, HashSet<Vector2Int>> placedCells,
        MapBlockData data
    )
    {
        if (mediumLayer == null || data == null)
            return;

        Dictionary<Vector2Int, MapTile> tiles =
            mediumLayer.GetFullTileDictionary();

        foreach (Vector2Int position in placedCells.Keys)
        {
            if (!tiles.TryGetValue(position, out MapTile startTile))
                continue;

            CircleGroup circleGroup =
                alchemyCircleScanner.FindConnectedGroupAt(
                    position,
                    tiles,
                    startTile
                );

            if (circleGroup == null)
                continue;

            if (circleGroup.Coordinates == null ||
                circleGroup.Coordinates.Count == 0)
            {
                continue;
            }

            RegisterCircleGroup(circleGroup, data);
        }
    }

    private void OnMediumTileRemoved(
        Dictionary<Vector2Int, HashSet<Vector2Int>> removedCells,
        MapBlockType blockType
    )
    {
        if (removedCells == null || removedCells.Count == 0)
            return;

        HashSet<Vector2Int> removedPositions =
            new HashSet<Vector2Int>(removedCells.Keys);

        HashSet<RuntimeAlchemyCircle> affectedCircles =
            new HashSet<RuntimeAlchemyCircle>();

        foreach (Vector2Int removedPosition in removedPositions)
        {
            if (circleByTile.TryGetValue(
                    removedPosition,
                    out RuntimeAlchemyCircle circle
                ))
            {
                affectedCircles.Add(circle);
            }

            circleByTile.Remove(removedPosition);
        }

        foreach (RuntimeAlchemyCircle affectedCircle in affectedCircles)
        {
            RebuildAffectedCircle(
                affectedCircle,
                removedPositions
            );
        }
    }

    private void RegisterCircleGroup(
        CircleGroup circleGroup,
        MapBlockData data
    )
    {
        HashSet<RuntimeAlchemyCircle> existingCircles =
            new HashSet<RuntimeAlchemyCircle>();

        foreach (Vector2Int coordinate in circleGroup.Coordinates)
        {
            if (!circleByTile.TryGetValue(
                    coordinate,
                    out RuntimeAlchemyCircle circle
                ))
            {
                continue;
            }

            if (circle == null)
                continue;

            if (circle.mapBlockData != data)
                continue;

            existingCircles.Add(circle);
        }

        RuntimeAlchemyCircle targetCircle;

        if (existingCircles.Count == 0)
        {
            targetCircle = CreateCircle(data);
        }
        else
        {
            targetCircle = GetFirstCircle(existingCircles);
        }

        if (targetCircle == null)
            return;

        foreach (RuntimeAlchemyCircle otherCircle in existingCircles)
        {
            if (otherCircle == null ||
                otherCircle == targetCircle)
            {
                continue;
            }

            MergeCircles(targetCircle, otherCircle);
        }

        foreach (Vector2Int coordinate in circleGroup.Coordinates)
        {
            targetCircle.occupiedTiles.Add(coordinate);
            circleByTile[coordinate] = targetCircle;
        }
    }

    private RuntimeAlchemyCircle CreateCircle(
        MapBlockData blockData
    )
    {
        RuntimeAlchemyCircle newCircle =
            new RuntimeAlchemyCircle
            {
                mapBlockData = blockData,
                occupiedTiles = new HashSet<Vector2Int>()
            };

        runtimeAlchemyCircles.Add(newCircle);

        return newCircle;
    }

    private void MergeCircles(
        RuntimeAlchemyCircle target,
        RuntimeAlchemyCircle source
    )
    {
        if (target == null || source == null)
            return;

        if (target == source)
            return;

        if (target.mapBlockData != source.mapBlockData)
            return;

        foreach (Vector2Int coordinate in source.occupiedTiles)
        {
            target.occupiedTiles.Add(coordinate);
            circleByTile[coordinate] = target;
        }

        runtimeAlchemyCircles.Remove(source);
    }

    private void RebuildAffectedCircle(
        RuntimeAlchemyCircle oldCircle,
        HashSet<Vector2Int> removedPositions
    )
    {
        if (oldCircle == null)
            return;

        HashSet<Vector2Int> remainingPositions =
            new HashSet<Vector2Int>(oldCircle.occupiedTiles);

        remainingPositions.ExceptWith(removedPositions);

        RemoveCircleRegistration(oldCircle);

        while (remainingPositions.Count > 0)
        {
            Vector2Int startPosition =
                AlchemyCircleScanner.GetFirst(remainingPositions);

            HashSet<Vector2Int> connectedGroup =
                alchemyCircleScanner.FindConnectedPositions(
                    startPosition,
                    remainingPositions
                );

            if (connectedGroup == null ||
                connectedGroup.Count == 0)
            {
                Debug.LogError(
                    "[RuntimeAlchemyCirclesManager] " +
                    "Scanner returned an empty connected group."
                );

                remainingPositions.Remove(startPosition);
                continue;
            }

            RuntimeAlchemyCircle newCircle =
                CreateCircle(oldCircle.mapBlockData);

            foreach (Vector2Int position in connectedGroup)
            {
                newCircle.occupiedTiles.Add(position);
                circleByTile[position] = newCircle;

                remainingPositions.Remove(position);
            }
        }
    }

    private void RemoveCircleRegistration(
        RuntimeAlchemyCircle circle
    )
    {
        runtimeAlchemyCircles.Remove(circle);

        foreach (Vector2Int position in circle.occupiedTiles)
        {
            if (circleByTile.TryGetValue(
                    position,
                    out RuntimeAlchemyCircle registeredCircle
                ) &&
                registeredCircle == circle)
            {
                circleByTile.Remove(position);
            }
        }
    }

    private RuntimeAlchemyCircle GetFirstCircle(HashSet<RuntimeAlchemyCircle> circles)
    {
        foreach (RuntimeAlchemyCircle circle in circles)
            return circle;

        return null;
    }

    private void RebuildAllCircles()
    {
        runtimeAlchemyCircles.Clear();
        circleByTile.Clear();

        if (mediumLayer == null)
            return;

        Dictionary<Vector2Int, MapTile> tiles =
            mediumLayer.GetFullTileDictionary();

        HashSet<Vector2Int> unvisited =
            new HashSet<Vector2Int>(tiles.Keys);

        while (unvisited.Count > 0)
        {
            Vector2Int startPosition =
                AlchemyCircleScanner.GetFirst(unvisited);

            if (!tiles.TryGetValue(
                    startPosition,
                    out MapTile startTile
                ))
            {
                unvisited.Remove(startPosition);
                continue;
            }

            CircleGroup circleGroup =
                alchemyCircleScanner.FindConnectedGroupAt(
                    startPosition,
                    tiles,
                    startTile
                );

            if (circleGroup == null ||
                circleGroup.Coordinates == null ||
                circleGroup.Coordinates.Count == 0)
            {
                unvisited.Remove(startPosition);
                continue;
            }

            RuntimeAlchemyCircle newCircle =
                CreateCircle(startTile.BlockData);

            foreach (Vector2Int coordinate in circleGroup.Coordinates)
            {
                newCircle.occupiedTiles.Add(coordinate);
                circleByTile[coordinate] = newCircle;

                unvisited.Remove(coordinate);
            }
        }
    }
}
