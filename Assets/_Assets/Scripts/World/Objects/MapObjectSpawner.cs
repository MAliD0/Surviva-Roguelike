using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapObjectSpawner
{
    private readonly MapBlockDataLibrary blockLibrary;
    private readonly MapObjectRegistry registry;
    private readonly Func<MapLayerType, MapLayerLogic> getLayer;
    private readonly Func<MapLayerType, MapLayerGraphics> getGraphics;
    private readonly Func<string> newId;

    public MapObjectSpawner(
        MapBlockDataLibrary blockLibrary,
        MapObjectRegistry registry,
        Func<MapLayerType, MapLayerLogic> getLayer,
        Func<MapLayerType, MapLayerGraphics> getGraphics,
        Func<string> newId
    )
    {
        this.blockLibrary = blockLibrary;
        this.registry = registry;
        this.getLayer = getLayer;
        this.getGraphics = getGraphics;
        this.newId = newId;
    }

    public void SpawnPlacedObjectOffline(
        MapLayerType layerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData blockData
    )
    {
        if (blockData == null)
            return;

        if (!TryGetPlacedAnchor(layerType, cells, out Vector2Int tileAnchor, out Vector2Int subtileAnchor))
            return;

        MapLayerLogic layer = getLayer?.Invoke(layerType);

        if (layer == null)
            return;

        Vector2 worldPosition = layer.SubtileToWorldPosition(tileAnchor, subtileAnchor);

        GameObject prefab = blockLibrary
            .GetMapBlockData(blockData.GetItemID())
            ?.gameObject;

        if (prefab == null)
        {
            Debug.LogError($"[MapObjectSpawner] Offline prefab id={blockData.GetItemID()} not found.");
            return;
        }

        GameObject go = UnityEngine.Object.Instantiate(
            prefab,
            worldPosition,
            Quaternion.identity
        );

        string id = "offline_" + newId.Invoke();

        registry.RegisterNetlessObject(
            layerType,
            cells,
            id,
            blockData.GetItemID(),
            worldPosition
        );

        MapLayerGraphics graphics = getGraphics?.Invoke(layerType);

        if (graphics == null)
            return;

        foreach (Vector2Int anchor in cells.Keys)
        {
            graphics.BindObject(
                anchor,
                cells[anchor].ToList(),
                go,
                id
            );
        }
    }

    public bool TryGetPlacedAnchor(
        MapLayerType layerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        out Vector2Int tileAnchor,
        out Vector2Int subtileAnchor
    )
    {
        tileAnchor = default;
        subtileAnchor = default;

        MapLayerLogic layer = getLayer?.Invoke(layerType);

        if (layer == null)
            return false;

        foreach (var tilePair in cells)
        {
            foreach (Vector2Int localSubtile in tilePair.Value)
            {
                MapTile mapTile = layer.GetMapTile(tilePair.Key, localSubtile);

                if (mapTile == null)
                    continue;

                tileAnchor = mapTile.TileAnchor;
                subtileAnchor = mapTile.SubtileAnchor;
                return true;
            }
        }

        return false;
    }
}