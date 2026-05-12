using System;
using UnityEngine;

public class CirclePortInstance: MonoBehaviour, IItemHolder
{
    public CirclePortType portType;
    public ItemSlot currentItem = new ItemSlot();
    public float storedEnergy;

    public Action<ItemSlot> onItemUpdate;

    public bool IsEmpty => currentItem.itemData == null;
    public bool HasSpace => !currentItem.IsFull();
    public ItemData CurrentItemData => currentItem.itemData;
    public int CurrentAmount => currentItem.amount;
    public int MaxAmount => currentItem.itemData.maxStack;

    public void AddEnergy(float amount)
    {
        storedEnergy += amount;
    }
    
    //return leftovers
    public int AddItem(ItemData itemData, int amount)
    {
        int amountLeft = amount;

        if(CanAddItem(itemData))
            amountLeft -= currentItem.AddCount(amount);
        
        onItemUpdate?.Invoke(currentItem);

        return amountLeft;
    }

    public bool CanAddItem(ItemData itemData)
    {
        return IsEmpty || currentItem.itemData == itemData; 
    }

    public bool CanRemoveItem(ItemData itemData)
    {
        return currentItem.itemData == itemData && currentItem.amount > 0;
    }

    public void Clear()
    {
        currentItem = new ItemSlot();
    }

    public bool ContainsItem(ItemData itemData)
    {
        return currentItem.itemData == itemData;
    }

    public int RemoveItem(ItemData itemData, int amount)
    {
        int amounLeft = amount;

        if(CanRemoveItem(itemData))
            amounLeft = currentItem.RemoveCount(amount);

        onItemUpdate?.Invoke(currentItem);
        
        return amounLeft;
    }
}
