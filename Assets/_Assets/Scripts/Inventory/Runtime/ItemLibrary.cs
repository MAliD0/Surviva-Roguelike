using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using UnityEngine;
[CreateAssetMenu(fileName = "ItemLibrary", menuName = "Map/ItemLibrary")]
    
public class ItemLibrary : ScriptableObject
{
    [SerializeField] string ItemsFolder = "Items";
    [ReadOnly][SerializeField] protected SerializedDictionary<string, ItemData> dataLibrary;

    [Button("Generate Library")]
    public void GenerateLibrary()
    {
        ItemData[] assets = Resources.LoadAll<ItemData>(ItemsFolder);
        foreach (ItemData asset in assets)
        {
            if (!dataLibrary.ContainsKey(asset.GetItemID()))
            {
                dataLibrary.Add(asset.GetItemID(), asset);
            }
            else
            {
                Debug.LogWarning($"Item {asset.GetItemID()} already has instance");
            }
        }
        OnUpdate();
    }

    [Button("Reset Library")]
    public void ResetLibrary()
    {
        dataLibrary = new SerializedDictionary<string, ItemData>();
        OnUpdate();
    }


    public void AddData(ItemData itemData)
    {
        if (!dataLibrary.ContainsKey(itemData.GetItemID()))
        {
            dataLibrary.Add(itemData.GetItemID(), itemData);
        }
        else
        {
            Debug.LogWarning($"Item {itemData.GetItemID()} already has instance");
        }
        OnUpdate();
    }

    private void OnUpdate()
    {
        foreach (var item in dataLibrary)
        {
            if(item.Value == null)
            {
                dataLibrary.Remove(item.Key);
                break;
            }
        }
    }

    public ItemData GetById(string id)
    {
        dataLibrary.TryGetValue(id, out ItemData itemData);
        return itemData;
    }

    public List<ItemData> getAllItems()
    {
        return dataLibrary.Values.ToList();
    }
}
