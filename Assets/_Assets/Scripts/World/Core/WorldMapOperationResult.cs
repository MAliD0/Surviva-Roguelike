using UnityEngine;

public readonly struct WorldMapOperationResult
{
    public bool Success { get; }
    public string Message { get; }

    public MapLayerType LayerType { get; }
    public MapLayerLogic Layer { get; }
    public MapBlockData BlockData { get; }

    public Vector2Int TileAnchor { get; }
    public Vector2Int SubtileAnchor { get; }

    public Vector2 WorldPosition { get; }

    public int CurrentHealth { get; }
    public int MaxHealth { get; }

    public bool Broken { get; }

    private WorldMapOperationResult(
        bool success,
        string message,
        MapLayerType layerType,
        MapLayerLogic layer,
        MapBlockData blockData,
        Vector2Int tileAnchor,
        Vector2Int subtileAnchor,
        Vector2 worldPosition,
        int currentHealth,
        int maxHealth,
        bool broken
    )
    {
        Success = success;
        Message = message;

        LayerType = layerType;
        Layer = layer;
        BlockData = blockData;

        TileAnchor = tileAnchor;
        SubtileAnchor = subtileAnchor;

        WorldPosition = worldPosition;

        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;

        Broken = broken;
    }

    public static WorldMapOperationResult Fail(string message)
    {
        return new WorldMapOperationResult(
            false,
            message,
            default,
            null,
            null,
            default,
            default,
            default,
            0,
            0,
            false
        );
    }

    public static WorldMapOperationResult Placed(
        MapLayerType layerType,
        MapLayerLogic layer,
        MapBlockData blockData,
        Vector2 worldPosition
    )
    {
        return new WorldMapOperationResult(
            true,
            "Block placed.",
            layerType,
            layer,
            blockData,
            default,
            default,
            worldPosition,
            0,
            0,
            false
        );
    }

    public static WorldMapOperationResult Damaged(
        MapLayerType layerType,
        MapLayerLogic layer,
        MapBlockData blockData,
        Vector2Int tileAnchor,
        Vector2Int subtileAnchor,
        Vector2 worldPosition,
        int currentHealth,
        int maxHealth,
        bool broken
    )
    {
        return new WorldMapOperationResult(
            true,
            broken ? "Block broken." : "Block damaged.",
            layerType,
            layer,
            blockData,
            tileAnchor,
            subtileAnchor,
            worldPosition,
            currentHealth,
            maxHealth,
            broken
        );
    }

    public static WorldMapOperationResult Destroyed(
        MapLayerType layerType,
        MapLayerLogic layer,
        MapBlockData blockData,
        Vector2Int tileAnchor,
        Vector2Int subtileAnchor,
        Vector2 worldPosition
    )
    {
        return new WorldMapOperationResult(
            true,
            "Block destroyed.",
            layerType,
            layer,
            blockData,
            tileAnchor,
            subtileAnchor,
            worldPosition,
            0,
            0,
            true
        );
    }
}