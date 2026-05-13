using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AlchemyItemDatabase : MonoBehaviour
{
    public ItemLibrary allAlchemyItems;
    public ResidueItemData residueItem;

    public static AlchemyItemDatabase Instance;

    void Awake()
    {
        Instance = this;
    }

    public List<AlchemyItemData> GetMatchableItems(AspectProfile aspectProfile)
    {

        return null;
    }
    public AlchemyItemData GetById(string id)
    {
        ItemData item = allAlchemyItems.GetById(id);

        return item as AlchemyItemData;
    }
    public ResidueItemData GetResidueItem()
    {
        return residueItem;
    }
    
    public List<AlchemyItemData> getAllAlchemyItems()
    {
        return allAlchemyItems.getAllItems().OfType<AlchemyItemData>().ToList();
    }
}
