# Inventory / Equipment System

## 目的

戦闘後のドロップ、インベントリ、装備、戦闘ステータスへの反映までをつなぐための最小システムです。

## 主な構成

- `ItemData`：アイテムの種類、スタック数、装備スロット、ステータス補正を定義します。
- `ItemStack`：同一 itemId の所持数を保持します。
- `PlayerInventory`：所持アイテムを管理します。Equipment は独立スタック、非 Equipment は同一 itemId でマージします。
- `PlayerEquipment`：Core / Armor / Accessory の装備スロットを管理します。
- `PlayerCombatStats`：装備ボーナスを合算し、攻撃力と最大 HP を提供します。

## 設計意図

アイテム、所持状態、装備状態、戦闘ステータスを分離し、装備変更時にはイベントを通じて最大 HP などを更新できるようにしています。
