#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Debugging.CardEffects;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.ConsoleCommands;

public sealed class DesignNecklaceTestConsoleCmd : AbstractConsoleCmd
{
    private static bool _running;
    public override string CmdName => "ms_test_necklace";
    public override string Args => "confirm";
    public override string Description => "Destructive necklace tests; disposable single-player combat only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        if (_running || player?.Character is not MaidenSuccubusCharacter || player.RunState.Players.Count != 1
            || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding
            || player.Creature.CombatState is not CombatState combat || combat.HittableEnemies.Count == 0
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_necklace confirm in a disposable single-player Maiden combat.");
        return new CmdResult(Run(player, combat), true, "Destructive tests started; see [DS27NecklaceTest].");
    }

    private static async Task Run(Player player, CombatState combat)
    {
        _running = true;
        bool previousTestMode = TestMode.IsOn;
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 necklace: " + name);
            checks++;
        }
        string Plain(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", "");
        var ctx = new CardEffectTestContext(combat, player);
        var choice = new BlockingPlayerChoiceContext();
        try
        {
            TestMode.IsOn = true;
            await ctx.PrepareSuite();
            var run = (RunState)player.RunState;
            var clear = ModelDb.Relic<ClearHeartNecklace>();
            var murky = ModelDb.Relic<MurkyHeartNecklace>();
            Check(clear.IsAllowed(run) && !murky.IsAllowed(run) && !murky.IsAllowedInShops,
                "only one normal acquisition candidate; legacy model still resolves");
            Check(clear.Title.GetFormattedText() == "清心项链" && murky.Title.GetFormattedText() == "浊心项链",
                "ownerless encyclopedia has stable titles");
            var foreign = Player.CreateForNewRun<Ironclad>(player.UnlockState, player.NetId + 1000);
            foreign.RunState = run;
            var otherMaiden = Player.CreateForNewRun<MaidenSuccubusCharacter>(player.UnlockState, player.NetId + 2000);
            otherMaiden.RunState = run;

            foreach (bool legacy in new[] { false, true })
            {
                HeartNecklace necklace = (HeartNecklace)(legacy ? murky : (RelicModel)clear).ToMutable();
                await RelicCmd.Obtain(necklace, player);
                int floor = necklace.FloorAddedToDeck;
                var id = necklace.Id;
                Check(necklace.Rarity == RelicRarity.Rare, "both persisted IDs have rare rarity");
                Check(!murky.IsAllowed(run) && !murky.IsAllowedAtNeow(player), "legacy ID never rolls as a second candidate");

                foreach (int corruption in Enumerable.Range(-5, 11))
                {
                    CorruptionCmd.Set(run, corruption);
                    bool isMurky = corruption is 3 or 4 or 5;
                    string title = isMurky ? "浊心项链" : "清心项链";
                    string description = isMurky
                        ? "回合开始时，如果≤5点欲望，获得1点欲望值。堕落值＜3：变奏。"
                        : "回合开始时，如果≥5点欲望，失去1点欲望值。堕落值≥3：变奏。";
                    Check(necklace.Title.GetFormattedText() == title, "current title follows run, not saved type");
                    Check(Plain(necklace.DynamicDescription.GetFormattedText()) == description,
                        "actual patched description exact punctuation and comparator");
                    Check(ReferenceEquals(player.Relics.Single(), necklace) && necklace.Id == id
                        && necklace.FloorAddedToDeck == floor, "variation retains identity slot and acquisition floor");

                    // Real resource pipeline below max; reaching ten has a separate
                    // penalty, so ten's necklace boundary is tested by pure rules.
                    for (int desire = 0; desire <= 9; desire++)
                    {
                        await Data.Desire.Set(player, desire);
                        await necklace.AfterPlayerTurnStart(choice, foreign);
                        await necklace.AfterPlayerTurnStart(choice, otherMaiden);
                        Check(Data.Desire.Get(player) == desire, "other player callbacks do not trigger");
                        await necklace.AfterPlayerTurnStart(choice, player);
                        int expected = isMurky
                            ? desire <= 5 ? desire + 1 : desire
                            : desire >= 5 ? desire - 1 : desire;
                        Check(Data.Desire.Get(player) == expected, "actual normal resource command changes exact amount");
                    }

                    HeartNecklace restored = (HeartNecklace)RelicModel.FromSerializable(necklace.ToSerializable());
                    restored.Owner = player; // Load does not call AfterObtained.
                    Check(restored.Id == id && restored.FloorAddedToDeck == floor, "old model ID and floor round trip");
                    Check(restored.Title.GetFormattedText() == title
                        && Plain(restored.DynamicDescription.GetFormattedText()) == description,
                        "loaded instance correct without subscription or pickup replay");
                    await Data.Desire.Set(player, 5);
                    await restored.AfterPlayerTurnStart(choice, player);
                    Check(Data.Desire.Get(player) == (isMurky ? 6 : 4), "loaded model executes inclusive five boundary");
                }

                CorruptionCmd.Set(run, 3);
                await Data.Desire.Set(player, 5);
                await Hook.AfterPlayerTurnStart(combat, choice, player);
                Check(Data.Desire.Get(player) == 6, "real turn dispatcher invokes the registered relic once");
                await Data.Desire.Set(player, 5);
                await PowerCmd.Apply<PreventNextDesireGainPower>(choice, ctx.Self, 1, ctx.Self, null);
                await necklace.AfterPlayerTurnStart(choice, player);
                Check(Data.Desire.Get(player) == 5 && !ctx.Self.HasPower<PreventNextDesireGainPower>(),
                    "normal gain prevention is respected and consumed");

                var foreignCopy = (HeartNecklace)RelicModel.FromSerializable(necklace.ToSerializable());
                foreignCopy.Owner = foreign;
                await Data.Desire.Set(foreign, 5);
                await foreignCopy.AfterPlayerTurnStart(choice, foreign);
                Check(Data.Desire.Get(foreign) == 5, "non-Maiden owner has no effect");
                await RelicCmd.Remove(necklace);
                await Data.Desire.Set(player, 5);
                await necklace.AfterPlayerTurnStart(choice, player);
                Check(Data.Desire.Get(player) == 5, "removed instance cannot trigger");
                Check(clear.IsAllowed(run), "normal eligibility stays with vanilla grab-bag ownership rules");
            }
            MaidenSuccubusMod.Logger.Info($"[DS27NecklaceTest] PASS {checks} assertions; disposable combat modified.");
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27NecklaceTest] FAIL " + ex);
            throw;
        }
        finally { TestMode.IsOn = previousTestMode; _running = false; }
    }
}
#endif
