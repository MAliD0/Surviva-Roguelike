using UnityEngine;

public readonly struct MapObjectRemovalResult
{
    public bool Success { get; }
    public string Message { get; }

    public bool NeedsNetworkUnbindByCells { get; }
    public bool NeedsNetlessRemoveRpc { get; }

    public string NetlessId { get; }
    public MapLayerType LayerType { get; }

    private MapObjectRemovalResult(
        bool success,
        string message,
        bool needsNetworkUnbindByCells,
        bool needsNetlessRemoveRpc,
        string netlessId,
        MapLayerType layerType
    )
    {
        Success = success;
        Message = message;
        NeedsNetworkUnbindByCells = needsNetworkUnbindByCells;
        NeedsNetlessRemoveRpc = needsNetlessRemoveRpc;
        NetlessId = netlessId;
        LayerType = layerType;
    }

    public static MapObjectRemovalResult NoObject()
    {
        return new MapObjectRemovalResult(
            false,
            "No registered object found.",
            false,
            false,
            null,
            default
        );
    }

    public static MapObjectRemovalResult RemovedOffline()
    {
        return new MapObjectRemovalResult(
            true,
            "Object removed offline.",
            false,
            false,
            null,
            default
        );
    }

    public static MapObjectRemovalResult RemovedNetworkObject(MapLayerType layerType)
    {
        return new MapObjectRemovalResult(
            true,
            "Network object removed.",
            true,
            false,
            null,
            layerType
        );
    }

    public static MapObjectRemovalResult RemovedNetlessObject(
        MapLayerType layerType,
        string netlessId
    )
    {
        return new MapObjectRemovalResult(
            true,
            "Netless object removed.",
            false,
            true,
            netlessId,
            layerType
        );
    }
}