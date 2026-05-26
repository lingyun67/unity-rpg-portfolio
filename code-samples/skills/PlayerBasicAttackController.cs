/// <summary>
/// 単体基礎攻撃と範囲基礎攻撃を実行するコンポーネント。共有リキャスト、最終ダメージ計算、範囲内ターゲット検索を担当する。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using System.Collections.Generic;
using UnityEngine;



















public class PlayerBasicAttackController : MonoBehaviour
{
    [Header("单体普通攻击")]
    [Tooltip("PlayerCombatStats 缺失时的伤害回退值。")]
    [SerializeField] private float fallbackNormalAttackDamage = 20f;
    [Tooltip("单体普通攻击有效射程（米）。")]
    [SerializeField] private float normalAttackRange          = 2.0f;

    [Header("基础攻击共享冷却")]
    [Tooltip("单体普通攻击和 AOE 普通攻击共享的固定冷却时间（秒）。")]
    [SerializeField] private float basicAttackRecast = 1.0f;

    [Header("AOE 普通攻击（键 4）")]
    [Tooltip("AOE 普通攻击的搜索半径（米）。")]
    [SerializeField] private float areaBasicAttackRadius           = 3f;
    [Tooltip("AOE 每个目标受到的伤害 = 普通攻击最终伤害 × 此倍率。")]
    [SerializeField] private float areaBasicAttackDamageMultiplier = 0.4f;

    
    private PlayerTargeting              _targeting;
    private PlayerCombatStats            _combatStats;
    private PlayerStatusEffectController _statusEffectController;
    private FactionComponent             _selfFaction;
    private HealthComponent              _selfHealth;
    private Animator                     _animator;

    
    private float _nextBasicAttackAllowedTime;

    

    private void Awake()
    {
        _targeting              = GetComponent<PlayerTargeting>();
        _combatStats            = GetComponent<PlayerCombatStats>();
        _statusEffectController = GetComponent<PlayerStatusEffectController>();
        _selfFaction            = GetComponent<FactionComponent>();
        _selfHealth             = GetComponent<HealthComponent>();
        _animator               = GetComponent<Animator>();

        if (_targeting == null)
            Debug.LogWarning("[PlayerBasicAttackController] PlayerTargeting not found.");
        if (_selfFaction == null)
            Debug.LogWarning("[PlayerBasicAttackController] FactionComponent not found.");
        if (_animator == null)
            Debug.LogWarning("[PlayerBasicAttackController] Animator not found.");
    }

    
    public float BasicAttackCooldownRemaining => Mathf.Max(0f, _nextBasicAttackAllowedTime - Time.time);
    
    public float BasicAttackCooldownDuration  => basicAttackRecast;
    
    public bool  IsBasicAttackReady           => Time.time >= _nextBasicAttackAllowedTime;


    
    
    
    
    
    public bool TrySingleTargetAttack()
    {
        
        if (!IsBasicAttackReady)
        {
            Debug.Log($"[PlayerBasicAttackController] Normal attack on cooldown ({(_nextBasicAttackAllowedTime - Time.time):F1}s remaining).");
            return false;
        }

        
        if (_targeting == null || _targeting.CurrentTarget == null)
        {
            Debug.Log("[PlayerBasicAttackController] No target selected.");
            return false;
        }

        Transform target = _targeting.CurrentTarget;
        if (target == null)
        {
            Debug.Log("[PlayerBasicAttackController] Target no longer exists.");
            return false;
        }

        
        var health = target.GetComponentInChildren<HealthComponent>()
                  ?? target.GetComponent<HealthComponent>();
        if (health == null)
        {
            Debug.LogWarning($"[PlayerBasicAttackController] {target.name} has no HealthComponent.");
            return false;
        }

        
        if (health.IsDead)
        {
            Debug.Log($"[PlayerBasicAttackController] {target.name} is already dead.");
            return false;
        }

        
        var targetFaction = target.GetComponent<FactionComponent>();
        if (targetFaction == null)
        {
            Debug.LogWarning($"[PlayerBasicAttackController] {target.name} has no FactionComponent.");
            return false;
        }
        if (_selfFaction != null && !_selfFaction.ShouldAttack(targetFaction.faction))
        {
            Debug.Log($"[PlayerBasicAttackController] {target.name} is not hostile.");
            return false;
        }

        
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > normalAttackRange)
        {
            Debug.Log($"[PlayerBasicAttackController] Target out of range ({dist:F1} / {normalAttackRange}m).");
            return false;
        }

        
        float finalDamage = CalculateNormalAttackDamage();
        StartBasicAttackRecast();
        health.TakeDamage(finalDamage, transform);
        Debug.Log($"[PlayerBasicAttackController] Normal attack hit: {target.name}, damage={finalDamage:F1}");

