# Player Skill System

## 目的

プレイヤースキルを `PlayerSkillData` と実行時状態に分け、入力、クールダウン、Active 状態、UI 表示、効果計算を分離するためのシステムです。

## 主な構成

- `PlayerSkillData`：ScriptableObject としてスキルの静的パラメータを保持します。
- `PlayerSkillManager`：入力を受け取り、RuntimeState を更新し、効果タイプごとに処理を振り分けます。
- `PlayerBasicAttackController`：Slot1 / Slot4 の基礎攻撃を実行します。
- `PlayerGuardCounterController`：Guard Resonance 後の反撃を管理します。
- `PlayerStatusEffectController`：ダメージ、攻撃力、回復量の倍率補正を計算します。
- `PlayerSkillCanvasUI` / `PlayerSkillBarCanvasUI`：スキルバー表示を担当します。

## 設計意図

`PlayerSkillManager` にすべての攻撃処理を集めず、効果ごとの実行担当へ分けています。これにより、入力管理、効果実行、UI 表示、ステータス補正を個別に変更しやすくしています。
