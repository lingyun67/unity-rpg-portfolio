/// <summary>
/// プレイヤースキルの実行時状態と入力分配を管理するコンポーネント。New Input System の入力を各スキル実行担当へ振り分ける。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;







[System.Serializable]
public class PlayerSkillRuntimeState
{
    [SerializeField] private PlayerSkillData skillData;
    [SerializeField] private float           activeRemainingTime;
    [SerializeField] private float           cooldownRemainingTime;

    

    public PlayerSkillData SkillData             => skillData;
    public string          SkillId               => skillData != null ? skillData.SkillId : "";
    public bool            IsActive              => activeRemainingTime   > 0f;
    public bool            IsOnCooldown          => cooldownRemainingTime > 0f;
    public bool            IsReady               => skillData != null && !IsActive && !IsOnCooldown;
    public float           ActiveRemainingTime   => activeRemainingTime;
    public float           CooldownRemainingTime => cooldownRemainingTime;

    
    public float CooldownNormalized
    {
        get
        {
            if (skillData == null || skillData.Cooldown <= 0f) return 0f;
            return Mathf.Clamp01(cooldownRemainingTime / skillData.Cooldown);
        }
    }

    
    public float ActiveNormalized
    {
        get
        {
            if (skillData == null || skillData.Duration <= 0f) return 0f;
            return Mathf.Clamp01(activeRemainingTime / skillData.Duration);
        }
    }

    

    public void Initialize(PlayerSkillData data)
    {
        skillData             = data;
        activeRemainingTime   = 0f;
        cooldownRemainingTime = 0f;
    }

    

    public void Tick(float deltaTime)
    {
        if (activeRemainingTime   > 0f) activeRemainingTime   = Mathf.Max(0f, activeRemainingTime   - deltaTime);
        if (cooldownRemainingTime > 0f) cooldownRemainingTime = Mathf.Max(0f, cooldownRemainingTime - deltaTime);
    }

    

    
    
    
    
    
    
    public bool TryActivate()
    {
        if (skillData == null)    return false;
        if (IsActive)             return false;
        if (IsOnCooldown)         return false;

        activeRemainingTime   = skillData.Duration;
        cooldownRemainingTime = skillData.Cooldown;
        return true;
    }

    

    public bool MatchesSkillId(string id)              => skillData != null && skillData.SkillId == id;
    public bool MatchesInputSlot(PlayerSkillInputSlot slot) => skillData != null && skillData.InputSlot == slot;
}














public class PlayerSkillManager : MonoBehaviour
{
    [Header("技能数据")]
    [SerializeField] private PlayerSkillData[] skills;

    [Header("调试")]
    [SerializeField] private bool logSkillActivation = true;

    
    [SerializeField]
    private List<PlayerSkillRuntimeState> runtimeStates = new List<PlayerSkillRuntimeState>();


    
    private PlayerSkillRuntimeState lastPressedSkillState;
    private PlayerGuardCounterController _guardCounterController;
    private PlayerBasicAttackController      _basicAttackController;
    private HealthComponent                  _playerHealth;

    

    

    
    
    
    
    public event Action<PlayerSkillRuntimeState> OnSkillActivated;

    public PlayerSkillRuntimeState LastPressedSkillState => lastPressedSkillState;

    

    
    public IReadOnlyList<PlayerSkillRuntimeState> RuntimeStates => runtimeStates;

    
    public PlayerSkillRuntimeState GetStateBySkillId(string skillId)
    {
        foreach (var s in runtimeStates)
            if (s.MatchesSkillId(skillId)) return s;
        return null;
    }

    
    public PlayerSkillRuntimeState GetStateByInputSlot(PlayerSkillInputSlot slot)
    {
        foreach (var s in runtimeStates)
            if (s.MatchesInputSlot(slot)) return s;
        return null;
    }

    
    public bool TryActivateSkillById(string skillId)
    {
        var state = GetStateBySkillId(skillId);
        if (state == null) return false;
        return TryActivateSkill(state);
    }

    
    public bool TryActivateSkillByInputSlot(PlayerSkillInputSlot slot)
    {
        var state = GetStateByInputSlot(slot);
        if (state == null) return false;
        return TryActivateSkill(state);
    }

    

