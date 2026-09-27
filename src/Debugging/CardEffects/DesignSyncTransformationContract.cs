#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Core.Temptation;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Debugging.CardEffects;

// Independent literal oracles for SYS-TRF-001/002, approved 2026-09-27.
// These run real commands, including engine amount-removal and damage hooks.
internal static class DesignSyncTransformationContract
{
    internal static void Extend(List<CardEffectSpec> specs)
    {
        int index = specs.FindIndex(spec => spec.CardType == typeof(Transform));
        CardEffectSpec spec = specs[index];
        specs[index] = spec with { Scenarios = [.. spec.Scenarios,
            new("ds27-armour-lifecycle", false, 14, (ctx, _) => Lifecycle(ctx)),
            new("ds27-attack-boundaries", false, 13, (ctx, _) => Attacks(ctx)),
            new("ds27-form-switching", false, 12, (ctx, _) => Switching(ctx)),
            new("ds27-release-payment", false, 10, (ctx, _) => Payment(ctx)),
        ] };
    }

    private static BlockingPlayerChoiceContext Choice() => new();
    private static MagicArmorPower Armor(CardEffectTestContext ctx) =>
        ctx.Self.GetPower<MagicArmorPower>() ?? throw new InvalidOperationException("Armour unexpectedly removed.");

