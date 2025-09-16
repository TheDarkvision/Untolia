using System.Collections.Generic;

namespace Untolia.Core.Inventory;

public enum ItemCategory
{
    Items,
    Equipment,
    KeyItems
}

public sealed class InventoryItemDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ItemCategory Category { get; set; } = ItemCategory.Items;
    public int StackLimit { get; set; } = 99; // 1 for non-stack
}

public sealed class InventoryStack
{
    public string ItemId { get; }
    public int Quantity { get; private set; }

    public InventoryStack(string itemId, int qty)
    {
        ItemId = itemId;
        Quantity = qty;
    }

    public int Add(int qty, int limitPerStack)
    {
        var canAdd = System.Math.Max(0, limitPerStack - Quantity);
        var added = System.Math.Min(canAdd, qty);
        Quantity += added;
        return added;
    }

    public int Remove(int qty)
    {
        var removed = System.Math.Min(Quantity, qty);
        Quantity -= removed;
        return removed;
    }

    public bool IsEmpty => Quantity <= 0;
}