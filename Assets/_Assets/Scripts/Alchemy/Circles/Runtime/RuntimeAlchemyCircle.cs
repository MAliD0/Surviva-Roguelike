using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

[Serializable]
public class RuntimeAlchemyCircle
{
    public MapBlockData mapBlockData;

    public HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>();

    public int TileCount => occupiedTiles.Count;
    public bool IsEmpty => occupiedTiles.Count == 0;

    [SerializeField] private SerializedDictionary<Vector2Int, AlchemyArrayComponentInstance> alchemicalCircleElements = new();

    public Action<Vector2Int> onMediumTileAdded;
    public Action<Vector2Int> onMediumTileRemoved;

    public Action<Vector2Int, MapBlockData> onCircleElementsAdded;
    public Action<Vector2Int, MapBlockData> onCircleElementsRemoved;

    public RuntimeAlchemyCircle()
    {
    }

    public RuntimeAlchemyCircle(MapBlockData blockData, IEnumerable<Vector2Int> coordinates)
    {
        mapBlockData = blockData;
        AddTiles(coordinates);
    }

    private void OnObjectCreatedHandler(MapLayerType type, Vector2Int int1, Vector2Int int2, GameObject @object)
    {
        Debug.Log("Enter:" + @object.name);
        if(type != MapLayerType.circleElementLayer) return;


        if(@object.TryGetComponent(out AlchemyArrayComponentInstance component))
        {
            alchemicalCircleElements.Add(int1, component);
        }
    }

    public bool Contains(Vector2Int position)
    {
        return occupiedTiles.Contains(position);
    }

    public bool AddTile(Vector2Int position)
    {
        if (!occupiedTiles.Add(position))
            return false;

        onMediumTileAdded?.Invoke(position);
        return true;
    }

    public bool RemoveTile(Vector2Int position)
    {
        if (!occupiedTiles.Remove(position))
            return false;

        alchemicalCircleElements.Remove(position);
        onMediumTileRemoved?.Invoke(position);

        return true;
    }

    public void AddTiles(IEnumerable<Vector2Int> positions)
    {
        if (positions == null)
            return;

        foreach (Vector2Int position in positions)
            AddTile(position);
    }

    public void RemoveTiles(IEnumerable<Vector2Int> positions)
    {
        if (positions == null)
            return;

        foreach (Vector2Int position in positions)
            RemoveTile(position);
    }

    public bool CanMergeWith(RuntimeAlchemyCircle other)
    {
        return other != null && mapBlockData == other.mapBlockData;
    }

    public bool MergeWith(RuntimeAlchemyCircle other)
    {
        if (!CanMergeWith(other))
            return false;

        AddTiles(other.occupiedTiles);

        foreach (KeyValuePair<Vector2Int, AlchemyArrayComponentInstance> element in other.alchemicalCircleElements)
            alchemicalCircleElements[element.Key] = element.Value;

        return true;
    }

    public bool TryAddElement(Vector2Int position, AlchemyArrayComponentInstance element)
    {
        if (!occupiedTiles.Contains(position) || element == null)
            return false;

        alchemicalCircleElements[position] = element;
        return true;
    }

    public bool TryGetElement(Vector2Int position, out AlchemyArrayComponentInstance element)
    {
        return alchemicalCircleElements.TryGetValue(position, out element);
    }

    public bool RemoveElement(Vector2Int position)
    {
        return alchemicalCircleElements.Remove(position);
    }

    public Vector2Int GetAnyTile()
    {
        foreach (Vector2Int position in occupiedTiles)
            return position;

        throw new InvalidOperationException("The runtime alchemy circle contains no tiles.");
    }

    public bool IsAdjacentTo(Vector2Int position)
    {
        foreach (Vector2Int direction in Directions)
        {
            if (occupiedTiles.Contains(position + direction))
                return true;
        }

        return false;
    }

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };
}
