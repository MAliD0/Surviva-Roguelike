using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    [HideInInspector]public static ItemDatabase instance;

    public MapBlockDataLibrary blockLibrary;
    public ItemLibrary itemLibrary;

    private void Awake()
    {
        instance = this;
    }
    public ItemData GetById(string itemId)
    {
        ItemData item = blockLibrary.GetById(itemId);
        ItemData itemData = itemLibrary.GetById(itemId);

        return item==null? itemData: item;
    }
}
