/// <summary>
/// プレイヤースキルの静的データを定義する ScriptableObject。入力スロット、効果タイプ、距離、倍率、表示情報を保持する。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
﻿using UnityEngine;


public enum PlayerSkillInputSlot
{
    None,
    Slot1, Slot2, Slot3, Slot4, Slot5,
    Slot6, Slot7, Slot8, Slot9,
}






public enum PlayerSkillEffectType
{
    None,
    DamageReduction,
    AttackPowerMultiplier,
    AreaDamage,
    GuardCounter,
    BasicMeleeAttack,
    BasicAreaAttack,
}


public enum PlayerSkillVisualType
{
    None,
    DefenseRing,
}









public enum PlayerSkillRangeType
{
    Self,
    Melee,
    Area,
    Ranged,
    Custom,
}





[CreateAssetMenu(fileName = "NewPlayerSkillData", menuName = "Game/Player Skill Data")]
public class PlayerSkillData : ScriptableObject
{
    [Header("基本信息")]
    [SerializeField] private string skillId   = "new_skill";
    [SerializeField] private string skillName = "New Skill";
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("本地化")]
    [Tooltip("将来接入 Localization 系统时使用的 Key。")]
    [SerializeField] private string localizationKey = "";

    [Header("输入设置")]
    [SerializeField] private PlayerSkillInputSlot inputSlot = PlayerSkillInputSlot.None;
    [SerializeField] private string keyLabel = "";

    [Header("时间参数")]
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float duration = 0f;

    [Header("技能效果")]
    [SerializeField] private PlayerSkillEffectType effectType            = PlayerSkillEffectType.None;
    [SerializeField] private float                 damageTakenMultiplier = 1f;
    [SerializeField] private float                 attackPowerMultiplier = 1f;

    [Header("AOE 伤害参数（EffectType = AreaDamage / BasicAreaAttack 时有效）")]
    [SerializeField] private float areaRadius           = 3f;
    [SerializeField] private float areaDamageMultiplier = 0.8f;

    [Header("治疗参数")]
    [SerializeField, Min(0f)]
    private float healingReceivedMultiplier = 1f;

    [Header("Guard Counter")]
    [Tooltip("true の場合、このスキルが Active 時に Guard Resonance が成功すると Radiant Riposte Ready を付与する。")]
    [SerializeField] private bool grantsGuardCounter = false;

    [Header("距离设置")]
    [SerializeField] private PlayerSkillRangeType rangeType   = PlayerSkillRangeType.Self;
    [SerializeField, Min(0f)] private float       customRange = 0f;

    [Header("视觉表现")]
    [SerializeField] private PlayerSkillVisualType visualType = PlayerSkillVisualType.None;

    

    public string                SkillId                   => skillId;
    public string                SkillName                 => skillName;
    public string                Description               => description;
    public Sprite                Icon                      => icon;
    public string                LocalizationKey           => localizationKey;
    public PlayerSkillInputSlot  InputSlot                 => inputSlot;
    public string                KeyLabel                  => keyLabel;
    public float                 Cooldown                  => cooldown;
    public float                 Duration                  => duration;
    public PlayerSkillEffectType EffectType                => effectType;
    public float                 DamageTakenMultiplier     => damageTakenMultiplier;
    public float                 AttackPowerMultiplier     => attackPowerMultiplier;
    public float                 AreaRadius                => areaRadius;
    public float                 AreaDamageMultiplier      => areaDamageMultiplier;
    public float                 HealingReceivedMultiplier => healingReceivedMultiplier;
    public bool                  GrantsGuardCounter        => grantsGuardCounter;
    public PlayerSkillRangeType  RangeType                 => rangeType;
    public float                 CustomRange               => customRange;
    public PlayerSkillVisualType VisualType                => visualType;

    
    
    
    
    public float EffectiveRange => rangeType switch
    {
        PlayerSkillRangeType.Self   => 0f,
        PlayerSkillRangeType.Melee  => 3f,
        PlayerSkillRangeType.Area   => 5f,
        PlayerSkillRangeType.Ranged => 20f,
        PlayerSkillRangeType.Custom => customRange,
        _                           => 0f
    };

    

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(skillId))
            Debug.LogWarning($"[PlayerSkillData] {name}: skillId is empty.");
        if (string.IsNullOrEmpty(skillName))
            Debug.LogWarning($"[PlayerSkillData] {name}: skillName is empty.");

        if (cooldown < 0f) cooldown = 0f;
        if (duration < 0f) duration = 0f;

        damageTakenMultiplier     = Mathf.Clamp(damageTakenMultiplier, 0f, 1f);
        if (attackPowerMultiplier    < 0f) attackPowerMultiplier    = 0f;
        if (areaRadius              < 0f) areaRadius              = 0f;
        if (areaDamageMultiplier    < 0f) areaDamageMultiplier    = 0f;
        if (healingReceivedMultiplier < 0f) healingReceivedMultiplier = 0f;
        if (customRange             < 0f) customRange             = 0f;
    }
}
