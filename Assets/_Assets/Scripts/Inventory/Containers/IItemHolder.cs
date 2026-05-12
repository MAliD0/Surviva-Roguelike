using System;

public interface IItemHolder 
{
    bool IsEmpty { get; }
    bool HasSpace { get; }

    ItemData CurrentItemData { get; }
    int CurrentAmount { get; }
    int MaxAmount { get; }

    bool CanAddItem(ItemData itemData);
    bool CanRemoveItem(ItemData itemData);

    //return leftovers
    int AddItem(ItemData itemData, int amount);
    int RemoveItem(ItemData itemData, int amount);

    bool ContainsItem(ItemData itemData);
    void Clear();
}
