#if DEBUG
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignRetentionOrbTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_retention_orbs";
    public override string Args => "confirm";
    public override string Description => "Destructive DS27 retain/upgrade/enchant tests; disposable combat only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        if (player?.Character is not MaidenSuccubusCharacter || player.RunState.Players.Count != 1
            || CombatManager.Instance.DebugOnlyGetState() is not CombatState combat
            || !CombatManager.Instance.IsInProgress || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_retention_orbs confirm in a disposable single-player Maiden combat.");
        if (_running) return new CmdResult(false, "Retention tests already running.");
        return new CmdResult(Run(new CardEffectTestContext(combat, player)), true, "Started; see [DS27RetentionTest] log.");
    }

    private static async Task Run(CardEffectTestContext ctx)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 retention assertion failed: " + name);
            checks++;
        }
        async Task FlushChoosing(CardModel card)
        {
            var selector = new TestCardSelector();
            selector.PrepareToSelect([card]);
            using (CardSelectCmd.UseSelector(selector)) await Hook.BeforeFlush(ctx.Combat, ctx.Player);
        }
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            foreach (bool eternal in new[] { false, true })
            {
                RelicModel orb = eternal ? ModelDb.Relic<EternalOrb>().ToMutable() : ModelDb.Relic<HeroOrb>().ToMutable();
                await RelicCmd.Obtain(orb, ctx.Player);
                for (int corruption = -5; corruption <= 5; corruption++)
                {
                    await ctx.Reset();
                    CorruptionCmd.Set((RunState)ctx.Player.RunState, corruption);
                    CardModel deck = ctx.Player.RunState.CreateCard<Bash>(ctx.Player);
                    await CardPileCmd.Add(deck, PileType.Deck, skipVisuals: true);
                    CardModel card = await ctx.Add<Bash>(PileType.Hand);
                    card.DeckVersion = deck;
                    card.EnergyCost.SetThisCombat(5);
                    CardModel other = await ctx.Add<DefendIronclad>(PileType.Hand);
                    bool permanent = eternal || corruption is -5 or -4;
                    bool upgrade = eternal || corruption is 4 or 5;
                    string description = System.Text.RegularExpressions.Regex.Replace(
                        orb.DynamicDescription.GetFormattedText(), @"\[[^\]]*\]", "");
                    string expectedText = eternal
                        ? "回合结束时，选择手牌中的1张牌添加保留，将其升级并附魔：沉眠精华。"
                        : permanent
                            ? "回合结束时，选择手牌中的1张牌添加保留。堕落值＞-4：变奏。"
                            : upgrade
                                ? "回合结束时，保留1张牌并将其升级。堕落值＜4：变奏。"
                                : "回合结束时，保留1张牌。堕落值≥4或≤-4：变奏。";
                    Check(description == expectedText, "formatted active description and punctuation");
                    await FlushChoosing(card);
                    Check(card.ShouldRetainThisTurn && !other.ShouldRetainThisTurn, "only selected card retained");
                    Check(card.Keywords.Contains(CardKeyword.Retain) == permanent, "temporary vs keyword retention");
                    Check(card.IsUpgraded == upgrade && !other.IsUpgraded, "upgrade branch and selection");
                    Check(!deck.IsUpgraded && !deck.Keywords.Contains(CardKeyword.Retain) && deck.Enchantment == null,
                        "combat changes never reach permanent deck");
                    Check((card.Enchantment is SlumberingEssence) == eternal, "real vanilla enchantment only on eternal");
                    Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == (eternal ? 4 : 5), "initial end phase reduces exactly once");
                    card.EndOfTurnCleanup();
                    Check(card.ShouldRetainThisTurn == permanent, "temporary retain expires, keyword persists");
                    if (eternal)
                    {
                        var enchantment = card.Enchantment;
                        await FlushChoosing(card);
                        Check(ReferenceEquals(enchantment, card.Enchantment), "existing enchantment is not replaced");
                        Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == 3, "next end phase reduces once, not twice");
                        await ctx.Play(card, ctx.PrimaryEnemy);
                        Check(card.EnergyCost.GetWithModifiers(CostModifiers.Local) == 5, "until-played cost resets after play");
                    }
                    await CardPileCmd.RemoveFromDeck([deck]);
                }
                await ctx.Reset();
                await Hook.BeforeFlush(ctx.Combat, ctx.Player); // Empty hand must not open a selector.
                await RelicCmd.Remove(orb);
            }

            // Wrong-player callbacks must not select or change the owner's hand.
            await ctx.Reset();
            await RelicCmd.Obtain<EternalOrb>(ctx.Player);
            CardModel untouched = await ctx.Add<Bash>(PileType.Hand);
            Player foreign = Player.CreateForNewRun<Ironclad>(ctx.Player.UnlockState, ctx.Player.NetId + 1000);
            await ctx.Player.GetRelic<EternalOrb>()!.BeforeFlushLate(new BlockingPlayerChoiceContext(), foreign);
            Check(!untouched.ShouldRetainThisTurn && !untouched.IsUpgraded && untouched.Enchantment == null,
                "foreign player callback leaves owner hand unchanged");
            await RelicCmd.Remove(ctx.Player.GetRelic<EternalOrb>()!);
            await ctx.Reset();
            await RelicCmd.Obtain<EternalOrb>(ctx.Player);
            CardModel enchanted = await ctx.Add<Bash>(PileType.Hand, upgraded: true);
            CardCmd.ApplyKeyword(enchanted, CardKeyword.Retain);
            var original = CombatEnchantmentCmd.ApplyVanilla<Sharp>(enchanted, 2);
            await ctx.Add<DefendIronclad>(PileType.Hand);
            await FlushChoosing(enchanted);
            Check(ReferenceEquals(original, enchanted.Enchantment) && original.Amount == 2, "does not replace or stack another enchantment");
            Check(enchanted.IsUpgraded && enchanted.Keywords.Contains(CardKeyword.Retain), "already upgraded/retained card remains valid");
            await RelicCmd.Remove(ctx.Player.GetRelic<EternalOrb>()!);
            await RelicCmd.Obtain<HeroOrb>(ctx.Player);
            var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
            Check(touch.SetupForPlayer(ctx.Player) && touch.UpgradedRelic == ModelDb.Relic<EternalOrb>().Id, "hero ancient mapping");
            await RelicCmd.Obtain(touch, ctx.Player);
            Check(ctx.Player.GetRelic<HeroOrb>() == null && ctx.Player.GetRelic<EternalOrb>() != null, "real Orobas replacement");
            MaidenSuccubusMod.Logger.Info($"[DS27RetentionTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27RetentionTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
