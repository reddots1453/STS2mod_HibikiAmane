#if DEBUG
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Cards;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Patches;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Util;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncFreeUntilPlayedContract
{
    private static CardModel? _probeCard;

    // Synthetic foreign cost providers exercise the actual final query/payment pipeline.
    private static void AddStars(CardModel card, ref decimal __result)
    {
        bool match = false;
        Safe.Run(() => match = ReferenceEquals(card, _probeCard), "FreeTest.Stars");
        if (match) __result += 4;
    }

    private static void AddSecondary(SecondaryResourceCostContext context, ref decimal __result)
    {
        bool match = false;
        Safe.Run(() => match = ReferenceEquals(context.Card, _probeCard), "FreeTest.Secondary");
        if (match) __result += 3;
    }

    public static async Task Run(CardEffectTestContext ctx, Action<bool, string> check)
    {
        var player = ctx.Player;
        var choice = new BlockingPlayerChoiceContext();
        var lust = (LustRouteRelic)ModelDb.Relic<LustRouteRelic>().ToMutable();
        lust.Stage = 4;
        await RelicCmd.Obtain(lust, player);
        await lust.BeforeCombatStart();
        var card = await ctx.Add<FocusedSlash>(PileType.Draw, upgraded: true);
        card.SecondaryCosts().Set(DesireResource.Id, 2);
        await Data.Desire.Set(player, 3);
        await Data.Desire.Modify(player, 1);
        check(card.Pile?.Type == PileType.Hand && FreeUntilPlayedCapability.IsActive(card), "actual awakened draw attaches entitlement");
        var untouched = await ctx.Add<FocusedSlash>(PileType.Hand);
        foreach (int resource in new[] { 1, 4, 6 })
        {
            await Data.Desire.Set(player, resource);
            check(card.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "free overrides changing FocusedSlash global cost");
            check(untouched.EnergyCost.GetWithModifiers(CostModifiers.All) == resource, "unmarked card retains dynamic cost");
            check(card.EnergyCost.GetWithModifiers(CostModifiers.Global) == resource, "global-only query not changed by local entitlement");
        }
        card.EndOfTurnCleanup();
        await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
        await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
        check(card.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "dynamic free survives turn and redraw");

        var clone = ctx.Combat.CloneCard(card);
        await CardPileCmd.Add(clone, PileType.Discard, skipVisuals: true);
        check(FreeUntilPlayedCapability.IsActive(clone) && clone.IsUpgraded, "clone inherits independent free entitlement and upgrade");
        clone.EnergyCost.AfterCardPlayedCleanup();
        check(!FreeUntilPlayedCapability.IsActive(clone) && FreeUntilPlayedCapability.IsActive(card), "clone cleanup cannot consume original entitlement");
        check(clone.EnergyCost.GetWithModifiers(CostModifiers.All) == 6, "clone dynamic cost restored");

        var saved = CardModel.FromSerializable(card.ToSerializable());
        check(ModelCapabilities.TryGet(saved, out ModelCapabilitySet? savedSet)
            && savedSet.Get<FreeUntilPlayedCapability>() != null, "native serialized capability restored");
        var restored = saved;
        ctx.Combat.AddCard(restored, player);
        await CardPileCmd.Add(restored, PileType.Discard, skipVisuals: true);
        check(restored.EnergyCost.GetWithModifiers(CostModifiers.All) == 0 && restored.IsUpgraded, "restored dynamic free query and upgrade");
        restored.EnergyCost.AfterCardPlayedCleanup();
        check(!FreeUntilPlayedCapability.IsActive(restored) && FreeUntilPlayedCapability.IsActive(card), "restored cleanup independent");
        var consumed = CardModel.FromSerializable(restored.ToSerializable());
        check(!ModelCapabilities.TryGet(consumed, out ModelCapabilitySet? consumedSet)
            || consumedSet.Get<FreeUntilPlayedCapability>() == null, "consumed entitlement not resurrected by save");

        // Q17: free fixed costs do not replace native X/Y payment semantics.
        var x = await ctx.Add<Whirlwind>(PileType.Discard);
        int xSpend = x.EnergyCost.GetAmountToSpend();
        GeneratedCardCostCmd.SetFreeUntilPlayed(x);
        check(x.EnergyCost.CostsX && x.EnergyCost.GetAmountToSpend() == xSpend, "native X semantics unchanged");
        var y = await ctx.Add<MaidenStrike>(PileType.Discard);
        y.SecondaryCosts().Set(DesireResource.Id, SecondaryResourceCost.X());
        int ySpend = SecondaryResourcePaymentResolver.Plan(y).Lines.Single().AmountToSpend;
        GeneratedCardCostCmd.SetFreeUntilPlayed(y);
        check(SecondaryResourcePaymentResolver.Plan(y).Lines.Single().AmountToSpend == ySpend, "native Y semantics unchanged");
        var curse = await ctx.Add<Injury>(PileType.Discard);
        int negative = curse.EnergyCost.GetWithModifiers(CostModifiers.All);
        GeneratedCardCostCmd.SetFreeUntilPlayed(curse);
        check(curse.EnergyCost.GetWithModifiers(CostModifiers.All) == negative, "negative unplayable cost unchanged");

        var harmony = new Harmony("MaidenSuccubus.Debug.FreeUntilPlayed");
        try
        {
            _probeCard = card;
            harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ModifyStarCost)),
                postfix: new HarmonyMethod(typeof(DesignSyncFreeUntilPlayedContract), nameof(AddStars)));
            harmony.Patch(AccessTools.Method(typeof(SecondaryResourceHook), nameof(SecondaryResourceHook.ModifyCost)),
                postfix: new HarmonyMethod(typeof(DesignSyncFreeUntilPlayedContract), nameof(AddSecondary)));
            // A later modifier overwrites the local zero; the final entitlement still wins.
            card.SecondaryCosts().Set(DesireResource.Id, new SecondaryResourceCost(2), SecondaryResourceCostDuration.UntilPlayed);
            await PowerCmd.Apply<DesirePaidWithHpPower>(choice, ctx.Self, 1, ctx.Self, null);
            check(card.GetStarCostWithModifiers() == 0, "final stars ignore synthetic global surcharge");
            var plan = SecondaryResourcePaymentResolver.Plan(card);
            var line = plan.Lines.Single(l => l.ResourceId == DesireResource.Id);
            check(line.IsFree && line.Cost == 0 && line.AmountToSpend == 0 && line.CanPlay
                && line.OriginalShortfall == 0 && line.CoveredShortfall == 0 && line.Shortfall == 0,
                "final required secondary line free after surcharge and HP replacement");
            foreach (var kind in new[] { SecondaryResourceUseKind.OptionalSpend, SecondaryResourceUseKind.ExtraSpend })
            {
                // Direct record probe, not a claim of optional-card game integration.
                var optional = line with { Kind = kind, Cost = 2, AmountToSpend = 2, IsFree = false };
                var before = optional;
                FreeUntilPlayedSecondaryPatch.Postfix(card, ref optional);
                check(optional == before, "optional and extra spending records unchanged");
            }
            int energy = player.PlayerCombatState!.Energy, desire = Data.Desire.Get(player);
            decimal hp = ctx.Self.CurrentHp;
            (int paid, int stars) = await card.SpendResources();
            check(paid == 0 && stars == 0 && player.PlayerCombatState.Energy == energy
                && Data.Desire.Get(player) == desire && ctx.Self.CurrentHp == hp,
                "actual paid-play spends no energy stars resource or replacement HP");
            check(ctx.Self.HasPower<DesirePaidWithHpPower>(), "free payment does not consume HP replacement power");
            await card.OnPlayWrapper(choice, ctx.PrimaryEnemy, isAutoPlay: false,
                new ResourceInfo { EnergySpent = paid, EnergyValue = paid, StarsSpent = stars, StarValue = stars }, skipCardPileVisuals: true);
            check(!FreeUntilPlayedCapability.IsActive(card), "actual wrapper cleanup consumes entitlement");
            check(card.EnergyCost.GetWithModifiers(CostModifiers.All) == 6 && card.GetStarCostWithModifiers() == 4,
                "dynamic surcharges return after actual played cleanup");
        }
        finally { _probeCard = null; harmony.UnpatchAll(harmony.Id); }
        await PowerCmd.Remove(ctx.Self.GetPower<DesirePaidWithHpPower>()!);
        await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
        int energyBefore = player.PlayerCombatState!.Energy, desireBefore = Data.Desire.Get(player);
        (int nextEnergy, int nextStars) = await card.SpendResources();
        await card.OnPlayWrapper(choice, ctx.PrimaryEnemy, isAutoPlay: false,
            new ResourceInfo { EnergySpent = nextEnergy, EnergyValue = nextEnergy, StarsSpent = nextStars, StarValue = nextStars },
            skipCardPileVisuals: true);
        check(player.PlayerCombatState.Energy == energyBefore - desireBefore && Data.Desire.Get(player) == desireBefore - 2,
            "second actual paid-play pays dynamic energy and original resource");

        GeneratedCardCostCmd.SetFreeUntilPlayed(untouched);
        var capability = ModelCapabilities.Get(untouched).Get<FreeUntilPlayedCapability>()!;
        await capability.AfterCombatEnd(null!); // Callback unit probe; natural combat end remains hand-test coverage.
        check(!FreeUntilPlayedCapability.IsActive(untouched), "combat end removes entitlement");
        GeneratedCardCostCmd.SetFreeUntilPlayed(untouched);
        await ModelCapabilities.Get(untouched).Get<FreeUntilPlayedCapability>()!.BeforeCombatStart();
        check(!FreeUntilPlayedCapability.IsActive(untouched), "next combat does not inherit stale entitlement");
        await RunXCosts(ctx, check);
    }

    private static async Task RunXCosts(CardEffectTestContext ctx, Action<bool, string> check)
    {
        var choice = new BlockingPlayerChoiceContext();
        foreach (bool untilPlayed in new[] { false, true })
        foreach (bool upgraded in new[] { false, true })
        foreach (var sample in new (int Energy, int Desire, bool Replay)[] { (2, 3, false), (2, 3, true), (0, 3, false), (2, 0, false) })
        {
            await ctx.Reset();
            await PlayerCmd.SetEnergy(sample.Energy, ctx.Player);
            await Data.Desire.Set(ctx.Player, sample.Desire);
            var card = await ctx.Add<AllHopeLost>(PileType.Hand, upgraded);
            if (sample.Replay) CardCmd.Enchant<Glam>(card, 1);
            if (untilPlayed) GeneratedCardCostCmd.SetFreeUntilPlayed(card);
            else GeneratedCardCostCmd.SetFreeThisTurn(card);
            check(card.EnergyCost.CostsX && card.SecondaryCosts().Get(DesireResource.Id).CostsX,
                "free preserves both X cost identities");
            if (untilPlayed)
            {
                card.EndOfTurnCleanup();
                check(FreeUntilPlayedCapability.IsActive(card), "X entitlement survives the turn boundary");
            }
            int start = CombatManager.Instance.History.Entries.Count();
            int hp = ctx.PrimaryEnemy.CurrentHp;
            (int energy, int stars) = await card.SpendResources();
            check(energy == sample.Energy && ctx.Player.PlayerCombatState!.Energy == 0
                && Data.Desire.Get(ctx.Player) == 0, "native paid X/Y consumes available resources despite fixed-cost free");
            await card.OnPlayWrapper(choice, ctx.PrimaryEnemy, isAutoPlay: false,
                new ResourceInfo { EnergySpent = energy, EnergyValue = energy, StarsSpent = stars, StarValue = stars },
                skipCardPileVisuals: true);
            int hits = (sample.Energy + (upgraded ? 1 : 0)) * (sample.Replay ? 2 : 1);
            check(hp - ctx.PrimaryEnemy.CurrentHp == 6 * sample.Desire * hits,
                "free dual X damage uses captured native values on every replay");
            check(CombatManager.Instance.History.Entries.Skip(start).OfType<DamageReceivedEntry>()
                .Count(hit => hit.CardSource == card && hit.Receiver == ctx.PrimaryEnemy) == hits,
                "free dual X preserves base upgraded zero and replay hit counts");
            check(!FreeUntilPlayedCapability.IsActive(card), "native final cleanup consumes X entitlement once");
        }

        // The native ThisTurn binding also covers fixed secondary costs without custom layers.
        await ctx.Reset();
        await PlayerCmd.SetEnergy(3, ctx.Player);
        await Data.Desire.Set(ctx.Player, 3);
        var fixedCard = await ctx.Add<MaidenStrike>(PileType.Hand);
        fixedCard.SecondaryCosts().Set(DesireResource.Id, 2);
        GeneratedCardCostCmd.SetFreeThisTurn(fixedCard);
        (int paid, int starPaid) = await fixedCard.SpendResources();
        check(paid == 0 && starPaid == 0 && ctx.Player.PlayerCombatState!.Energy == 3
            && Data.Desire.Get(ctx.Player) == 3, "native ThisTurn frees fixed energy and secondary cost");
        await fixedCard.OnPlayWrapper(choice, ctx.PrimaryEnemy, isAutoPlay: false,
            new ResourceInfo { EnergySpent = paid, EnergyValue = paid, StarsSpent = starPaid, StarValue = starPaid },
            skipCardPileVisuals: true);
        await CardPileCmd.Add(fixedCard, PileType.Hand, skipVisuals: true);
        (paid, starPaid) = await fixedCard.SpendResources();
        check(paid == 1 && ctx.Player.PlayerCombatState!.Energy == 2 && Data.Desire.Get(ctx.Player) == 1,
            "native ThisTurn binding clears after play and restores both fixed costs");
        await fixedCard.OnPlayWrapper(choice, ctx.PrimaryEnemy, isAutoPlay: false,
            new ResourceInfo { EnergySpent = paid, EnergyValue = paid, StarsSpent = starPaid, StarValue = starPaid },
            skipCardPileVisuals: true);
    }
}
#endif
