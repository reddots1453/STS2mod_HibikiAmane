#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Powers;
using MaidenSuccubus.Core.Scriptures;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Powers.Scriptures;

namespace MaidenSuccubus.Debugging.CardEffects;

internal static class DesignSyncScriptureContract
{
    internal static readonly Type[] Types = [typeof(GuardianScripture), typeof(NimbleScripture),
        typeof(PunishmentScripture), typeof(WisdomScripture), typeof(VitalityScripture), typeof(BlissScripture)];

    internal static async Task Run(CardEffectTestContext ctx, CardModel card, bool upgraded)
    {
        int duration = upgraded ? 3 : 2;
        string sentence = card switch
        {
            GuardianScripture => "每回合结束时获得3点格挡。",
            NimbleScripture => "获得2层临时敏捷。",
            PunishmentScripture => "每回合结束时对随机敌人给予1层断罪。",
            WisdomScripture => "每回合开始时获得抽1张牌。",
            VitalityScripture => "回合开始时获得〈能量〉。",
            BlissScripture => "回合开始时失去〈欲望〉。\n若0〈欲望〉，获得〈能量〉并抽1张牌。",
            _ => throw new InvalidOperationException("Not a scripture"),
        };
        var outside = ctx.Player.RunState.CreateCard(ModelDb.GetById<CardModel>(card.Id), ctx.Player);
        if (upgraded) CardCmd.Upgrade(outside);
        foreach (var (instance, pile) in new[] { (outside, PileType.Deck), (card, PileType.Hand) })
            ctx.AssertEqual("scripture exact full text " + pile, $"持续{duration}回合。\n{sentence}",
                DesignSyncHolyTextContract.Normalize(instance.GetDescriptionForPile(pile)), effect: false);
        ctx.AssertEqual("scripture zero energy", 0, card.EnergyCost.Canonical, effect: false);
        ctx.AssertTrue("scripture not randomly generated", !card.CanBeGeneratedByModifiers, effect: false);

        var choice = new BlockingPlayerChoiceContext();
        var foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
        foreign.RunState = ctx.Player.RunState;
        await ctx.ApplyPower<DexterityPower>(ctx.Self, 1);
        await ctx.ApplyPower<HolyResonancePower>(ctx.Self, 2);
        await ctx.AddFillerCards(PileType.Draw, 8);
        if (card is BlissScripture) await Data.Desire.Set(ctx.Player, 2);
        var events = new List<(ScripturePowerTemplate Power, int Remaining)>();
        void Observe(ScriptureTriggered value)
        {
            if (value.Owner == ctx.Self) events.Add((value.Power, value.Power.Amount));
        }
        ScriptureCmd.Triggered += Observe;
        try
        {
            int blockBefore = ctx.Self.Block;
            await ctx.Play(card);
            var power = ctx.Self.Powers.OfType<ScripturePowerTemplate>().Single();
            ctx.AssertEqual("duration", duration, power.Amount);
            ctx.AssertEqual("independent instances", PowerInstanceType.Instanced, power.InstanceType, effect: false);
            ctx.AssertEqual("visible buff", PowerType.Buff, power.Type, effect: false);
            ctx.AssertTrue("power visible", power.IsVisible, effect: false);
            ctx.AssertEqual("no scripture trigger on play", 0, events.Count);
            ctx.AssertBlock("no premature resonance on application", blockBefore, 0);
            ctx.AssertPower<DexterityPower>("only Nimble grants immediate Dexterity", ctx.Self, card is NimbleScripture ? 3 : 1);
            ctx.AssertEqual("remaining duration participates in buff layer count", 3 + duration + (card is NimbleScripture ? 2 : 0),
                PowerLayerQuery.CountBuffLayers(ctx.Self));
            if (power is GuardianScripturePower)
            {
                var description = power.SmartDescription;
                ctx.AssertEqual("guardian power includes current Dexterity", "剩余回合内，每回合结束时获得4点格挡。",
                    DesignSyncHolyTextContract.Normalize(description.GetFormattedText()), effect: false);
            }

            string Snapshot() => string.Join("/", power.Amount, events.Count, ctx.Self.Block,
                ctx.PowerAmount(ctx.Self, "DexterityPower"), ctx.Player.PlayerCombatState!.Energy,
                ctx.CountCards<StrikeIronclad>(PileType.Hand), Data.Desire.Get(ctx.Player),
                ctx.Enemies.Sum(enemy => ctx.PowerAmount(enemy, "CondemnationPower")));
            bool atStart = card is WisdomScripture or VitalityScripture or BlissScripture;
            for (int turn = 1; turn <= duration; turn++)
            {
                string beforeWrongPhase = Snapshot();
                await power.AfterPlayerTurnStart(choice, foreign);
                await power.AfterSideTurnEnd(choice, ctx.PrimaryEnemy.Side, [ctx.Self]);
                await power.AfterSideTurnEnd(choice, ctx.Self.Side, [ctx.PrimaryEnemy]);
                if (atStart) await power.AfterSideTurnEnd(choice, ctx.Self.Side, [ctx.Self]);
                else await power.AfterPlayerTurnStart(choice, ctx.Player);
                ctx.AssertEqual("wrong phase/player/participants have no effect", beforeWrongPhase, Snapshot());

                int block = ctx.Self.Block, energy = ctx.Player.PlayerCombatState!.Energy;
                int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
                int condemnation = ctx.Enemies.Sum(enemy => ctx.PowerAmount(enemy, "CondemnationPower"));
                if (atStart) await power.AfterPlayerTurnStart(choice, ctx.Player);
                else await power.AfterSideTurnEnd(choice, ctx.Self.Side, [ctx.Self]);
                ctx.AssertEqual("one event per real effect/tick", turn, events.Count);
                ctx.AssertTrue("event identifies same instance before countdown", events[^1].Power == power
                    && events[^1].Remaining == duration - turn + 1);
                ctx.AssertBlock("actual effect plus resonance block per tick", block,
                    card is GuardianScripture ? 7 : card is NimbleScripture ? 5 : 3);
                bool blissAtZero = card is BlissScripture && turn >= 2;
                ctx.AssertEqual("energy at correct phase", card is VitalityScripture || blissAtZero ? 1 : 0,
                    ctx.Player.PlayerCombatState.Energy - energy);
                ctx.AssertPileDelta<StrikeIronclad>("draw at correct phase", PileType.Hand, hand,
                    card is WisdomScripture || blissAtZero ? 1 : 0);
                ctx.AssertEqual("condemnation per tick", card is PunishmentScripture ? 1 : 0,
                    ctx.Enemies.Sum(enemy => ctx.PowerAmount(enemy, "CondemnationPower")) - condemnation);
                if (card is BlissScripture)
                    ctx.AssertEqual("reduce first then check zero", Math.Max(0, 2 - turn), Data.Desire.Get(ctx.Player));
                ctx.AssertEqual("remaining duration after benefit", duration - turn, power.Amount);
                ctx.AssertEqual("last effect resolves before removal", turn < duration, ctx.Self.Powers.Contains(power));
                int extraDexterity = card is NimbleScripture && turn < duration ? 2 : 0;
                ctx.AssertPower<DexterityPower>("Nimble expires without removing unrelated Dexterity", ctx.Self, 1 + extraDexterity);
                ctx.AssertEqual("buff layer sum counts remaining turns", 3 + duration - turn + extraDexterity,
                    PowerLayerQuery.CountBuffLayers(ctx.Self));
            }

            string afterExpiry = Snapshot();
            await power.AfterPlayerTurnStart(choice, ctx.Player);
            await power.AfterSideTurnEnd(choice, ctx.Self.Side, [ctx.Self]);
            ctx.AssertEqual("expired reference cannot trigger again", afterExpiry, Snapshot());
            if (card is NimbleScripture) await IndependentNimble(ctx);
        }
        finally { ScriptureCmd.Triggered -= Observe; }
    }

