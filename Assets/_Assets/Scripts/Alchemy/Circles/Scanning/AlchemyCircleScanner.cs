using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AlchemyCircleScanner
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public List<CircleGroup> FindAllConnectedGroups(
        IEnumerable<Vector2Int> positions
    )
    {
        HashSet<Vector2Int> unvisited = new(positions);
        List<CircleGroup> groups = new();

        int groupIndex = 0;

        while (unvisited.Count > 0)
        {
            Vector2Int startPosition = GetFirst(unvisited);

            CircleGroup group = FindGroup(
                startPosition,
                unvisited,
                groupIndex
            );

            groups.Add(group);
            groupIndex++;
        }

        return groups;
    }

    public CircleGroup FindConnectedGroupAt(Vector2Int startPosition, Dictionary<Vector2Int, MapTile> tiles, MapTile mapTile)
    {
        CircleGroup group = new(mapTile);

        if (mapTile == null)
            return group;

        Queue<Vector2Int> open = new();
        HashSet<Vector2Int> visited = new();

        open.Enqueue(startPosition);
        visited.Add(startPosition);

        while (open.Count > 0)
        {
            Vector2Int current = open.Dequeue();
            group.Coordinates.Add(current);

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbour = current + direction;

                if (visited.Contains(neighbour))
                    continue;

                if (!tiles.TryGetValue(neighbour, out MapTile neighbourTile))
                    continue;

                if (neighbourTile.BlockData != mapTile.BlockData)
                    continue;

                visited.Add(neighbour);
                open.Enqueue(neighbour);
            }
        }

        return group;
    }

    public HashSet<Vector2Int> FindConnectedPositions(
        Vector2Int startPosition,
        HashSet<Vector2Int> allowedPositions
    )
    {
        HashSet<Vector2Int> result = new();
        Queue<Vector2Int> open = new();

        open.Enqueue(startPosition);
        result.Add(startPosition);

        while (open.Count > 0)
        {
            Vector2Int current = open.Dequeue();

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbour = current + direction;

                if (!allowedPositions.Contains(neighbour))
                    continue;

                if (!result.Add(neighbour))
                    continue;

                open.Enqueue(neighbour);
            }
        }

        return result;
    }

    private CircleGroup FindGroup(
        Vector2Int startPosition,
        HashSet<Vector2Int> unvisited,
        int groupIndex
    )
    {
        CircleGroup group = new(groupIndex);
        Queue<Vector2Int> open = new();

        open.Enqueue(startPosition);
        unvisited.Remove(startPosition);

        while (open.Count > 0)
        {
            Vector2Int current = open.Dequeue();
            group.Coordinates.Add(current);

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbour = current + direction;

                if (!unvisited.Remove(neighbour))
                    continue;

                open.Enqueue(neighbour);
            }
        }

        return group;
    }

    public static Vector2Int GetFirst(
        HashSet<Vector2Int> positions
    )
    {
        foreach (Vector2Int position in positions)
            return position;

        throw new System.InvalidOperationException(
            "Cannot get a position from an empty collection."
        );
    }
}

public sealed class CircleGroup
{
    public int LocalIndex { get; }
    public List<Vector2Int> Coordinates { get; } = new();
    public MapTile groupTile;

    public CircleGroup(int localIndex)
    {
        LocalIndex = localIndex;
    }

    public CircleGroup(MapTile mapTile)
    {
        this.groupTile = mapTile;
    }
}