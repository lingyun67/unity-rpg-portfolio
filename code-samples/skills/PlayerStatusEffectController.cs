/// <summary>
/// Active なプレイヤースキル状態から、被ダメージ、通常攻撃ダメージ、被回復量の補正を計算するコンポーネント。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using UnityEngine;












public class PlayerStatusEffectController : MonoBehaviour
{
    [Header("技能管理器引用（留空时自动查找）")]
    [SerializeField] private PlayerSkillManager skillManager;

    [Header("调试")]
    [SerializeField] private bool logDamageModification = true;

    

    private void Awake()
    {
        ResolveSkillManager();
    }

    

    
    
    
    
    
    
    public float ModifyIncomingDamage(float damage)
    {
        if (damage <= 0f)           return damage;
        if (skillManager == null)   return damage;

        float finalDamage = damage;
        var   states      = skillManager.RuntimeStates;

        foreach (var state in states)
        {
            if (state == null)            continue;
            if (state.SkillData == null)  continue;
            if (!state.IsActive)          continue;
            if (state.SkillData.EffectType != PlayerSkillEffectType.DamageReduction) continue;

            finalDamage *= state.SkillData.DamageTakenMultiplier;
        }

        if (logDamageModification && !Mathf.Approximately(finalDamage, damage))
            Debug.Log($"[PlayerStatusEffectController] Damage modified: original={damage:F1}, final={finalDamage:F1}");

        return finalDamage;
    }


    
    
    
    
    
    public float ModifyOutgoingNormalAttackDamage(float baseDamage)
    {
        if (baseDamage <= 0f)        return baseDamage;
        if (skillManager == null)    return baseDamage;

        float finalDamage = baseDamage;
        var   states      = skillManager.RuntimeStates;
        if (states == null) return baseDamage;

        foreach (var state in states)
        {
            if (state == null)           continue;
            if (state.SkillData == null) continue;
            if (!state.IsActive)         continue;
            if (state.SkillData.EffectType != PlayerSkillEffectType.AttackPowerMultiplier) continue;

            finalDamage *= state.SkillData.AttackPowerMultiplier;
        }

        if (logDamageModification && !Mathf.Approximately(finalDamage, baseDamage))
            Debug.Log($"[PlayerStatusEffectController] Outgoing damage modified: base={baseDamage:F1}, final={finalDamage:F1}");

        return finalDamage;
    }



    
    
    
    
    
    
    
    public float GetIncomingHealingReceivedMultiplier()
    {
        if (skillManager == null) return 1f;

        float multiplier = 1f;
        var   states     = skillManager.RuntimeStates;
        if (states == null) return 1f;

        foreach (var state in states)
        {
            if (state == null)           continue;
            if (state.SkillData == null) continue;
            if (!state.IsActive)         continue;

            multiplier *= state.SkillData.HealingReceivedMultiplier;
        }
        return multiplier;
    }

    
    
    
    
    
    public float ModifyIncomingHealing(float baseHealing)
    {
        if (baseHealing <= 0f)    return 0f;
        if (skillManager == null) return baseHealing;

        float result = baseHealing * GetIncomingHealingReceivedMultiplier();

        if (logDamageModification && !Mathf.Approximately(result, baseHealing))
            Debug.Log($"[PlayerStatusEffectController] Healing modified: base={baseHealing:F1}, final={result:F1}");

        return result;
    }


    private void ResolveSkillManager()
    {
        if (skillManager != null) return;

        skillManager = GetComponent<PlayerSkillManager>();
        if (skillManager != null) return;

        skillManager = FindFirstObjectByType<PlayerSkillManager>();
        if (skillManager == null)
            Debug.LogWarning("[PlayerStatusEffectController] PlayerSkillManager not found. Damage modification disabled.");
    }
}
