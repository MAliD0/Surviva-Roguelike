using UnityEngine;
using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName ="MapBlockLibrary", menuName = "Map/BlockLibrary")]
public class MapBlockDataLibrary : ScriptableObject
{
    [ReadOnly][SerializeField] SerializedDictionary<string, MapBlockData> dataLibrary;
    [SerializeField] private string[] resourceFolders =
    {
        "Tiles",
        "Alchemy/Items"
    };

    [Button("Generate Library")]
    public void GenerateLibrary()
    {
        dataLibrary ??= new SerializedDictionary<string, MapBlockData>();
        dataLibrary.Clear();

        foreach (string folder in resourceFolders)
        {
            MapBlockData[] assets = Resources.LoadAll<MapBlockData>(folder);

            foreach (MapBlockData asset in assets)
            {
                if (asset == null)
                    continue;

                string id = asset.GetItemID();

                if (!dataLibrary.ContainsKey(id))
                    dataLibrary.Add(id, asset);
                else
                    Debug.LogWarning($"Block {id} already has instance");
            }
        }
    }

    [Button("Reset Library")]
    public void ResetLibrary()
    {
        dataLibrary ??= new SerializedDictionary<string, MapBlockData>();
        dataLibrary.Clear();
    }


    public void AddData(MapBlockData mapBlockData)
    {
        if (!dataLibrary.ContainsKey(mapBlockData.GetItemID()))
        {
            dataLibrary.Add(mapBlockData.GetItemID(), mapBlockData);
        }
        else
        {
            Debug.LogWarning($"Item {mapBlockData.GetItemID()} already has instance");
        }
    }

    public MapBlockData GetById(string id)
    {
        dataLibrary.TryGetValue(id, out MapBlockData mapBlockData);
        return mapBlockData;
    }
}
