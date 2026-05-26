# ファイル索引

| ファイル | 説明 |
|---|---|
| `skills/PlayerSkillData.cs` | プレイヤースキルの静的データを定義する ScriptableObject。入力スロット、効果タイプ、距離、倍率、表示情報を保持する。 |
| `skills/PlayerSkillManager.cs` | プレイヤースキルの実行時状態と入力分配を管理するコンポーネント。New Input System の入力を各スキル実行担当へ振り分ける。 |
| `skills/PlayerBasicAttackController.cs` | 単体基礎攻撃と範囲基礎攻撃を実行するコンポーネント。共有リキャスト、最終ダメージ計算、範囲内ターゲット検索を担当する。 |
| `skills/PlayerGuardCounterController.cs` | Guard Resonance 成功後の Radiant Riposte 反撃機会を管理するコンポーネント。反撃ウィンドウ、対象、射程、死亡時クリアを扱う。 |
| `skills/PlayerStatusEffectController.cs` | Active なプレイヤースキル状態から、被ダメージ、通常攻撃ダメージ、被回復量の補正を計算するコンポーネント。 |
| `skills/PlayerSkillCanvasUI.cs` | Canvas 上の単体スキルスロット UI。通常スキル、基礎攻撃共有クールダウン、GuardCounter の Ready 表示に対応する。 |
| `skills/PlayerSkillBarCanvasUI.cs` | PlayerSkillManager の RuntimeStates からスキルスロットを動的生成し、右下にスキルバーを配置する UI コンポーネント。 |
| `skills/PlayerMitigationVisualFeedback.cs` | Iron Bulwark の Active 状態を参照し、プレイヤー足元に防御リングを表示する視覚フィードバック。 |
| `hikari/HikariSupportController.cs` | Hikari の自動回復、光負荷、Guard Resonance、Overflow Counter を管理するプロトタイプコンポーネント。 |
| `enemy/EnemyAI.cs` | 敵の有限状態機械を管理するコンポーネント。徘徊、追跡、通常攻撃、スポーン地点帰還、NavMeshAgent 優先移動を扱う。 |
| `enemy/EnemySkillController.cs` | 敵スキルの詠唱、クールダウン、範囲提示、ダメージ判定を実行するコンポーネント。CastAttack / CircleAoE / DonutAoE に対応する。 |
| `enemy/EnemySkillData.cs` | 敵スキルの静的パラメータを保持する ScriptableObject。ダメージ、詠唱時間、クールダウン、範囲、AoE 半径を定義する。 |
| `enemy/EnemyCastBarUI.cs` | 敵の詠唱状況を頭上に表示するデバッグ用 UI。OnGUI と WorldToScreenPoint を使って描画する。 |
| `enemy/EnemyDropper.cs` | 敵死亡時のドロップ抽選と拾得 Prefab 生成を担当するコンポーネント。地面配置補正と複数ドロップに対応する。 |
| `enemy/EnemyWorldManager.cs` | シーン内の全敵に対して、戦闘離脱とスポーン地点帰還を命令するためのグローバル入口。 |
| `inventory-equipment/ItemData.cs` | アイテムの静的データを定義する ScriptableObject。種別、レアリティ、スタック数、装備スロット、ステータス補正を保持する。 |
| `inventory-equipment/ItemStack.cs` | 同一 itemId のアイテム所持数を保持するデータクラス。インベントリ内部でスタック管理に使用する。 |
| `inventory-equipment/PlayerInventory.cs` | プレイヤーの所持アイテムを管理するインベントリ。Equipment は独立スタック、非 Equipment は同一 itemId でマージする。 |
| `inventory-equipment/PlayerEquipment.cs` | Core / Armor / Accessory の装備スロットを管理するコンポーネント。装備変更時にイベントを発火する。 |
| `inventory-equipment/PlayerCombatStats.cs` | 基礎ステータスと装備ボーナスを合算し、現在攻撃力と最大 HP を提供するコンポーネント。 |
