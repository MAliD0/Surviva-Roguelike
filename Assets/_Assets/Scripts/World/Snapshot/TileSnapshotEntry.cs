using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct TileSnapshotEntry : INetworkSerializable
{
    public MapLayerType layer;
    public string itemId;
    public V2I anchor;
    public V2I localAnchor;
    public int hp;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref layer);
        s.SerializeValue(ref itemId);
        s.SerializeValue(ref anchor);
        s.SerializeValue(ref localAnchor);
        s.SerializeValue(ref hp);
    }
}