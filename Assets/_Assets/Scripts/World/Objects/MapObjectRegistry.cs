using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MapObjectRegistry
{
    [Serializable]
    public struct NetlessEntry
    {
        public MapLayerType layer;
        public Vector2Int anchor;
        public Vector2Int localAnchor;
        public List<DictEntry> occupiedTiles;
        public string itemId;
        public Vector3 pos;
    }

    private readonly SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, ulong>>> anchorToNetId =
        new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, ulong>>>();

    private readonly SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, string>>> anchorToNetlessId =
        new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, string>>>();

    private readonly SerializedDictionary<string, NetlessEntry> netlessRegistry =
        new SerializedDictionary<string, NetlessEntry>();

    private readonly Func<MapLayerType, MapLayerLogic> getLayer;

    public MapObjectRegistry(Func<MapLayerType, MapLayerLogic> getLayer)
    {
        this.getLayer = getLayer;
    }

    public Dictionary<string, NetlessEntry> GetNetlessRegistry()
    {
        return netlessRegistry;
    }

    public void Clear()
    {
        anchorToNetId.Clear();
        anchorToNetlessId.Clear();
        netlessRegistry.Clear();
    }

    public void RegisterNetworkObject(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        ulong networkObjectId
    )
    {
        if (!TryGetPlacedAnchor(layer, cells, out Vector2Int tileAnchor, out Vector2Int subtileAnchor))
            return;

        if (!anchorToNetId.TryGetValue(layer, out var layerDict))
        {
            layerDict = new SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, ulong>>();
            anchorToNetId[layer] = layerDict;
        }

        if (!layerDict.TryGetValue(tileAnchor, out var subtileDict))
        {
            subtileDict = new SerializedDictionary<Vector2Int, ulong>();
            layerDict[tileAnchor] = subtileDict;
        }

        subtileDict[subtileAnchor] = networkObjectId;
    }

    public void RegisterNetlessObject(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        string id,
        string itemId,
        Vector3 worldPos
    )
    {
        if (!TryGetPlacedAnchor(layer, cells, out Vector2Int tileAnchor, out Vector2Int subtileAnchor))
            return;

        if (!anchorToNetlessId.TryGetValue(layer, out var layerDict))
        {
            layerDict = new SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, string>>();
            anchorToNetlessId[layer] = layerDict;
        }

        if (!layerDict.TryGetValue(tileAnchor, out var subtileDict))
        {
            subtileDict = new SerializedDictionary<Vector2Int, string>();
            layerDict[tileAnchor] = subtileDict;
        }

        subtileDict[subtileAnchor] = id;

        netlessRegistry[id] = new NetlessEntry
        {
            layer = layer,
            anchor = tileAnchor,
            localAnchor = subtileAnchor,
            occupiedTiles = DictEntry.SerializeDictionary(cells),
            itemId = itemId,
            pos = worldPos
        };
    }

    public bool TryFindNetworkObject(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        out Vector2Int registeredTile,
        out Vector2Int registeredSubtile,
        out ulong networkId
    )
    {
        registeredTile = default;
        registeredSubtile = default;
        networkId = 0;

        if (!anchorToNetId.TryGetValue(layer, out var layerDict))
            return false;

        foreach (var tilePair in cells)
        {
            if (!layerDict.TryGetValue(tilePair.Key, out var subtileDict))
                continue;

            foreach (Vector2Int subtile in tilePair.Value)
            {
                if (subtileDict.TryGetValue(subtile, out networkId))
                {
                    registeredTile = tilePair.Key;
                    registeredSubtile = subtile;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryFindNetlessObject(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        out Vector2Int registeredTile,
        out Vector2Int registeredSubtile,
        out string id
    )
    {
        registeredTile = default;
        registeredSubtile = default;
        id = null;

        if (!anchorToNetlessId.TryGetValue(layer, out var layerDict))
            return false;

        foreach (var tilePair in cells)
        {
            if (!layerDict.TryGetValue(tilePair.Key, out var subtileDict))
                continue;

            foreach (Vector2Int subtile in tilePair.Value)
            {
                if (subtileDict.TryGetValue(subtile, out id))
                {
                    registeredTile = tilePair.Key;
                    registeredSubtile = subtile;
                    return true;
                }
            }
        }

        return false;
    }

    public void RemoveNetworkObject(
        MapLayerType layer,
        Vector2Int tile,
        Vector2Int subtile
    )
    {
        if (!anchorToNetId.TryGetValue(layer, out var layerDict))
            return;

        if (!layerDict.TryGetValue(tile, out var subtileDict))
            return;

        subtileDict.Remove(subtile);

        if (subtileDict.Count == 0)
            layerDict.Remove(tile);

        if (layerDict.Count == 0)
            anchorToNetId.Remove(layer);
    }

    public void RemoveNetlessObject(
        MapLayerType layer,
        Vector2Int tile,
        Vector2Int subtile,
        string id
    )
    {
        if (anchorToNetlessId.TryGetValue(layer, out var layerDict))
        {
            if (layerDict.TryGetValue(tile, out var subtileDict))
            {
                subtileDict.Remove(subtile);

                if (subtileDict.Count == 0)
                    layerDict.Remove(tile);
            }

            if (layerDict.Count == 0)
                anchorToNetlessId.Remove(layer);
        }

        if (!string.IsNullOrEmpty(id))
            netlessRegistry.Remove(id);
    }

    private bool TryGetPlacedAnchor(
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

    public IEnumerable<NetworkObjectRegistryEntry> GetNetworkObjectEntries()
    {
        foreach (var layerPair in anchorToNetId)
        {
            MapLayerType layer = layerPair.Key;

            foreach (var tilePair in layerPair.Value)
            {
                Vector2Int tileAnchor = tilePair.Key;

                foreach (var subtilePair in tilePair.Value)
                {
                    Vector2Int subtileAnchor = subtilePair.Key;
                    ulong networkObjectId = subtilePair.Value;

                    yield return new NetworkObjectRegistryEntry(
                        layer,
                        tileAnchor,
                        subtileAnchor,
                        networkObjectId
                    );
                }
            }
        }
    }
}