# 战斗中临时附魔框架

## 实例关系

玩家的永久牌组位于 `Player.Deck`，其中的卡牌由 `RunState` 持有并写入存档。

每次进入战斗时，`Player.PopulateCombatState` 会逐张深克隆永久牌：

1. `CombatState.CloneCard(deckCard)` 创建独立的战斗卡牌实例。
2. `combatCard.DeckVersion = deckCard` 保存对永久实例的引用。
3. 战斗实例进入 `PlayerCombatState.DrawPile`。

附魔也会随卡牌深克隆，因此永久附魔在战斗实例上有独立的
`EnchantmentModel` 实例。战斗结束时，`PlayerCombatState.AfterCombatEnd`
清空手牌、抽牌堆、弃牌堆、消耗堆和打出区；普通战斗状态不会自动写回
`DeckVersion`。

## 永久附魔与临时附魔

- 对 `Player.Deck` 中的卡调用 `CardCmd.Enchant`：永久附魔，会进入卡牌存档。
- 对战斗牌堆中的克隆卡调用 `CardCmd.Enchant`：效果只存在于该战斗实例，
  默认不会改变永久牌。
- 少数原版逻辑会主动写 `Card.DeckVersion`，例如 `Goopy` 在战斗中成长时
  同步增加永久附魔层数。因此 Mod 的临时附魔禁止访问或修改
  `Card.DeckVersion`。
- `CardModel` 只有一个 `Enchantment` 槽位。永久附魔被克隆进战斗后，
  不能同时再挂第二个临时附魔。若设计要求二者共存，需要另做独立的
  “战斗修饰器”槽位，不能直接复用原生 `Enchantment` 字段。

## 使用

临时附魔继承：

```csharp
[RegisterEnchantment]
public sealed class ExampleCombatEnchantment
    : CombatOnlyEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://MaidenSuccubus/images/enchantments/example.png");
}
```

在卡牌或 Power 的战斗效果中调用：

```csharp
CombatEnchantmentCmd.Apply<ExampleCombatEnchantment>(targetCard, 1m);
```

该命令只接受当前战斗五个牌堆中的实例，拒绝永久牌组实例和已有附魔的牌，
并且不会向 `CardsEnchanted` 的局外历史记录写入一次永久附魔事件。

原版“锋利”“伶俐”使用经过逐类审计的
`CombatEnchantmentCmd.ApplyVanilla<T>()`；未经审计、可能主动写回
`DeckVersion` 的原版附魔会被拒绝。

“感染”本身是可由事件永久给予的正式附魔，因此不继承
`CombatOnlyEnchantmentTemplate`。其向相邻手牌传播时改用
`CombatEnchantmentCmd.ApplyAudited<InfectionEnchantment>()`，只附着到
战斗副本，仍遵守已有永久附魔时拒绝临时附魔的规则。

## 生命周期注意事项

不需要在战斗结束时逐张清除临时附魔；整个战斗卡牌实例会被销毁。
反而不应依赖 `CardCmd.ClearEnchantment` 做回滚，因为部分附魔的
`OnEnchant` 会直接增加关键词或修改卡牌字段，而基础清除函数只解除
`Enchantment` 引用，并不保证撤销这些字段修改。
