# Hikari Burden System

## 目的

Hikari の回復を単なる無料の自動回復にせず、光負荷というリスクとリターンを持つ支援システムにするためのプロトタイプです。

## 主な概念

- Light Mend：プレイヤー HP が一定以下のときに行う小回復。
- Emergency Prayer：プレイヤー HP が危険域のときに行う大回復。
- Burden：Hikari の光負荷。回復により上昇し、時間経過や Guard Resonance により低下します。
- Light Overflow：光負荷が高い危険領域。回復効率が下がる一方、Overflow Counter の条件になります。
- Channel Lockdown：光負荷が上限に達した状態。通常回復が停止します。
- Guard Resonance：プレイヤーが被ダメージ軽減中に敵の CastAttack を受けると、Hikari の光負荷を下げます。
- Overflow Counter：Light Overflow 中に Guard Resonance が成功したとき、攻撃者に追加ダメージを与えます。

## 設計意図

プレイヤーが正しく Tank として強攻撃を受け止めるほど Hikari の負荷が下がり、逆に失敗して回復に頼るほど光負荷が高まる構造にしています。
