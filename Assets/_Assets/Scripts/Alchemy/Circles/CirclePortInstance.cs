using System;
using UnityEngine;
using UnityEngine.AI;

public class CirclePortInstance: AlchemyArrayComponentInstance, IItemHolder, IInteractable
{
    public CirclePortType portType;
    public ItemSlot currentItem = new ItemSlot();
    public float storedEnergy;

    public event Action<ItemSlot> OnItemChanged;

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
        
        OnItemChanged?.Invoke(currentItem);

        return amountLeft;
    }

    public int SetItem(ItemSlot itemSlot)
    {
        if (itemSlot == null || itemSlot.itemData == null || itemSlot.amount <= 0)
        {
            Clear();
            return 0;
        }

        currentItem = new ItemSlot(itemSlot);

        OnItemChanged?.Invoke(currentItem);

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
        OnItemChanged?.Invoke(currentItem);
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
            case CirclePortType.ResidueOutput:
            {
                if (currentItem != null && !currentItem.IsEmpty())
                {
                    ItemSlot copy = new ItemSlot(currentItem);

                    int leftover = playerManager.AddItem(copy);

                    if (leftover <= 0)
                    {
                        Clear();
                    }
                    else
                    {
                        currentItem.amount = leftover;
                        OnItemChanged?.Invoke(currentItem);
                    }
                }

                break;
            }
        }
    }

    public int RemoveItem(ItemData itemData, int amount)
    {
        int amounLeft = amount;

        if(CanRemoveItem(itemData))
            amounLeft = currentItem.RemoveCount(amount);

        OnItemChanged?.Invoke(currentItem);
        
        return amounLeft;
    }
}
