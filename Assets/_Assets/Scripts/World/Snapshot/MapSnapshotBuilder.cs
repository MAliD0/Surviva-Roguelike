using System.Collections.Generic;
using UnityEngine;

public class MapSnapshotBuilder
{
    private readonly WorldMapManager world;

    public MapSnapshotBuilder(WorldMapManager world)
    {
        this.world = world;
    }

    public List<TileSnapshotEntry> BuildTileSnapshot()
    {
        var result = new List<TileSnapshotEntry>();

        AddLayerSnapshot(MapLayerType.backGround, world.GetLayer(MapLayerType.backGround), result);
        AddLayerSnapshot(MapLayerType.foreGround, world.GetLayer(MapLayerType.foreGround), result);
        AddLayerSnapshot(MapLayerType.boatGround, world.GetLayer(MapLayerType.boatGround), result);
        AddLayerSnapshot(MapLayerType.onBoatGround, world.GetLayer(MapLayerType.onBoatGround), result);

        return result;
    }

    private void AddLayerSnapshot(
        MapLayerType layerType,
        MapLayerLogic layer,
        List<TileSnapshotEntry> result
    )
    {
        if (layer == null)
            return;

        var seenAnchors = new HashSet<string>();

        foreach (Vector2Int tileCell in layer.LayerTiles.Keys)
        {
            foreach (MapTile mapTile in layer.LayerTiles[tileCell].Values)
            {
                if (mapTile == null || mapTile.BlockData == null)
                    continue;

                MapBlockData blockData = mapTile.BlockData;

                Vector2Int tileAnchor = mapTile.TileAnchor;
                Vector2Int subtileAnchor = mapTile.SubtileAnchor;

                string uniqueKey = $"{layerType}|{tileAnchor.x},{tileAnchor.y}|{subtileAnchor.x},{subtileAnchor.y}";

                if (!seenAnchors.Add(uniqueKey))
                    continue;

                int hp = blockData.breakable
                    ? layer.GetHealth(tileAnchor, subtileAnchor)
                    : 0;

                result.Add(new TileSnapshotEntry
                {
                    layer = layerType,
                    itemId = blockData.GetItemID(),
                    anchor = tileAnchor,
                    localAnchor = subtileAnchor,
                    hp = hp
                });
            }
        }
    }
}