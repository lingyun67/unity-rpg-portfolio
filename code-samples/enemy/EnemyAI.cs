/// <summary>
/// 敵の有限状態機械を管理するコンポーネント。徘徊、追跡、通常攻撃、スポーン地点帰還、NavMeshAgent 優先移動を扱う。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Idle, Chase, Attack, ReturnToSpawn, Wander }













public class EnemyAI : MonoBehaviour
{
    [Header("检测")]
    public FOVDetector fovDetector;

    [Header("移动")]
    public float moveSpeed        = 3.5f;
    public float stoppingDistance = 1.2f;
    public float rotationSpeed    = 6f;
    public Rigidbody rb;

    [Header("攻击")]
    public float attackRange  = 1.5f;
    public float attackDamage = 10f;
    [Tooltip("两次攻击之间的冷却时间（秒）")]
    public float attackCooldown = 2f;

    [Header("动画")]
    public Animator animator;

    [Header("仇恨")]
    [SerializeField] private float sightHateAmount      = 10f;
    [SerializeField] private float damageHateMultiplier = 1f;
    [SerializeField] private float disengageDistance    = 15f;
    [SerializeField] private float disengageDelay       = 3f;
    private float disengageTimer = 0f;

    [Header("返回出生点")]
    [SerializeField] private float returnToSpawnStopDistance   = 0.5f;
    [Tooltip("出生点の NavMesh サンプリング最大距離（m）")]
    [SerializeField] private float returnNavMeshSampleDistance = 3f;

    [Header("活动范围")]
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float leashRadius  = 25f;

    [Header("游荡")]
    [Tooltip("游荡移动速度（建议低于 moveSpeed）")]
    [SerializeField] private float wanderMoveSpeed          = 2f;
    [Tooltip("判定到达游荡目标点的距离阈值")]
    [SerializeField] private float wanderPointReachDistance = 0.8f;
    [Tooltip("每次待机的最短时间（秒）")]
    [SerializeField] private float minIdleTime              = 2f;
    [Tooltip("每次待机的最长时间（秒）")]
    [SerializeField] private float maxIdleTime              = 5f;

    [Header("NavMesh Chase")]
    [Tooltip("Chase 中に Agent destination を更新する間隔（秒）")]
    [SerializeField] private float chaseDestinationUpdateInterval = 0.2f;
    [Tooltip("目標位置の NavMesh サンプリング最大距離（m）")]
    [SerializeField] private float chaseNavMeshSampleDistance     = 2f;

    
    public EnemyState currentState { get; private set; } = EnemyState.Idle;

    private Transform        currentTarget;
    private Vector3          moveDirection = Vector3.zero;
    private FactionComponent myFaction;
    private HealthComponent  myHealth;
    private float            attackCooldownTimer = 0f;

    
    private readonly Dictionary<Transform, float> hateTable = new Dictionary<Transform, float>();

    
    private float scanTimer = 0f;
    private const float scanInterval = 0.2f;

    
    private Vector3    _spawnPosition;
    private Quaternion _spawnRotation;

    
    private Vector3 _spawnAreaCenter;      
    private bool    _hasSpawnAreaContext;  

    private Vector3 WanderCenter => _hasSpawnAreaContext ? _spawnAreaCenter : _spawnPosition;
    private Vector3 LeashCenter  => _hasSpawnAreaContext ? _spawnAreaCenter : _spawnPosition;

    
    private Vector3 _wanderTarget;
    private float   _idleTimer = 0f;

    
    private NavMeshAgent _agent;
    private bool         _hasAgent;
    private NavMeshPath  _wanderPath;              
    private NavMeshPath  _returnPath;              
    private NavMeshPath  _chasePath;               
    private bool         _returningWithAgent;      
    private bool         _chasingWithAgent;        
    private float        _nextChaseDestinationUpdateTime; 

    
    
    private Vector3 _lastValidChaseDestination;
    
    private bool    _hasLastValidChaseDestination;
    
    private EnemySkillController _skillController;


    
    void Awake()
    {
        if (animator    == null) animator    = GetComponent<Animator>();
        if (fovDetector == null) fovDetector = GetComponent<FOVDetector>();
        myFaction = GetComponent<FactionComponent>();
        myHealth  = GetComponent<HealthComponent>();

        _spawnPosition      = transform.position;
        _spawnRotation      = transform.rotation;
        _spawnAreaCenter    = _spawnPosition;
        _hasSpawnAreaContext = false;
        _skillController    = GetComponent<EnemySkillController>();
    }

