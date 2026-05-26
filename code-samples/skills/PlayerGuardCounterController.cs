/// <summary>
/// Guard Resonance 成功後の Radiant Riposte 反撃機会を管理するコンポーネント。反撃ウィンドウ、対象、射程、死亡時クリアを扱う。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using UnityEngine;
















public class PlayerGuardCounterController : MonoBehaviour
{
    

    [Header("Hikari 参照（空の場合 Start() で自動検索）")]
    [SerializeField] private HikariSupportController hikariSupport;

    [Header("玩家戦闘ステータス参照（空の場合 GetComponent で自動解決）")]
    [SerializeField] private PlayerCombatStats combatStats;

    [Header("反撃パラメータ")]
    [Tooltip("反撃機会の有効時間（秒）。")]
    [SerializeField] private float counterWindowSeconds = 10f;

    [Tooltip("反撃伤害の PDU 倍率。1PDU = 20 enemy damage（BALANCE_BASELINE.md Tier 1）。")]
    [SerializeField] private float counterDamagePdu = 3f;

    

    private bool             _isReady;
    private float            _remainingWindow;
    private Transform        _counterTarget;
    private HealthComponent  _playerHealth;   

    

    private void Awake()
    {
        if (combatStats == null)
            combatStats = GetComponent<PlayerCombatStats>();
        if (combatStats == null)
            Debug.LogWarning("[RadiantRiposte] PlayerCombatStats not found.");

        _playerHealth = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        ResolveHikariSupport();
        SubscribeToPlayerDeath();
    }

    private void OnDestroy()
    {
        UnsubscribeFromHikari();
        UnsubscribeFromPlayerDeath();
    }

    private void Update()
    {
        if (_isReady)
        {
            _remainingWindow -= Time.deltaTime;
            if (_remainingWindow <= 0f)
            {
                Debug.Log("[RadiantRiposte] 反撃機会が期限切れ（10秒）。");
                ClearCounter();
            }
        }
    }

    

    
    
    
    
    
    private void HandleGuardResonanceTriggered(Transform attacker, bool grantsGuardCounter)
    {
        if (!grantsGuardCounter)
        {
            Debug.Log("[RadiantRiposte] Guard Resonance 触发（grantsGuardCounter=false）— Radiant Riposte は更新しない。");
            return;
        }

        _isReady         = true;
        _counterTarget   = attacker;
        _remainingWindow = counterWindowSeconds;
        Debug.Log($"[RadiantRiposte] Radiant Riposte Ready（Iron Bulwark 授権）— 攻击者: {(attacker != null ? attacker.name : "null")} | 有效时间: {counterWindowSeconds}s");
    }

    

    private void HandlePlayerDied()
    {
        if (!_isReady) return;
        Debug.Log("[RadiantRiposte] 玩家死亡 — Radiant Riposte Ready 清除。");
        ClearCounter();
    }

    

    
    private bool IsPlayerAlive => _playerHealth == null || !_playerHealth.IsDead;

    
    public bool IsCounterReady => _isReady;

    
    public float CounterRemainingTime => _remainingWindow;

    
    public float CounterWindowSeconds => counterWindowSeconds;

    
    
    
    
    public bool CanUseCounter
    {
        get
        {
            if (!IsPlayerAlive)           return false;
            if (!_isReady || _counterTarget == null) return false;
            var h = _counterTarget.GetComponent<HealthComponent>()
                 ?? _counterTarget.GetComponentInParent<HealthComponent>();
            return h != null && !h.IsDead;
        }
    }

    
    public bool IsReady => _isReady;
    public float RemainingWindow => _remainingWindow;

    

    
    
    
    
    
    public bool TryUseCounter(PlayerSkillData skillData)
    {
        
        if (!IsPlayerAlive)
        {
            Debug.Log("[RadiantRiposte] 玩家已死亡 — 反撃不可。");
            ClearCounter();
            return false;
        }

        if (!CanUseCounter)
        {
            if (_isReady && _counterTarget == null)
            {
                Debug.Log("[RadiantRiposte] 攻击者が null のため反撃失败。Ready 清除。");
                ClearCounter();
            }
            else if (_isReady)
            {
                var hCheck = _counterTarget.GetComponent<HealthComponent>()
                          ?? _counterTarget.GetComponentInParent<HealthComponent>();
                if (hCheck != null && hCheck.IsDead)
                {
                    Debug.Log("[RadiantRiposte] 攻击者已死亡，反撃失败。Ready 清除。");
                    ClearCounter();
                }
            }
            return false;
        }

        
        
        if (skillData != null)
        {
            float maxRange = skillData.EffectiveRange;
            if (maxRange > 0f)
            {
                float dist = Vector3.Distance(transform.position, _counterTarget.position);
                if (dist > maxRange)
                {
                    Debug.Log($"[RadiantRiposte] 攻击者が射程外 ({dist:F1}m > {maxRange}m) — 反撃失败。Ready は保持。");
                    return false; 
                }
            }
        }

        var targetHealth = _counterTarget.GetComponent<HealthComponent>()
                        ?? _counterTarget.GetComponentInParent<HealthComponent>();

        float basePdu = combatStats != null ? combatStats.BaseNormalAttackDamage : 20f;
        float damage  = basePdu * counterDamagePdu;

        var sourceLabel = new CombatTextSourceLabel
        {
            localizationKey = skillData != null ? skillData.LocalizationKey : "skill.player.radiant_riposte.name",
            fallbackText    = skillData != null ? skillData.SkillName        : "Radiant Riposte"
        };

        targetHealth.TakeDamage(damage, transform, sourceLabel);
        Debug.Log($"[RadiantRiposte] 守護反击命中！ 目标: {targetHealth.name} | 伤害: {damage} ({counterDamagePdu} PDU) | 来源: {sourceLabel.GetDisplayText()}");

        ClearCounter();
        return true;
    }

    

    private void ClearCounter()
    {
        _isReady         = false;
        _counterTarget   = null;
        _remainingWindow = 0f;
    }

    private void ResolveHikariSupport()
    {
        if (hikariSupport == null)
            hikariSupport = Object.FindFirstObjectByType<HikariSupportController>();

        if (hikariSupport == null)
        {
            Debug.LogWarning("[RadiantRiposte] HikariSupportController が見つかりません。");
            return;
        }

        hikariSupport.OnGuardResonanceTriggered += HandleGuardResonanceTriggered;
        Debug.Log($"[RadiantRiposte] HikariSupportController に購読完了: {hikariSupport.gameObject.name}");
    }

    private void UnsubscribeFromHikari()
    {
        if (hikariSupport != null)
            hikariSupport.OnGuardResonanceTriggered -= HandleGuardResonanceTriggered;
    }

    private void SubscribeToPlayerDeath()
    {
        if (_playerHealth != null)
            _playerHealth.OnDied += HandlePlayerDied;
    }

    private void UnsubscribeFromPlayerDeath()
    {
        if (_playerHealth != null)
            _playerHealth.OnDied -= HandlePlayerDied;
    }
}
