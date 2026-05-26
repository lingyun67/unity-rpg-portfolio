/// <summary>
/// 敵スキルの詠唱、クールダウン、範囲提示、ダメージ判定を実行するコンポーネント。CastAttack / CircleAoE / DonutAoE に対応する。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;











public class EnemySkillController : MonoBehaviour
{
    [Header("スキルリスト（Inspector で設定）")]
    [SerializeField] private List<EnemySkillData> _skills = new List<EnemySkillData>();

    
    private readonly Dictionary<EnemySkillData, float> _nextAvailableTime
        = new Dictionary<EnemySkillData, float>();

    
    private Coroutine      _currentCastCoroutine;
    private EnemySkillData _currentSkill;
    private Transform      _currentCastTarget;
    private float          _currentCastElapsed;
    private float          _currentCastDuration;

    
    private EnemyAI         _enemyAI;
    private HealthComponent _healthComponent;

    

    
    public bool IsCasting { get; private set; }

    
    public EnemySkillData CurrentSkill => _currentSkill;

    
    public float CurrentCastElapsed => _currentCastElapsed;

    
    public float CurrentCastDuration => _currentCastDuration;

    
    public float CurrentCastRemaining => Mathf.Max(0f, _currentCastDuration - _currentCastElapsed);

    
    public float CurrentCastProgress =>
        _currentCastDuration > 0f
            ? Mathf.Clamp01(_currentCastElapsed / _currentCastDuration)
            : (_currentCastDuration <= 0f && IsCasting ? 1f : 0f);

    
    public IReadOnlyList<EnemySkillData> Skills => _skills;

    
    public bool HasAnySkill => _skills != null && _skills.Count > 0;

    
    void Awake()
    {
        _enemyAI         = GetComponent<EnemyAI>();
        _healthComponent = GetComponent<HealthComponent>();
    }

    
    
    
    
    void OnDisable()
    {
        if (IsCasting)
        {
            if (_currentCastCoroutine != null)
            {
                StopCoroutine(_currentCastCoroutine);
                _currentCastCoroutine = null;
            }
            CleanupCast();
        }
    }

    

    public bool CanUseSkill(EnemySkillData skill)
    {
        if (skill == null) return false;
        if (IsCasting)     return false;
        return IsSkillReady(skill);
    }

    public bool IsSkillReady(EnemySkillData skill)
    {
        if (skill == null) return false;
        if (!_nextAvailableTime.TryGetValue(skill, out float nextTime))
            return true;
        return Time.time >= nextTime;
    }

    

    public void StartCooldown(EnemySkillData skill)
    {
        if (skill == null) return;
        _nextAvailableTime[skill] = Time.time + skill.Cooldown;
    }

    

    public bool TryGetReadySkillInRange(Transform target, out EnemySkillData skill)
    {
        skill = null;
        if (target == null)                         return false;
        if (_skills == null || _skills.Count == 0) return false;

        float distToTarget = Vector3.Distance(transform.position, target.position);
        foreach (var s in _skills)
        {
            if (s == null)              continue;
            if (!IsSkillReady(s))       continue;
            if (distToTarget > s.Range) continue;
            skill = s;
            return true;
        }
        return false;
    }

    

    
    
    
    
    public bool TryStartSkill(EnemySkillData skill, Transform target)
    {
        if (skill == null)                                return false;
        if (target == null && skill.SkillType == EnemySkillType.CastAttack) return false;
        if (IsCasting)                                    return false;
        if (!CanUseSkill(skill))                          return false;
        
        if (skill.SkillType == EnemySkillType.CastAttack)
        {
            _currentCastCoroutine = StartCoroutine(CastAttackRoutine(skill, target));
        }
        else if (skill.SkillType == EnemySkillType.CircleAoE)
        {
            _currentCastCoroutine = StartCoroutine(CircleAoERoutine(skill));
        }
        else if (skill.SkillType == EnemySkillType.DonutAoE)
        {
            _currentCastCoroutine = StartCoroutine(DonutAoERoutine(skill));
        }
        else
        {
            return false;
        }
        return true;
    }

    
    
    
    
    
    

    
    
    
    
    
    

    
    
    
    
    
    
