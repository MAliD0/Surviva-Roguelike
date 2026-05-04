using UnityEngine;

public class MapPlacementValidator
{
    private readonly MapBlockDataLibrary blockLibrary;

    private readonly System.Func<MapLayerType, MapLayerLogic> getLayer;

    public MapPlacementValidator(
        MapBlockDataLibrary blockLibrary,
        System.Func<MapLayerType, MapLayerLogic> getLayer
    )
    {
        this.blockLibrary = blockLibrary;
        this.getLayer = getLayer;
    }

    public PlacementResult Validate(Vector2 worldPosition, string blockId)
    {
        if (string.IsNullOrEmpty(blockId))
        {
            return PlacementResult.Fail(
                PlacementFailReason.MissingBlockData,
                "Block id is null or empty."
            );
        }

        if (blockLibrary == null)
        {
            return PlacementResult.Fail(
                PlacementFailReason.MissingBlockLibrary,
                "MapBlockDataLibrary reference is missing."
            );
        }

        MapBlockData blockData = blockLibrary.GetMapBlockData(blockId);

        return Validate(worldPosition, blockData);
    }

    public PlacementResult Validate(Vector2 worldPosition, MapBlockData blockData)
    {
        if (blockData == null)
        {
            return PlacementResult.Fail(
                PlacementFailReason.MissingBlockData,
                "MapBlockData is null."
            );
        }

        MapLayerLogic targetLayer = getLayer?.Invoke(blockData.mapLayerType);

        if (targetLayer == null)
        {
            return PlacementResult.Fail(
                PlacementFailReason.MissingTargetLayer,
                $"Target layer is missing for layer type: {blockData.mapLayerType}"
            );
        }

        PlacementResult layerRuleResult = ValidateLayerRules(worldPosition, blockData);

        if (!layerRuleResult.Success)
            return layerRuleResult;

        bool canBePlaced = targetLayer.CanBePlaced(worldPosition, blockData);

        if (!canBePlaced)
        {
            return PlacementResult.Fail(
                PlacementFailReason.Occupied,
                $"Target layer rejected placement at world position {worldPosition}."
            );
        }

        return PlacementResult.Ok();
    }

    private PlacementResult ValidateLayerRules(Vector2 worldPosition, MapBlockData blockData)
    {
        switch (blockData.mapLayerType)
        {
            case MapLayerType.foreGround:
                return ValidateForegroundPlacement(worldPosition, blockData);

            case MapLayerType.onBoatGround:
                return ValidateOnBoatGroundPlacement(worldPosition, blockData);

            default:
                return PlacementResult.Ok();
        }
    }

    private PlacementResult ValidateForegroundPlacement(Vector2 worldPosition, MapBlockData blockData)
    {
        MapLayerLogic baseLayer = getLayer?.Invoke(MapLayerType.backGround);

        if (baseLayer == null)
        {
            return PlacementResult.Fail(
                PlacementFailReason.MissingTargetLayer,
                "Base layer is missing. Foreground placement requires base ground."
            );
        }

        bool hasGroundUnderFootprint = baseLayer.IsFootprintFullyOccupied(
            worldPosition,
            blockData.blockSize.x,
            blockData.blockSize.y
        );

        if (!hasGroundUnderFootprint)
        {
            return PlacementResult.Fail(
                PlacementFailReason.MissingBaseGround,
                $"Foreground object requires background/base ground under full footprint at {worldPosition}."
            );
        }

        return PlacementResult.Ok();
    }

    private PlacementResult ValidateOnBoatGroundPlacement(Vector2 worldPosition, MapBlockData blockData)
    {
        MapLayerLogic baseLayer = getLayer?.Invoke(MapLayerType.backGround);

        if (baseLayer == null)
        {
            return PlacementResult.Fail(
                PlacementFailReason.MissingTargetLayer,
                "Base layer is missing. Cannot validate onBoatGround rule."
            );
        }

        bool baseLayerOccupied = baseLayer.IsFootprintOccupied(
            worldPosition,
            blockData.blockSize.x,
            blockData.blockSize.y
        );

        if (baseLayerOccupied)
        {
            return PlacementResult.Fail(
                PlacementFailReason.InvalidLayerRule,
                $"onBoatGround object cannot be placed on occupied background/base layer at {worldPosition}."
            );
        }

        return PlacementResult.Ok();
    }
}    
