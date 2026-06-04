using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class GhostBuildManager : MonoBehaviour
{
    public static GhostBuildManager Instance { get; private set; }

    [Header("Ghost Layer")]
    [SerializeField] private MapLayerLogic ghostLayerLogic;
    [SerializeField] private MapLayerGraphics ghostLayerGraphics;

    [FoldoutGroup("Colours")]
    [SerializeField] private Color possiblePlacementColor = new Color(0f, 1f, 0f, 0.5f);

    [FoldoutGroup("Colours")]
    [SerializeField] private Color impossiblePlacementColor = new Color(1f, 0f, 0f, 0.5f);

    [SerializeField, ReadOnly] private Color currentColor;

    [Header("Debug")]
    [SerializeField, ReadOnly] private Vector2Int lastAnchorPoint;
    [SerializeField, ReadOnly] private Vector2Int lastSubtilePoint;
    [SerializeField, ReadOnly] private PlacementFailReason lastFailReason;
    [SerializeField, ReadOnly] private string lastFailMessage;

    private MapBlockData lastBlockData;
    private bool hasGhost;

    private static readonly Vector2Int InvalidCell = new Vector2Int(int.MinValue, int.MinValue);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeGhostLayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (ghostLayerLogic != null)
            ghostLayerLogic.onMapTilePlaced -= OnGhostTilePlaced;
    }

    private void InitializeGhostLayer()
    {
        ghostLayerLogic = new MapLayerLogic(
            new MapBounds(-127, 127, -127, 127, false)
        );

        if (ghostLayerGraphics == null)
        {
            Debug.LogError("[GhostBuildManager] ghostLayerGraphics is missing.");
            return;
        }

        ghostLayerGraphics.Init(ghostLayerLogic);
        ghostLayerLogic.onMapTilePlaced += OnGhostTilePlaced;

        ResetLastPosition();
    }

    public bool PlaceGhost(Vector2 worldPosition, MapBlockData blockData)
    {
        if (blockData == null)
        {
            ClearGhost();
            return false;
        }

        if (WorldMapManager.Instance == null)
        {
            ClearGhost();
            return false;
        }

        Vector2Int newAnchorPoint = ghostLayerLogic.WorldToCell(worldPosition);
        Vector2Int newSubtilePoint = ghostLayerLogic.WorldToLocalSubtile(worldPosition);

        PlacementResult placementResult = WorldMapManager.Instance.GetPlacementResult(
            worldPosition,
            blockData.GetItemID()
        );

        bool placementSucceeded = placementResult.Success;

        bool samePosition =
            hasGhost &&
            lastBlockData == blockData &&
            lastAnchorPoint == newAnchorPoint &&
            lastSubtilePoint == newSubtilePoint &&
            lastFailReason == placementResult.Reason;

        if (samePosition)
            return placementSucceeded;

        lastBlockData = blockData;
        lastAnchorPoint = newAnchorPoint;
        lastSubtilePoint = newSubtilePoint;
        lastFailReason = placementResult.Reason;
        lastFailMessage = placementResult.Message;

        RebuildGhost(worldPosition, blockData, placementResult);

        return placementSucceeded;
    }

    public void ClearGhost()
    {
        if (ghostLayerLogic == null)
            return;

        ghostLayerLogic.RemoveAllTiles();

        hasGhost = false;
        lastBlockData = null;
        lastFailReason = PlacementFailReason.None;
        lastFailMessage = string.Empty;

        ResetLastPosition();
    }

    private void RebuildGhost(
        Vector2 worldPosition,
        MapBlockData blockData,
        PlacementResult placementResult
    )
    {
        ghostLayerLogic.RemoveAllTiles();

        currentColor = placementResult.Success
            ? possiblePlacementColor
            : impossiblePlacementColor;

        if (ghostLayerGraphics != null && ghostLayerGraphics.LayerVisualsTiles != null)
            ghostLayerGraphics.LayerVisualsTiles.color = currentColor;

        bool ghostPlaced = ghostLayerLogic.PlaceBlock(worldPosition, blockData);

        if (!ghostPlaced)
        {
            Debug.LogWarning(
                $"[GhostBuildManager] Ghost layer failed to place preview for {blockData.GetItemID()} at {worldPosition}."
            );
        }

        if (!placementResult.Success)
            Debug.Log(placementResult.ToString());

        hasGhost = true;
    }

    private void OnGhostTilePlaced(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        if (data == null)
            return;

        if (data.mapBlockType != MapBlockType.GameObject)
            return;

        if (!TryGetFirstCell(cells, out Vector2Int anchorTile, out Vector2Int anchorSubtile))
            return;

        Vector2 worldPosition = GetGhostObjectSpawnPosition(
            anchorTile,
            anchorSubtile,
            data
        );        
        GameObject prefab = WorldMapManager.Instance.blockLibrary
            .GetById(data.GetItemID())
            ?.gameObject;

        if (prefab == null)
        {
            Debug.LogError($"[GhostBuildManager] Prefab id={data.GetItemID()} not found.");
            return;
        }

        GameObject ghostObject = Instantiate(prefab, worldPosition, Quaternion.identity);

        PrepareGhostObject(ghostObject);

        foreach (Vector2Int tileAnchor in cells.Keys)
        {
            ghostLayerGraphics.BindObject(
                tileAnchor,
                cells[tileAnchor].ToList(),
                ghostObject,
                "ghostBlock"
            );
        }
    }

    private void PrepareGhostObject(GameObject ghostObject)
    {
        try
        {
            ghostObject.tag = "Ghost";
        }
        catch
        {
            Debug.LogWarning("[GhostBuildManager] Tag 'Ghost' does not exist. Add it in Unity Tags or remove this assignment.");
        }
        
        SpriteRenderer[] renderers = ghostObject.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.color = currentColor;
        }

        Collider2D[] colliders = ghostObject.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        Rigidbody2D[] rigidbodies = ghostObject.GetComponentsInChildren<Rigidbody2D>(true);

        foreach (Rigidbody2D rb in rigidbodies)
        {
            rb.simulated = false;
        }

        MonoBehaviour[] behaviours = ghostObject.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            behaviour.enabled = false;
        }
    }

    private bool TryGetFirstCell(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        out Vector2Int tile,
        out Vector2Int subtile
    )
    {
        foreach (var pair in cells)
        {
            foreach (Vector2Int localSubtile in pair.Value)
            {
                tile = pair.Key;
                subtile = localSubtile;
                return true;
            }
        }

        tile = default;
        subtile = default;
        return false;
    }
    private Vector2 GetGhostObjectSpawnPosition(
        Vector2Int tileAnchor,
        Vector2Int subtileAnchor,
        MapBlockData blockData
    )
    {
        Vector2 basePosition = ghostLayerLogic.SubtileToWorldPosition(tileAnchor, subtileAnchor);

        if (blockData == null || !blockData.gridAligned)
            return basePosition;

        Vector2 size = new Vector2(
            Mathf.Max(1, blockData.blockSize.x),
            Mathf.Max(1, blockData.blockSize.y)
        );

        return basePosition + size * 0.5f;
    }
    private void ResetLastPosition()
    {
        lastAnchorPoint = InvalidCell;
        lastSubtilePoint = InvalidCell;
    }
}