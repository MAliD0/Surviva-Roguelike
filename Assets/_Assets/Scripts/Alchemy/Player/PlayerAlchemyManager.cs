using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAlchemyManager : MonoBehaviour
{
    [SerializeField] Inventory playerInventory;
    [SerializeField] AlchemyEngine alchemyEngine;

    public void CreateAlchemyRequest(List<ItemSlot> inputItems, AlchemyProcessType alchemyProcessType, CircleInstance circle)
    {
        foreach (var itemSlot in inputItems)
        {
            playerInventory.RemoveItem(itemSlot.itemData, itemSlot.amount);    
        }
    }
}
