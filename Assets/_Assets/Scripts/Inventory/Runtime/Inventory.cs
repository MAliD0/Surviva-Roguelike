using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] List<ItemSlot> slots;
    [Space]
    public int inventorySize = 10;
    [FoldoutGroup("Test")][SerializeField] ItemData data;
    [FoldoutGroup("Test")][SerializeField] [Range(1,15)]int number;

    public Action onInventoryUpdate;

    [Button]
    private void InitSlots()
    {
        slots = new List<ItemSlot>(inventorySize);
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new ItemSlot());
        };
    }

    [Button]
    private void AddItemTest()
    {
        AddItem(data, number);
    }
    [Button]
    private void RemoveItemTest()
    {
        RemoveItem(data, number);
    }

    public bool HasItem(ItemData item, int number)
    {
        List<ItemSlot> items = slots.FindAll(x => x.itemData == item);

        int numberInInventory = 0;

        foreach (var i in items)
        {
            numberInInventory += i.amount;
        }

        return numberInInventory >= number;
    }

    public int AddItem(string data, int number)
    {
        return AddItem(ItemDatabase.instance.GetById(data), number);
    }
    public int AddItem(ItemData itemData, int amount)
    {
        return AddItem(new ItemSlot(itemData, amount));
    }
    public int AddItem(ItemSlot incoming)
    {
        if (incoming == null || incoming.itemData == null || incoming.amount <= 0)
            return incoming != null ? incoming.amount : 0;

        int amountLeft = incoming.amount;

        // 1. Stack with compatible existing stacks.
        foreach (ItemSlot slot in slots)
        {
            if (slot == null || slot.IsEmpty())
                continue;

            if (!slot.CanStackWith(incoming))
                continue;

            amountLeft = slot.AddCount(amountLeft);

            if (amountLeft <= 0)
            {
                onInventoryUpdate?.Invoke();
                return 0;
            }
        }

        // 2. Add into empty slots.
        foreach (ItemSlot slot in slots)
        {
            if (slot == null || !slot.IsEmpty())
                continue;

            slot.itemData = incoming.itemData;
            slot.amount = 0;
            slot.alchemyData = incoming.alchemyData != null
                ? incoming.alchemyData.Clone()
                : null;

            amountLeft = slot.AddCount(amountLeft);

            if (amountLeft <= 0)
            {
                onInventoryUpdate?.Invoke();
                return 0;
            }
        }

        onInventoryUpdate?.Invoke();
        return amountLeft;
    }

    public int RemoveItem(ItemData data, int number)
    {
        int excess = number;
        while (true)
        {
            ItemSlot itemSlot = slots.FindLast(x => x.itemData == data);

            if (itemSlot != null)
            {
                excess = itemSlot.RemoveCount(excess);
                if (excess > 0)
                    continue;
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        print($"-{data.Name}: {number}|{excess}");
        onInventoryUpdate?.Invoke();
        return excess;
    }
    
    public void MoveItems(int from, int to)
    {
        if (from < 0 || from >= slots.Count) return;
        if (to < 0 || to >= slots.Count) return;
        if (from == to) return;

        ItemSlot source = slots[from];
        ItemSlot target = slots[to];

        if (source == null || source.IsEmpty())
            return;

        if (target == null)
            return;

        // Move into empty slot.
        if (target.IsEmpty())
        {
            target.SetItemData(source);
            source.Clear();

            onInventoryUpdate?.Invoke();
            return;
        }

        // If cannot stack, swap.
        if (!target.CanStackWith(source))
        {
            ItemSlot sourceCopy = new ItemSlot(source);
            ItemSlot targetCopy = new ItemSlot(target);

            source.SetItemData(targetCopy);
            target.SetItemData(sourceCopy);

            onInventoryUpdate?.Invoke();
            return;
        }

        // Stack compatible items.
        int leftover = target.AddCount(source.amount);
        int moved = source.amount - leftover;

        source.RemoveCount(moved);

        onInventoryUpdate?.Invoke();
    }
    public List<ItemSlot> GetInventoryItems()
    {
        return slots;
    }
}
