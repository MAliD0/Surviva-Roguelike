namespace World
{
    public enum PlacementFailReason
    {
        None,

        MissingBlockData,
        MissingTargetLayer,

        OutOfBounds,
        Occupied,
        MissingBaseGround,
        InvalidLayerRule,

        LayerRejected
    }
}