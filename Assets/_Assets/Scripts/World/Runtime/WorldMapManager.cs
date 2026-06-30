using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

    [FoldoutGroup("Layer References")]
    [Header("Fore Layer")]
    [SerializeField] private MapLayerGraphics foreLayerGraphics;

    [FoldoutGroup("Layer References")]
    [Header("Boat Layer")]
    [SerializeField] private MapLayerGraphics mediumLayerGraphics;

    [FoldoutGroup("Layer References")]
    [Header("On Boat Layer")]
    [SerializeField] private MapLayerGraphics circleElementsLayerGraphics;

    public static WorldMapManager Instance { get; private set; }

    private MapPlacementValidator placementValidator;
    private WorldMapService worldMapService;
    private MapObjectRegistry mapObjectRegistry;
    private MapObjectSpawner mapObjectSpawner;
    private WorldMapLayers worldMapLayers;
    private WorldMapLayerEventRouter layerEventRouter;
    private WorldLootService worldLootService;
    
    [SerializeField] private WorldMapNetworkSync networkSync;

    public MapBlockDataLibrary BlockLibrary => blockLibrary;
    public WorldMapNetworkSync NetworkSync => networkSync;
    
    public event Action<MapLayerType, Vector2Int, Vector2Int, GameObject> onObjectCreated
    {
        add => mapObjectRegistry.onObjectCreated += value;
        remove => mapObjectRegistry.onObjectCreated -= value;
    }

    public event Action<MapLayerType, Vector2Int, Vector2Int, GameObject> onObjectDestroyed
    {
        add => mapObjectRegistry.onObjectDestroyed += value;
        remove => mapObjectRegistry.onObjectDestroyed -= value;
    }

    public bool TryGetRuntimeObject(MapLayerType layer, Vector2Int tile, Vector2Int subtile, out GameObject gameObject)
    {
        return mapObjectRegistry.TryGetRuntimeObject(layer, tile, subtile, out gameObject);
    }
    
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

        InitLayers();

        InitObjectRegistry();
        InitObjectSpawner();

        InitPlacementValidator();
        InitWorldMapService();
        InitLayerEventRouter();
        InitNetworkSync();
        InitWorldLootService();
    }

    private void Start()
    {
        
        if (runMode == WorldMapRunMode.Offline)
        {
            SubscribeAuthoritativeLayerEvents();
            return;
        }

        if (ConnectionManager.instance != null)
        {
            ConnectionManager.instance.onServerActivate += OnServerActivateEvent;
        }

        if (IsOnlineMode && IsServer)
        {
            SubscribeAuthoritativeLayerEvents();
        }

        if (!IsOnlineMode)
        {
            SubscribeAuthoritativeLayerEvents();
        }
    }


    private void OnDestroy()
    {
        UnsubscribeAuthoritativeLayerEvents();

        if (ConnectionManager.instance != null)
            ConnectionManager.instance.onServerActivate -= OnServerActivateEvent;
    }
    private void InitWorldLootService()
    {
        worldLootService = new WorldLootService();
    }
    private void OnServerActivated(bool active)
    {
        if (!active)
            return;

        if (!IsServer)
            return;

        SubscribeAuthoritativeLayerEvents();
    }
    private void InitLayerEventRouter()
    {
        layerEventRouter = new WorldMapLayerEventRouter(
            GetLayer,
            OnAuthoritativeTilePlaced,
            OnAuthoritativeTileRemoved
        );
    }
    private void InitNetworkSync()
    {
        if (networkSync == null)
            networkSync = GetComponent<WorldMapNetworkSync>();

        if (networkSync == null)
        {
            Debug.LogError("[WorldMapManager] WorldMapNetworkSync component is missing.");
            return;
        }

        networkSync.Init(this);
    }

    private void InitWorldMapService()
    {
        worldMapService = new WorldMapService(
            blockLibrary,
            placementValidator,
            GetLayer
        );
    }
    private void InitObjectSpawner()
    {
        mapObjectSpawner = new MapObjectSpawner(
            blockLibrary,
            mapObjectRegistry,
            GetLayer,
            GetGraphics,
            NewId
        );
    }

    private void InitLayers()
    {
        worldMapLayers = new WorldMapLayers(
            CreateBounds(),
            baseLayerGraphics,
            foreLayerGraphics,
            mediumLayerGraphics,
            circleElementsLayerGraphics
        );
    }

    private void SubscribeAuthoritativeLayerEvents()
    {
        layerEventRouter?.Subscribe();
    }

    private void UnsubscribeAuthoritativeLayerEvents()
    {
        layerEventRouter?.Unsubscribe();
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

    private void OnServerActivateEvent(string value)
    {
        OnServerActivated(true);
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

            networkSync.SetTileRequestServerRpc(worldPosition, blockId);
            return true;
        }

        return PlaceBlockLocal(worldPosition, blockId, syncClients: IsOnlineMode && IsServer);
    }

    public bool TryDamageBlock(Vector2 worldPosition, int amount)
    {
        if (IsOnlineMode && !IsServer)
        {
            networkSync.DamageTileRequestServerRpc(worldPosition, amount);
            return true;
        }

        return DamageBlockLocal(worldPosition, amount, syncClients: IsOnlineMode && IsServer);
    }

    public bool TryDestroyBlock(Vector2Int tile, Vector2Int subtile)
    {
        if (IsOnlineMode && !IsServer)
        {
            networkSync.DestroyTileRequestServerRpc(tile, subtile);
            return true;
        }

        return DestroyBlockLocal(tile, subtile, syncClients: IsOnlineMode && IsServer);
    }

    ///
    /// Network entry methods
    /// 
    
    public bool PlaceBlockFromNetwork(Vector2 worldPosition, string blockId)
    {
        if (!IsServer)
            return false;

        return PlaceBlockLocal(worldPosition, blockId, syncClients: true);
    }

    public bool DamageBlockFromNetwork(Vector2 worldPosition, int amount)
    {
        if (!IsServer)
            return false;

        return DamageBlockLocal(worldPosition, amount, syncClients: true);
    }

    public bool DestroyBlockFromNetwork(Vector2Int tile, Vector2Int subtile)
    {
        if (!IsServer)
            return false;

        return DestroyBlockLocal(tile, subtile, syncClients: true);
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
            networkSync.SetTileForClientsClientRpc(
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
            networkSync.UpdateTileHealthClientRpc(
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

        worldLootService.SpawnLootForBlock(result.BlockData, result.WorldPosition);
        
        result.Layer.RemoveTile(result.TileAnchor, result.SubtileAnchor);

        if (syncClients)
        {
            networkSync.DestroyTileForClientsClientRpc(
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

        worldLootService.SpawnLootForBlock(result.BlockData, result.WorldPosition);

        if (syncClients)
        {
            networkSync.DestroyTileForClientsClientRpc(
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
            mapObjectSpawner.SpawnPlacedObjectOffline(layer, cells, data);
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

        MapObjectRemovalResult result = mapObjectSpawner.RemovePlacedObject(
            layer,
            cells,
            IsOnlineMode
        );

        if (!result.Success)
        {
            Debug.LogWarning($"[WorldMapManager] Object removal failed: {result.Message}");
            return;
        }

        if (IsOnlineMode)
            networkSync.SyncObjectRemovalResult(result, cells);
    }
    // =========================================================
    // Object spawning
    // =========================================================

    private void SpawnPlacedObjectOnline(
        MapLayerType layer,
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        if (!IsServer)
            return;

        MapObjectSpawnResult result = mapObjectSpawner.SpawnPlacedObjectOnline(
            layer,
            cells,
            data
        );

        if (!result.Success)
        {
            Debug.LogWarning($"[WorldMapManager] Online object spawn failed: {result.Message}");
            return;
        }

        networkSync.SyncObjectSpawnResult(result);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private ClientRpcParams SendAllExceptHostOrDefault()
    {
        if (ConnectionManager.instance != null)
            return ConnectionManager.instance.SendAllExceptHost();

        return default;
    }

    public MapLayerLogic GetLayer(MapLayerType type)
    {
        return worldMapLayers?.GetLayer(type);
    }

    public MapLayerGraphics GetGraphics(MapLayerType type)
    {
        return worldMapLayers?.GetGraphics(type);
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

        worldMapLayers?.ClearTilemaps();

        InitLayers();
        InitLayerEventRouter();

        InitObjectRegistry();
        InitObjectSpawner();

        InitPlacementValidator();
        InitWorldMapService();
        InitNetworkSync();

        InitWorldLootService();

        if (runMode == WorldMapRunMode.Offline || IsServer)
            SubscribeAuthoritativeLayerEvents();
    }

    public void ClearLocalTilemapsAndLayerData()
    {
        worldMapLayers?.ClearTilemaps();
        worldMapLayers?.ClearLayerData();
    }

    [Button]
    public void ClearTilemapsForServer()
    {
        networkSync.ClearTilemapsClientRpc();
    }


}