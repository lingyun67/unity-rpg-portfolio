/// <summary>
/// 同一 itemId のアイテム所持数を保持するデータクラス。インベントリ内部でスタック管理に使用する。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
﻿using System;
using UnityEngine;





[Serializable]
public class ItemStack
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int count;

    public ItemData ItemData => itemData;
    public int Count => count;
    public string ItemId => itemData != null ? itemData.ItemId : string.Empty;
    public string ItemName => itemData != null ? itemData.ItemName : string.Empty;

    
    public bool IsFull => itemData != null && count >= itemData.MaxStack;

    
    public int RemainingCapacity
    {
        get
        {
            if (itemData == null) return 0;
            int remaining = itemData.MaxStack - count;
            return remaining < 0 ? 0 : remaining;
        }
    }

    public ItemStack(ItemData itemData, int count)
    {
        this.itemData = itemData;
        if (count < 1) count = 1;
        if (itemData != null && count > itemData.MaxStack) count = itemData.MaxStack;
        this.count = count;
    }

    
    
    
    
    public int AddCount(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[ItemStack] AddCount called with invalid amount: {amount}");
            return 0;
        }
        if (IsFull) return 0;
        int actualAdd = Mathf.Min(amount, RemainingCapacity);
        count += actualAdd;
        return actualAdd;
    }

    
    
    
    
    public int RemoveCount(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[ItemStack] RemoveCount called with invalid amount: {amount}");
            return 0;
        }
        int actualRemove = Mathf.Min(amount, count);
        count -= actualRemove;
        return actualRemove;
    }
}
