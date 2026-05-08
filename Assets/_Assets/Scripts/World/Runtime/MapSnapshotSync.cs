using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// Late-join синхронизация: тайлы чанками, нетворк-лесс GO, биндинг сетевых GO.
/// Без вращений/доп. фич — только базовая догрузка нового клиента.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class MapSnapshotSync : NetworkBehaviour
{
    [Header("Refs (автопоиск при спавне)")]
    [SerializeField] private WorldMapManager world;
    [SerializeField] private MapBlockDataLibrary blockLibrary;
    private MapSnapshotBuilder snapshotBuilder;

    [Header("Chunk sizes")]
    [SerializeField] private int tileChunkSize = 80;
    [SerializeField] private int netlessChunkSize = 30;

    [Header("Options")]
    [Tooltip("Присылать биндинг сетевых GO (если нет биндера на префабах)")]
    [SerializeField] private bool sendNetworkedBindSnapshot = true;

    // ----- lifecycle -----
    public override void OnNetworkSpawn()
    {
        if (!world)
            world = WorldMapManager.Instance;

        if (!blockLibrary)
            blockLibrary = world ? world.BlockLibrary : null;

        if (world != null)
            snapshotBuilder = new MapSnapshotBuilder(world);

        if (IsServer)
            NetworkManager.OnClientConnectedCallback += OnClientConnectedServer;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedServer;
    }

    // ----- server entry -----
    private void OnClientConnectedServer(ulong clientId)
    {
        StartCoroutine(SendFullSnapshotRoutine(clientId));
    }

    private IEnumerator SendFullSnapshotRoutine(ulong clientId)
    {
        if (world == null || blockLibrary == null)
        {
            Debug.LogWarning("[MapSnapshotSync] Missing refs");
            yield break;
        }

        if (snapshotBuilder == null)
            snapshotBuilder = new MapSnapshotBuilder(world);
        
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        // 1) Тайлы чанками
        var tiles = snapshotBuilder.BuildTileSnapshot();
        Debug.Log($"[MapSnapshotSync] Tile snapshot count: {tiles.Count}");

        for (int i = 0; i < tiles.Count; i += tileChunkSize)
        {
            var slice = tiles.GetRange(i, Mathf.Min(tileChunkSize, tiles.Count - i)).ToArray();
            var isLast = (i + tileChunkSize) >= tiles.Count;
            ApplyTileSnapshotChunkClientRpc(slice, isLast, target);
            yield return new WaitForSeconds(0.03f);
        }

        // 2) Netless objects in chunks.
        // Reuse WorldMapNetworkSync.SpawnNetlessClientRpc because it already knows how
        // to instantiate and bind netless objects using occupied tile cells.
        var netlessEntries = new List<KeyValuePair<string, MapObjectRegistry.NetlessEntry>>(
            world.GetNetlessRegistry()
        );


        for (int i = 0; i < netlessEntries.Count; i += netlessChunkSize)
        {
            int end = Mathf.Min(i + netlessChunkSize, netlessEntries.Count);

            for (int j = i; j < end; j++)
            {
                var kv = netlessEntries[j];
                var entry = kv.Value;

                world.NetworkSync.SpawnNetlessClientRpc(
                    entry.layer,
                    entry.itemId,
                    entry.occupiedTiles.ToArray(),
                    entry.pos,
                    kv.Key,
                    target
                );
            }

            yield return new WaitForSeconds(0.03f);
        }

        // 3) Сетевые GO — биндинг (если не используете Binder на префабах)
        if (sendNetworkedBindSnapshot)
        {
            int sent = 0;
            foreach (NetworkObjectRegistryEntry entry in world.GetNetworkObjectRegistryEntries())
            {
                MapLayerType layer = entry.Layer;
                MapLayerLogic logic = world.GetLayer(layer);

                if (logic == null)
                    continue;

                MapTile tile = logic.GetMapTile(entry.TileAnchor, entry.SubtileAnchor);

                if (tile?.BlockData == null)
                    continue;

                ulong netId = entry.NetworkObjectId;

                // Binding is still commented out in your current file.
                // Later, when we finish binding snapshot, use entry.TileAnchor / entry.SubtileAnchor
                // to reconstruct occupied cells.

                if (++sent % 50 == 0)
                    yield return new WaitForSeconds(0.03f);
            }
        }
    }

    // ----- client RPCs -----
    [ClientRpc]
    private void ApplyTileSnapshotChunkClientRpc(TileSnapshotEntry[] chunk, bool isLast, ClientRpcParams p = default)
    {
        if (world == null || blockLibrary == null) return;
        foreach (var e in chunk)
        {
            var layer = world.GetLayer(e.layer);
            var data = blockLibrary.GetById(e.itemId);
            if (layer == null || data == null) continue;

            // идемпотентность
            if (!layer.IsSubTilePresented(e.anchor.ToVector2Int(), e.localAnchor.ToVector2Int()))
            {
                layer.PlaceBlock(
                    e.anchor.ToVector2Int(),
                    e.localAnchor.ToVector2Int(),
                    data
                );
            }

            if (data.breakable)
                layer.SetHealth(e.anchor, e.localAnchor,data, e.hp, true);
        }
    }

}
