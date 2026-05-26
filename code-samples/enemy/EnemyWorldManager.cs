/// <summary>
/// シーン内の全敵に対して、戦闘離脱とスポーン地点帰還を命令するためのグローバル入口。
/// </summary>
/// <remarks>
/// 公開用ポートフォリオ向けにコメントのみ日本語化・整理しています。
/// C# の処理、フィールド名、メソッド名、文字列リテラル、Inspector 表示文字列は変更していません。
/// </remarks>
using UnityEngine;







public class EnemyWorldManager : MonoBehaviour
{
    
    
    
    
    public void ForceAllLivingEnemiesReturnToSpawn()
    {
        var allEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var enemy in allEnemies)
        {
            enemy.ForceDisengageAndReturnToSpawn();
            count++;
        }

        Debug.Log($"[EnemyWorldManager] 命令了 {count} 个敌人返回出生点。");
    }
}
