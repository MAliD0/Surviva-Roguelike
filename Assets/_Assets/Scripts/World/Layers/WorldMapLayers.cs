using UnityEngine;

public class WorldMapLayers
{
    private readonly MapLayerGraphics baseLayerGraphics;
    private readonly MapLayerGraphics foreLayerGraphics;
    private readonly MapLayerGraphics mediumLayerGraphics;
    private readonly MapLayerGraphics circleElementLayerGraphics;

    public MapLayerLogic BaseLayer { get; private set; }
    public MapLayerLogic ForeLayer { get; private set; }
    public MapLayerLogic MediumLayer { get; private set; }
    public MapLayerLogic CircleElementLayer { get; private set; }

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
        this.mediumLayerGraphics = boatLayerGraphics;
        this.circleElementLayerGraphics = onBoatLayerGraphics;

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
        MediumLayer = new MapLayerLogic(bounds, MapLayerResolution.Tile);
        CircleElementLayer = new MapLayerLogic(bounds, MapLayerResolution.Tile);
    }

    private void InitGraphics()
    {
        baseLayerGraphics?.Init(BaseLayer);
        foreLayerGraphics?.Init(ForeLayer);
        mediumLayerGraphics?.Init(MediumLayer);
        circleElementLayerGraphics?.Init(CircleElementLayer);
    }

    public MapLayerLogic GetLayer(MapLayerType type)
    {
        switch (type)
        {
            case MapLayerType.backGround:
                return BaseLayer;

            case MapLayerType.foreGround:
                return ForeLayer;

            case MapLayerType.mediumLayer:
                return MediumLayer;

            case MapLayerType.circleElementLayer:
                return CircleElementLayer;

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

            case MapLayerType.mediumLayer:
                return mediumLayerGraphics;

            case MapLayerType.circleElementLayer:
                return circleElementLayerGraphics;

            default:
                return baseLayerGraphics;
        }
    }

    public void ClearTilemaps()
    {
        circleElementLayerGraphics?.ClearTilemap();
        baseLayerGraphics?.ClearTilemap();
        foreLayerGraphics?.ClearTilemap();
        mediumLayerGraphics?.ClearTilemap();
    }

    public void ClearLayerData()
    {
        CircleElementLayer?.LayerTiles.Clear();
        BaseLayer?.LayerTiles.Clear();
        MediumLayer?.LayerTiles.Clear();
        ForeLayer?.LayerTiles.Clear();

        CircleElementLayer?.anchorHp.Clear();
        BaseLayer?.anchorHp.Clear();
        MediumLayer?.anchorHp.Clear();
        ForeLayer?.anchorHp.Clear();
    }
}