# Enemy System

## 目的

単純な追跡・通常攻撃だけでなく、Tank 玩法に必要な詠唱攻撃と範囲攻撃を持つ敵を作るためのシステムです。

## 主な構成

- `EnemyAI`：FSM、ヘイト、NavMeshAgent 優先移動、スポーン地点帰還を担当します。
- `EnemySkillData`：敵スキルの静的データを保持します。
- `EnemySkillController`：CastAttack / CircleAoE / DonutAoE の詠唱、クールダウン、判定を実行します。
- `EnemyCastBarUI`：詠唱バー表示を行います。
- `EnemyDropper`：死亡時のドロップを処理します。
- `EnemyWorldManager`：プレイヤー死亡時などに全敵へ帰還命令を出します。

## 設計意図

敵の通常 AI と敵スキルを分け、`EnemyAI` にスキル処理を直接書き込みすぎない構造にしています。これにより、通常攻撃しか持たない敵と、Boss 的な詠唱攻撃を持つ敵を同じ基盤で扱えます。
