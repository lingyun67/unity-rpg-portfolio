/// <summary>
/// Iron Bulwark の Active 状態を参照し、プレイヤー足元に防御リングを表示する視覚フィードバック。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using UnityEngine;








public class PlayerMitigationVisualFeedback : MonoBehaviour
{
    [Header("技能管理器引用（留空时自动查找）")]
    [SerializeField] private PlayerSkillManager skillManager;
    [SerializeField] private string             skillId = "iron_bulwark";

    [Header("光环形状")]
    [SerializeField] private float radius          = 1.2f;
    [SerializeField] private float yOffset         = 0.05f;
    [SerializeField] private float lineWidth       = 0.06f;
    [SerializeField] private int   segments        = 96;

    [Header("光环动画")]
    [SerializeField] private float pulseSpeed       = 3f;
    [SerializeField] private float pulseScaleAmount = 0.08f;
    [SerializeField] private float rotationSpeed    = 60f;

    [Header("颜色")]
    [SerializeField] private Color activeColor = new Color(0.25f, 0.75f, 1f, 0.75f);

    
    private GameObject   _ringGO;
    private LineRenderer _lr;
    private Material     _runtimeMat;
    private bool         _ready;    

    

    private void Awake()
    {
        ResolveSkillManager();
        if (skillManager == null)
            Debug.LogWarning("[MitigationFX] PlayerSkillManager not found. Visual feedback may not activate.");

        _ready = BuildRing();
    }

    private void OnEnable()
    {
        
        if (_ringGO != null)
            _ringGO.SetActive(ShouldShowMitigationRing());
    }

    private void OnDisable()
    {
        if (_ringGO != null) _ringGO.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_runtimeMat != null) Destroy(_runtimeMat);
    }

    private void Update()
    {
        if (!_ready) return;

        bool shouldShow = ShouldShowMitigationRing();
        if (_ringGO.activeSelf != shouldShow)
            _ringGO.SetActive(shouldShow);

        if (!shouldShow) return;

        
        _ringGO.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);

        
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount;
        _ringGO.transform.localScale = new Vector3(pulse, 1f, pulse);
    }

    

    
    
    
    
    private bool ShouldShowMitigationRing()
    {
        if (skillManager == null) return false;
        var state = skillManager.GetStateBySkillId(skillId);
        if (state == null) return false;
        return state.IsActive;
    }

    private void ResolveSkillManager()
    {
        if (skillManager != null) return;
        skillManager = GetComponent<PlayerSkillManager>();
        if (skillManager != null) return;
        skillManager = GetComponentInParent<PlayerSkillManager>();
        if (skillManager != null) return;
        skillManager = FindFirstObjectByType<PlayerSkillManager>();
    }

    
    private bool BuildRing()
    {
        
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            Debug.LogWarning("[MitigationFX] No usable Shader found. Visual feedback disabled.");
            return false;
        }

        _runtimeMat       = new Material(shader);
        _runtimeMat.name  = "MitigationRingMat_Runtime";
        _runtimeMat.color = activeColor;

        
        _ringGO = new GameObject("MitigationRing_Runtime");
        _ringGO.transform.SetParent(transform, false);
        _ringGO.transform.localPosition = Vector3.zero;
        _ringGO.transform.localScale    = Vector3.one;

        
        _lr = _ringGO.AddComponent<LineRenderer>();
        _lr.useWorldSpace      = false;
        _lr.loop               = true;
        _lr.positionCount      = segments;
        _lr.startWidth         = lineWidth;
        _lr.endWidth           = lineWidth;
        _lr.startColor         = activeColor;
        _lr.endColor           = activeColor;
        _lr.material           = _runtimeMat;
        _lr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lr.receiveShadows     = false;

        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            _lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, yOffset, Mathf.Sin(angle) * radius));
        }

        _ringGO.SetActive(false);
        return true;
    }
}
