using System;
using UnityEngine;

public class WorldMapService
{
    private readonly MapBlockDataLibrary blockLibrary;
    private readonly MapPlacementValidator placementValidator;
    private readonly Func<MapLayerType, MapLayerLogic> getLayer;

    public WorldMapService(
        MapBlockDataLibrary blockLibrary,
        MapPlacementValidator placementValidator,
        Func<MapLayerType, MapLayerLogic> getLayer
    )
    {
        this.blockLibrary = blockLibrary;
        this.placementValidator = placementValidator;
        this.getLayer = getLayer;
    }

    public PlacementResult GetPlacementResult(Vector2 worldPosition, string blockId)
    {
        if (placementValidator == null)
        {
            return PlacementResult.Fail(
                PlacementFailReason.MissingTargetLayer,
                "Placement validator is missing."
            );
        }

        return placementValidator.Validate(worldPosition, blockId);
    }

    public WorldMapOperationResult PlaceBlock(Vector2 worldPosition, string blockId)
    {
        PlacementResult placementResult = GetPlacementResult(worldPosition, blockId);

        if (!placementResult.Success)
            return WorldMapOperationResult.Fail(placementResult.ToString());

        if (blockLibrary == null)
            return WorldMapOperationResult.Fail("Block library is missing.");

        MapBlockData blockData = blockLibrary.GetById(blockId);

        if (blockData == null)
            return WorldMapOperationResult.Fail($"BlockData not found for id: {blockId}");

        MapLayerLogic targetLayer = getLayer?.Invoke(blockData.mapLayerType);

        if (targetLayer == null)
            return WorldMapOperationResult.Fail($"Target layer is missing: {blockData.mapLayerType}");

        bool placed = targetLayer.PlaceBlock(worldPosition, blockData);

        if (!placed)
            return WorldMapOperationResult.Fail("Target layer rejected placement.");

        return WorldMapOperationResult.Placed(
            blockData.mapLayerType,
            targetLayer,
            blockData,
            worldPosition
        );
    }

    public WorldMapOperationResult DamageBlock(Vector2 worldPosition, int amount)
    {
        MapLayerType layerType = DetectLayerByCell(worldPosition);
        MapLayerLogic layer = getLayer?.Invoke(layerType);

        if (layer == null)
            return WorldMapOperationResult.Fail("Target layer is missing.");

        MapTile mapTile = layer.GetMapTile(worldPosition);

        if (mapTile == null)
            return WorldMapOperationResult.Fail("No map tile found at world position.");

        MapBlockData blockData = mapTile.BlockData;

        if (blockData == null)
            return WorldMapOperationResult.Fail("Block data is missing.");

        if (!blockData.breakable)
            return WorldMapOperationResult.Fail("Block is not breakable.");

        Vector2Int tileAnchor = mapTile.TileAnchor;
        Vector2Int subtileAnchor = mapTile.SubtileAnchor;

        bool broken = layer.Damage(
            tileAnchor,
            subtileAnchor,
            blockData,
            Mathf.Max(1, amount)
        );

        int currentHealth = layer.GetHealth(tileAnchor, subtileAnchor);

        return WorldMapOperationResult.Damaged(
            layerType,
            layer,
            blockData,
            tileAnchor,
            subtileAnchor,
            worldPosition,
            currentHealth,
            blockData.maxHealth,
            broken
        );
    }

    public WorldMapOperationResult DestroyBlock(Vector2Int clickedTileCell, Vector2Int clickedLocalSubtile)
    {
        bool found = TryFindLayerWithSubtile(
            clickedTileCell,
            clickedLocalSubtile,
            out MapLayerType layerType,
            out MapLayerLogic layer
        );

        if (!found || layer == null)
            return WorldMapOperationResult.Fail("No block found at clicked subtile.");

        MapTile mapTile = layer.GetMapTile(clickedTileCell, clickedLocalSubtile);

        if (mapTile == null)
            return WorldMapOperationResult.Fail("MapTile is null.");

        MapBlockData blockData = mapTile.BlockData;

        Vector2Int tileAnchor = mapTile.TileAnchor;
        Vector2Int subtileAnchor = mapTile.SubtileAnchor;

        Vector2 worldPosition = layer.SubtileToWorldPosition(tileAnchor, subtileAnchor);

        layer.RemoveTile(tileAnchor, subtileAnchor);

        return WorldMapOperationResult.Destroyed(
            layerType,
            layer,
            blockData,
            tileAnchor,
            subtileAnchor,
            worldPosition
        );
    }

    public MapLayerType DetectLayerByCell(Vector2 worldPosition)
    {
        MapLayerLogic foreLayer = getLayer?.Invoke(MapLayerType.foreGround);
        MapLayerLogic baseLayer = getLayer?.Invoke(MapLayerType.backGround);
        MapLayerLogic mediumLayer = getLayer?.Invoke(MapLayerType.mediumLayer);
        MapLayerLogic circleElementLayer = getLayer?.Invoke(MapLayerType.circleElementLayer);

        if (circleElementLayer != null && circleElementLayer.IsTilePresented(worldPosition))
            return MapLayerType.circleElementLayer;

        if (mediumLayer != null && mediumLayer.IsTilePresented(worldPosition))
            return MapLayerType.mediumLayer;

        if (foreLayer != null && foreLayer.IsTilePresented(worldPosition))
            return MapLayerType.foreGround;

        if (baseLayer != null && baseLayer.IsTilePresented(worldPosition))
            return MapLayerType.backGround;

        return MapLayerType.backGround;
    }

    private bool TryFindLayerWithSubtile(
        Vector2Int tileCell,
        Vector2Int localSubtile,
        out MapLayerType layerType,
        out MapLayerLogic layer
    )
    {

        if (TryLayerContainsSubtile(MapLayerType.circleElementLayer, tileCell, localSubtile, out layer))
        {
            layerType = MapLayerType.circleElementLayer;
            return true;
        }

        if (TryLayerContainsSubtile(MapLayerType.mediumLayer, tileCell, localSubtile, out layer))
        {
            layerType = MapLayerType.mediumLayer;
            return true;
        }

        if (TryLayerContainsSubtile(MapLayerType.foreGround, tileCell, localSubtile, out layer))
        {
            layerType = MapLayerType.foreGround;
            return true;
        }

        if (TryLayerContainsSubtile(MapLayerType.backGround, tileCell, localSubtile, out layer))
        {
            layerType = MapLayerType.backGround;
            return true;
        }

        layerType = MapLayerType.backGround;
        layer = null;
        return false;
    }

    private bool TryLayerContainsSubtile(
        MapLayerType layerType,
        Vector2Int tileCell,
        Vector2Int localSubtile,
        out MapLayerLogic layer
    )
    {
        layer = getLayer?.Invoke(layerType);

        if (layer == null)
            return false;

        return layer.IsSubTilePresented(tileCell, localSubtile);
    }
}