/// <summary>
/// 敵死亡時のドロップ抽選と拾得 Prefab 生成を担当するコンポーネント。地面配置補正と複数ドロップに対応する。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using System.Collections.Generic;
using UnityEngine;









public class EnemyDropper : MonoBehaviour
{
    
    [System.Serializable]
    public class DropEntry
    {
        public ItemData item;
        [Range(0f, 1f)] public float dropChance = 1f;
        public Vector3 offset;
    }

    [Header("掉落列表（新）")]
    [SerializeField] private List<DropEntry> drops = new List<DropEntry>();

    
    [Header("单物品掉落（旧 fallback，drops 为空时使用）")]
    [SerializeField] private ItemData dropItem;

    
    [Header("拾取物 Prefab")]
    [SerializeField] private PickupItem pickupPrefab;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.2f, 0f);

    
    [Header("Ground Placement")]
    [Tooltip("有效にすると Raycast で地面を検出し、掉落物を地面に密着させる")]
    [SerializeField] private bool alignDropsToGround = true;
    [Tooltip("候補位置から上方にどれだけ Raycast 開始点を上げるか（m）")]
    [SerializeField] private float groundRaycastStartHeight = 5f;
    [Tooltip("Raycast の最大検出距離（m）")]
    [SerializeField] private float groundRaycastDistance = 20f;
    [Tooltip("地面ヒット点から掉落物を浮かせるオフセット（m）")]
    [SerializeField] private float groundOffset = 0.1f;
    [Tooltip("地面として判定する Layer。デフォルト（~0）は全レイヤー対象")]
    [SerializeField] private LayerMask groundLayerMask = ~0;
    [Tooltip("敵自身の Collider 上端より何 m 高い命中まで有効とみなすか。斜面・地形起伏の許容誤差。")]
    [SerializeField] private float maxDropHeightAboveOwnerBounds = 0.2f;

    private HealthComponent _health;
    private PlayerTeaBuffController _teaBuffController;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        if (_health == null)
            Debug.LogWarning("[EnemyDropper] HealthComponent not found on this GameObject.");
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDied -= HandleDied;
    }

    private void OnValidate()
    {
        if (groundRaycastStartHeight      < 0f)   groundRaycastStartHeight      = 0f;
        if (groundRaycastDistance         < 0.1f)  groundRaycastDistance         = 0.1f;
        if (groundOffset                  < 0f)   groundOffset                  = 0f;
        if (maxDropHeightAboveOwnerBounds < 0f)   maxDropHeightAboveOwnerBounds = 0f;
    }

    
    private PlayerTeaBuffController GetTeaBuffController()
    {
        if (_teaBuffController == null)
            _teaBuffController = FindFirstObjectByType<PlayerTeaBuffController>();
        return _teaBuffController;
    }

    private void HandleDied()
    {
        if (pickupPrefab == null)
        {
            Debug.LogWarning("[EnemyDropper] pickupPrefab is not assigned. No item will drop.");
            return;
        }

        var teaBuff = GetTeaBuffController();
        float dropChanceMult   = teaBuff != null ? teaBuff.GetNonGuaranteedDropChanceMultiplier() : 1f;
        float materialExtraChance = teaBuff != null ? teaBuff.GetMaterialExtraQuantityChance()    : 0f;

        
        if (drops != null && drops.Count > 0)
        {
            foreach (var entry in drops)
            {
                if (entry.item == null)
                {
                    Debug.LogWarning("[EnemyDropper] DropEntry has null item, skipping.");
                    continue;
                }

                
                float finalChance = entry.dropChance < 1f
                    ? Mathf.Clamp01(entry.dropChance * dropChanceMult)
                    : entry.dropChance;

                if (Random.value <= finalChance)
                {
                    Vector3 candidate = transform.position + dropOffset + entry.offset;
                    Vector3 pos       = GetGroundedDropPosition(candidate);
                    SpawnDrop(entry.item, pos);

                    
                    if (entry.item.ItemType == ItemType.Material && materialExtraChance > 0f)
                    {
                        if (Random.value < materialExtraChance)
                        {
                            
                            Vector3 extraCandidate = candidate + new Vector3(0.3f, 0f, 0.3f);
                            Vector3 extraPos       = GetGroundedDropPosition(extraCandidate);
                            SpawnDrop(entry.item, extraPos);
                            Debug.Log($"[EnemyDropper] Material extra drop: {entry.item.ItemName} at {extraPos}");
                        }
                    }
                }
            }
            return;
        }

        
        if (dropItem == null)
        {
            Debug.LogWarning("[EnemyDropper] dropItem is not assigned and drops list is empty. No item will drop.");
            return;
        }

        Vector3 legacyCandidate = transform.position + dropOffset;
        Vector3 spawnPos        = GetGroundedDropPosition(legacyCandidate);
        SpawnDrop(dropItem, spawnPos);
        Debug.Log($"[EnemyDropper] Dropped (legacy): {dropItem.ItemName} at {spawnPos}");
    }

    private void SpawnDrop(ItemData item, Vector3 pos)
    {
        PickupItem dropped = Instantiate(pickupPrefab, pos, Quaternion.identity);
        dropped.SetItemData(item);
        Debug.Log($"[EnemyDropper] Dropped: {item.ItemName} at {pos}");
    }

    
    
    
    
    
    
    private Vector3 GetGroundedDropPosition(Vector3 candidatePosition)
    {
        if (!alignDropsToGround)
            return candidatePosition;

        Vector3 rayStart = candidatePosition + Vector3.up * groundRaycastStartHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
                            groundRaycastDistance, groundLayerMask,
                            QueryTriggerInteraction.Ignore))
        {
            
            float maxAllowedY = GetMaxAllowedDropGroundY();
            if (hit.point.y <= maxAllowedY)
            {
                return hit.point + Vector3.up * groundOffset;
            }

            
            return candidatePosition;
        }

        
        return candidatePosition;
    }

    
    
    
    
    
    private float GetMaxAllowedDropGroundY()
    {
        float maxBoundsY = float.MinValue;
        bool  found      = false;

        
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            if (col.isTrigger) continue;
            float top = col.bounds.max.y;
            if (top > maxBoundsY)
            {
                maxBoundsY = top;
                found      = true;
            }
        }

        if (found)
            return maxBoundsY + maxDropHeightAboveOwnerBounds;

        
        return transform.position.y + maxDropHeightAboveOwnerBounds;
    }
}
