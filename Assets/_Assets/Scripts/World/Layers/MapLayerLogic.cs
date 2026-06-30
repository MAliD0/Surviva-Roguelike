using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MapLayerResolution
{
    Tile,
    Subtile
}

/// <summary>
/// Logical storage for one map layer.
///
/// Internally, all layers use:
/// LayerTiles[tileCoordinate][localSubtileCoordinate] = MapTile.
///
/// Tile-only layers expose a simpler full-tile API and normalize placement
/// to local subtile (0,0).
/// </summary>
[Serializable]
public class MapLayerLogic
{
    public SerializedDictionary<
        Vector2Int,
        SerializedDictionary<Vector2Int, MapTile>
    > LayerTiles;

    public SerializedDictionary<
        Vector2Int,
        SerializedDictionary<Vector2Int, int>
    > anchorHp;

    public event Action<
        Dictionary<Vector2Int, HashSet<Vector2Int>>,
        MapBlockData
    > onMapTilePlaced;

    public event Action<
        Dictionary<Vector2Int, HashSet<Vector2Int>>,
        MapBlockType
    > onMapTileRemoved;

    public event Action onAllTilesRemoved;

    public event Action<
        Vector2Int,
        Vector2Int,
        MapBlockData,
        int,
        int
    > onTileHealthChanged;

    public MapLayerResolution Resolution { get; }

    public bool AllowsSubtilePlacement =>
        Resolution == MapLayerResolution.Subtile;

    private readonly MapBounds _bounds;

    private const float CellSize = 0.125f;
    private const int SubtilesPerCell = 8;

    private static readonly Vector2Int FullTileSubtile = Vector2Int.zero;

    /// <summary>
    /// Compatibility constructor.
    /// Old layers continue supporting subtiles by default.
    /// </summary>
    public MapLayerLogic(int mapWidth, int mapHeight)
        : this(
            new MapBounds(
                0,
                mapWidth - 1,
                0,
                mapHeight - 1,
                useBounds: true
            ),
            MapLayerResolution.Subtile
        )
    {
    }

    public MapLayerLogic(
        int mapWidth,
        int mapHeight,
        MapLayerResolution resolution
    )
        : this(
            new MapBounds(
                0,
                mapWidth - 1,
                0,
                mapHeight - 1,
                useBounds: true
            ),
            resolution
        )
    {
    }

    /// <summary>
    /// Compatibility constructor.
    /// Subtile placement remains enabled unless explicitly disabled.
    /// </summary>
    public MapLayerLogic(MapBounds bounds)
        : this(bounds, MapLayerResolution.Subtile)
    {
    }

    public MapLayerLogic(
        MapBounds bounds,
        MapLayerResolution resolution
    )
    {
        _bounds = bounds;
        Resolution = resolution;

        LayerTiles = new SerializedDictionary<
            Vector2Int,
            SerializedDictionary<Vector2Int, MapTile>
        >();

        anchorHp = new SerializedDictionary<
            Vector2Int,
            SerializedDictionary<Vector2Int, int>
        >();
    }

    #region Reading

    public MapTile GetMapTile(Vector2 worldPosition)
    {
        Vector2Int tile = WorldToCell(worldPosition);
        Vector2Int subtile = WorldToPlacementSubtile(worldPosition);

        return GetMapTile(tile, subtile);
    }

    public MapTile GetMapTile(
        Vector2Int tile,
        Vector2Int subtile
    )
    {
        subtile = NormalizeSubtile(subtile);

        if (!LayerTiles.TryGetValue(tile, out var inner))
            return null;

        if (inner == null)
            return null;

        inner.TryGetValue(subtile, out MapTile value);
        return value;
    }

    public bool TryGetMapTile(
        Vector2Int tile,
        Vector2Int subtile,
        out MapTile mapTile
    )
    {
        mapTile = null;
        subtile = NormalizeSubtile(subtile);

        if (!LayerTiles.TryGetValue(tile, out var inner))
            return false;

        if (inner == null)
            return false;

        return inner.TryGetValue(subtile, out mapTile);
    }

