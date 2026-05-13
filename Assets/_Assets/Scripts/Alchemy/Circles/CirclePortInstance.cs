using System;
using UnityEngine;
using UnityEngine.AI;

public class CirclePortInstance: MonoBehaviour, IItemHolder, IInteractable
{
    public CirclePortType portType;
    public ItemSlot currentItem = new ItemSlot();
    public float storedEnergy;

    public Action<ItemSlot> onItemUpdate;

    public bool IsEmpty => currentItem.itemData == null;
    public bool HasSpace => IsEmpty || !currentItem.IsFull();
    public ItemData CurrentItemData => currentItem.itemData;
    public int CurrentAmount => currentItem.amount;
    public int MaxAmount => currentItem.itemData != null ? currentItem.itemData.maxStack : 0;
    
    public void AddEnergy(float amount)
    {
        storedEnergy += amount;
    }
    
    //return leftovers
    public int AddItem(ItemData itemData, int amount)
    {
        int amountLeft = amount;

        if (CanAddItem(itemData))
        {
            currentItem.itemData = itemData;
            amountLeft = currentItem.AddCount(amount);
        }
        
        onItemUpdate?.Invoke(currentItem);

        return amountLeft;
    }

    public int SetItem(ItemSlot itemSlot)
    {
        currentItem = itemSlot;

        return 0;
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
        onItemUpdate?.Invoke(currentItem);
    }

    public bool ContainsItem(ItemData itemData)
    {
        return currentItem.itemData == itemData;
    }

    public void OnInteract(GameObject interactor, ulong interacterId)
    {
        interactor.TryGetComponent<PlayerManager>(out PlayerManager playerManager);

        switch (portType)
        {
            case CirclePortType.ItemInput:
                ItemSlot itemSlot = playerManager.GetCurrentHeldItem();
                if (itemSlot.CanRemove(1))
                {
                    this.AddItem(itemSlot.itemData, 1);
                    playerManager.RemoveItem(itemSlot.itemData, 1);
                }
            break;

            case CirclePortType.ItemOutput:
                if(this.currentItem.itemData != null)
                {
                    playerManager.AddItem(currentItem.itemData, currentItem.amount); 
                    Clear();
                }
            break;
        }


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