    private static async Task IndependentNimble(CardEffectTestContext ctx)
    {
        var choice = new BlockingPlayerChoiceContext();
        await ctx.Play(ctx.Create<NimbleScripture>());
        await ctx.Play(ctx.Create<NimbleScripture>(true));
        var copies = ctx.Self.Powers.OfType<NimbleScripturePower>().OrderBy(power => power.Amount).ToArray();
        ctx.AssertEqual("same scripture has two separate instances", 2, copies.Length);
        ctx.AssertPower<DexterityPower>("two applications each add two", ctx.Self, 5);
        for (int tick = 1; tick <= 3; tick++)
        {
            foreach (var copy in copies) await copy.AfterSideTurnEnd(choice, ctx.Self.Side, [ctx.Self]);
            ctx.AssertEqual("independent durations expire separately", tick == 1 ? 2 : tick == 2 ? 1 : 0,
                ctx.Self.Powers.OfType<NimbleScripturePower>().Count());
            ctx.AssertPower<DexterityPower>("each expiry removes only its two Dexterity", ctx.Self, tick == 1 ? 5 : tick == 2 ? 3 : 1);
        }
        await ctx.Play(ctx.Create<NimbleScripture>(true));
        var removed = ctx.Self.Powers.OfType<NimbleScripturePower>().Single();
        await PowerCmd.Remove(removed);
        ctx.AssertPower<DexterityPower>("early removal cleans its own bonus", ctx.Self, 1);
        int block = ctx.Self.Block;
        await removed.AfterSideTurnEnd(choice, ctx.Self.Side, [ctx.Self]);
        ctx.AssertBlock("removed positive-duration instance cannot publish resonance", block, 0);
    }
}
#endif
