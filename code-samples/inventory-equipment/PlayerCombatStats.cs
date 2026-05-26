/// <summary>
/// 基礎ステータスと装備ボーナスを合算し、現在攻撃力と最大 HP を提供するコンポーネント。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
﻿using UnityEngine;







public class PlayerCombatStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseNormalAttackDamage = 20f;
    [SerializeField] private float baseMaxHealth          = 100f;

    private PlayerEquipment  _playerEquipment;
    private HealthComponent  _healthComponent;

    

    public float BaseNormalAttackDamage => baseNormalAttackDamage;
    public float BaseMaxHealth          => baseMaxHealth;

    
    
    
    
    public float EquipmentAttackPowerBonus
    {
        get
        {
            if (_playerEquipment == null) return 0f;
            return GetAttackPowerBonus(_playerEquipment.EquippedCore)
                 + GetAttackPowerBonus(_playerEquipment.EquippedArmor)
                 + GetAttackPowerBonus(_playerEquipment.EquippedAccessory);
        }
    }

    
    
    
    
    public float EquipmentMaxHealthBonus
    {
        get
        {
            if (_playerEquipment == null) return 0f;
            return GetMaxHealthBonus(_playerEquipment.EquippedCore)
                 + GetMaxHealthBonus(_playerEquipment.EquippedArmor)
                 + GetMaxHealthBonus(_playerEquipment.EquippedAccessory);
        }
    }

    public float CurrentNormalAttackDamage => baseNormalAttackDamage + EquipmentAttackPowerBonus;
    public float CurrentMaxHealth          => Mathf.Max(1f, baseMaxHealth + EquipmentMaxHealthBonus);

    

    
    
    
    
    
    public void ApplyCurrentMaxHealth(bool keepCurrentRatio = false)
    {
        if (_healthComponent == null)
        {
            Debug.LogWarning("[PlayerCombatStats] ApplyCurrentMaxHealth: HealthComponent not found.");
            return;
        }
        float newMax        = CurrentMaxHealth;
        float beforeMax     = _healthComponent.maxHealth;
        float beforeCurrent = _healthComponent.currentHealth;
        _healthComponent.SetMaxHealth(newMax, keepCurrentRatio);
        Debug.Log($"[PlayerCombatStats] ApplyCurrentMaxHealth: keepRatio={keepCurrentRatio}, maxHealth {beforeMax}->{_healthComponent.maxHealth}, currentHealth {beforeCurrent}->{_healthComponent.currentHealth}");
    }

    

    private void Awake()
    {
        _playerEquipment = GetComponent<PlayerEquipment>();
        _healthComponent = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        if (_playerEquipment != null)
            _playerEquipment.OnEquipmentChanged += HandleEquipmentChanged;
    }

    private void OnDisable()
    {
        if (_playerEquipment != null)
            _playerEquipment.OnEquipmentChanged -= HandleEquipmentChanged;
    }

    

    private void HandleEquipmentChanged()
    {
        ApplyCurrentMaxHealth(keepCurrentRatio: false);
    }

    
    private static float GetAttackPowerBonus(ItemData item)
    {
        return item != null ? item.AttackPowerBonus : 0f;
    }

    
    private static float GetMaxHealthBonus(ItemData item)
    {
        return item != null ? item.MaxHealthBonus : 0f;
    }
}
