/// <summary>
/// Core / Armor / Accessory の装備スロットを管理するコンポーネント。装備変更時にイベントを発火する。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
﻿using UnityEngine;






public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private ItemData equippedCore;
    [SerializeField] private ItemData equippedArmor;
    [SerializeField] private ItemData equippedAccessory;

    
    public event System.Action OnEquipmentChanged;

    

    
    public ItemData EquippedCore => equippedCore;

    
    public bool HasCoreEquipped => equippedCore != null;

    

    
    public ItemData EquippedArmor => equippedArmor;

    
    public bool HasArmorEquipped => equippedArmor != null;

    

    
    public ItemData EquippedAccessory => equippedAccessory;

    
    public bool HasAccessoryEquipped => equippedAccessory != null;

    

    
    
    
    
    
    public bool EquipCore(ItemData item, out ItemData replacedItem)
    {
        replacedItem = null;

        if (item == null)
        {
            Debug.LogWarning("[PlayerEquipment] EquipCore called with null item.");
            return false;
        }
        if (item.ItemType != ItemType.Equipment)
        {
            Debug.LogWarning($"[PlayerEquipment] {item.ItemName}（ID: {item.ItemId}）は Equipment タイプではないため装備できません。ItemType: {item.ItemType}");
            return false;
        }
        if (item.EquipmentSlotType != EquipmentSlotType.Core)
        {
            Debug.LogWarning($"[PlayerEquipment] {item.ItemName}（ID: {item.ItemId}）は Core スロット用ではありません。EquipmentSlotType: {item.EquipmentSlotType}");
            return false;
        }

        replacedItem = equippedCore;
        equippedCore = item;

        if (replacedItem != null)
            Debug.Log($"[PlayerEquipment] Core 装備を交換：{replacedItem.ItemName}→{item.ItemName}（ID: {item.ItemId}）");
        else
            Debug.Log($"[PlayerEquipment] Core スロットに装備：{item.ItemName}（ID: {item.ItemId}）");

        OnEquipmentChanged?.Invoke();
        return true;
    }

    
    public bool EquipCore(ItemData item)
    {
        return EquipCore(item, out _);
    }

    

    
    
    
    
    
    public bool EquipArmor(ItemData item, out ItemData replacedItem)
    {
        replacedItem = null;

        if (item == null)
        {
            Debug.LogWarning("[PlayerEquipment] EquipArmor called with null item.");
            return false;
        }
        if (item.ItemType != ItemType.Equipment)
        {
            Debug.LogWarning($"[PlayerEquipment] {item.ItemName}（ID: {item.ItemId}）は Equipment タイプではないため装備できません。ItemType: {item.ItemType}");
            return false;
        }
        if (item.EquipmentSlotType != EquipmentSlotType.Armor)
        {
            Debug.LogWarning($"[PlayerEquipment] {item.ItemName}（ID: {item.ItemId}）は Armor スロット用ではありません。EquipmentSlotType: {item.EquipmentSlotType}");
            return false;
        }

        replacedItem  = equippedArmor;
        equippedArmor = item;

        if (replacedItem != null)
            Debug.Log($"[PlayerEquipment] Armor 装備を交換：{replacedItem.ItemName}→{item.ItemName}（ID: {item.ItemId}）");
        else
            Debug.Log($"[PlayerEquipment] Armor スロットに装備：{item.ItemName}（ID: {item.ItemId}）");

        OnEquipmentChanged?.Invoke();
        return true;
    }

    
    public bool EquipArmor(ItemData item)
    {
        return EquipArmor(item, out _);
    }

    

    
    
    
    
    
    public bool EquipAccessory(ItemData item, out ItemData replacedItem)
    {
        replacedItem = null;

        if (item == null)
        {
            Debug.LogWarning("[PlayerEquipment] EquipAccessory called with null item.");
            return false;
        }
        if (item.ItemType != ItemType.Equipment)
        {
            Debug.LogWarning($"[PlayerEquipment] {item.ItemName}（ID: {item.ItemId}）は Equipment タイプではないため装備できません。ItemType: {item.ItemType}");
            return false;
        }
        if (item.EquipmentSlotType != EquipmentSlotType.Accessory)
        {
            Debug.LogWarning($"[PlayerEquipment] {item.ItemName}（ID: {item.ItemId}）は Accessory スロット用ではありません。EquipmentSlotType: {item.EquipmentSlotType}");
            return false;
        }

        replacedItem       = equippedAccessory;
        equippedAccessory  = item;

        if (replacedItem != null)
            Debug.Log($"[PlayerEquipment] Accessory 装備を交換：{replacedItem.ItemName}→{item.ItemName}（ID: {item.ItemId}）");
        else
            Debug.Log($"[PlayerEquipment] Accessory スロットに装備：{item.ItemName}（ID: {item.ItemId}）");

        OnEquipmentChanged?.Invoke();
        return true;
    }

    
    public bool EquipAccessory(ItemData item)
    {
        return EquipAccessory(item, out _);
    }

    

    
    public ItemData UnequipCore()
    {
        if (equippedCore == null)
        {
            Debug.LogWarning("[PlayerEquipment] UnequipCore called but Core slot is empty.");
            return null;
        }
        ItemData removed = equippedCore;
        equippedCore = null;
        Debug.Log($"[PlayerEquipment] Core スロットから取り外し：{removed.ItemName}（ID: {removed.ItemId}）");
        OnEquipmentChanged?.Invoke();
        return removed;
    }

    
    public ItemData UnequipArmor()
    {
        if (equippedArmor == null)
        {
            Debug.LogWarning("[PlayerEquipment] UnequipArmor called but Armor slot is empty.");
            return null;
        }
        ItemData removed = equippedArmor;
        equippedArmor = null;
        Debug.Log($"[PlayerEquipment] Armor スロットから取り外し：{removed.ItemName}（ID: {removed.ItemId}）");
        OnEquipmentChanged?.Invoke();
        return removed;
    }

    
    public ItemData UnequipAccessory()
    {
        if (equippedAccessory == null)
        {
            Debug.LogWarning("[PlayerEquipment] UnequipAccessory called but Accessory slot is empty.");
            return null;
        }
        ItemData removed = equippedAccessory;
        equippedAccessory = null;
        Debug.Log($"[PlayerEquipment] Accessory スロットから取り外し：{removed.ItemName}（ID: {removed.ItemId}）");
        OnEquipmentChanged?.Invoke();
        return removed;
    }

    

    
    
    
    
    
    public void ClearEquipment()
    {
        bool anyEquipped = equippedCore != null || equippedArmor != null || equippedAccessory != null;

        equippedCore      = null;
        equippedArmor     = null;
        equippedAccessory = null;

        Debug.Log("[PlayerEquipment] 全装備スロットをクリアしました。");
        if (anyEquipped) OnEquipmentChanged?.Invoke();
    }
}