    private static async Task Lifecycle(CardEffectTestContext ctx)
    {
        ctx.AssertTrue("normal form cannot gain armour",
            !await TransformationCmd.GainArmor(Choice(), ctx.Self, 9, null));
        ctx.AssertTrue("normal form has no armour power", TransformationCmd.GetArmor(ctx.Self) == null);
        await TransformationCmd.EnterImmaculateRobe(Choice(), ctx.Self, null);
        ctx.AssertEqual("entry has three", 3, Armor(ctx).Amount);
        await TransformationCmd.GainArmor(Choice(), ctx.Self, 99, null);
        ctx.AssertEqual("gain clamps to five", 5, Armor(ctx).Amount);
        ctx.AssertTrue("capped gain is no-op", !await TransformationCmd.GainArmor(Choice(), ctx.Self, 1, null));
        ctx.AssertEqual("extra armour does not change baseline", 10, Temptation.BaseValue(ctx.Self));
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 99, null);
        ctx.AssertEqual("oversize loss clamps to zero", 0, Armor(ctx).Amount);
        ctx.AssertTrue("zero power survives engine removal check", !Armor(ctx).ShouldRemoveDueToAmount());
        ctx.AssertTrue("zero preserves transformed form", TransformationCmd.IsTransformed(ctx.Self));
        ctx.AssertEqual("zero missing armour derives from three", 70, Temptation.BaseValue(ctx.Self));
        await TransformationCmd.GainArmor(Choice(), ctx.Self, 1, null);
        ctx.AssertEqual("zero can be repaired without reentering", 1, Armor(ctx).Amount);
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 1, null);
        ctx.AssertTrue("exact loss also preserves form", TransformationCmd.IsTransformed(ctx.Self));
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 1, null);
        ctx.AssertTrue("next zero loss exits", !TransformationCmd.IsTransformed(ctx.Self));
        ctx.AssertTrue("exit removes zero power", TransformationCmd.GetArmor(ctx.Self) == null);
        await ctx.ApplyPower<StrengthPower>(ctx.Self, 1);
        await PowerCmd.Decrement(ctx.Self.GetPower<StrengthPower>()!);
        ctx.AssertTrue("vanilla zero counters still removed", !ctx.Self.HasPower<StrengthPower>());
        await MaidenSuccubus.Data.Desire.Set(ctx.Player, 3);
        await TransformationCmd.EnterCorruptRobe(Choice(), ctx.Self, null);
        ctx.AssertEqual("alternate form undamaged baseline", 20, Temptation.BaseValue(ctx.Self));
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 99, null);
        ctx.AssertEqual("actual loss reward once per settlement", 4, MaidenSuccubus.Data.Desire.Get(ctx.Player));
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 1, null);
        ctx.AssertEqual("zero attempt grants no loss reward", 4, MaidenSuccubus.Data.Desire.Get(ctx.Player));
        ctx.AssertTrue("alternate form zero attempt exits", !TransformationCmd.IsTransformed(ctx.Self));
    }

    private static async Task Attacks(CardEffectTestContext ctx)
    {
        await TransformationCmd.EnterImmaculateRobe(Choice(), ctx.Self, null);
        decimal preview = Armor(ctx).ModifyHpLostAfterOstyLate(ctx.Self, 20, ValueProp.Move, ctx.PrimaryEnemy, null);
        ctx.AssertEqual("preview uses 33 percent reduction", 13.4m, preview);
        ctx.AssertTrue("preview does not use the turn", !Armor(ctx).UsedThisTurn);
        await CreatureCmd.GainBlock(ctx.Self, 20, ValueProp.Unpowered, null);
        await CreatureCmd.Damage(Choice(), ctx.Self, 20, ValueProp.Move, ctx.PrimaryEnemy);
        ctx.AssertEqual("fully blocked attack retains armour", 3, Armor(ctx).Amount);
        ctx.AssertTrue("fully blocked attack retains use", !Armor(ctx).UsedThisTurn);
        await CreatureCmd.Damage(Choice(), ctx.Self, 4, ValueProp.Unpowered | ValueProp.Move, ctx.PrimaryEnemy);
        ctx.AssertEqual("unpowered damage retains armour", 3, Armor(ctx).Amount);
        await CreatureCmd.Damage(Choice(), ctx.Self, 4, ValueProp.Move, ctx.Self);
        ctx.AssertEqual("self damage retains armour", 3, Armor(ctx).Amount);
        int before = ctx.Self.CurrentHp;
        await CreatureCmd.Damage(Choice(), ctx.Self, 20, ValueProp.Move, ctx.PrimaryEnemy);
        ctx.AssertEqual("real damage uses vanilla integer rounding", 13, before - ctx.Self.CurrentHp);
        ctx.AssertEqual("first hit loses one", 2, Armor(ctx).Amount);
        before = ctx.Self.CurrentHp;
        await CreatureCmd.Damage(Choice(), ctx.Self, 20, ValueProp.Move, ctx.PrimaryEnemy);
        ctx.AssertEqual("second hit not reduced", 20, before - ctx.Self.CurrentHp);
        ctx.AssertEqual("second hit retains armour", 2, Armor(ctx).Amount);
        await Armor(ctx).BeforeSideTurnStart(Choice(), CombatSide.Enemy, ctx.Enemies, ctx.Combat);
        ctx.AssertTrue("enemy side does not reset use", Armor(ctx).UsedThisTurn);
        await Armor(ctx).BeforeSideTurnStart(Choice(), CombatSide.Player, [ctx.Self], ctx.Combat);
        ctx.AssertTrue("player side resets use", !Armor(ctx).UsedThisTurn);
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 2, null);
        before = ctx.Self.CurrentHp;
        await CreatureCmd.Damage(Choice(), ctx.Self, 20, ValueProp.Move, ctx.PrimaryEnemy);
        ctx.AssertEqual("zero armour provides no reduction", 20, before - ctx.Self.CurrentHp);
        ctx.AssertTrue("actual zero hit exits despite unchanged modifier", !TransformationCmd.IsTransformed(ctx.Self));
        await TransformationCmd.EnterImmaculateRobe(Choice(), ctx.Self, null);
        await CreatureCmd.GainBlock(ctx.Self, 10, ValueProp.Unpowered, null);
        before = ctx.Self.CurrentHp;
        await CreatureCmd.Damage(Choice(), ctx.Self, 20, ValueProp.Move, ctx.PrimaryEnemy);
        ctx.AssertEqual("partial block reduces only remaining HP loss", 6, before - ctx.Self.CurrentHp);
        ctx.AssertEqual("partial block consumes exactly one", 2, Armor(ctx).Amount);
    }

    private static async Task Switching(CardEffectTestContext ctx)
    {
        Transform normal = ctx.Create<Transform>();
        LightPowerRelease eternal = ctx.Create<LightPowerRelease>();
        await TransformationCmd.EnterImmaculateRobe(Choice(), ctx.Self, null);
        ctx.AssertTrue("same form card denied", !normal.ShouldPlay(normal, AutoPlayType.None));
        ctx.AssertTrue("different form card allowed", eternal.ShouldPlay(eternal, AutoPlayType.None));
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 1, null);
        await TransformationCmd.EnterImmaculateRobe(Choice(), ctx.Self, null);
        ctx.AssertEqual("same form command cannot refresh armour", 2, Armor(ctx).Amount);
        Armor(ctx).UsedThisTurn = true;
        await ctx.Play(eternal);
        ctx.AssertTrue("eternal is independently transformed", TransformationCmd.IsTransformed(ctx.Self));
        ctx.AssertTrue("previous form removed", !ctx.Self.HasPower<ImmaculateRobePower>());
        ctx.AssertEqual("different form resets armour", 3, Armor(ctx).Amount);
        ctx.AssertTrue("switching does not refresh first-hit allowance", Armor(ctx).UsedThisTurn);
        ctx.AssertTrue("same eternal denied", !eternal.ShouldPlay(eternal, AutoPlayType.None));
        ctx.AssertTrue("return to normal robe allowed", normal.ShouldPlay(normal, AutoPlayType.None));
        await ctx.Self.GetPower<EternalRobePower>()!.AfterPlayerTurnStart(Choice(), ctx.Player);
        ctx.AssertEqual("eternal grants nine independently", 9, ctx.PowerAmount<MagicAmplificationPower>(ctx.Self));
        await PowerCmd.Remove(ctx.Self.GetPower<MagicAmplificationPower>()!);
        await ctx.Play(normal);
        ctx.AssertTrue("return removes eternal", !ctx.Self.HasPower<EternalRobePower>());
        await ctx.Self.GetPower<ImmaculateRobePower>()!.AfterPlayerTurnStart(Choice(), ctx.Player);
        ctx.AssertEqual("returned robe grants only one", 1, ctx.PowerAmount<MagicAmplificationPower>(ctx.Self));
        await TransformationCmd.Exit(Choice(), ctx.Self);
        ctx.AssertTrue("explicit exit removes all forms", !TransformationCmd.IsTransformed(ctx.Self));
        ctx.AssertTrue("explicit exit removes armour", TransformationCmd.GetArmor(ctx.Self) == null);
    }

    private static async Task Payment(CardEffectTestContext ctx)
    {
        CardModel source = ctx.Create<DarkElement>();
        await TransformationCmd.EnterImmaculateRobe(Choice(), ctx.Self, null);
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 2, null);
        TestCardSelector selector = new();
        selector.PrepareToSelect([0]);
        using (CardSelectCmd.UseSelector(selector))
            ctx.AssertTrue("accept pays last armour", await TransformationCmd.PayOverdraft(Choice(), ctx.Self, source));
        ctx.AssertEqual("last payment leaves zero", 0, Armor(ctx).Amount);
        ctx.AssertTrue("last payment retains form", TransformationCmd.IsTransformed(ctx.Self));
        ctx.AssertTrue("zero cannot pay or open selection", !await TransformationCmd.PayOverdraft(Choice(), ctx.Self, source));
        ctx.AssertTrue("failed payment does not exit", TransformationCmd.IsTransformed(ctx.Self));
        await TransformationCmd.GainArmor(Choice(), ctx.Self, 1, null);
        selector = new();
        selector.PrepareToSelect([1]);
        using (CardSelectCmd.UseSelector(selector))
            ctx.AssertTrue("decline does not pay", !await TransformationCmd.PayOverdraft(Choice(), ctx.Self, source));
        ctx.AssertEqual("decline retains armour", 1, Armor(ctx).Amount);
        await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
        await TransformationCmd.LoseArmor(Choice(), ctx.Self, 1, null);
        await ctx.Play(source, ctx.PrimaryEnemy);
        ctx.AssertEqual("amplification consumed once", 0, ctx.PowerAmount<MagicAmplificationPower>(ctx.Self));
        ctx.AssertEqual("amplification never charges zero armour", 0, Armor(ctx).Amount);
        ctx.AssertTrue("amplified release retains form", TransformationCmd.IsTransformed(ctx.Self));
    }
}
#endif
