using UnityEngine;

[System.Serializable]
public class ItemSlot
{
    public ItemData itemData;
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
        this.alchemyData = alchemyData != null ? alchemyData.Clone() : null;
    }

    public ItemSlot(ItemSlot itemSlot)
    {
        if (itemSlot == null)
        {
            itemData = null;
            amount = 0;
            alchemyData = null;
            return;
        }

        itemData = itemSlot.itemData;
        amount = itemSlot.amount;
        alchemyData = itemSlot.alchemyData != null
            ? itemSlot.alchemyData.Clone()
            : null;
    }

    public bool IsEmpty()
    {
        return itemData == null || amount <= 0;
    }

    public bool IsFull()
    {
        if (itemData == null)
            return false;

        return amount >= itemData.maxStack;
    }

    public bool CanAdd(int addAmount)
    {
        if (itemData == null)
            return false;

        return itemData.maxStack - amount >= addAmount;
    }

    public bool CanRemove(int removeAmount)
    {
        return amount >= removeAmount;
    }
    public bool CanStackWith(ItemSlot other)
    {
        if (other == null)
            return false;

        if (itemData == null || other.itemData == null)
            return false;

        if (itemData != other.itemData)
            return false;

        // For now: any custom alchemy data means do not stack.
        if (alchemyData != null || other.alchemyData != null)
            return false;

        return true;
    }

    // returns leftover
    public int AddCount(int count)
    {
        if (itemData == null || count <= 0)
            return count;

        int availableSize = itemData.maxStack - amount;
        int addValue = Mathf.Min(availableSize, count);

        amount += addValue;

        return count - addValue;
    }

    // returns leftover
    public int RemoveCount(int count)
    {
        if (count <= 0)
            return 0;

        int removeNumber = Mathf.Min(count, amount);
        amount -= removeNumber;

        if (amount <= 0)
            Clear();

        return count - removeNumber;
    }

    public void SetItemData(ItemData data, int amount = 0)
    {
        itemData = data;
        this.amount = amount;
        alchemyData = null;
    }

    public void SetItemData(ItemSlot item)
    {
        if (item == null || item.itemData == null || item.amount <= 0)
        {
            Clear();
            return;
        }

        itemData = item.itemData;
        amount = item.amount;
        alchemyData = item.alchemyData != null
            ? item.alchemyData.Clone()
            : null;
    }

    public void Clear()
    {
        itemData = null;
        amount = 0;
        alchemyData = null;
    }
}