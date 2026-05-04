using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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

    private MapPlacementValidator placementValidator;
    private WorldMapService worldMapService;
    private MapObjectRegistry mapObjectRegistry;

    public Action<GameObject, Vector2Int, string, string> onObjectInstantiated;

    private bool authoritativeEventsSubscribed = false;



    public Dictionary<string, MapObjectRegistry.NetlessEntry> GetNetlessRegistry()
    {
        return mapObjectRegistry.GetNetlessRegistry();
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
        InitObjectRegistry();
        InitPlacementValidator();
        InitWorldMapService();

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
    private void InitWorldMapService()
    {
        worldMapService = new WorldMapService(
            blockLibrary,
            placementValidator,
            GetLayer
        );
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

    private void InitObjectRegistry()
    {
        mapObjectRegistry = new MapObjectRegistry(GetLayer);
    }

    private void InitPlacementValidator()
    {
        placementValidator = new MapPlacementValidator(
            blockLibrary,
            GetLayer
        );
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
        return GetPlacementResult(worldPosition, blockId).Success;
    }

    public PlacementResult GetPlacementResult(Vector2 worldPosition, string blockId)
    {
        if (worldMapService == null)
        {
            InitPlacementValidator();
            InitWorldMapService();
        }

        return worldMapService.GetPlacementResult(worldPosition, blockId);
    }

    public bool CheckIfPlacementIsPossible(Vector2 worldPosition, string blockId)
    {
        return GetPlacementResult(worldPosition, blockId).Success;
    }
    public bool TryPlaceBlock(Vector2 worldPosition, string blockId)
    {
        if (string.IsNullOrEmpty(blockId))
            return false;

        if (IsOnlineMode && !IsServer)
        {
            PlacementResult placementResult = GetPlacementResult(worldPosition, blockId);

            if (!placementResult.Success)
            {
                Debug.LogWarning(placementResult.ToString());
                return false;
            }

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
        WorldMapOperationResult result = worldMapService.PlaceBlock(worldPosition, blockId);

        if (!result.Success)
        {
            Debug.LogWarning(result.Message);
            return false;
        }

        if (syncClients)
        {
            SetTileForClientsClientRpc(
                result.BlockData.mapLayerType,
                result.BlockData.GetItemID(),
                worldPosition,
                SendAllExceptHostOrDefault()
            );
        }

        return true;
    }
    private bool DamageBlockLocal(Vector2 worldPosition, int amount, bool syncClients)
    {
        WorldMapOperationResult result = worldMapService.DamageBlock(worldPosition, amount);

        if (!result.Success)
        {
            Debug.LogWarning(result.Message);
            return false;
        }

        if (syncClients)
        {
            UpdateTileHealthClientRpc(
                result.LayerType,
                result.TileAnchor,
                result.SubtileAnchor,
                result.CurrentHealth,
                result.MaxHealth,
                SendAllExceptHostOrDefault()
            );
        }

        if (!result.Broken)
            return true;

        if (LootSpawnerManager.Instance != null)
            LootSpawnerManager.Instance.SpawnLootForBlock(result.BlockData, result.WorldPosition);

        result.Layer.RemoveTile(result.TileAnchor, result.SubtileAnchor);

        if (syncClients)
        {
            DestroyTileForClientsClientRpc(
                result.TileAnchor,
                result.SubtileAnchor,
                result.LayerType,
                SendAllExceptHostOrDefault()
            );
        }

        return true;
    }

    private bool DestroyBlockLocal(Vector2Int tile, Vector2Int subtile, bool syncClients)
    {
        WorldMapOperationResult result = worldMapService.DestroyBlock(tile, subtile);

        if (!result.Success)
        {
            Debug.LogWarning(result.Message);
            return false;
        }

        if (result.BlockData != null && LootSpawnerManager.Instance != null)
            LootSpawnerManager.Instance.SpawnLootForBlock(result.BlockData, result.WorldPosition);

        if (syncClients)
        {
            DestroyTileForClientsClientRpc(
                result.TileAnchor,
                result.SubtileAnchor,
                result.LayerType,
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

        if (mapObjectRegistry.TryFindNetworkObject(layer, cells, out var registeredTile, out var registeredSubtile, out ulong networkId))        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out var networkObject))
            {
                networkObject.Despawn(true);
            }

            mapObjectRegistry.RemoveNetworkObject(layer, registeredTile, registeredSubtile);

            if (IsOnlineMode)
                BindObjectByNetIdClientRpc(serializedCells, 0, layer);
            else
                GetGraphics(layer).UnbindByCells(serializedCells, destroyNonNetworked: true);

            return;
        }

        if (mapObjectRegistry.TryFindNetlessObject(layer, cells, out var netlessTile, out var netlessSubtile, out string id))
        {
            mapObjectRegistry.RemoveNetlessObject(layer, netlessTile, netlessSubtile, id);

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

        mapObjectRegistry.RegisterNetlessObject(layer, cells, id, data.GetItemID(), worldPos);
        
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

            mapObjectRegistry.RegisterNetworkObject(layer, cells, networkObject.NetworkObjectId);
            
            BindObjectByNetIdClientRpc(serializedCells, networkObject.NetworkObjectId, layer);
        }
        else
        {
            string id = NewId();

            mapObjectRegistry.RegisterNetlessObject(layer, cells, id, data.GetItemID(), worldPos);
            
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

        foreach (var kv in mapObjectRegistry.GetNetlessRegistry())
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

    public IEnumerable<NetworkObjectRegistryEntry> GetNetworkObjectRegistryEntries()
    {
        if (mapObjectRegistry == null)
            return Array.Empty<NetworkObjectRegistryEntry>();

        return mapObjectRegistry.GetNetworkObjectEntries();
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
        InitObjectRegistry();
        InitPlacementValidator();
        InitWorldMapService();

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