    private void Awake()
    {
        BuildRuntimeStates();
        _guardCounterController = GetComponent<PlayerGuardCounterController>();
        _basicAttackController  = GetComponent<PlayerBasicAttackController>();
        _playerHealth = GetComponent<HealthComponent>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        foreach (var state in runtimeStates)
            state.Tick(dt);

        HandleSkillInput();
    }

    

    private void BuildRuntimeStates()
    {
        runtimeStates.Clear();

        if (skills == null || skills.Length == 0)
        {
            Debug.LogWarning("[PlayerSkillManager] skills 数组为空，没有任何技能被注册。");
            return;
        }

        var registeredIds = new HashSet<string>();

        foreach (var skill in skills)
        {
            if (skill == null)
            {
                Debug.LogWarning("[PlayerSkillManager] skills 数组中存在 null 元素，已跳过。");
                continue;
            }

            if (registeredIds.Contains(skill.SkillId))
            {
                Debug.LogWarning($"[PlayerSkillManager] 重复的 skillId: \"{skill.SkillId}\"，已跳过。");
                continue;
            }

            var state = new PlayerSkillRuntimeState();
            state.Initialize(skill);
            runtimeStates.Add(state);
            registeredIds.Add(skill.SkillId);
        }

        Debug.Log($"[PlayerSkillManager] 注册了 {runtimeStates.Count} 个技能。");
    }

    

private void HandleSkillInput()
    {
        
        if (_playerHealth != null && _playerHealth.IsDead) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        foreach (var state in runtimeStates)
        {
            if (state.SkillData == null) continue;

            if (WasInputSlotPressed(state.SkillData.InputSlot))
            {
                
                lastPressedSkillState = state;
                
                if (state.SkillData.EffectType == PlayerSkillEffectType.BasicMeleeAttack)
                {
                    _basicAttackController?.TryExecuteBasicMeleeAttack(state.SkillData);
                }
                else if (state.SkillData.EffectType == PlayerSkillEffectType.BasicAreaAttack)
                {
                    _basicAttackController?.TryExecuteBasicAreaAttack(state.SkillData);
                }
                else if (state.SkillData.EffectType == PlayerSkillEffectType.GuardCounter)
                {
                    if (_guardCounterController != null)
                        _guardCounterController.TryUseCounter(state.SkillData);
                }
                else
                {
                    TryActivateSkill(state);
                }
            }
        }
    }

    private bool TryActivateSkill(PlayerSkillRuntimeState state)
    {
        if (state == null || state.SkillData == null) return false;

        if (state.TryActivate())
        {
            OnSkillActivated?.Invoke(state);
            if (logSkillActivation)
                Debug.Log($"[PlayerSkillManager] Activated skill: {state.SkillData.SkillName} ({state.SkillData.SkillId})");
            return true;
        }

        
        if (logSkillActivation)
        {
            if (state.IsActive)
                Debug.Log($"[PlayerSkillManager] {state.SkillData.SkillName}: 技能正在生效中（{state.ActiveRemainingTime:F1}s 剩余）。");
            else if (state.IsOnCooldown)
                Debug.Log($"[PlayerSkillManager] {state.SkillData.SkillName}: 冷却中（{state.CooldownRemainingTime:F1}s 剩余）。");
        }
        return false;
    }

    

    private bool WasInputSlotPressed(PlayerSkillInputSlot slot)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        switch (slot)
        {
            case PlayerSkillInputSlot.Slot1: return kb.digit1Key.wasPressedThisFrame;
            case PlayerSkillInputSlot.Slot2: return kb.digit2Key.wasPressedThisFrame;
            case PlayerSkillInputSlot.Slot3: return kb.digit3Key.wasPressedThisFrame;
            case PlayerSkillInputSlot.Slot4: return kb.digit4Key.wasPressedThisFrame;
            case PlayerSkillInputSlot.Slot5: return kb.digit5Key.wasPressedThisFrame;
            case PlayerSkillInputSlot.Slot6: return kb.digit6Key.wasPressedThisFrame;
            case PlayerSkillInputSlot.Slot7: return kb.digit7Key.wasPressedThisFrame;
            case PlayerSkillInputSlot.Slot8: return kb.digit8Key.wasPressedThisFrame;
            case PlayerSkillInputSlot.Slot9: return kb.digit9Key.wasPressedThisFrame;
            default:                         return false;
        }
    }
}
