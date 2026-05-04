    public enum PlacementFailReason
    {
        None,

        MissingBlockData,
        MissingBlockLibrary,
        MissingTargetLayer,

        OutOfBounds,
        Occupied,
        MissingBaseGround,
        InvalidLayerRule,

        LayerRejected
    }