        TriggerAttackAnimation();
        return true;
    }
    

    
    
    
    
    
    public bool TryExecuteBasicMeleeAttack(PlayerSkillData skillData)
    {
        float range = skillData != null ? skillData.EffectiveRange : normalAttackRange;

        if (!IsBasicAttackReady)
        {
            Debug.Log($"[PlayerBasicAttackController] Normal attack on cooldown ({BasicAttackCooldownRemaining:F1}s remaining).");
            return false;
        }
        if (_targeting == null || _targeting.CurrentTarget == null)
        {
            Debug.Log("[PlayerBasicAttackController] No target selected.");
            return false;
        }
        Transform target = _targeting.CurrentTarget;
        var health = target.GetComponentInChildren<HealthComponent>() ?? target.GetComponent<HealthComponent>();
        if (health == null || health.IsDead) return false;
        var targetFaction = target.GetComponent<FactionComponent>();
        if (targetFaction == null) return false;
        if (_selfFaction != null && !_selfFaction.ShouldAttack(targetFaction.faction)) return false;
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > range)
        {
            Debug.Log($"[PlayerBasicAttackController] Target out of range ({dist:F1}/{range}m).");
            return false;
        }
        float finalDamage = CalculateNormalAttackDamage();
        StartBasicAttackRecast();
        health.TakeDamage(finalDamage, transform);
        Debug.Log($"[PlayerBasicAttackController] Melee hit: {target.name}, damage={finalDamage:F1}");
        TriggerAttackAnimation();
        return true;
    }

    
    
    
    
    
    
    public bool TryExecuteBasicAreaAttack(PlayerSkillData skillData)
    {
        if (!IsBasicAttackReady)
        {
            Debug.Log($"[PlayerBasicAttackController] AOE attack on cooldown ({BasicAttackCooldownRemaining:F1}s remaining).");
            return false;
        }
        float radius = skillData != null ? skillData.EffectiveRange : areaBasicAttackRadius;
        float mult   = (skillData != null && skillData.AreaDamageMultiplier > 0f)
                       ? skillData.AreaDamageMultiplier
                       : areaBasicAttackDamageMultiplier;
        float normalDamage = CalculateNormalAttackDamage();
        float aoeDamage    = normalDamage * mult;
        var hits    = Physics.OverlapSphere(transform.position, radius);
        var damaged = new HashSet<HealthComponent>();
        int hitCount = 0;
        foreach (var col in hits)
        {
            if (col == null) continue;
            var health = col.GetComponentInParent<HealthComponent>();
            if (health == null || !damaged.Add(health)) continue;
            if (health.gameObject == gameObject || health.IsDead) continue;
            var tf = col.GetComponentInParent<FactionComponent>();
            if (tf == null || _selfFaction == null) continue;
            if (!_selfFaction.ShouldAttack(tf.faction)) continue;
            health.TakeDamage(aoeDamage, transform);
            hitCount++;
        }
        StartBasicAttackRecast();
        Debug.Log($"[PlayerBasicAttackController] AOE executed. radius={radius}m, aoeDmg={aoeDamage:F1}, hits={hitCount}");
        TriggerAttackAnimation();
        return true;
    }


    
    
    
    
    
    public bool TryAreaBasicAttack()
    {
        
        if (!IsBasicAttackReady)
        {
            Debug.Log($"[PlayerBasicAttackController] AOE attack on cooldown ({(_nextBasicAttackAllowedTime - Time.time):F1}s remaining).");
            return false;
        }

        float normalDamage = CalculateNormalAttackDamage();
        float aoeDamage    = normalDamage * areaBasicAttackDamageMultiplier;

        var hits    = Physics.OverlapSphere(transform.position, areaBasicAttackRadius);
        var damaged = new HashSet<HealthComponent>();
        int hitCount = 0;

        foreach (var col in hits)
        {
            if (col == null) continue;

            var health = col.GetComponentInParent<HealthComponent>();
            if (health == null) continue;

            
            if (!damaged.Add(health)) continue;

            
            if (health.gameObject == gameObject) continue;

            
            if (health.IsDead) continue;

            
            var targetFaction = col.GetComponentInParent<FactionComponent>();
            if (targetFaction == null) continue;
            if (_selfFaction == null) continue;
            if (!_selfFaction.ShouldAttack(targetFaction.faction)) continue;

            health.TakeDamage(aoeDamage, transform);
            hitCount++;
            Debug.Log($"[PlayerBasicAttackController] AOE hit: {health.gameObject.name}, damage={aoeDamage:F1}");
        }

        
        StartBasicAttackRecast();
        Debug.Log($"[PlayerBasicAttackController] AOE attack executed. radius={areaBasicAttackRadius}, normalDmg={normalDamage:F1}, aoeDmg={aoeDamage:F1}, targets hit={hitCount}");

        TriggerAttackAnimation();
        return true;
    }

    

    private void StartBasicAttackRecast()
    {
        _nextBasicAttackAllowedTime = Time.time + basicAttackRecast;
    }

    
    
    
    
    
    private float CalculateNormalAttackDamage()
    {
        float damage = _combatStats != null
            ? _combatStats.CurrentNormalAttackDamage
            : fallbackNormalAttackDamage;

        if (_statusEffectController != null)
            damage = _statusEffectController.ModifyOutgoingNormalAttackDamage(damage);

        return damage;
    }

    
    private void TriggerAttackAnimation()
    {
        if (_animator != null && (_selfHealth == null || !_selfHealth.IsDead))
            _animator.SetTrigger("Attack");
    }
}
