using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CirclePortInstance
{
    public CirclePortType portType;
    public ItemSlot currentItem;
    public float storedEnergy;

    public bool CanAcceptItem(ItemSlot item)
    {
        return currentItem == null;
    }
    public void SetItem(ItemSlot item)
    {
        currentItem = item;
    }
    public ItemSlot TakeItem()
    {
        return currentItem;
    }
    public void AddEnergy(float amount)
    {
        storedEnergy += amount;
    }
}
