using System.Collections.Generic;
using UnityEngine;

public readonly struct MapObjectSpawnResult
{
    public bool Success { get; }
    public string Message { get; }

    public bool IsNetworkObject { get; }
    public ulong NetworkObjectId { get; }

    public string NetlessId { get; }
    public string ItemId { get; }

    public MapLayerType LayerType { get; }
    public Vector3 WorldPosition { get; }
    public Dictionary<Vector2Int, HashSet<Vector2Int>> Cells { get; }

    private MapObjectSpawnResult(
        bool success,
        string message,
        bool isNetworkObject,
        ulong networkObjectId,
        string netlessId,
        string itemId,
        MapLayerType layerType,
        Vector3 worldPosition,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells
    )
    {
        Success = success;
        Message = message;
        IsNetworkObject = isNetworkObject;
        NetworkObjectId = networkObjectId;
        NetlessId = netlessId;
        ItemId = itemId;
        LayerType = layerType;
        WorldPosition = worldPosition;
        Cells = cells;
    }

    public static MapObjectSpawnResult Fail(string message)
    {
        return new MapObjectSpawnResult(
            false,
            message,
            false,
            0,
            null,
            null,
            default,
            default,
            null
        );
    }

    public static MapObjectSpawnResult NetworkObject(
        MapLayerType layerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        ulong networkObjectId,
        string itemId,
        Vector3 worldPosition
    )
    {
        return new MapObjectSpawnResult(
            true,
            "Network object spawned.",
            true,
            networkObjectId,
            null,
            itemId,
            layerType,
            worldPosition,
            cells
        );
    }

    public static MapObjectSpawnResult NetlessObject(
        MapLayerType layerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        string netlessId,
        string itemId,
        Vector3 worldPosition
    )
    {
        return new MapObjectSpawnResult(
            true,
            "Netless object prepared.",
            false,
            0,
            netlessId,
            itemId,
            layerType,
            worldPosition,
            cells
        );
    }
}