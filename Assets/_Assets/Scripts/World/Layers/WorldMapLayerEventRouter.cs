using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldMapLayerEventRouter
{
    private readonly Func<MapLayerType, MapLayerLogic> getLayer;

    private readonly Action<
        MapLayerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>>,
        MapBlockData
    > onTilePlaced;

    private readonly Action<
        MapLayerType,
        Dictionary<Vector2Int, HashSet<Vector2Int>>,
        MapBlockType
    > onTileRemoved;

    private bool subscribed;

    public WorldMapLayerEventRouter(
        Func<MapLayerType, MapLayerLogic> getLayer,
        Action<MapLayerType, Dictionary<Vector2Int, HashSet<Vector2Int>>, MapBlockData> onTilePlaced,
        Action<MapLayerType, Dictionary<Vector2Int, HashSet<Vector2Int>>, MapBlockType> onTileRemoved
    )
    {
        this.getLayer = getLayer;
        this.onTilePlaced = onTilePlaced;
        this.onTileRemoved = onTileRemoved;
    }

    public void Subscribe()
    {
        if (subscribed)
            return;

        subscribed = true;

        MapLayerLogic baseLayer = getLayer?.Invoke(MapLayerType.backGround);
        MapLayerLogic foreLayer = getLayer?.Invoke(MapLayerType.foreGround);
        MapLayerLogic boatLayer = getLayer?.Invoke(MapLayerType.mediumLayer);
        MapLayerLogic onBoatLayer = getLayer?.Invoke(MapLayerType.circleElementLayer);

        if (baseLayer != null)
        {
            baseLayer.onMapTilePlaced += OnBaseLayerTilePlaced;
            baseLayer.onMapTileRemoved += OnBaseLayerTileRemoved;
        }

        if (foreLayer != null)
        {
            foreLayer.onMapTilePlaced += OnForeLayerTilePlaced;
            foreLayer.onMapTileRemoved += OnForeLayerTileRemoved;
        }

        if (boatLayer != null)
        {
            boatLayer.onMapTilePlaced += OnMediumLayerTilePlaced;
            boatLayer.onMapTileRemoved += OnMediumLayerTileRemoved;
        }

        if (onBoatLayer != null)
        {
            onBoatLayer.onMapTilePlaced += OnCircleElementLayerTilePlaced;
            onBoatLayer.onMapTileRemoved += OnCircleElemtLayerTileRemoved;
        }
    }

    public void Unsubscribe()
    {
        if (!subscribed)
            return;

        subscribed = false;

        MapLayerLogic baseLayer = getLayer?.Invoke(MapLayerType.backGround);
        MapLayerLogic foreLayer = getLayer?.Invoke(MapLayerType.foreGround);
        MapLayerLogic mediumLayer = getLayer?.Invoke(MapLayerType.mediumLayer);
        MapLayerLogic circleElementLayer = getLayer?.Invoke(MapLayerType.circleElementLayer);

        if (baseLayer != null)
        {
            baseLayer.onMapTilePlaced -= OnBaseLayerTilePlaced;
            baseLayer.onMapTileRemoved -= OnBaseLayerTileRemoved;
        }

        if (foreLayer != null)
        {
            foreLayer.onMapTilePlaced -= OnForeLayerTilePlaced;
            foreLayer.onMapTileRemoved -= OnForeLayerTileRemoved;
        }

        if (mediumLayer != null)
        {
            mediumLayer.onMapTilePlaced -= OnMediumLayerTilePlaced;
            mediumLayer.onMapTileRemoved -= OnMediumLayerTileRemoved;
        }

        if (circleElementLayer != null)
        {
            circleElementLayer.onMapTilePlaced -= OnCircleElementLayerTilePlaced;
            circleElementLayer.onMapTileRemoved -= OnCircleElemtLayerTileRemoved;
        }
    }

    private void OnBaseLayerTilePlaced(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        onTilePlaced?.Invoke(MapLayerType.backGround, cells, data);
    }

    private void OnForeLayerTilePlaced(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        onTilePlaced?.Invoke(MapLayerType.foreGround, cells, data);
    }

    private void OnMediumLayerTilePlaced(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        onTilePlaced?.Invoke(MapLayerType.mediumLayer, cells, data);
    }

    private void OnCircleElementLayerTilePlaced(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        onTilePlaced?.Invoke(MapLayerType.circleElementLayer, cells, data);
    }

    private void OnBaseLayerTileRemoved(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockType type
    )
    {
        onTileRemoved?.Invoke(MapLayerType.backGround, cells, type);
    }

    private void OnForeLayerTileRemoved(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockType type
    )
    {
        onTileRemoved?.Invoke(MapLayerType.foreGround, cells, type);
    }

    private void OnMediumLayerTileRemoved(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockType type
    )
    {
        onTileRemoved?.Invoke(MapLayerType.mediumLayer, cells, type);
    }

    private void OnCircleElemtLayerTileRemoved(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockType type
    )
    {
        onTileRemoved?.Invoke(MapLayerType.circleElementLayer, cells, type);
    }
}