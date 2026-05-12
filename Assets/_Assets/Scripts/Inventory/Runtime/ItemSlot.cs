using System;
using UnityEngine;

[Serializable]
public class ItemSlot
{
    public ItemData itemData; //{ get; private set; }
    public int amount;

    public ItemSlot()
    {
        itemData = null;
        amount = 0;
    }
    public ItemSlot(ItemData data, int num = 0)
    {
        this.itemData = data;
        this.amount = num;
    }
    public ItemSlot(ItemSlot itemSlot)
    {
        this.itemData = itemSlot.itemData;
        this.amount = itemSlot.amount;
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
        if(item == null)
        {
            itemData = null;
            amount = 0;
        }
        else
        {
            this.itemData = item.itemData;
            this.amount = item.amount;
        }
    }
}
