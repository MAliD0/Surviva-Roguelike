using UnityEngine;

public readonly struct NetworkObjectRegistryEntry
{
    public MapLayerType Layer { get; }
    public Vector2Int TileAnchor { get; }
    public Vector2Int SubtileAnchor { get; }
    public ulong NetworkObjectId { get; }

    public NetworkObjectRegistryEntry(
        MapLayerType layer,
        Vector2Int tileAnchor,
        Vector2Int subtileAnchor,
        ulong networkObjectId
    )
    {
        Layer = layer;
        TileAnchor = tileAnchor;
        SubtileAnchor = subtileAnchor;
        NetworkObjectId = networkObjectId;
    }
}