    /// <summary>
    /// Gets the logical block occupying an entire tile.
    ///
    /// This method is intended for tile-only layers.
    /// </summary>
    public bool TryGetFullTile(
        Vector2Int tilePosition,
        out MapTile mapTile
    )
    {
        mapTile = null;

        if (Resolution != MapLayerResolution.Tile)
            return false;

        if (!LayerTiles.TryGetValue(tilePosition, out var subtiles))
            return false;

        if (subtiles == null || subtiles.Count == 0)
            return false;

        if (subtiles.TryGetValue(FullTileSubtile, out mapTile))
            return true;

        // Defensive fallback for old serialized data.
        foreach (MapTile value in subtiles.Values)
        {
            mapTile = value;
            return mapTile != null;
        }

        return false;
    }

    /// <summary>
    /// Returns a simple tile coordinate -> MapTile view.
    ///
    /// The returned dictionary is a copy. Editing it does not edit the layer.
    /// </summary>
    public Dictionary<Vector2Int, MapTile> GetFullTileDictionary()
    {
        Dictionary<Vector2Int, MapTile> result = new();

        if (Resolution != MapLayerResolution.Tile)
        {
            Debug.LogWarning(
                "[MapLayerLogic] GetFullTileDictionary was called " +
                "on a subtile layer."
            );

            return result;
        }

        foreach (var tilePair in LayerTiles)
        {
            if (TryGetFullTile(tilePair.Key, out MapTile mapTile))
                result[tilePair.Key] = mapTile;
        }

        return result;
    }

    public IEnumerable<KeyValuePair<Vector2Int, MapTile>>
        EnumerateFullTiles()
    {
        if (Resolution != MapLayerResolution.Tile)
            yield break;

        foreach (Vector2Int tilePosition in LayerTiles.Keys)
        {
            if (TryGetFullTile(tilePosition, out MapTile mapTile))
            {
                yield return new KeyValuePair<Vector2Int, MapTile>(
                    tilePosition,
                    mapTile
                );
            }
        }
    }

    public bool IsTilePresented(Vector2 worldPosition)
    {
        Vector2Int tile = WorldToCell(worldPosition);
        Vector2Int subtile = WorldToPlacementSubtile(worldPosition);

        return IsSubTilePresented(tile, subtile);
    }

    public bool IsTilePresented(
        Vector2 worldPosition,
        int sizeX,
        int sizeY
    )
    {
        Vector2Int tile = WorldToCell(worldPosition);
        Vector2Int subtile = WorldToPlacementSubtile(worldPosition);

        return IsFootprintOccupied(
            tile,
            subtile,
            sizeX,
            sizeY
        );
    }

    public bool IsSubTilePresented(
        Vector2Int tile,
        Vector2Int subtile
    )
    {
        subtile = NormalizeSubtile(subtile);

        if (!LayerTiles.TryGetValue(tile, out var inner))
            return false;

        return inner.ContainsKey(subtile);
    }

    #endregion

    #region Coordinate conversion

    public Vector2Int WorldToCell(Vector2 worldPosition)
    {
        return UtillityMath.VectorToVectorInt(worldPosition);
    }

    public Vector2Int WorldToLocalSubtile(Vector2 worldPosition)
    {
        Vector2Int cell = WorldToCell(worldPosition);

        float relativeX = worldPosition.x - cell.x;
        float relativeY = worldPosition.y - cell.y;

        int localX = Mathf.FloorToInt(relativeX / CellSize);
        int localY = Mathf.FloorToInt(relativeY / CellSize);

        localX = Mathf.Clamp(localX, 0, SubtilesPerCell - 1);
        localY = Mathf.Clamp(localY, 0, SubtilesPerCell - 1);

        return new Vector2Int(localX, localY);
    }

    public Vector2Int WorldToPlacementSubtile(Vector2 worldPosition)
    {
        if (!AllowsSubtilePlacement)
            return FullTileSubtile;

        return WorldToLocalSubtile(worldPosition);
    }

    public (
        Vector2Int tileCell,
        Vector2Int localSubtile
    ) GetAnchorFromClick(Vector2 worldPosition)
    {
        return (
            WorldToCell(worldPosition),
            WorldToPlacementSubtile(worldPosition)
        );
    }

    private Vector2Int NormalizeSubtile(Vector2Int subtile)
    {
        if (!AllowsSubtilePlacement)
            return FullTileSubtile;

        return subtile;
    }