    void OnEnable()
    {
        if (myHealth != null) myHealth.OnDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        if (myHealth != null) myHealth.OnDamaged -= HandleDamaged;
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        _agent    = GetComponent<NavMeshAgent>();
        _hasAgent = _agent != null;
        if (_hasAgent)
        {
            _wanderPath      = new NavMeshPath();
            _returnPath      = new NavMeshPath();
            _chasePath       = new NavMeshPath();
            _agent.isStopped = true;
        }

        SetupIdleTimer();

        if (wanderRadius > 0f && wanderRadius > leashRadius)
            Debug.LogWarning($"[EnemyAI] {gameObject.name}: wanderRadius({wanderRadius}) が leashRadius({leashRadius}) を超えています。");
    }

    
    
    private bool CanUseAgent() =>
        _hasAgent && _agent != null && _agent.enabled && _agent.isOnNavMesh;

    

    
    
    
    
    private void ClearRigidbodyVelocity()
    {
        if (rb == null) return;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    
    
    
    
    
    private void StopAgentMovement(bool resetPath = false)
    {
        if (!_hasAgent || _agent == null || !_agent.enabled) return;
        _agent.isStopped = true;
        if (resetPath && _agent.isOnNavMesh) _agent.ResetPath();
    }

    
    
    
    
    private void PrepareAgentDrivenMovement()
    {
        ClearRigidbodyVelocity();
    }

    
    
    
    
    
    private void StopMovementForAttack()
    {
        StopAgentMovement(true);
        if (rb != null) rb.isKinematic = false;
        ClearRigidbodyVelocity();
    }

    
    
    
    
    private void StopAgentAndRestoreRigidbody()
    {
        StopAgentMovement(true);
        if (rb != null) rb.isKinematic = false;
        ClearRigidbodyVelocity();
    }

    void Update()
    {
        if (currentState == EnemyState.ReturnToSpawn) return;

        CheckLeashRadius();
        if (currentState == EnemyState.ReturnToSpawn) return;

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            ScanForTarget();
            scanTimer = scanInterval;
        }
        UpdateDisengage();
        UpdateState();

        if (currentState == EnemyState.Idle || currentState == EnemyState.Wander)
            UpdateIdleWanderCycle();
    }

    
    void UpdateDisengage()
    {
        if (currentTarget == null) { disengageTimer = 0f; return; }

        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist > disengageDistance)
        {
            disengageTimer += Time.deltaTime;
            if (disengageTimer >= disengageDelay)
            {
                disengageTimer = 0f;
                EnterReturnToSpawn();
            }
        }
        else { disengageTimer = 0f; }
    }

    
    private void EnterReturnToSpawn()
    {
        
        if (currentState == EnemyState.Wander && CanUseAgent())
            StopAgentAndRestoreRigidbody();
        else if (currentState == EnemyState.Chase && _chasingWithAgent)
            StopAgentAndRestoreRigidbody();

        _chasingWithAgent             = false;
        _hasLastValidChaseDestination = false;  
        _skillController?.CancelCasting("ReturnToSpawn");

        hateTable.Clear();
        currentTarget       = null;
        disengageTimer      = 0f;
        attackCooldownTimer = 0f;
        currentState        = EnemyState.ReturnToSpawn;
        animator?.SetBool("IsAttacking", false);

        
        _returningWithAgent = false;
        if (CanUseAgent())
        {
            if (NavMesh.SamplePosition(_spawnPosition, out NavMeshHit spawnHit,
                                       returnNavMeshSampleDistance, NavMesh.AllAreas))
            {
                _agent.CalculatePath(spawnHit.position, _returnPath);
                if (_returnPath.status == NavMeshPathStatus.PathComplete)
                {
                    _returningWithAgent = true;
                    PrepareAgentDrivenMovement();  
                    rb.isKinematic      = true;
                    _agent.speed        = moveSpeed;
                    _agent.isStopped    = false;
                    _agent.SetDestination(spawnHit.position);
                }
            }
        }

        if (!_returningWithAgent)
        {
            StopAgentMovement(true);
            if (rb != null) rb.isKinematic = false;
        }

        Debug.Log($"[EnemyAI] {gameObject.name} 开始返回出生点。(Agent={_returningWithAgent})");
    }

    private float GetHorizontalDistanceFromSpawn()
    {
        Vector3 currentFlat = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 centerFlat  = new Vector3(LeashCenter.x,         0f, LeashCenter.z);
        return Vector3.Distance(currentFlat, centerFlat);
    }

    private void CheckLeashRadius()
    {
        if (currentState == EnemyState.ReturnToSpawn) return;
        if (!enabled) return;
        if (myHealth != null && myHealth.IsDead) return;

        if (GetHorizontalDistanceFromSpawn() > leashRadius)
        {
            Debug.Log($"[EnemyAI] {gameObject.name} 超过活动边界（leashRadius={leashRadius}），开始返回出生点。");
            EnterReturnToSpawn();
        }
    }

    private void HandleReturnToSpawn()
    {
        if (!enabled) return;

        if (_returningWithAgent)
        {
            if (CanUseAgent())
            {
                Vector3 vel = _agent.velocity;
                moveDirection = vel.sqrMagnitude > 0.01f
                    ? new Vector3(vel.x, 0f, vel.z).normalized
                    : Vector3.zero;

                if (!_agent.pathPending && _agent.remainingDistance <= returnToSpawnStopDistance)
                    FinishReturnToSpawn();
                return;
            }
            else
            {
                
                _returningWithAgent = false;
                StopAgentMovement(true);
                if (rb != null) rb.isKinematic = false;
            }
        }

        Vector3 currentFlat    = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 spawnFlat      = new Vector3(_spawnPosition.x,     0f, _spawnPosition.z);
        float   horizontalDist = Vector3.Distance(currentFlat, spawnFlat);

        if (horizontalDist > returnToSpawnStopDistance)
        {
            moveDirection = new Vector3(
                _spawnPosition.x - transform.position.x, 0f,
                _spawnPosition.z - transform.position.z).normalized;

            rb.linearVelocity = new Vector3(
                moveDirection.x * moveSpeed,
                rb.linearVelocity.y,
                moveDirection.z * moveSpeed);

            if (moveDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    Quaternion.LookRotation(moveDirection),
                    rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            FinishReturnToSpawn();
        }
    }

    private void FinishReturnToSpawn()
    {
        bool wasUsingAgent  = _returningWithAgent;
        _returningWithAgent = false;

        StopAgentMovement(true);

        if (!wasUsingAgent)
            transform.position = new Vector3(_spawnPosition.x, transform.position.y, _spawnPosition.z);
        transform.rotation = _spawnRotation;

        if (rb != null) rb.isKinematic = false;
        ClearRigidbodyVelocity();

        if (myHealth != null) myHealth.RestoreFullHealth();

        currentState        = EnemyState.Idle;
        attackCooldownTimer = 0f;
        moveDirection       = Vector3.zero;
        SetupIdleTimer();
        animator?.SetBool("IsAttacking", false);
        animator?.SetFloat("Speed", 0f);
        Debug.Log($"[EnemyAI] {gameObject.name} 已回到出生点，重置完成。");
    }

    
    private bool IsValidTarget(Transform t)
    {
        if (t == null) return false;
        var h = t.GetComponent<HealthComponent>();
        if (h == null || h.IsDead) return false;
        if (myFaction == null) return false;
        var fc = t.GetComponent<FactionComponent>();
        if (fc == null) return false;
        return myFaction.ShouldAttack(fc.faction);
    }

    private void AddHate(Transform target, float amount)
    {
        if (!IsValidTarget(target)) return;
        if (hateTable.ContainsKey(target)) hateTable[target] += amount;
        else hateTable[target] = amount;
        SelectHighestHateTarget();
    }

    private void RemoveInvalidHateTargets()
    {
        var invalid = hateTable.Keys.Where(t => !IsValidTarget(t)).ToList();
        foreach (var t in invalid) hateTable.Remove(t);
    }

    private void SelectHighestHateTarget()
    {
        RemoveInvalidHateTargets();
        if (hateTable.Count == 0) { currentTarget = null; return; }
        currentTarget = hateTable.OrderByDescending(kv => kv.Value).First().Key;
    }

    
    void ScanForTarget()
    {
        SelectHighestHateTarget();
        if (currentTarget != null) return;
        if (fovDetector == null) return;
        foreach (var fc in FindObjectsOfType<FactionComponent>())
        {
            if (fc.gameObject == gameObject) continue;
            if (myFaction != null && !myFaction.ShouldAttack(fc.faction)) continue;
            if (fovDetector.CanSeeTarget(fc.transform))
            {
                if (!hateTable.ContainsKey(fc.transform))
                    AddHate(fc.transform, sightHateAmount);
                break;
            }
        }
    }

    void HandleDamaged(float amount, Transform attacker)
    {
        AddHate(attacker, amount * damageHateMultiplier);
    }

    
void UpdateState()
    {
        if (currentState == EnemyState.ReturnToSpawn) return;

        if (currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);

            
            
            if (currentState == EnemyState.Attack &&
                _skillController != null && _skillController.IsCasting)
                return;

            TransitionTo(dist <= attackRange ? EnemyState.Attack : EnemyState.Chase);
            return;
        }

        if (currentState == EnemyState.Idle || currentState == EnemyState.Wander) return;
        TransitionTo(EnemyState.Idle);
    }

    void TransitionTo(EnemyState next)
    {
        if (currentState == next) return;

        
        if (currentState == EnemyState.Wander && CanUseAgent())
            StopAgentAndRestoreRigidbody();
        else if (currentState == EnemyState.Chase && _chasingWithAgent)
            StopAgentAndRestoreRigidbody();

        
        if (currentState == EnemyState.Attack)
            _skillController?.CancelCasting("LeaveAttack");

        
        if (currentState == EnemyState.Chase)
        {
            _chasingWithAgent             = false;
            _hasLastValidChaseDestination = false;
        }

        currentState = next;
        switch (next)
        {
            case EnemyState.Idle:
                animator?.SetBool("IsAttacking", false);
                SetupIdleTimer();
                break;

            case EnemyState.Wander:
                animator?.SetBool("IsAttacking", false);
                if (CanUseAgent())
                {
                    PrepareAgentDrivenMovement();  
                    rb.isKinematic   = true;
                    _agent.speed     = wanderMoveSpeed;
                    _agent.isStopped = false;
                    _agent.SetDestination(_wanderTarget);
                }
                break;

            case EnemyState.Chase:
                animator?.SetBool("IsAttacking", false);
                _chasingWithAgent               = false;
                _nextChaseDestinationUpdateTime = 0f;
                if (CanUseAgent())
                {
                    _chasingWithAgent = true;
                    PrepareAgentDrivenMovement();  
                    rb.isKinematic    = true;
                    _agent.speed      = moveSpeed;
                    _agent.isStopped  = false;
                    _agent.ResetPath();
                    
                    TryUpdateAgentChaseDestination();
                }
                else
                {
                    rb.isKinematic = false;
                }
                break;

            case EnemyState.ReturnToSpawn:
                animator?.SetBool("IsAttacking", false);
                break;

            case EnemyState.Attack:
                attackCooldownTimer = 0f;
                animator?.SetBool("IsAttacking", false);
                
                
                StopMovementForAttack();
                break;
        }
    }

    
    
    
    
    
    
    private bool TryUpdateAgentChaseDestination()
    {
        if (currentTarget == null) return false;
        if (!CanUseAgent()) return false;

        if (NavMesh.SamplePosition(currentTarget.position, out NavMeshHit targetHit,
                                   chaseNavMeshSampleDistance, NavMesh.AllAreas))
        {
            _agent.CalculatePath(targetHit.position, _chasePath);
            if (_chasePath.status == NavMeshPathStatus.PathComplete)
            {
                _agent.SetDestination(targetHit.position);
                _lastValidChaseDestination    = targetHit.position;
                _hasLastValidChaseDestination = true;
                return true;
            }
        }
        
        return false;
    }

    
    private void SetupIdleTimer()
    {
        _idleTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void UpdateIdleWanderCycle()
    {
        if (currentTarget != null) return;

        if (currentState == EnemyState.Idle)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                if (wanderRadius <= 0f) { SetupIdleTimer(); return; }

                if (TryPickWanderPoint(out Vector3 point))
                {
                    _wanderTarget = point;
                    TransitionTo(EnemyState.Wander);
                }
                else
                {
                    SetupIdleTimer();
                }
            }
        }
        else if (currentState == EnemyState.Wander)
        {
            bool arrived;

            if (_hasAgent && _agent != null && _agent.enabled)
            {
                if (_agent.isOnNavMesh)
                {
                    arrived = !_agent.pathPending &&
                              _agent.remainingDistance <= wanderPointReachDistance;
                }
                else
                {
                    StopAgentAndRestoreRigidbody();
                    arrived = true;
                }
            }
            else
            {
                Vector3 flatPos    = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 flatTarget = new Vector3(_wanderTarget.x,      0f, _wanderTarget.z);
                arrived = Vector3.Distance(flatPos, flatTarget) <= wanderPointReachDistance;
            }

            if (arrived) TransitionTo(EnemyState.Idle);
        }
    }

    private bool TryPickWanderPoint(out Vector3 point)
    {
        point = Vector3.zero;
        for (int i = 0; i < 10; i++)
        {
            Vector2 circle    = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = new Vector3(
                WanderCenter.x + circle.x,
                transform.position.y,
                WanderCenter.z + circle.y);

            float distFromCenter = Vector3.Distance(
                new Vector3(candidate.x, 0f, candidate.z),
                new Vector3(WanderCenter.x, 0f, WanderCenter.z));
            if (distFromCenter > leashRadius) continue;

            if (CanUseAgent())
            {
                if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                {
                    float sampledDist = Vector3.Distance(
                        new Vector3(navHit.position.x, 0f, navHit.position.z),
                        new Vector3(WanderCenter.x, 0f, WanderCenter.z));
                    if (sampledDist > leashRadius) continue;

                    _agent.CalculatePath(navHit.position, _wanderPath);
                    if (_wanderPath.status == NavMeshPathStatus.PathComplete)
                    {
                        point = navHit.position;
                        return true;
                    }
                }
            }
            else
            {
                point = candidate;
                return true;
            }
        }
        return false;
    }

    
    public void OnAttackHit()
    {
        if (!enabled || currentTarget == null) return;
        var health = currentTarget.GetComponent<HealthComponent>();
        if (health == null) return;
        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist <= attackRange * 1.2f)
            health.TakeDamage(attackDamage);
    }

    
    void FixedUpdate()
    {
        if (rb == null) return;
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.fixedDeltaTime;

        switch (currentState)
        {
            case EnemyState.Idle:
                moveDirection = Vector3.zero;
                if (!rb.isKinematic)
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                break;
            case EnemyState.Wander:
                HandleWanderMovement();
                break;
            case EnemyState.Chase:
                HandleChaseMovement();
                
                if (_skillController == null || !_skillController.IsCasting)
                    TryNormalAttack();
                break;
            case EnemyState.Attack:
                moveDirection = Vector3.zero;
                if (!rb.isKinematic)
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                FaceTarget();
                
                if (_skillController != null && _skillController.IsCasting)
                    break;
                
                if (_skillController != null && currentTarget != null &&
                    _skillController.TryGetReadySkillInRange(currentTarget, out EnemySkillData readySkill) &&
                    _skillController.TryStartSkill(readySkill, currentTarget))
                    break;
                
                if (animator != null)
                {
                    bool inAttackAnim = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
                    if (inAttackAnim)
                        animator.SetBool("IsAttacking", false);
                    else
                        TryNormalAttack();
                }
                break;
            case EnemyState.ReturnToSpawn:
                HandleReturnToSpawn();
                break;
        }
        animator?.SetFloat("Speed", moveDirection.magnitude, 0.1f, Time.fixedDeltaTime);
    }

    private void HandleWanderMovement()
    {
        if (_hasAgent && _agent != null && _agent.enabled)
        {
            if (_agent.isOnNavMesh)
            {
                Vector3 vel = _agent.velocity;
                moveDirection = vel.sqrMagnitude > 0.01f
                    ? new Vector3(vel.x, 0f, vel.z).normalized
                    : Vector3.zero;
                return;
            }
            else { StopAgentAndRestoreRigidbody(); }
        }

        
        Vector3 flatPos    = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(_wanderTarget.x,      0f, _wanderTarget.z);
        float   dist       = Vector3.Distance(flatPos, flatTarget);

        if (dist > wanderPointReachDistance)
        {
            moveDirection = new Vector3(
                _wanderTarget.x - transform.position.x, 0f,
                _wanderTarget.z - transform.position.z).normalized;

            rb.linearVelocity = new Vector3(
                moveDirection.x * wanderMoveSpeed,
                rb.linearVelocity.y,
                moveDirection.z * wanderMoveSpeed);

            if (moveDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    Quaternion.LookRotation(moveDirection),
                    rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            moveDirection     = Vector3.zero;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    
    
    
    
    
    
    private void HandleChaseMovement()
    {
        if (currentTarget == null) return;

        
        if (_chasingWithAgent)
        {
            if (!CanUseAgent())
            {
                
                _chasingWithAgent             = false;
                _hasLastValidChaseDestination = false;
                StopAgentAndRestoreRigidbody();
                ChaseTargetRigidbody();
                return;
            }

            
            if (Time.time >= _nextChaseDestinationUpdateTime)
            {
                _nextChaseDestinationUpdateTime = Time.time + chaseDestinationUpdateInterval;
                bool updated = TryUpdateAgentChaseDestination();

                if (!updated)
                {
                    
                    
                    
                    if (!_agent.hasPath && _hasLastValidChaseDestination)
                        _agent.SetDestination(_lastValidChaseDestination);
                    
                }
            }

            
            Vector3 vel = _agent.velocity;
            moveDirection = vel.sqrMagnitude > 0.01f
                ? new Vector3(vel.x, 0f, vel.z).normalized
                : Vector3.zero;
            return;
        }

        
        ChaseTargetRigidbody();
    }

    
    private void ChaseTargetRigidbody()
    {
        if (currentTarget == null) return;
        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist <= stoppingDistance)
        {
            moveDirection     = Vector3.zero;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            FaceTarget();
            return;
        }
        moveDirection = (currentTarget.position - transform.position).normalized;
        rb.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed);
        FaceTarget();
    }

    void FaceTarget()
    {
        if (currentTarget == null) return;
        Vector3 dir = currentTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                rotationSpeed * Time.fixedDeltaTime);
    }


    
    
    
    
    private void TryNormalAttack()
    {
        if (currentTarget == null) return;
        if (myHealth != null && myHealth.IsDead) return;
        if (animator == null) return;
        if (attackCooldownTimer > 0f) return;

        var targetHealth = currentTarget.GetComponent<HealthComponent>();
        if (targetHealth == null || targetHealth.IsDead) return;

        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist > attackRange) return;

        
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return;

        attackCooldownTimer = attackCooldown;
        animator.SetBool("IsAttacking", true);
    }


    
    public void ResetToSpawn()
    {
        _skillController?.CancelCasting("ResetToSpawn");
        hateTable.Clear();
        currentTarget                 = null;
        disengageTimer                = 0f;
        _returningWithAgent           = false;
        _chasingWithAgent             = false;
        _hasLastValidChaseDestination = false;

        if (_hasAgent && _agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            if (_agent.isOnNavMesh)
                _agent.Warp(_spawnPosition);
            else
                transform.position = _spawnPosition;
        }
        else
        {
            transform.position = _spawnPosition;
        }
        transform.rotation = _spawnRotation;

        if (rb != null) rb.isKinematic = false;
        ClearRigidbodyVelocity();

        if (myHealth != null) myHealth.RestoreFullHealth();

        currentState        = EnemyState.Idle;
        attackCooldownTimer = 0f;
        moveDirection       = Vector3.zero;
        SetupIdleTimer();
        animator?.SetBool("IsAttacking", false);
        animator?.SetFloat("Speed", 0f);
    }

    public void ForceDisengageAndReturnToSpawn()
    {
        if (!enabled) return;
        if (myHealth != null && myHealth.IsDead) return;
        if (currentState == EnemyState.ReturnToSpawn) return;

        Debug.Log($"[EnemyAI] {gameObject.name} 外部指令により強制脱戦、出生点へ帰還。");
        _skillController?.CancelCasting("ForceDisengage");
        EnterReturnToSpawn();
    }

    
    
    
    
    
    
    
    
    
    
    
    
    public void SetSpawnAreaContext(
        Vector3    areaCenter,
        float      areaWanderRadius,
        float      areaLeashRadius,
        Vector3    spawnPosition,
        Quaternion spawnRotation)
    {
        _hasSpawnAreaContext = true;
        _spawnAreaCenter     = areaCenter;

        wanderRadius = Mathf.Max(0f, areaWanderRadius);
        leashRadius  = Mathf.Max(wanderRadius, areaLeashRadius);

        _spawnPosition = spawnPosition;
        _spawnRotation = spawnRotation;
    }
}
