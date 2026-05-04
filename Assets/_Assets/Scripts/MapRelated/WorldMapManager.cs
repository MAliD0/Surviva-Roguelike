using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum WorldMapRunMode
{
    Offline,
    Online
}

/// <summary>
/// High-level world map manager.
/// 
/// Offline:
/// - directly modifies local map
/// - spawns normal local GameObjects
/// 
/// Online:
/// - clients send ServerRpc requests
/// - server is authoritative
/// - server syncs map data and object spawns to clients
/// </summary>
public class WorldMapManager : NetworkBehaviour
{
    [Header("Settings")]
    public MapBlockDataLibrary blockLibrary;

    [FoldoutGroup("Mode")]
    [SerializeField] private WorldMapRunMode runMode = WorldMapRunMode.Offline;

    [FoldoutGroup("Bounds")]
    [SerializeField] private bool useBounds = false;

    [FoldoutGroup("Bounds")]
    [SerializeField] private int minX = -128, maxX = 127;

    [FoldoutGroup("Bounds")]
    [SerializeField] private int minY = -128, maxY = 127;

    [FoldoutGroup("Layer References")]
    [Header("Back Layer")]
    [SerializeField] private MapLayerGraphics baseLayerGraphics;
    [SerializeField] private MapLayerLogic baseLayer;

    [FoldoutGroup("Layer References")]
    [Header("Fore Layer")]
    [SerializeField] private MapLayerGraphics foreLayerGraphics;
    [SerializeField] private MapLayerLogic foreLayer;

    [FoldoutGroup("Layer References")]
    [Header("Boat Layer")]
    [SerializeField] private MapLayerGraphics boatLayerGraphics;
    [SerializeField] private MapLayerLogic boatLayer;

    [FoldoutGroup("Layer References")]
    [Header("On Boat Layer")]
    [SerializeField] private MapLayerGraphics onBoatLayerGraphics;
    [SerializeField] private MapLayerLogic onBoatLayer;

    public static WorldMapManager Instance { get; private set; }

    public Action<GameObject, Vector2Int, string, string> onObjectInstantiated;

    [FoldoutGroup("Server References")]
    [SerializeField]
    public SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, ulong>>> _anchorToNetId =
        new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, ulong>>>();

    [FoldoutGroup("Server References")]
    [SerializeField]
    private SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, string>>> _anchorToNetlessId =
        new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, string>>>();

    [FoldoutGroup("Server References")]
    [SerializeField]
    private SerializedDictionary<string, NetlessEntry> _netlessRegistry =
        new SerializedDictionary<string, NetlessEntry>();

    private bool authoritativeEventsSubscribed = false;

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

    public Dictionary<string, NetlessEntry> GetNetlessRegistry()
    {
        return _netlessRegistry;
    }

    private bool IsOnlineMode
    {
        get
        {
            return runMode == WorldMapRunMode.Online
                && ConnectionManager.instance != null
                && ConnectionManager.instance.isActiveAndEnabled
                && NetworkManager.Singleton != null;
        }
    }

    private bool IsAuthoritative
    {
        get
        {
            if (!IsOnlineMode)
                return true;

            return IsServer;
        }
    }

    private string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CreateLayers();
        InitGraphics();
        InitRegistries();

        if (runMode == WorldMapRunMode.Offline)
        {
            SubscribeAuthoritativeLayerEvents();
            return;
        }

        if (ConnectionManager.instance != null)
        {
            ConnectionManager.instance.onServerActivate += (x)=>{ OnServerActivated(true); };
        }

        if (IsOnlineMode && IsServer)
        {
            SubscribeAuthoritativeLayerEvents();
            SubscribeNetworkCallbacks();
        }