    #endregion

    #region Footprints

    public Dictionary<Vector2Int, HashSet<Vector2Int>>
        GetFootprintCellLocalPairs(
            Vector2Int tile,
            Vector2Int subtile,
            int sizeX,
            int sizeY,
            bool anchorIsTopLeft = false
        )
    {
        Dictionary<Vector2Int, HashSet<Vector2Int>> result = new();

        if (!AllowsSubtilePlacement)
        {
            result[tile] = CreateFullTileSubtileSet();
            return result;
        }

        Vector2Int baseCell = tile;
        Vector2Int baseLocal = subtile;

        int startLocalX;
        int startLocalY;

        if (anchorIsTopLeft)
        {
            startLocalX = baseLocal.x;
            startLocalY = baseLocal.y;
        }
        else
        {
            startLocalX =
                baseLocal.x - Mathf.FloorToInt(sizeX / 2f);

            startLocalY =
                baseLocal.y - Mathf.FloorToInt(sizeY / 2f);
        }

        for (int dx = 0; dx < sizeX; dx++)
        {
            for (int dy = 0; dy < sizeY; dy++)
            {
                int globalLocalX = startLocalX + dx;
                int globalLocalY = startLocalY + dy;

                int cellOffsetX = Mathf.FloorToInt(
                    globalLocalX / (float)SubtilesPerCell
                );

                int cellOffsetY = Mathf.FloorToInt(
                    globalLocalY / (float)SubtilesPerCell
                );

                int localX =
                    globalLocalX -
                    cellOffsetX * SubtilesPerCell;

                int localY =
                    globalLocalY -
                    cellOffsetY * SubtilesPerCell;

                Vector2Int tileCell = new(
                    baseCell.x + cellOffsetX,
                    baseCell.y + cellOffsetY
                );

                Vector2Int localPosition = new(
                    localX,
                    localY
                );

                if (!result.TryGetValue(tileCell, out var localSet))
                {
                    localSet = new HashSet<Vector2Int>();
                    result.Add(tileCell, localSet);
                }

                localSet.Add(localPosition);
            }
        }

        return result;
    }

    public Dictionary<Vector2Int, HashSet<Vector2Int>>
        GetFootprintCellLocalPairs(
            Vector2 worldPosition,
            int sizeX,
            int sizeY,
            bool anchorIsTopLeft = false
        )
    {
        Vector2Int baseCell = WorldToCell(worldPosition);
        Vector2Int baseLocal =
            WorldToPlacementSubtile(worldPosition);

        return GetFootprintCellLocalPairs(
            baseCell,
            baseLocal,
            sizeX,
            sizeY,
            anchorIsTopLeft
        );
    }

