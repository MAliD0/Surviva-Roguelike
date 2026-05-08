using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

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
            .GetById(blockData.GetItemID())
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

    public MapObjectSpawnResult SpawnPlacedObjectOnline(
        MapLayerType layerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData blockData
    )
    {
        if (blockData == null)
            return MapObjectSpawnResult.Fail("BlockData is null.");

        if (!TryGetPlacedAnchor(layerType, cells, out Vector2Int tileAnchor, out Vector2Int subtileAnchor))
            return MapObjectSpawnResult.Fail("Could not find placed anchor.");

        MapLayerLogic layer = getLayer?.Invoke(layerType);

        if (layer == null)
            return MapObjectSpawnResult.Fail($"Layer is missing: {layerType}");

        Vector2 worldPosition = layer.SubtileToWorldPosition(tileAnchor, subtileAnchor);

        GameObject prefab = blockLibrary
            .GetById(blockData.GetItemID())
            ?.gameObject;

        if (prefab == null)
            return MapObjectSpawnResult.Fail($"Prefab id={blockData.GetItemID()} not found.");

        string itemId = blockData.GetItemID();

        if (prefab.TryGetComponent<NetworkObject>(out _))
        {
            GameObject go = UnityEngine.Object.Instantiate(
                prefab,
                worldPosition,
                Quaternion.identity
            );

            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            networkObject.Spawn();

            registry.RegisterNetworkObject(
                layerType,
                cells,
                networkObject.NetworkObjectId
            );

            return MapObjectSpawnResult.NetworkObject(
                layerType,
                cells,
                networkObject.NetworkObjectId,
                itemId,
                worldPosition
            );
        }

        string id = newId.Invoke();

        registry.RegisterNetlessObject(
            layerType,
            cells,
            id,
            itemId,
            worldPosition
        );

        return MapObjectSpawnResult.NetlessObject(
            layerType,
            cells,
            id,
            itemId,
            worldPosition
        );
    }

    public MapObjectRemovalResult RemovePlacedObject(
        MapLayerType layerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        bool isOnlineMode
    )
    {
        DictEntry[] serializedCells = DictEntry.SerializeDictionary(cells).ToArray();

        if (registry.TryFindNetworkObject(
                layerType,
                cells,
                out Vector2Int registeredTile,
                out Vector2Int registeredSubtile,
                out ulong networkId
            ))
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out NetworkObject networkObject))
            {
                networkObject.Despawn(true);
            }

            registry.RemoveNetworkObject(
                layerType,
                registeredTile,
                registeredSubtile
            );

            if (isOnlineMode)
                return MapObjectRemovalResult.RemovedNetworkObject(layerType);

            MapLayerGraphics graphics = getGraphics?.Invoke(layerType);

            if (graphics != null)
                graphics.UnbindByCells(serializedCells, destroyNonNetworked: true);

            return MapObjectRemovalResult.RemovedOffline();
        }

        if (registry.TryFindNetlessObject(
                layerType,
                cells,
                out Vector2Int netlessTile,
                out Vector2Int netlessSubtile,
                out string id
            ))
        {
            registry.RemoveNetlessObject(
                layerType,
                netlessTile,
                netlessSubtile,
                id
            );

            if (isOnlineMode)
                return MapObjectRemovalResult.RemovedNetlessObject(layerType, id);

            MapLayerGraphics graphics = getGraphics?.Invoke(layerType);

            if (graphics != null)
                graphics.UnbindById(id);

            return MapObjectRemovalResult.RemovedOffline();
        }

        MapLayerGraphics fallbackGraphics = getGraphics?.Invoke(layerType);

        if (fallbackGraphics != null && !isOnlineMode)
        {
            fallbackGraphics.UnbindByCells(serializedCells, destroyNonNetworked: true);
            return MapObjectRemovalResult.RemovedOffline();
        }

        if (isOnlineMode)
            return MapObjectRemovalResult.RemovedNetworkObject(layerType);

        return MapObjectRemovalResult.NoObject();
    }
}