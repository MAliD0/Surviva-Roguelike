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

    [Header("Chunk sizes")]
    [SerializeField] private int tileChunkSize = 80;
    [SerializeField] private int netlessChunkSize = 30;

    [Header("Options")]
    [Tooltip("Присылать биндинг сетевых GO (если нет биндера на префабах)")]
    [SerializeField] private bool sendNetworkedBindSnapshot = true;

    // -------- DTO --------
    [Serializable]
    public struct TileSnapshotEntry : INetworkSerializable
    {
        public MapLayerType layer;
        public string itemId; // MapBlockData.GetItemID()
        public V2I anchor;    // integer tileIndex anchor (existing)
        public V2I localAnchor; // optional: subtile anchor inside that tileIndex
        public Vector3 pos;     // optional: precise world position for the anchor subtile
        public int hp;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref layer);
            s.SerializeValue(ref itemId);
            s.SerializeValue(ref anchor);
            s.SerializeValue(ref localAnchor);
            s.SerializeValue(ref hp);
        }
    }

    // ----- lifecycle -----
    public override void OnNetworkSpawn()
    {
        if (!world) world = WorldMapManager.Instance;
        if (!blockLibrary) blockLibrary = world ? world.blockLibrary : null;

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

        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        // 1) Тайлы чанками
        var tiles = BuildTileSnapshot();
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

        Debug.Log($"[MapSnapshotSync] Netless snapshot count: {netlessEntries.Count}");

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

                if (++sent % 200 == 0)
                    yield return new WaitForSeconds(0.03f);
            }
        }
    }

    // ----- snapshot builders (server) -----
    private List<TileSnapshotEntry> BuildTileSnapshot()
    {
        var result = new List<TileSnapshotEntry>();

        void AddLayer(MapLayerType t, MapLayerLogic logic)
        {
            if (logic == null) return;
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int tileIndex in logic.LayerTiles.Keys)
            {
                foreach (var subtile in logic.LayerTiles[tileIndex].Values)
                {
                    var blockData = subtile.BlockData;
                    var anchor = subtile.ParentTile;
                    var localAnchor = subtile.Position;

                    if (!seen.Add(anchor)) continue; // один раз на мульти-группу

                    int hp = blockData.breakable ? logic.GetHealth(anchor, localAnchor) : 0;
                    result.Add(new TileSnapshotEntry { layer = t, itemId = blockData.GetItemID(), anchor = anchor, localAnchor = localAnchor,hp = hp });

                }
            }
        }

        AddLayer(MapLayerType.backGround, world.GetLayer(MapLayerType.backGround));
        AddLayer(MapLayerType.foreGround, world.GetLayer(MapLayerType.foreGround));
        AddLayer(MapLayerType.boatGround, world.GetLayer(MapLayerType.boatGround));
        AddLayer(MapLayerType.onBoatGround, world.GetLayer(MapLayerType.onBoatGround));
        return result;
    }

    private static V2I[] ToV2IArray(Vector2Int[] arr)
    {
        var res = new V2I[arr.Length];
        for (int i = 0; i < arr.Length; i++) res[i] = arr[i];
        return res;
    }

    // ----- client RPCs -----
    [ClientRpc]
    private void ApplyTileSnapshotChunkClientRpc(TileSnapshotEntry[] chunk, bool isLast, ClientRpcParams p = default)
    {
        if (world == null || blockLibrary == null) return;
        print("Enter ApplyTileSnapshotChunkClientRpc");
        foreach (var e in chunk)
        {
            var layer = world.GetLayer(e.layer);
            var data = blockLibrary.GetMapBlockData(e.itemId);
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


    [ClientRpc]
    private void BindObjectByNetIdClientRpc(V2I[] cellsV2I, ulong netId, MapLayerType layer, ClientRpcParams p = default)
    {
        var sm = NetworkManager.Singleton.SpawnManager;
        if (!sm.SpawnedObjects.TryGetValue(netId, out var no))
        {
            StartCoroutine(RetryBind(cellsV2I, netId, layer));
            return;
        }

        var cells = new List<Vector2Int>(cellsV2I.Length);
        foreach (var c in cellsV2I) cells.Add(c);
        //world.GetGraphics(layer).BindObject(cells, no.gameObject, null);
    }

    private IEnumerator RetryBind(V2I[] cellsV2I, ulong netId, MapLayerType layer)
    {
        var sm = NetworkManager.Singleton.SpawnManager;
        for (int i = 0; i < 60; i++) // до ~1 сек при 60 FPS
        {
            yield return null;
            if (sm.SpawnedObjects.TryGetValue(netId, out var no))
            {
                var cells = new List<Vector2Int>(cellsV2I.Length);
                foreach (var c in cellsV2I) cells.Add(c);
                //world.GetGraphics(layer).BindObject(cells, no.gameObject, null);
                yield break;
            }
        }
        Debug.LogWarning($"[MapSnapshotSync] RetryBind timeout for netId={netId}");
    }
}
