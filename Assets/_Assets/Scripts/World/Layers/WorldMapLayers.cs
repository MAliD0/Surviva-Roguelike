using UnityEngine;

public class WorldMapLayers
{
    private readonly MapLayerGraphics baseLayerGraphics;
    private readonly MapLayerGraphics foreLayerGraphics;
    private readonly MapLayerGraphics boatLayerGraphics;
    private readonly MapLayerGraphics onBoatLayerGraphics;

    public MapLayerLogic BaseLayer { get; private set; }
    public MapLayerLogic ForeLayer { get; private set; }
    public MapLayerLogic BoatLayer { get; private set; }
    public MapLayerLogic OnBoatLayer { get; private set; }

    public WorldMapLayers(
        MapBounds bounds,
        MapLayerGraphics baseLayerGraphics,
        MapLayerGraphics foreLayerGraphics,
        MapLayerGraphics boatLayerGraphics,
        MapLayerGraphics onBoatLayerGraphics
    )
    {
        this.baseLayerGraphics = baseLayerGraphics;
        this.foreLayerGraphics = foreLayerGraphics;
        this.boatLayerGraphics = boatLayerGraphics;
        this.onBoatLayerGraphics = onBoatLayerGraphics;

        Rebuild(bounds);
    }

    public void Rebuild(MapBounds bounds)
    {
        CreateLayers(bounds);
        InitGraphics();
    }

    private void CreateLayers(MapBounds bounds)
    {
        BaseLayer = new MapLayerLogic(bounds);
        ForeLayer = new MapLayerLogic(bounds);
        BoatLayer = new MapLayerLogic(bounds);
        OnBoatLayer = new MapLayerLogic(bounds);
    }

    private void InitGraphics()
    {
        baseLayerGraphics?.Init(BaseLayer);
        foreLayerGraphics?.Init(ForeLayer);
        boatLayerGraphics?.Init(BoatLayer);
        onBoatLayerGraphics?.Init(OnBoatLayer);
    }

    public MapLayerLogic GetLayer(MapLayerType type)
    {
        switch (type)
        {
            case MapLayerType.backGround:
                return BaseLayer;

            case MapLayerType.foreGround:
                return ForeLayer;

            case MapLayerType.boatGround:
                return BoatLayer;

            case MapLayerType.onBoatGround:
                return OnBoatLayer;

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

    public void ClearTilemaps()
    {
        onBoatLayerGraphics?.ClearTilemap();
        baseLayerGraphics?.ClearTilemap();
        foreLayerGraphics?.ClearTilemap();
        boatLayerGraphics?.ClearTilemap();
    }

    public void ClearLayerData()
    {
        OnBoatLayer?.LayerTiles.Clear();
        BaseLayer?.LayerTiles.Clear();
        BoatLayer?.LayerTiles.Clear();
        ForeLayer?.LayerTiles.Clear();

        OnBoatLayer?.anchorHp.Clear();
        BaseLayer?.anchorHp.Clear();
        BoatLayer?.anchorHp.Clear();
        ForeLayer?.anchorHp.Clear();
    }
}