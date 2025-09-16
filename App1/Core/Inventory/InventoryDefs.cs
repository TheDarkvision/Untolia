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
    public InventoryStack(string itemId, int qty)
    {
        ItemId = itemId;
        Quantity = qty;
    }

    public string ItemId { get; }
    public int Quantity { get; private set; }

    public bool IsEmpty => Quantity <= 0;

    public int Add(int qty, int limitPerStack)
    {
        var canAdd = Math.Max(0, limitPerStack - Quantity);
        var added = Math.Min(canAdd, qty);
        Quantity += added;
        return added;
    }

    public int Remove(int qty)
    {
        var removed = Math.Min(Quantity, qty);
        Quantity -= removed;
        return removed;
    }
}