        if (!IsOnlineMode)
        {
            SubscribeAuthoritativeLayerEvents();
        }
    }

    private void OnDestroy()
    {
        if (ConnectionManager.instance != null)
            ConnectionManager.instance.onServerActivate -= (x)=> {OnServerActivated(true);};

        if (NetworkManager != null)
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedServer;
    }

    private void OnServerActivated(bool active)
    {
        if (!active)
            return;

        if (!IsServer)
            return;

        SubscribeAuthoritativeLayerEvents();
        SubscribeNetworkCallbacks();
    }

    private void SubscribeNetworkCallbacks()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedServer;
            NetworkManager.OnClientConnectedCallback += OnClientConnectedServer;
        }
    }

    private void SubscribeAuthoritativeLayerEvents()
    {
        if (authoritativeEventsSubscribed)
            return;

        authoritativeEventsSubscribed = true;

        baseLayer.onMapTilePlaced += OnBaseLayerTilePlaced;
        foreLayer.onMapTilePlaced += OnForeLayerTilePlaced;
        boatLayer.onMapTilePlaced += OnBoatLayerTilePlaced;
        onBoatLayer.onMapTilePlaced += OnOnBoatLayerTilePlaced;

        baseLayer.onMapTileRemoved += OnBaseLayerTileRemoved;
        foreLayer.onMapTileRemoved += OnForeLayerTileRemoved;
        boatLayer.onMapTileRemoved += OnBoatLayerTileRemoved;
        onBoatLayer.onMapTileRemoved += OnOnBoatLayerTileRemoved;
    }

    private void UnsubscribeAuthoritativeLayerEvents()
    {
        if (!authoritativeEventsSubscribed)
            return;

        authoritativeEventsSubscribed = false;

        baseLayer.onMapTilePlaced -= OnBaseLayerTilePlaced;
        foreLayer.onMapTilePlaced -= OnForeLayerTilePlaced;
        boatLayer.onMapTilePlaced -= OnBoatLayerTilePlaced;
        onBoatLayer.onMapTilePlaced -= OnOnBoatLayerTilePlaced;

        baseLayer.onMapTileRemoved -= OnBaseLayerTileRemoved;
        foreLayer.onMapTileRemoved -= OnForeLayerTileRemoved;
        boatLayer.onMapTileRemoved -= OnBoatLayerTileRemoved;
        onBoatLayer.onMapTileRemoved -= OnOnBoatLayerTileRemoved;
    }

    private void OnBaseLayerTilePlaced(Dictionary<Vector2Int, HashSet<Vector2Int>> cells, MapBlockData data)
        => OnAuthoritativeTilePlaced(MapLayerType.backGround, cells, data);

    private void OnForeLayerTilePlaced(Dictionary<Vector2Int, HashSet<Vector2Int>> cells, MapBlockData data)
        => OnAuthoritativeTilePlaced(MapLayerType.foreGround, cells, data);

    private void OnBoatLayerTilePlaced(Dictionary<Vector2Int, HashSet<Vector2Int>> cells, MapBlockData data)
        => OnAuthoritativeTilePlaced(MapLayerType.boatGround, cells, data);

    private void OnOnBoatLayerTilePlaced(Dictionary<Vector2Int, HashSet<Vector2Int>> cells, MapBlockData data)
        => OnAuthoritativeTilePlaced(MapLayerType.onBoatGround, cells, data);

    private void OnBaseLayerTileRemoved(Dictionary<Vector2Int, HashSet<Vector2Int>> cells, MapBlockType type)
        => OnAuthoritativeTileRemoved(MapLayerType.backGround, cells, type);

    private void OnForeLayerTileRemoved(Dictionary<Vector2Int, HashSet<Vector2Int>> cells, MapBlockType type)
        => OnAuthoritativeTileRemoved(MapLayerType.foreGround, cells, type);

    private void OnBoatLayerTileRemoved(Dictionary<Vector2Int, HashSet<Vector2Int>> cells, MapBlockType type)
        => OnAuthoritativeTileRemoved(MapLayerType.boatGround, cells, type);

    private void OnOnBoatLayerTileRemoved(Dictionary<Vector2Int, HashSet<Vector2Int>> cells, MapBlockType type)
        => OnAuthoritativeTileRemoved(MapLayerType.onBoatGround, cells, type);

    private void CreateLayers()
    {
        baseLayer = new MapLayerLogic(CreateBounds());
        foreLayer = new MapLayerLogic(CreateBounds());
        boatLayer = new MapLayerLogic(CreateBounds());
        onBoatLayer = new MapLayerLogic(CreateBounds());
    }

    private void InitGraphics()
    {
        baseLayerGraphics.Init(baseLayer);
        foreLayerGraphics.Init(foreLayer);
        boatLayerGraphics.Init(boatLayer);
        onBoatLayerGraphics.Init(onBoatLayer);
    }

    private void InitRegistries()
    {
        _netlessRegistry = new SerializedDictionary<string, NetlessEntry>();
        _anchorToNetId = new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, ulong>>>();
        _anchorToNetlessId = new SerializedDictionary<MapLayerType, SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, string>>>();
    }

    private MapBounds CreateBounds()
    {
        return new MapBounds(minX, maxX, minY, maxY, useBounds);
    }

    // =========================================================
    // Public API
    // =========================================================

    public bool CanPlaceBlock(Vector2 worldPosition, string blockId)
    {
        var data = blockLibrary.GetMapBlockData(blockId);
        return ValidatePlacement(worldPosition, data);
    }

    public bool CheckIfPlacementIsPossible(Vector2 worldPosition, string blockId)
    {
        return CanPlaceBlock(worldPosition, blockId);
    }

    public bool TryPlaceBlock(Vector2 worldPosition, string blockId)
    {
        if (string.IsNullOrEmpty(blockId))
            return false;

        if (IsOnlineMode && !IsServer)
        {
            if (!CanPlaceBlock(worldPosition, blockId))
                return false;

            SetTileRequestServerRpc(worldPosition, blockId);
            return true;
        }

        return PlaceBlockLocal(worldPosition, blockId, syncClients: IsOnlineMode && IsServer);
    }

    public bool TryDamageBlock(Vector2 worldPosition, int amount)
    {
        if (IsOnlineMode && !IsServer)
        {
            DamageTileRequestServerRpc(worldPosition, amount);
            return true;
        }

        return DamageBlockLocal(worldPosition, amount, syncClients: IsOnlineMode && IsServer);
    }

    public bool TryDestroyBlock(Vector2Int tile, Vector2Int subtile)
    {
        if (IsOnlineMode && !IsServer)
        {
            DestroyTileRequestServerRpc(tile, subtile);
            return true;
        }

        return DestroyBlockLocal(tile, subtile, syncClients: IsOnlineMode && IsServer);
    }

    // =========================================================
    // RPC entry points
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void SetTileRequestServerRpc(Vector2 pos, string mapBlockDataId)
    {
        PlaceBlockLocal(pos, mapBlockDataId, syncClients: true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageTileRequestServerRpc(Vector2 pos, int amount)
    {
        DamageBlockLocal(pos, amount, syncClients: true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyTileRequestServerRpc(Vector2Int tile, Vector2Int subtile)
    {
        DestroyBlockLocal(tile, subtile, syncClients: true);
    }

    [ClientRpc]
    private void SetTileForClientsClientRpc(
        MapLayerType tileType,
        string mapBlockDataID,
        Vector2 position,
        ClientRpcParams rpcParams = default
    )
    {
        var layer = GetLayer(tileType);
        var data = blockLibrary.GetMapBlockData(mapBlockDataID);

        if (layer == null || data == null)
            return;

        layer.PlaceBlock(position, data);
    }

    [ClientRpc]
    private void DestroyTileForClientsClientRpc(
        Vector2Int tile,
        Vector2Int subtile,
        MapLayerType tileType,
        ClientRpcParams rpcParams = default
    )
    {
        var layer = GetLayer(tileType);

        if (layer == null)
            return;

        layer.RemoveTile(tile, subtile);
    }

    [ClientRpc]
    private void UpdateTileHealthClientRpc(
        MapLayerType layerType,
        Vector2Int anchor,
        Vector2Int subtile,
        int hp,
        int maxHp,
        ClientRpcParams rpcParams = default
    )
    {
        var layer = GetLayer(layerType);
        var tile = layer?.GetMapTile(anchor, subtile);
        var data = tile?.BlockData;

        if (layer == null || data == null)
            return;

        layer.SetHealth(anchor, subtile, data, hp, fireEvent: true);
    }

    // =========================================================
    // Local authoritative logic
    // =========================================================

    private bool PlaceBlockLocal(Vector2 worldPosition, string blockId, bool syncClients)
    {
        var data = blockLibrary.GetMapBlockData(blockId);

        if (!ValidatePlacement(worldPosition, data))
            return false;

        var targetLayer = GetLayer(data.mapLayerType);

        if (targetLayer == null)
            return false;

        if (!targetLayer.PlaceBlock(worldPosition, data))
            return false;

        if (syncClients)
        {
            SetTileForClientsClientRpc(
                data.mapLayerType,
                data.GetItemID(),
                worldPosition,
                SendAllExceptHostOrDefault()
            );
        }

        return true;
    }

    private bool DamageBlockLocal(Vector2 worldPosition, int amount, bool syncClients)
    {
        MapLayerType layerType = DetectLayerByCell(worldPosition);
        MapLayerLogic layer = GetLayer(layerType);

        if (layer == null)
            return false;

        var tile = layer.GetMapTile(worldPosition);
        var data = tile?.BlockData;

        if (data == null || !data.breakable)
            return false;

        Vector2Int anchor = tile.TileAnchor;
        Vector2Int subtile = tile.SubtileAnchor;

        bool broken = layer.Damage(anchor, subtile, data, Mathf.Max(1, amount));

        if (syncClients)
        {
            UpdateTileHealthClientRpc(
                layerType,
                anchor,
                subtile,
                layer.GetHealth(anchor, subtile),
                data.maxHealth,
                SendAllExceptHostOrDefault()
            );
        }

        if (!broken)
            return true;

        if (LootSpawnerManager.Instance != null)
            LootSpawnerManager.Instance.SpawnLootForBlock(data, worldPosition);

        layer.RemoveTile(anchor, subtile);

        if (syncClients)
        {
            DestroyTileForClientsClientRpc(
                anchor,
                subtile,
                layerType,
                SendAllExceptHostOrDefault()
            );
        }

        return true;
    }

    private bool DestroyBlockLocal(Vector2Int tile, Vector2Int subtile, bool syncClients)
    {
        MapLayerLogic layer = null;
        MapLayerType layerType = MapLayerType.backGround;

        if (foreLayer.IsSubTilePresented(tile, subtile))
        {
            layer = foreLayer;
            layerType = MapLayerType.foreGround;
        }
        else if (baseLayer.IsSubTilePresented(tile, subtile))
        {
            layer = baseLayer;
            layerType = MapLayerType.backGround;
        }
        else if (boatLayer.IsSubTilePresented(tile, subtile))
        {
            layer = boatLayer;
            layerType = MapLayerType.boatGround;
        }
        else if (onBoatLayer.IsSubTilePresented(tile, subtile))
        {
            layer = onBoatLayer;
            layerType = MapLayerType.onBoatGround;
        }

        if (layer == null)
            return false;

        var mapTile = layer.GetMapTile(tile, subtile);
        var data = mapTile?.BlockData;

        if (data != null && LootSpawnerManager.Instance != null)
            LootSpawnerManager.Instance.SpawnLootForBlock(data, layer.SubtileToWorldPosition(tile, subtile));

        layer.RemoveTile(tile, subtile);

        if (syncClients)
        {
            DestroyTileForClientsClientRpc(
                tile,
                subtile,
                layerType,
                SendAllExceptHostOrDefault()
            );
        }

        return true;
    }

    // Testing/local helper
    public void SetTile(Vector2 pos, MapBlockData data)
    {
        if (data == null)
            return;

        PlaceBlockLocal(pos, data.GetItemID(), syncClients: false);
    }

    // =========================================================
    // Placement validation
    // =========================================================

    private bool ValidatePlacement(Vector2 pos, MapBlockData data)
    {
        if (data == null)
            return false;

        var targetLayer = GetLayer(data.mapLayerType);

        if (targetLayer == null)
        {
            Debug.LogError("[ValidatePlacement] targetLayer null");
            return false;
        }

        if (data.mapLayerType == MapLayerType.foreGround)
        {
            if (!baseLayer.IsFootprintFullyOccupied(pos, data.blockSize.x, data.blockSize.y))
            {
                Debug.LogWarning($"[Rules] ForeGround without BackGround at {pos}");
                return false;
            }
        }

        if (data.mapLayerType == MapLayerType.onBoatGround)
        {
            if (baseLayer.IsFootprintOccupied(pos, data.blockSize.x, data.blockSize.y))
            {
                Debug.LogWarning($"[Rules] onBoatGround on occupied BackGround at {pos}");
                return false;
            }
        }

        return targetLayer.CanBePlaced(pos, data);
    }

    // =========================================================
    // Authoritative tile callbacks
    // =========================================================

    private void OnAuthoritativeTilePlaced(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        if (!IsAuthoritative)
            return;

        if (data == null || data.mapBlockType != MapBlockType.GameObject)
            return;

        if (IsOnlineMode)
            SpawnPlacedObjectOnline(layer, cells, data);
        else
            SpawnPlacedObjectOffline(layer, cells, data);
    }

    private void OnAuthoritativeTileRemoved(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockType type
    )
    {
        if (!IsAuthoritative)
            return;

        if (type != MapBlockType.GameObject)
            return;

        var serializedCells = DictEntry.SerializeDictionary(cells).ToArray();

        if (TryFindRegisteredNetId(layer, cells, out var registeredTile, out var registeredSubtile, out ulong networkId))
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out var networkObject))
            {
                networkObject.Despawn(true);
            }

            RemoveRegisteredNetId(layer, registeredTile, registeredSubtile);

            if (IsOnlineMode)
                BindObjectByNetIdClientRpc(serializedCells, 0, layer);
            else
                GetGraphics(layer).UnbindByCells(serializedCells, destroyNonNetworked: true);

            return;
        }

        if (TryFindRegisteredNetlessId(layer, cells, out var netlessTile, out var netlessSubtile, out string id))
        {
            RemoveRegisteredNetlessId(layer, netlessTile, netlessSubtile, id);

            if (IsOnlineMode)
                RemoveNetlessClientRpc(id, layer);
            else
                GetGraphics(layer).UnbindById(id);

            return;
        }

        if (IsOnlineMode)
            UnbindByCellsClientRpc(serializedCells, layer, destroyNonNetworked: true);
        else
            GetGraphics(layer).UnbindByCells(serializedCells, destroyNonNetworked: true);
    }

    // =========================================================
    // Object spawning
    // =========================================================

    private void SpawnPlacedObjectOffline(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        if (!TryGetPlacedAnchor(layer, cells, out var tileAnchor, out var subtileAnchor))
            return;

        Vector2 worldPos = GetLayer(layer).SubtileToWorldPosition(tileAnchor, subtileAnchor);
        var prefab = blockLibrary.GetMapBlockData(data.GetItemID())?.gameObject;

        if (!prefab)
        {
            Debug.LogError($"[SpawnPlacedObjectOffline] Prefab id={data.GetItemID()} not found");
            return;
        }

        var go = Instantiate(prefab, worldPos, Quaternion.identity);
        string id = "offline_" + NewId();

        RegisterNetlessObject(layer, cells, id, data.GetItemID(), worldPos);

        foreach (Vector2Int anchor in cells.Keys)
        {
            GetGraphics(layer).BindObject(anchor, cells[anchor].ToList(), go, id);
        }
    }

    private void SpawnPlacedObjectOnline(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        if (!IsServer)
            return;

        if (!TryGetPlacedAnchor(layer, cells, out var tileAnchor, out var subtileAnchor))
            return;

        Vector2 worldPos = GetLayer(layer).SubtileToWorldPosition(tileAnchor, subtileAnchor);
        var prefab = blockLibrary.GetMapBlockData(data.GetItemID())?.gameObject;

        if (!prefab)
        {
            Debug.LogError($"[SpawnPlacedObjectOnline] Prefab id={data.GetItemID()} not found");
            return;
        }

        var serializedCells = DictEntry.SerializeDictionary(cells).ToArray();

        if (prefab.TryGetComponent<NetworkObject>(out _))
        {
            var go = Instantiate(prefab, worldPos, Quaternion.identity);
            var networkObject = go.GetComponent<NetworkObject>();

            networkObject.Spawn();

            RegisterNetObject(layer, cells, networkObject.NetworkObjectId);

            BindObjectByNetIdClientRpc(serializedCells, networkObject.NetworkObjectId, layer);
        }
        else
        {
            string id = NewId();

            RegisterNetlessObject(layer, cells, id, data.GetItemID(), worldPos);

            SpawnNetlessClientRpc(
                layer,
                data.GetItemID(),
                serializedCells,
                worldPos,
                id
            );
        }
    }

    [ClientRpc]
    private void BindObjectByNetIdClientRpc(
        DictEntry[] serializedTiles,
        ulong netId,
        MapLayerType layer,
        ClientRpcParams rpcParams = default
    )
    {
        if (netId == 0)
        {
            GetGraphics(layer).UnbindByCells(serializedTiles, destroyNonNetworked: false);
            return;
        }

        Dictionary<Vector2Int, HashSet<Vector2Int>> tiles =
            DictEntry.DictEntryToDictionary(serializedTiles.ToList());

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var networkObject))
        {
            foreach (Vector2Int anchor in tiles.Keys)
                StartCoroutine(RetryBind(anchor, tiles[anchor].ToArray(), netId, layer));

            return;
        }

        foreach (Vector2Int anchor in tiles.Keys)
        {
            GetGraphics(layer).BindObject(anchor, tiles[anchor].ToList(), networkObject.gameObject, null);
        }
    }

    private System.Collections.IEnumerator RetryBind(
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
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var networkObject))
            {
                GetGraphics(layer).BindObject(tile, new List<Vector2Int>(subtiles), networkObject.gameObject, null);
                yield break;
            }
        }

        Debug.LogWarning($"[RetryBind] net object {netId} not found");
    }

    [ClientRpc]
    private void SpawnNetlessClientRpc(
        MapLayerType layer,
        string itemId,
        DictEntry[] occupiedTiles,
        Vector3 pos,
        string id,
        ClientRpcParams rpcParams = default
    )
    {
        var prefab = blockLibrary.GetMapBlockData(itemId)?.gameObject;

        if (!prefab)
        {
            Debug.LogError($"[SpawnNetless] Prefab {itemId} not found");
            return;
        }

        Dictionary<Vector2Int, HashSet<Vector2Int>> tiles =
            DictEntry.DictEntryToDictionary(occupiedTiles.ToList());

        var go = Instantiate(prefab, pos, Quaternion.identity);

        foreach (Vector2Int anchor in tiles.Keys)
        {
            GetGraphics(layer).BindObject(anchor, tiles[anchor].ToList(), go, id);
        }
    }

    [ClientRpc]
    private void RemoveNetlessClientRpc(
        string id,
        MapLayerType layer,
        ClientRpcParams rpcParams = default
    )
    {
        GetGraphics(layer).UnbindById(id);
    }

    [ClientRpc]
    private void UnbindByCellsClientRpc(
        DictEntry[] cells,
        MapLayerType layer,
        bool destroyNonNetworked,
        ClientRpcParams rpcParams = default
    )
    {
        GetGraphics(layer).UnbindByCells(cells, destroyNonNetworked);
    }

    // =========================================================
    // Registry helpers
    // =========================================================

    private void RegisterNetObject(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        ulong networkObjectId
    )
    {
        if (!TryGetPlacedAnchor(layer, cells, out var tileAnchor, out var subtileAnchor))
            return;

        if (!_anchorToNetId.TryGetValue(layer, out var layerDict))
        {
            layerDict = new SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, ulong>>();
            _anchorToNetId[layer] = layerDict;
        }

        if (!layerDict.TryGetValue(tileAnchor, out var subtileDict))
        {
            subtileDict = new SerializedDictionary<Vector2Int, ulong>();
            layerDict[tileAnchor] = subtileDict;
        }

        subtileDict[subtileAnchor] = networkObjectId;
    }

    private void RegisterNetlessObject(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        string id,
        string itemId,
        Vector3 worldPos
    )
    {
        if (!TryGetPlacedAnchor(layer, cells, out var tileAnchor, out var subtileAnchor))
            return;

        if (!_anchorToNetlessId.TryGetValue(layer, out var layerDict))
        {
            layerDict = new SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, string>>();
            _anchorToNetlessId[layer] = layerDict;
        }

        if (!layerDict.TryGetValue(tileAnchor, out var subtileDict))
        {
            subtileDict = new SerializedDictionary<Vector2Int, string>();
            layerDict[tileAnchor] = subtileDict;
        }

        subtileDict[subtileAnchor] = id;

        _netlessRegistry[id] = new NetlessEntry
        {
            layer = layer,
            anchor = tileAnchor,
            localAnchor = subtileAnchor,
            occupiedTiles = DictEntry.SerializeDictionary(cells),
            itemId = itemId,
            pos = worldPos
        };
    }

    private bool TryFindRegisteredNetId(
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

        if (!_anchorToNetId.TryGetValue(layer, out var layerDict))
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

    private bool TryFindRegisteredNetlessId(
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

        if (!_anchorToNetlessId.TryGetValue(layer, out var layerDict))
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

    private void RemoveRegisteredNetId(
        MapLayerType layer,
        Vector2Int tile,
        Vector2Int subtile
    )
    {
        if (!_anchorToNetId.TryGetValue(layer, out var layerDict))
            return;

        if (!layerDict.TryGetValue(tile, out var subtileDict))
            return;

        subtileDict.Remove(subtile);

        if (subtileDict.Count == 0)
            layerDict.Remove(tile);

        if (layerDict.Count == 0)
            _anchorToNetId.Remove(layer);
    }

    private void RemoveRegisteredNetlessId(
        MapLayerType layer,
        Vector2Int tile,
        Vector2Int subtile,
        string id
    )
    {
        if (_anchorToNetlessId.TryGetValue(layer, out var layerDict))
        {
            if (layerDict.TryGetValue(tile, out var subtileDict))
            {
                subtileDict.Remove(subtile);

                if (subtileDict.Count == 0)
                    layerDict.Remove(tile);
            }

            if (layerDict.Count == 0)
                _anchorToNetlessId.Remove(layer);
        }

        if (!string.IsNullOrEmpty(id))
            _netlessRegistry.Remove(id);
    }

    // =========================================================
    // Late join sync
    // =========================================================

    private void OnClientConnectedServer(ulong clientId)
    {
        if (!IsServer)
            return;

        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };

        foreach (var kv in _netlessRegistry)
        {
            var entry = kv.Value;

            SpawnNetlessClientRpc(
                entry.layer,
                entry.itemId,
                entry.occupiedTiles.ToArray(),
                entry.pos,
                kv.Key,
                target
            );
        }
    }

    // =========================================================
    // Helpers
    // =========================================================

    private bool TryGetPlacedAnchor(
        MapLayerType layerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        out Vector2Int tileAnchor,
        out Vector2Int subtileAnchor
    )
    {
        tileAnchor = default;
        subtileAnchor = default;

        var layer = GetLayer(layerType);

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

    private ClientRpcParams SendAllExceptHostOrDefault()
    {
        if (ConnectionManager.instance != null)
            return ConnectionManager.instance.SendAllExceptHost();

        return default;
    }

    public MapLayerLogic GetLayer(MapLayerType type)
    {
        switch (type)
        {
            case MapLayerType.backGround:
                return baseLayer;
            case MapLayerType.foreGround:
                return foreLayer;
            case MapLayerType.boatGround:
                return boatLayer;
            case MapLayerType.onBoatGround:
                return onBoatLayer;
            default:
                return null;
        }
    }

    public MapLayerGraphics GetGraphics(MapLayerType type)
    {
        switch (type)
        {
            case MapLayerType.backGround:
                return baseLayerGraphics;
            case MapLayerType.foreGround:
                return foreLayerGraphics;
            case MapLayerType.boatGround:
                return boatLayerGraphics;
            case MapLayerType.onBoatGround:
                return onBoatLayerGraphics;
            default:
                return baseLayerGraphics;
        }
    }

    private MapLayerType DetectLayerByCell(Vector2 pos)
    {
        if (foreLayer.IsTilePresented(pos))
            return MapLayerType.foreGround;

        if (baseLayer.IsTilePresented(pos))
            return MapLayerType.backGround;

        if (onBoatLayer.IsTilePresented(pos))
            return MapLayerType.onBoatGround;

        if (boatLayer.IsTilePresented(pos))
            return MapLayerType.boatGround;

        return MapLayerType.backGround;
    }

    // =========================================================
    // Utility / testing
    // =========================================================

    public void initiateForTesting()
    {
        runMode = WorldMapRunMode.Offline;
        ClearAllLayers();
    }

    [Button]
    public void ClearAllLayers()
    {
        UnsubscribeAuthoritativeLayerEvents();

        onBoatLayerGraphics.ClearTilemap();
        baseLayerGraphics.ClearTilemap();
        foreLayerGraphics.ClearTilemap();
        boatLayerGraphics.ClearTilemap();

        CreateLayers();
        InitGraphics();
        InitRegistries();

        if (runMode == WorldMapRunMode.Offline || IsServer)
            SubscribeAuthoritativeLayerEvents();
    }

    [Button]
    public void ClearTilemapsForServer()
    {
        ClearTilemapsClientRpc();
    }

    [ClientRpc]
    private void ClearTilemapsClientRpc()
    {
        onBoatLayerGraphics.ClearTilemap();
        baseLayerGraphics.ClearTilemap();
        foreLayerGraphics.ClearTilemap();
        boatLayerGraphics.ClearTilemap();

        onBoatLayer.LayerTiles.Clear();
        baseLayer.LayerTiles.Clear();
        boatLayer.LayerTiles.Clear();
        foreLayer.LayerTiles.Clear();
    }
}