    public bool IsFootprintFullyOccupied(
        Dictionary<Vector2Int, HashSet<Vector2Int>> pairs,
        int sizeX,
        int sizeY,
        bool anchorIsTopLeft = false
    )
    {
        foreach (var tilePair in pairs)
        {
            if (!_bounds.Contains(tilePair.Key))
                return false;

            foreach (Vector2Int localPosition in tilePair.Value)
            {
                if (!IsSubTilePresented(
                        tilePair.Key,
                        localPosition
                    ))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsFootprintOccupied(
        Dictionary<Vector2Int, HashSet<Vector2Int>> pairs,
        int sizeX,
        int sizeY,
        bool anchorIsTopLeft = false
    )
    {
        foreach (var tilePair in pairs)
        {
            if (!_bounds.Contains(tilePair.Key))
                return true;

            foreach (Vector2Int localPosition in tilePair.Value)
            {
                if (IsSubTilePresented(
                        tilePair.Key,
                        localPosition
                    ))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsFootprintFullyOccupied(
        Vector2 worldPosition,
        int sizeX,
        int sizeY,
        bool anchorIsTopLeft = false
    )
    {
        var pairs = GetFootprintCellLocalPairs(
            worldPosition,
            sizeX,
            sizeY,
            anchorIsTopLeft
        );

        return IsFootprintFullyOccupied(
            pairs,
            sizeX,
            sizeY,
            anchorIsTopLeft
        );
    }

    public bool IsFootprintOccupied(
        Vector2Int tile,
        Vector2Int subtile,
        int sizeX,
        int sizeY,
        bool anchorIsTopLeft = false
    )
    {
        var pairs = GetFootprintCellLocalPairs(
            tile,
            subtile,
            sizeX,
            sizeY,
            anchorIsTopLeft
        );

        return IsFootprintOccupied(
            pairs,
            sizeX,
            sizeY,
            anchorIsTopLeft
        );
    }

    public bool IsFootprintOccupied(
        Vector2 worldPosition,
        int sizeX,
        int sizeY,
        bool anchorIsTopLeft = false
    )
    {
        var pairs = GetFootprintCellLocalPairs(
            worldPosition,
            sizeX,
            sizeY,
            anchorIsTopLeft
        );

        return IsFootprintOccupied(
            pairs,
            sizeX,
            sizeY,
            anchorIsTopLeft
        );
    }

    #endregion

    #region Placement

    public bool CanBePlaced(
        Vector2 worldPosition,
        MapBlockData data
    )
    {
        return CanBePlaced(
            WorldToCell(worldPosition),
            WorldToPlacementSubtile(worldPosition),
            data
        );
    }

    public bool CanBePlaced(
        Vector2Int tile,
        Vector2Int subtile,
        MapBlockData data
    )
    {
        if (data == null)
            return false;

        if (!_bounds.Contains(tile))
            return false;

        subtile = NormalizeSubtile(subtile);

        if (!AllowsSubtilePlacement)
        {
            if (data.mapBlockType == MapBlockType.GameObject &&
                !data.gridAligned)
            {
                Debug.LogWarning(
                    $"[MapLayerLogic] {data.GetItemID()} cannot be " +
                    "placed on a tile-only layer because it is not " +
                    "grid aligned."
                );

                return false;
            }

            return !LayerTiles.ContainsKey(tile);
        }

        if (data.mapBlockType == MapBlockType.Tile)
            return !LayerTiles.ContainsKey(tile);

        if (data.mapBlockType == MapBlockType.GameObject)
        {
            if (data.gridAligned)
                return !LayerTiles.ContainsKey(tile);

            return !IsFootprintOccupied(
                tile,
                subtile,
                data.blockSize.x,
                data.blockSize.y,
                anchorIsTopLeft: false
            );
        }

        return false;
    }

    public bool PlaceBlock(
        Vector2Int tileIndex,
        Vector2Int subtileIndex,
        MapBlockData data
    )
    {
        subtileIndex = NormalizeSubtile(subtileIndex);

        if (!CanBePlaced(tileIndex, subtileIndex, data))
            return false;

        Vector2Int anchorTile = tileIndex;
        Vector2Int anchorSubtile = subtileIndex;

        Dictionary<Vector2Int, HashSet<Vector2Int>> group;

        bool occupiesWholeTile =
            !AllowsSubtilePlacement ||
            data.mapBlockType == MapBlockType.Tile ||
            (
                data.mapBlockType == MapBlockType.GameObject &&
                data.gridAligned
            );

        if (occupiesWholeTile)
        {
            anchorSubtile = FullTileSubtile;

            group = PlaceWholeTile(
                tileIndex,
                data,
                anchorTile,
                anchorSubtile
            );
        }
        else
        {
            group = PlaceSubtileFootprint(
                tileIndex,
                subtileIndex,
                data,
                anchorTile,
                anchorSubtile
            );
        }

        if (data.breakable)
        {
            SetHealth(
                anchorTile,
                anchorSubtile,
                data,
                Mathf.Max(1, data.maxHealth),
                fireEvent: true
            );
        }

        onMapTilePlaced?.Invoke(group, data);
        return true;
    }

    public bool PlaceBlock(
        Vector2 worldPosition,
        MapBlockData data
    )
    {
        return PlaceBlock(
            WorldToCell(worldPosition),
            WorldToPlacementSubtile(worldPosition),
            data
        );
    }

    private Dictionary<Vector2Int, HashSet<Vector2Int>>
        PlaceWholeTile(
            Vector2Int tile,
            MapBlockData data,
            Vector2Int anchorTile,
            Vector2Int anchorSubtile
        )
    {
        EnsureTileStorage(tile);

        HashSet<Vector2Int> occupiedSubtiles =
            CreateFullTileSubtileSet();

        foreach (Vector2Int localSubtile in occupiedSubtiles)
        {
            LayerTiles[tile][localSubtile] = new MapTile(
                localSubtile,
                data,
                tile,
                anchorSubtile,
                anchorTile
            );
        }

        return new Dictionary<
            Vector2Int,
            HashSet<Vector2Int>
        >
        {
            [tile] = occupiedSubtiles
        };
    }

    private Dictionary<Vector2Int, HashSet<Vector2Int>>
        PlaceSubtileFootprint(
            Vector2Int tile,
            Vector2Int subtile,
            MapBlockData data,
            Vector2Int anchorTile,
            Vector2Int anchorSubtile
        )
    {
        var group = GetFootprintCellLocalPairs(
            tile,
            subtile,
            data.blockSize.x,
            data.blockSize.y,
            anchorIsTopLeft: false
        );

        foreach (var tilePair in group)
        {
            EnsureTileStorage(tilePair.Key);

            foreach (Vector2Int localSubtile in tilePair.Value)
            {
                LayerTiles[tilePair.Key][localSubtile] =
                    new MapTile(
                        localSubtile,
                        data,
                        tilePair.Key,
                        anchorSubtile,
                        anchorTile
                    );
            }
        }

        return group;
    }

    #endregion

    #region Removal

    public void RemoveTile(
        Vector2Int clickedTile,
        Vector2Int clickedSubtile
    )
    {
        clickedSubtile = NormalizeSubtile(clickedSubtile);

        if (!LayerTiles.TryGetValue(
                clickedTile,
                out var subtiles
            ))
        {
            return;
        }

        if (!subtiles.TryGetValue(
                clickedSubtile,
                out MapTile clickedMapTile
            ))
        {
            return;
        }

        MapBlockData data = clickedMapTile.BlockData;

        Vector2Int anchorTile =
            clickedMapTile.TileAnchor;

        Vector2Int anchorSubtile =
            clickedMapTile.SubtileAnchor;

        Dictionary<Vector2Int, HashSet<Vector2Int>>
            occupiedTiles;

        bool occupiesWholeTile =
            !AllowsSubtilePlacement ||
            data.mapBlockType == MapBlockType.Tile ||
            data.gridAligned;

        if (occupiesWholeTile)
        {
            if (!LayerTiles.TryGetValue(
                    anchorTile,
                    out var tileSubtiles
                ))
            {
                return;
            }

            occupiedTiles = new Dictionary<
                Vector2Int,
                HashSet<Vector2Int>
            >
            {
                [anchorTile] =
                    tileSubtiles.Keys.ToHashSet()
            };

            LayerTiles.Remove(anchorTile);
        }
        else
        {
            occupiedTiles = GetFootprintCellLocalPairs(
                anchorTile,
                anchorSubtile,
                data.blockSize.x,
                data.blockSize.y
            );

            foreach (var tilePair in occupiedTiles)
            {
                if (!LayerTiles.TryGetValue(
                        tilePair.Key,
                        out var storedSubtiles
                    ))
                {
                    continue;
                }

                foreach (Vector2Int localSubtile in tilePair.Value)
                    storedSubtiles.Remove(localSubtile);

                if (storedSubtiles.Count == 0)
                    LayerTiles.Remove(tilePair.Key);
            }
        }

        RemoveHealth(anchorTile, anchorSubtile);

        onMapTileRemoved?.Invoke(
            occupiedTiles,
            data.mapBlockType
        );
    }

    public void RemoveAllTiles()
    {
        LayerTiles = new SerializedDictionary<
            Vector2Int,
            SerializedDictionary<Vector2Int, MapTile>
        >();

        anchorHp = new SerializedDictionary<
            Vector2Int,
            SerializedDictionary<Vector2Int, int>
        >();

        onAllTilesRemoved?.Invoke();
    }

    #endregion

    #region Health

    public int GetHealth(
        Vector2Int anchor,
        Vector2Int subtile
    )
    {
        subtile = NormalizeSubtile(subtile);

        if (!anchorHp.TryGetValue(anchor, out var healthBySubtile))
            return -1;

        if (!healthBySubtile.TryGetValue(subtile, out int hp))
            return -1;

        return hp;
    }

    public void SetHealth(
        Vector2Int anchor,
        Vector2Int subtile,
        MapBlockData data,
        int hp,
        bool fireEvent = true
    )
    {
        if (data == null || !data.breakable)
            return;

        subtile = NormalizeSubtile(subtile);

        int maximumHealth = Mathf.Max(1, data.maxHealth);
        hp = Mathf.Clamp(hp, 0, maximumHealth);

        if (!anchorHp.TryGetValue(
                anchor,
                out var healthBySubtile
            ))
        {
            healthBySubtile =
                new SerializedDictionary<Vector2Int, int>();

            anchorHp.Add(anchor, healthBySubtile);
        }

        if (hp <= 0)
        {
            healthBySubtile.Remove(subtile);

            if (healthBySubtile.Count == 0)
                anchorHp.Remove(anchor);
        }
        else
        {
            healthBySubtile[subtile] = hp;
        }

        if (fireEvent)
        {
            onTileHealthChanged?.Invoke(
                anchor,
                subtile,
                data,
                hp,
                maximumHealth
            );
        }
    }

    public bool Damage(
        Vector2Int anchor,
        Vector2Int subtile,
        MapBlockData data,
        int amount
    )
    {
        if (data == null || !data.breakable)
            return false;

        subtile = NormalizeSubtile(subtile);

        int maximumHealth = Mathf.Max(1, data.maxHealth);
        int currentHealth = GetHealth(anchor, subtile);

        if (currentHealth < 0)
            currentHealth = maximumHealth;

        int nextHealth = Mathf.Clamp(
            currentHealth - Mathf.Max(1, amount),
            0,
            maximumHealth
        );

        SetHealth(
            anchor,
            subtile,
            data,
            nextHealth,
            fireEvent: true
        );

        return nextHealth <= 0;
    }

    private void RemoveHealth(
        Vector2Int anchor,
        Vector2Int subtile
    )
    {
        subtile = NormalizeSubtile(subtile);

        if (!anchorHp.TryGetValue(anchor, out var healthBySubtile))
            return;

        healthBySubtile.Remove(subtile);

        if (healthBySubtile.Count == 0)
            anchorHp.Remove(anchor);
    }

    #endregion

    #region World positions

    public Vector2 SubtileToWorldPosition(
        Vector2Int tileCell,
        Vector2Int localSubtile,
        bool center = true
    )
    {
        if (!AllowsSubtilePlacement)
        {
            return new Vector2(
                tileCell.x + (center ? 0.5f : 0f),
                tileCell.y + (center ? 0.5f : 0f)
            );
        }

        int cellOffsetX = Mathf.FloorToInt(
            localSubtile.x / (float)SubtilesPerCell
        );

        int cellOffsetY = Mathf.FloorToInt(
            localSubtile.y / (float)SubtilesPerCell
        );

        int localX =
            localSubtile.x -
            cellOffsetX * SubtilesPerCell;

        int localY =
            localSubtile.y -
            cellOffsetY * SubtilesPerCell;

        Vector2Int resultCell = new(
            tileCell.x + cellOffsetX,
            tileCell.y + cellOffsetY
        );

        float centerOffset = center
            ? CellSize * 0.5f
            : 0f;

        float x =
            resultCell.x +
            localX * CellSize +
            centerOffset;

        float y =
            resultCell.y +
            localY * CellSize +
            centerOffset;

        return new Vector2(x, y);
    }

    #endregion

    #region Internal helpers

    private void EnsureTileStorage(Vector2Int tile)
    {
        if (LayerTiles.ContainsKey(tile))
            return;

        LayerTiles.Add(
            tile,
            new SerializedDictionary<Vector2Int, MapTile>()
        );
    }

    private static HashSet<Vector2Int>
        CreateFullTileSubtileSet()
    {
        HashSet<Vector2Int> result = new();

        for (int x = 0; x < SubtilesPerCell; x++)
        {
            for (int y = 0; y < SubtilesPerCell; y++)
                result.Add(new Vector2Int(x, y));
        }

        return result;
    }

    #endregion
}

