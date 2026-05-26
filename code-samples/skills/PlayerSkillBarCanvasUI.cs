/// <summary>
/// PlayerSkillManager の RuntimeStates からスキルスロットを動的生成し、右下にスキルバーを配置する UI コンポーネント。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using UnityEngine;
using System.Collections.Generic;








public class PlayerSkillBarCanvasUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private PlayerSkillManager  skillManager;
    [SerializeField] private PlayerSkillCanvasUI slotTemplate;  
    [SerializeField] private Transform           slotRoot;      

    [Header("レイアウト")]
    [SerializeField] private Vector2 slotSize    = new Vector2(96f, 112f);
    [SerializeField] private float   spacing     = 8f;

    [Header("SkillBar 位置（右下からのオフセット）")]
    [SerializeField] private float rightOffset  = 40f;
    [SerializeField] private float bottomOffset = 40f;

    [Header("動作")]
    [SerializeField] private bool rebuildOnStart        = true;
    [SerializeField] private bool hideTemplateAtRuntime = true;

    private readonly List<PlayerSkillCanvasUI> _spawnedSlots = new List<PlayerSkillCanvasUI>();

    

    private void Start()
    {
        if (slotRoot == null) slotRoot = transform;
        ResolveSkillManager();
        if (rebuildOnStart) RebuildSlots();
    }

    

    public void RebuildSlots()
    {
        if (skillManager == null)
        {
            Debug.LogWarning("[PlayerSkillBarCanvasUI] PlayerSkillManager not found.");
            return;
        }
        if (slotTemplate == null)
        {
            Debug.LogWarning("[PlayerSkillBarCanvasUI] slotTemplate is not set.");
            return;
        }

        
        if (hideTemplateAtRuntime)
            slotTemplate.gameObject.SetActive(false);

        
        foreach (var s in _spawnedSlots)
            if (s != null) Destroy(s.gameObject);
        _spawnedSlots.Clear();

        var states = skillManager.RuntimeStates;
        int count  = states != null ? states.Count : 0;

        
        
        var rootRT = slotRoot as RectTransform;
        if (rootRT != null)
        {
            float totalWidth    = count * slotSize.x + Mathf.Max(0, count - 1) * spacing;
            rootRT.anchorMin        = new Vector2(1f, 0f);
            rootRT.anchorMax        = new Vector2(1f, 0f);
            rootRT.pivot            = new Vector2(1f, 0f);
            rootRT.anchoredPosition = new Vector2(-rightOffset, bottomOffset);
            rootRT.sizeDelta        = new Vector2(totalWidth, slotSize.y);
        }

        if (count == 0)
        {
            Debug.Log("[PlayerSkillBarCanvasUI] No skills registered.");
            return;
        }

        
        
        
        for (int i = 0; i < count; i++)
        {
            var go   = Instantiate(slotTemplate.gameObject, slotRoot);
            go.name  = "SkillSlot_" + states[i].SkillId;
            go.SetActive(true);

            var rt        = go.GetComponent<RectTransform>();
            rt.anchorMin  = new Vector2(0f, 0f);
            rt.anchorMax  = new Vector2(0f, 0f);
            rt.pivot      = new Vector2(0f, 0f);
            rt.sizeDelta  = slotSize;
            rt.anchoredPosition = new Vector2(i * (slotSize.x + spacing), 0f);

            var slot = go.GetComponent<PlayerSkillCanvasUI>();
            if (slot == null)
            {
                Debug.LogWarning("[PlayerSkillBarCanvasUI] slotTemplate has no PlayerSkillCanvasUI.");
                continue;
            }
            slot.Initialize(skillManager, states[i]);
            _spawnedSlots.Add(slot);
        }

        Debug.Log($"[PlayerSkillBarCanvasUI] Built {_spawnedSlots.Count} slot(s).");
    }

    

    private void ResolveSkillManager()
    {
        if (skillManager != null) return;
        skillManager = FindFirstObjectByType<PlayerSkillManager>();
        if (skillManager == null)
            Debug.LogWarning("[PlayerSkillBarCanvasUI] PlayerSkillManager not found in scene.");
    }
}
