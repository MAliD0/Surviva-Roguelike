using System;
using UnityEngine;

[Serializable]
public class ItemSlot
{
    public ItemData itemData; //{ get; private set; }
    public int amount;
    public AlchemyItemStackData alchemyData;

    public ItemSlot()
    {
        itemData = null;
        amount = 0;
        alchemyData = null;
    }

    public ItemSlot(ItemData data, int amount = 0)
    {
        this.itemData = data;
        this.amount = amount;
        this.alchemyData = null;
    }

    public ItemSlot(ItemData data, int amount, AlchemyItemStackData alchemyData)
    {
        this.itemData = data;
        this.amount = amount;
        this.alchemyData = alchemyData;
    }

    public ItemSlot(ItemSlot itemSlot)
    {
        itemData = itemSlot.itemData;
        amount = itemSlot.amount;
        alchemyData = itemSlot.alchemyData != null
            ? itemSlot.alchemyData.Clone()
            : null;
    }

    public bool IsFull()
    {
        return amount == itemData.maxStack;
    }

    public bool CanAdd(int amount)
    {
        return itemData.maxStack - this.amount >= amount;
    }
    public bool CanRemove(int amount)
    {
        return  this.amount >= amount;
    }
    //return leftovers
    public int AddCount(int count)
    {
        int availableSize = itemData.maxStack - amount;
        int addValue = Mathf.Min(availableSize, count);
        amount += addValue;
        return count-addValue;
    }

    public int RemoveCount(int count)
    {
        int removeNumber = Mathf.Min(count, amount);
        amount -= removeNumber;
        if(amount == 0)
        {
            itemData = null;
        }
        return count-removeNumber;
    }

    public void SetItemData(ItemData data, int number = 0)
    {
        this.itemData = data;
        this.amount = number;
    }
    public void SetItemData(ItemSlot item)
    {
        if (item == null)
        {
            itemData = null;
            amount = 0;
            alchemyData = null;
        }
        else
        {
            itemData = item.itemData;
            amount = item.amount;
            alchemyData = item.alchemyData != null
                ? item.alchemyData.Clone()
                : null;
        }
    }
}
