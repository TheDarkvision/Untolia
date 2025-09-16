using System;
using System.Collections.Generic;
using System.Linq;

namespace Untolia.Core.Inventory;

public sealed class InventoryService
{
    // Static definitions by id
    private readonly Dictionary<string, InventoryItemDef> _defs = new(StringComparer.OrdinalIgnoreCase);

    // Stacks keyed by category
    private readonly List<InventoryStack> _items = new();      // Items
    private readonly List<InventoryStack> _equipment = new();  // Equipment
    private readonly List<InventoryStack> _keys = new();       // Key Items

    public IEnumerable<InventoryItemDef> Definitions => _defs.Values;

    public void RegisterDef(InventoryItemDef def)
    {
        if (string.IsNullOrWhiteSpace(def.Id))
            throw new ArgumentException("Item def must have id");

        _defs[def.Id] = def;
    }

    public InventoryItemDef? GetDef(string id) => _defs.TryGetValue(id, out var d) ? d : null;

    private List<InventoryStack> GetList(ItemCategory cat) => cat switch
    {
        ItemCategory.Items => _items,
        ItemCategory.Equipment => _equipment,
        ItemCategory.KeyItems => _keys,
        _ => _items
    };

    // Add items (observing stack limits); returns actually added quantity
    public int Add(string itemId, int qty)
    {
        if (qty <= 0) return 0;
        var def = GetDef(itemId) ?? throw new InvalidOperationException($"Unknown item id: {itemId}");
        var list = GetList(def.Category);
        var remaining = qty;

        // Fill existing stacks first
        foreach (var st in list.Where(s => s.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)))
        {
            if (remaining <= 0) break;
            remaining -= st.Add(remaining, System.Math.Max(1, def.StackLimit));
        }

        // Create new stacks while needed
        while (remaining > 0)
        {
            var take = System.Math.Min(remaining, System.Math.Max(1, def.StackLimit));
            list.Add(new InventoryStack(itemId, take));
            remaining -= take;
        }

        // Combine small stacks if over-fragmented (optional)
        MergeStacks(list, def);

        return qty;
    }

    // Remove quantity; returns actually removed
    public int Remove(string itemId, int qty)
    {
        if (qty <= 0) return 0;
        var def = GetDef(itemId) ?? throw new InvalidOperationException($"Unknown item id: {itemId}");
        var list = GetList(def.Category);

        var remaining = qty;
        foreach (var st in list.Where(s => s.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            if (remaining <= 0) break;
            var removed = st.Remove(remaining);
            remaining -= removed;
            if (st.IsEmpty) list.Remove(st);
        }

        return qty - remaining;
    }

    // Get a view of stacks for UI (ordered by name)
    public IReadOnlyList<(InventoryStack stack, InventoryItemDef def)> GetStacks(ItemCategory cat)
    {
        var list = GetList(cat);
        var joined = list
            .Select(s => (s, GetDef(s.ItemId)!))
            .Where(t => t.Item2 != null)
            .OrderBy(t => t.Item2.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return joined;
    }

    public int CountOf(string itemId)
    {
        var def = GetDef(itemId);
        if (def == null) return 0;
        var list = GetList(def.Category);
        return list.Where(s => s.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase)).Sum(s => s.Quantity);
    }

    public bool Has(string itemId, int qty = 1) => CountOf(itemId) >= qty;

    // Use/Equip hooks (game-specific). Returns true if consumed/handled.
    public bool Use(string itemId)
    {
        var def = GetDef(itemId);
        if (def == null) return false;

        switch (def.Category)
        {
            case ItemCategory.Items:
                // Example: consume one
                return Remove(itemId, 1) > 0;
            case ItemCategory.Equipment:
                // Equip flow would be elsewhere; inventory might not consume it
                return false;
            case ItemCategory.KeyItems:
                // Usually not consumed
                return false;
        }
        return false;
    }

    public void ClearAll()
    {
        _items.Clear();
        _equipment.Clear();
        _keys.Clear();
    }

    private static void MergeStacks(List<InventoryStack> list, InventoryItemDef def)
    {
        if (def.StackLimit <= 1) return;
        // merge by item id
        var buckets = list.GroupBy(s => s.ItemId, StringComparer.OrdinalIgnoreCase).ToList();
        list.Clear();
        foreach (var g in buckets)
        {
            var total = g.Sum(s => s.Quantity);
            while (total > 0)
            {
                var take = System.Math.Min(total, def.StackLimit);
                list.Add(new InventoryStack(g.Key, take));
                total -= take;
            }
        }
    }
}
