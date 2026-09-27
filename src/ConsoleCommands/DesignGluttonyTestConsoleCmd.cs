#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Destructive, explicit real relic/potion commands in a disposable run.</summary>
public sealed class DesignGluttonyTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_gluttony";
    public override string Args => "confirm";
    public override string Description => "Destructive Gluttony tests; disposable run only";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (_running || issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1 || CombatManager.Instance.IsInProgress
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_gluttony confirm outside combat in a disposable single-player Maiden run.");
        return new CmdResult(Run(issuingPlayer), true, "Destructive tests started; see [DS27GluttonyTest].");
    }

    private static async Task Run(Player player)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 gluttony: " + name);
            checks++;
        }
        async Task Reset(int occupied)
        {
            foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
            foreach (PotionModel potion in player.Potions.ToArray()) await PotionCmd.Discard(potion);
            if (player.MaxPotionCount < 3) await PlayerCmd.GainMaxPotionCount(3 - player.MaxPotionCount, player);
            if (player.MaxPotionCount > 3) await PlayerCmd.LoseMaxPotionCount(player.MaxPotionCount - 3, player);
            await CreatureCmd.SetMaxAndCurrentHp(player.Creature, 50);
            for (int i = 0; i < occupied; i++) await PotionCmd.TryToProcure<FruitJuice>(player);
            Check(player.Potions.Count() == occupied, "fixture potion slots populated");
        }
        try
        {
            TestMode.IsOn = true;
            foreach (int stage in new[] { 0, 1, 2, 3, 4 })
            foreach (int occupied in new[] { 0, 1, 3 })
            {
                await Reset(occupied);
                var original = player.Potions.ToArray();
                int rngBefore = player.RunState.Rng.CombatPotionGeneration.ToSerializable().counter;
                var relic = (GluttonyRouteRelic)ModelDb.Relic<GluttonyRouteRelic>().ToMutable();
                relic.Stage = stage;
                await RelicCmd.Obtain(relic, player);
                int wantHp = stage is 1 or 2 ? 54 : 50;
                int wantSlots = stage is 1 or 2 ? 4 : 3;
                int wantPotions = stage >= 3 ? 3 : occupied;
                Check(player.Creature.MaxHp == wantHp, "actual pickup max HP stage " + stage);
                Check(player.MaxPotionCount == wantSlots, "actual pickup slot count");
                Check(player.Potions.Count() == wantPotions, "fill only actual empty slots");
                Check(original.All(potion => player.Potions.Contains(potion)), "existing potion objects preserved");
                if (stage >= 3 && occupied == 3)
                    Check(player.RunState.Rng.CombatPotionGeneration.ToSerializable().counter == rngBefore,
                        "full inventory does not advance generation RNG");
                int rngAfter = player.RunState.Rng.CombatPotionGeneration.ToSerializable().counter;
                await relic.AfterObtained();
                Check(player.Creature.MaxHp == wantHp && player.MaxPotionCount == wantSlots
                    && player.Potions.Count() == wantPotions, "duplicate pickup no second reward");
                Check(player.RunState.Rng.CombatPotionGeneration.ToSerializable().counter == rngAfter,
                    "duplicate pickup does not roll more potions");
                var restored = (GluttonyRouteRelic)ModelDb.Relic<GluttonyRouteRelic>().ToMutable();
                SavedProperties.From(relic)!.Fill(restored);
                restored.Owner = player;
                await restored.AfterObtained();
                Check(player.Creature.MaxHp == wantHp && player.MaxPotionCount == wantSlots
                    && player.Potions.Count() == wantPotions, "saved pickup receipt prevents replay");

                Player other = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
                other.RunState = player.RunState;
                var otherPotion = ModelDb.Potion<FruitJuice>().ToMutable();
                otherPotion.Owner = other;
                await relic.AfterPotionUsed(otherPotion, player.Creature);
                Check(player.Creature.MaxHp == wantHp, "other owner's potion targeting us cannot trigger");

                if (player.Potions.Any()) await PotionCmd.Discard(player.Potions.First());
                Check(player.Creature.MaxHp == wantHp, "discard is not use");
                var juice = ModelDb.Potion<FruitJuice>().ToMutable();
                Check((await PotionCmd.TryToProcure(juice, player)).success, "known usable potion acquired");
                Check(player.Creature.MaxHp == wantHp, "acquisition is not use");
                decimal nativeHp = juice.DynamicVars.MaxHp.BaseValue;
                await juice.OnUseWrapper(new BlockingPlayerChoiceContext(), player.Creature);
                Check(player.Creature.MaxHp == wantHp + nativeHp + (stage >= 3 ? 4 : 0),
                    "actual potion-use wrapper triggers awakened bonus exactly once");
                Check(!player.Potions.Contains(juice), "actual potion use removes potion");
            }
            // The two trial rewards are separate instances, so each grants its own permanent bonus.
            await Reset(0);
            for (int stage = 1; stage <= 2; stage++)
            {
                foreach (var old in player.Relics.OfType<GluttonyRouteRelic>().ToArray()) await RelicCmd.Remove(old);
                var next = (GluttonyRouteRelic)ModelDb.Relic<GluttonyRouteRelic>().ToMutable();
                next.Stage = stage;
                await RelicCmd.Obtain(next, player);
            }
            Check(player.Creature.MaxHp == 58 && player.MaxPotionCount == 5, "two distinct trial pickups each grant four HP and one slot");
            MaidenSuccubusMod.Logger.Info($"[DS27GluttonyTest] PASS {checks} assertions; disposable run modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27GluttonyTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
