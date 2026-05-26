/// <summary>
/// 敵の詠唱状況を頭上に表示するデバッグ用 UI。OnGUI と WorldToScreenPoint を使って描画する。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
﻿using UnityEngine;










public class EnemyCastBarUI : MonoBehaviour
{
    [Header("ワールド空間オフセット（敵の位置からの頭上）")]
    public Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);

    [Header("バーサイズ")]
    public float barWidth  = 110f;
    public float barHeight = 10f;

    
    private EnemySkillController _skillController;

    
    private GUIStyle _skillNameStyle;
    private GUIStyle _timeStyle;

    
    void Awake()
    {
        _skillController = GetComponent<EnemySkillController>();
    }

    
    void OnGUI()
    {
        
        if (_skillController == null)
        {
            _skillController = GetComponent<EnemySkillController>();
            if (_skillController == null) return;
        }

        if (!_skillController.IsCasting) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        if (_skillController.CurrentCastDuration <= 0f) return;

        
        EnsureStyles();

        
        Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + worldOffset);
        if (screenPos.z < 0f) return;  

        float screenX = screenPos.x;
        float screenY = Screen.height - screenPos.y;  
        float halfW   = barWidth * 0.5f;

        
        var   skill    = _skillController.CurrentSkill;
        string skillName = (skill != null && !string.IsNullOrEmpty(skill.DisplayName))
                               ? skill.DisplayName
                               : "Casting";
        float elapsed  = _skillController.CurrentCastElapsed;
        float duration = _skillController.CurrentCastDuration;
        float progress = Mathf.Clamp01(_skillController.CurrentCastProgress);
        string timeText = $"{elapsed:F1} / {duration:F1}s";

        
        const float lineH = 18f;
        const float gap   = 2f;

        float nameY = screenY - lineH - gap - barHeight - gap;
        float barY  = screenY - barHeight - gap;
        float timeY = screenY + gap;

        
        GUI.Label(new Rect(screenX - halfW, nameY, barWidth, lineH), skillName, _skillNameStyle);

        
        var prevColor = GUI.color;
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        GUI.DrawTexture(new Rect(screenX - halfW, barY, barWidth, barHeight), Texture2D.whiteTexture);

        
        if (progress > 0f)
        {
            GUI.color = new Color(0.95f, 0.45f, 0.10f, 0.90f);
            GUI.DrawTexture(new Rect(screenX - halfW, barY, barWidth * progress, barHeight), Texture2D.whiteTexture);
        }

        GUI.color = prevColor;  

        
        GUI.Label(new Rect(screenX - halfW, timeY, barWidth, lineH), timeText, _timeStyle);
    }

    
    
    
    
    
    private void EnsureStyles()
    {
        if (_skillNameStyle == null)
        {
            _skillNameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
            };
            _skillNameStyle.normal.textColor = Color.white;
        }

        if (_timeStyle == null)
        {
            _timeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 11,
                fontStyle = FontStyle.Normal,
            };
            _timeStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        }
    }
}