    private IEnumerator DonutAoERoutine(EnemySkillData skill)
    {
        IsCasting            = true;
        _currentSkill        = skill;
        _currentCastTarget   = null;
        _currentCastElapsed  = 0f;
        _currentCastDuration = skill.CastTime;

        float inner = skill.AoeInnerRadius;
        float outer = skill.AoeOuterRadius;
        Debug.Log($"[EnemySkillController] {gameObject.name}: DonutAoE 読条開始 [{skill.DisplayName}] inner={inner} outer={outer} castTime={skill.CastTime}s");

        
        GameObject telegraph = null;
        if (skill.AoeTelegraphPrefab != null)
        {
            telegraph = Instantiate(skill.AoeTelegraphPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            
            telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var c = telegraph.GetComponent<Collider>();
            if (c != null) Destroy(c);
            var r = telegraph.GetComponent<Renderer>();
            if (r != null)
            {
                var m = new Material(Shader.Find("Standard"));
                m.color = new Color(1f, 0.15f, 0.05f, 0.4f);
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = 3000;
                r.material = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        
        if (telegraph != null)
        {
            telegraph.transform.position = transform.position;
            var ctrl = telegraph.GetComponent<DonutAoETelegraphController>();
            if (ctrl != null)
                ctrl.Setup(inner, outer);
        }

        
        while (_currentCastElapsed < _currentCastDuration)
        {
            if (_healthComponent != null && _healthComponent.IsDead)
            {
                if (telegraph != null) Destroy(telegraph);
                CleanupCast();
                yield break;
            }
            if (telegraph != null)
                telegraph.transform.position = transform.position;
            _currentCastElapsed += Time.deltaTime;
            yield return null;
        }
        _currentCastElapsed = _currentCastDuration;

        
        if (telegraph != null) { Destroy(telegraph); telegraph = null; }

        
        if (_healthComponent != null && _healthComponent.IsDead)
        {
            Debug.Log($"[EnemySkillController] {gameObject.name}: DonutAoE caster 死亡 — 判定なし");
        }
        else
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                Vector3 centerFlat = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 playerFlat = new Vector3(playerGO.transform.position.x, 0f, playerGO.transform.position.z);
                float dist = Vector3.Distance(centerFlat, playerFlat);
                bool hit = dist > inner && dist <= outer;
                if (hit)
                {
                    var ph = playerGO.GetComponent<HealthComponent>();
                    if (ph != null && !ph.IsDead)
                    {
                        
                        ph.TakeDamage(skill.Damage, transform);
                        Debug.Log($"[EnemySkillController] {gameObject.name}: [{skill.DisplayName}] DonutAoE 命中 dist={dist:F2} inner={inner} outer={outer} dmg={skill.Damage}");
                    }
                }
                else
                {
                    string reason = dist <= inner ? "内圈安全区" : "外圈之外";
                    Debug.Log($"[EnemySkillController] {gameObject.name}: [{skill.DisplayName}] DonutAoE 不命中 ({reason}) dist={dist:F2}");
                }
            }
        }

        StartCooldown(skill);
        CleanupCast();
    }

    private IEnumerator CircleAoERoutine(EnemySkillData skill)
    {
        IsCasting            = true;
        _currentSkill        = skill;
        _currentCastTarget   = null; 
        _currentCastElapsed  = 0f;
        _currentCastDuration = skill.CastTime;

        Debug.Log($"[EnemySkillController] {gameObject.name}: CircleAoE 読条開始 [{skill.DisplayName}] radius={skill.AoeRadius} castTime={skill.CastTime}s");

        
        GameObject telegraph = null;
        if (skill.AoeTelegraphPrefab != null)
        {
            telegraph = Instantiate(skill.AoeTelegraphPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            
            telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var col = telegraph.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var rend = telegraph.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0.15f, 0.05f, 0.45f);
                mat.SetFloat("_Mode", 3f);            
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                rend.material   = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        
        if (telegraph != null)
        {
            float d = skill.AoeRadius * 2f;
            telegraph.transform.localScale = new Vector3(d, 0.05f, d);
            telegraph.transform.position   = transform.position; 
        }

        
        while (_currentCastElapsed < _currentCastDuration)
        {
            if (_healthComponent != null && _healthComponent.IsDead)
            {
                Debug.Log($"[EnemySkillController] {gameObject.name}: CircleAoE 中断（caster 死亡）");
                if (telegraph != null) Destroy(telegraph);
                CleanupCast();
                yield break;
            }
            
            if (telegraph != null)
                telegraph.transform.position = transform.position;

            _currentCastElapsed += Time.deltaTime;
            yield return null;
        }
        _currentCastElapsed = _currentCastDuration;

        
        if (telegraph != null)
        {
            Destroy(telegraph);
            telegraph = null;
        }

        
        if (_healthComponent != null && _healthComponent.IsDead)
        {
            Debug.Log($"[EnemySkillController] {gameObject.name}: CircleAoE 読条完了時 caster 死亡 — 判定なし");
        }
        else
        {
            
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                Vector3 centerFlat = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 playerFlat = new Vector3(playerGO.transform.position.x, 0f, playerGO.transform.position.z);
                float dist = Vector3.Distance(centerFlat, playerFlat);

                if (dist <= skill.AoeRadius)
                {
                    var playerHealth = playerGO.GetComponent<HealthComponent>();
                    if (playerHealth != null && !playerHealth.IsDead)
                    {
                        
                        
                        playerHealth.TakeDamage(skill.Damage, transform);
                        Debug.Log($"[EnemySkillController] {gameObject.name}: [{skill.DisplayName}] CircleAoE 命中 dist={dist:F2}/{skill.AoeRadius} dmg={skill.Damage}");
                    }
                }
                else
                {
                    Debug.Log($"[EnemySkillController] {gameObject.name}: [{skill.DisplayName}] CircleAoE 範囲外 — 不命中 dist={dist:F2}/{skill.AoeRadius}");
                }
            }
        }

        StartCooldown(skill);
        CleanupCast();
    }

private IEnumerator CastAttackRoutine(EnemySkillData skill, Transform target)
    {
        IsCasting             = true;
        _currentSkill         = skill;
        _currentCastTarget    = target;
        _currentCastElapsed   = 0f;
        _currentCastDuration  = skill.CastTime;

        Debug.Log($"[EnemySkillController] {gameObject.name}: 読条開始 [{skill.DisplayName}] castTime={skill.CastTime}s");

        
        while (_currentCastElapsed < _currentCastDuration)
        {
            
            if (_healthComponent != null && _healthComponent.IsDead)
            {
                Debug.Log($"[EnemySkillController] {gameObject.name}: 施法中断（caster 死亡）");
                CleanupCast();
                yield break;
            }
            
            if (target == null)
            {
                Debug.Log($"[EnemySkillController] {gameObject.name}: 施法中断（target が null）");
                CleanupCast();
                yield break;
            }
            _currentCastElapsed += Time.deltaTime;
            yield return null;
        }
        _currentCastElapsed = _currentCastDuration; 

        
        bool hit = false;
        if (skill != null && target != null)
        {
            
            if (_healthComponent != null && _healthComponent.IsDead)
            {
                Debug.Log($"[EnemySkillController] {gameObject.name}: 読条完了時 caster 死亡 — ダメージなし");
            }
            else
            {
                var targetHealth = target.GetComponent<HealthComponent>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    
                    _lastDamageSkillData = skill;
                    _lastDamageSkillTime  = Time.time;
                    targetHealth.TakeDamage(skill.Damage, transform);
                    Debug.Log($"[EnemySkillController] {gameObject.name}: [{skill.DisplayName}] 命中！ダメージ={skill.Damage}（距離チェックなし）");
                    hit = true;
                }
            }
        }

        if (!hit)
            Debug.Log($"[EnemySkillController] {gameObject.name}: [{skill?.DisplayName}] 不命中（target 死亡 / caster 死亡 / target null）");

        StartCooldown(skill);
        CleanupCast();
    }

    

    
    
    
    
    public void CancelCasting(string reason)
    {
        if (!IsCasting) return;

        if (_currentCastCoroutine != null)
        {
            StopCoroutine(_currentCastCoroutine);
            _currentCastCoroutine = null;
        }

        Debug.Log($"[EnemySkillController] {gameObject.name}: 施法キャンセル [{_currentSkill?.DisplayName}] 理由={reason}");
        CleanupCast();
    }


    
    
    
    public void InterruptCurrentCast()
    {
        CancelCasting("Interrupted");
    }


    

    private EnemySkillData _lastDamageSkillData;
    private float          _lastDamageSkillTime = -999f;

    
    public EnemySkillData LastDamageSkillData => _lastDamageSkillData;
    
    public float          LastDamageSkillTime  => _lastDamageSkillTime;

    
    
    private void CleanupCast()
    {
        IsCasting             = false;
        _currentSkill         = null;
        _currentCastTarget    = null;
        _currentCastCoroutine = null;
        _currentCastElapsed   = 0f;
        _currentCastDuration  = 0f;
    }
}
