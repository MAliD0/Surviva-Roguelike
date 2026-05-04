using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class WorldMapNetworkSync : NetworkBehaviour
{
    private WorldMapManager world;

    public void Init(WorldMapManager worldMapManager)
    {
        world = worldMapManager;
    }

    // =========================================================
    // Server request RPCs
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void SetTileRequestServerRpc(Vector2 pos, string mapBlockDataId)
    {
        if (world == null)
            return;

        world.PlaceBlockFromNetwork(pos, mapBlockDataId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageTileRequestServerRpc(Vector2 pos, int amount)
    {
        if (world == null)
            return;

        world.DamageBlockFromNetwork(pos, amount);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyTileRequestServerRpc(Vector2Int tile, Vector2Int subtile)
    {
        if (world == null)
            return;

        world.DestroyBlockFromNetwork(tile, subtile);
    }

    // =========================================================
    // Client sync RPCs
    // =========================================================

    [ClientRpc]
    public void SetTileForClientsClientRpc(
        MapLayerType tileType,
        string mapBlockDataID,
        Vector2 position,
        ClientRpcParams rpcParams = default
    )
    {
        if (world == null)
            return;

        MapLayerLogic layer = world.GetLayer(tileType);
        MapBlockData data = world.BlockLibrary.GetMapBlockData(mapBlockDataID);

        if (layer == null || data == null)
            return;

        layer.PlaceBlock(position, data);
    }

    [ClientRpc]
    public void DestroyTileForClientsClientRpc(
        Vector2Int tile,
        Vector2Int subtile,
        MapLayerType tileType,
        ClientRpcParams rpcParams = default
    )
    {
        if (world == null)
            return;

        MapLayerLogic layer = world.GetLayer(tileType);

        if (layer == null)
            return;

        layer.RemoveTile(tile, subtile);
    }

    [ClientRpc]
    public void UpdateTileHealthClientRpc(
        MapLayerType layerType,
        Vector2Int anchor,
        Vector2Int subtile,
        int hp,
        int maxHp,
        ClientRpcParams rpcParams = default
    )
    {
        if (world == null)
            return;

        MapLayerLogic layer = world.GetLayer(layerType);
        MapTile tile = layer?.GetMapTile(anchor, subtile);
        MapBlockData data = tile?.BlockData;

        if (layer == null || data == null)
            return;

        layer.SetHealth(anchor, subtile, data, hp, fireEvent: true);
    }

    [ClientRpc]
    public void BindObjectByNetIdClientRpc(
        DictEntry[] serializedTiles,
        ulong netId,
        MapLayerType layer,
        ClientRpcParams rpcParams = default
    )
    {
        if (world == null)
            return;

        if (netId == 0)
        {
            world.GetGraphics(layer).UnbindByCells(
                serializedTiles,
                destroyNonNetworked: false
            );

            return;
        }

        Dictionary<Vector2Int, HashSet<Vector2Int>> tiles =
            DictEntry.DictEntryToDictionary(serializedTiles.ToList());

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject networkObject))
        {
            foreach (Vector2Int anchor in tiles.Keys)
            {
                StartCoroutine(
                    RetryBind(
                        anchor,
                        tiles[anchor].ToArray(),
                        netId,
                        layer
                    )
                );
            }

            return;
        }

        foreach (Vector2Int anchor in tiles.Keys)
        {
            world.GetGraphics(layer).BindObject(
                anchor,
                tiles[anchor].ToList(),
                networkObject.gameObject,
                null
            );
        }
    }

    private IEnumerator RetryBind(
        Vector2Int tile,
        Vector2Int[] subtiles,
        ulong netId,
        MapLayerType layer
    )
    {
        for (int i = 0; i < 10; i++)
        {
            yield return null;

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject networkObject))
            {
                world.GetGraphics(layer).BindObject(
                    tile,
                    new List<Vector2Int>(subtiles),
                    networkObject.gameObject,
                    null
                );

                yield break;
            }
        }

        Debug.LogWarning($"[WorldMapNetworkSync] RetryBind net object {netId} not found.");
    }

    [ClientRpc]
    public void SpawnNetlessClientRpc(
        MapLayerType layer,
        string itemId,
        DictEntry[] occupiedTiles,
        Vector3 pos,
        string id,
        ClientRpcParams rpcParams = default
    )
    {
        if (world == null)
            return;

        GameObject prefab = world.BlockLibrary.GetMapBlockData(itemId)?.gameObject;

        if (prefab == null)
        {
            Debug.LogError($"[WorldMapNetworkSync] SpawnNetless prefab {itemId} not found.");
            return;
        }

        Dictionary<Vector2Int, HashSet<Vector2Int>> tiles =
            DictEntry.DictEntryToDictionary(occupiedTiles.ToList());

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);

        foreach (Vector2Int anchor in tiles.Keys)
        {
            world.GetGraphics(layer).BindObject(
                anchor,
                tiles[anchor].ToList(),
                go,
                id
            );
        }
    }

    [ClientRpc]
    public void RemoveNetlessClientRpc(
        string id,
        MapLayerType layer,
        ClientRpcParams rpcParams = default
    )
    {
        if (world == null)
            return;

        world.GetGraphics(layer).UnbindById(id);
    }

    [ClientRpc]
    public void UnbindByCellsClientRpc(
        DictEntry[] cells,
        MapLayerType layer,
        bool destroyNonNetworked,
        ClientRpcParams rpcParams = default
    )
    {
        if (world == null)
            return;

        world.GetGraphics(layer).UnbindByCells(cells, destroyNonNetworked);
    }
}