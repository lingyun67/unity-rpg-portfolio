/// <summary>
/// プレイヤーの所持アイテムを管理するインベントリ。Equipment は独立スタック、非 Equipment は同一 itemId でマージする。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;







public class PlayerInventory : MonoBehaviour
{
    private readonly List<ItemStack> _items = new List<ItemStack>();

    
    public event Action OnInventoryChanged;

    
    public int ItemCount
    {
        get
        {
            int total = 0;
            foreach (var stack in _items) total += stack.Count;
            return total;
        }
    }

    
    public int StackCount => _items.Count;

    
    public IReadOnlyList<ItemStack> Items => _items;

    

    
    
    
    
    
    public bool AddItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[PlayerInventory] AddItem called with null item.");
            return false;
        }

        if (item.ItemType == ItemType.Equipment)
        {
            _items.Add(new ItemStack(item, 1));
        }
        else
        {
            ItemStack existing = FindNonFullStack(item);
            if (existing != null)
                existing.AddCount(1);
            else
                _items.Add(new ItemStack(item, 1));
        }

        Debug.Log($"获得：{item.ItemName}（ID: {item.ItemId}），当前持有总数：{ItemCount}，当前 stack 数：{StackCount}");
        PrintInventorySummary();
        OnInventoryChanged?.Invoke();
        return true;
    }

    

    
    
    
    
    
    public bool RemoveItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[PlayerInventory] RemoveItem called with null item.");
            return false;
        }

        ItemStack target = FindFirstStack(item);
        if (target == null)
        {
            Debug.LogWarning($"[PlayerInventory] RemoveItem: {item.ItemName}（ID: {item.ItemId}）not found in inventory.");
            return false;
        }

        if (target.Count > 1)
            target.RemoveCount(1);
        else
            _items.Remove(target);

        Debug.Log($"移除：{item.ItemName}（ID: {item.ItemId}），当前持有总数：{ItemCount}，当前 stack 数：{StackCount}");
        PrintInventorySummary();
        OnInventoryChanged?.Invoke();
        return true;
    }

    

    
    
    
    
    public ItemData FindFirstEquipmentBySlot(EquipmentSlotType slotType)
    {
        foreach (var stack in _items)
        {
            if (stack.ItemData == null) continue;
            if (stack.Count <= 0) continue;
            if (stack.ItemData.ItemType != ItemType.Equipment) continue;
            if (stack.ItemData.EquipmentSlotType != slotType) continue;
            return stack.ItemData;
        }
        return null;
    }

    

    private ItemStack FindNonFullStack(ItemData item)
    {
        bool hasId = !string.IsNullOrEmpty(item.ItemId);
        foreach (var stack in _items)
        {
            if (stack.ItemData == null) continue;
            if (stack.IsFull) continue;
            if (hasId ? stack.ItemId == item.ItemId : stack.ItemName == item.ItemName)
                return stack;
        }
        return null;
    }

    private ItemStack FindFirstStack(ItemData item)
    {
        bool hasId = !string.IsNullOrEmpty(item.ItemId);
        foreach (var stack in _items)
        {
            if (stack.ItemData == null) continue;
            if (hasId ? stack.ItemId == item.ItemId : stack.ItemName == item.ItemName)
                return stack;
        }
        return null;
    }

    private void PrintInventorySummary()
    {
        var sb = new StringBuilder("[PlayerInventory] 当前库存：\n");
        foreach (var stack in _items)
            sb.AppendLine($"  - {stack.ItemName} x {stack.Count}");
        Debug.Log(sb.ToString());
    }
}
