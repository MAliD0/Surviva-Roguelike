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
        MapLayerLogic boatLayer = getLayer?.Invoke(MapLayerType.boatGround);
        MapLayerLogic onBoatLayer = getLayer?.Invoke(MapLayerType.onBoatGround);

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
            boatLayer.onMapTilePlaced += OnBoatLayerTilePlaced;
            boatLayer.onMapTileRemoved += OnBoatLayerTileRemoved;
        }

        if (onBoatLayer != null)
        {
            onBoatLayer.onMapTilePlaced += OnOnBoatLayerTilePlaced;
            onBoatLayer.onMapTileRemoved += OnOnBoatLayerTileRemoved;
        }
    }

    public void Unsubscribe()
    {
        if (!subscribed)
            return;

        subscribed = false;

        MapLayerLogic baseLayer = getLayer?.Invoke(MapLayerType.backGround);
        MapLayerLogic foreLayer = getLayer?.Invoke(MapLayerType.foreGround);
        MapLayerLogic boatLayer = getLayer?.Invoke(MapLayerType.boatGround);
        MapLayerLogic onBoatLayer = getLayer?.Invoke(MapLayerType.onBoatGround);

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

        if (boatLayer != null)
        {
            boatLayer.onMapTilePlaced -= OnBoatLayerTilePlaced;
            boatLayer.onMapTileRemoved -= OnBoatLayerTileRemoved;
        }

        if (onBoatLayer != null)
        {
            onBoatLayer.onMapTilePlaced -= OnOnBoatLayerTilePlaced;
            onBoatLayer.onMapTileRemoved -= OnOnBoatLayerTileRemoved;
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

    private void OnBoatLayerTilePlaced(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        onTilePlaced?.Invoke(MapLayerType.boatGround, cells, data);
    }

    private void OnOnBoatLayerTilePlaced(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockData data
    )
    {
        onTilePlaced?.Invoke(MapLayerType.onBoatGround, cells, data);
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

    private void OnBoatLayerTileRemoved(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockType type
    )
    {
        onTileRemoved?.Invoke(MapLayerType.boatGround, cells, type);
    }

    private void OnOnBoatLayerTileRemoved(
        Dictionary<Vector2Int, HashSet<Vector2Int>> cells,
        MapBlockType type
    )
    {
        onTileRemoved?.Invoke(MapLayerType.onBoatGround, cells, type);
    }
}