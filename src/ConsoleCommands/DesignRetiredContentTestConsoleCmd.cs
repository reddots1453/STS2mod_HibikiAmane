#if DEBUG
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Enchantments;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Native model/candidate/save contracts. Does not execute legacy effects.</summary>
public sealed class DesignRetiredContentTestConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_test_retired";
    public override string Args => "confirm";
    public override string Description => "DS27 retired acquisition and native save contracts; disposable solo run only";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (issuingPlayer?.Character is not MaidenSuccubusCharacter
            || issuingPlayer.RunState.Players.Count != 1
            || args.Length != 1 || args[0] != "confirm")
            return new CmdResult(false, "Use ms_test_retired confirm in a disposable single-player Maiden run.");
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("DS27 retired assertion failed: " + name);
            checks++;
        }
        try
        {
            CardModel[] retired = [ModelDb.Card<MagicResonance>(), ModelDb.Card<SemenAppetite>()];
            foreach (CardModel canonical in retired)
            {
                Check(RetiredCardCatalog.IsRetired(canonical), "explicit retired identity");
                Check(ReferenceEquals(ModelDb.GetById<CardModel>(canonical.Id), canonical), "model ID still resolves");
                Check(canonical.Pool.AllCards.Contains(canonical), "registered pool preserves identity");
                Check(!canonical.ShouldShowInCardLibrary, "native library excludes retired card");
                Check(!canonical.CanBeGeneratedInCombat && !canonical.CanBeGeneratedByModifiers,
                    "both native generation flags disabled");
                foreach (CardMultiplayerConstraint constraint in Enum.GetValues<CardMultiplayerConstraint>())
                    Check(!canonical.Pool.GetUnlockedCards(issuingPlayer.UnlockState, constraint).Contains(canonical),
                        "reward/shop/unlock candidate excluded for " + constraint);
                Check(!AllMaidenSuccubusCards.GetCanonicalCards().Contains(canonical), "custom canonical candidates");
                Check(!AllMaidenSuccubusCards.GetUnlockedCards(issuingPlayer).Contains(canonical), "custom unlocked candidates");
                Check(!CardFactory.FilterForCombat(canonical.Pool.AllCards).Contains(canonical), "native combat factory filter");
                foreach (bool upgraded in new[] { false, true })
                {
                    CardModel copy = canonical.ToMutable();
                    if (upgraded)
                    {
                        copy.UpgradeInternal();
                        copy.FinalizeUpgradeInternal();
                    }
                    // Seed a pre-retirement save via the same internal attachment
                    // used by load; CanEnchant is intentionally false for NEW use.
                    copy.EnchantInternal(ModelDb.Enchantment<EnergyOverloadEnchantment>().ToMutable(), 2);
                    CardModel loaded = CardModel.FromSerializable(copy.ToSerializable());
                    Check(loaded.Id == canonical.Id && loaded.GetType() == canonical.GetType(), "no save remap");
                    Check(loaded.IsUpgraded == upgraded, "saved upgrade retained");
                    Check(loaded.Enchantment is EnergyOverloadEnchantment { Amount: 2 }, "retired enchantment save retained");
                    Check(ReferenceEquals(loaded.Enchantment!.Card, loaded), "loaded enchantment belongs to loaded card");
                    Check(copy.DynamicVars.All(pair => loaded.DynamicVars[pair.Key].BaseValue == pair.Value.BaseValue),
                        "legacy dynamic values preserved");
                    CardModel clone = loaded.CreateClone();
                    Check(clone.Id == loaded.Id && clone.IsUpgraded == loaded.IsUpgraded, "clone identity and upgrade");
                    Check(!ReferenceEquals(clone.Enchantment, loaded.Enchantment)
                        && ReferenceEquals(clone.Enchantment!.Card, clone), "clone enchantment isolated");
                    Check(!loaded.ShouldShowInCardLibrary && !loaded.CanBeGeneratedInCombat, "load does not reopen acquisition");
                }
                // Use a detached run-owned model as source; do not change deck or RNG.
                CardModel source = canonical.ToMutable();
                source.Owner = issuingPlayer;
                foreach (bool inCombat in new[] { false, true })
                    Check(CardFactory.GetDefaultTransformationOptions(source, inCombat).All(c => !RetiredCardCatalog.IsRetired(c)),
                        "native transform options exclude retired identities");
                Check(canonical.Enchantment == null && !canonical.IsUpgraded, "canonical remains immutable");
            }
            foreach (CardPoolModel pool in AllMaidenSuccubusCards.Pools)
            {
                foreach (CardMultiplayerConstraint constraint in Enum.GetValues<CardMultiplayerConstraint>())
                {
                    var expected = pool.AllCards.Where(c => !RetiredCardCatalog.IsRetired(c)
                        && (constraint == CardMultiplayerConstraint.None
                            || c.MultiplayerConstraint == CardMultiplayerConstraint.None
                            || c.MultiplayerConstraint == constraint))
                        .Select(c => c.Id).ToHashSet();
                    var actual = pool.GetUnlockedCards(issuingPlayer.UnlockState, constraint)
                        .Select(c => c.Id).ToHashSet();
                    Check(actual.SetEquals(expected), "all non-retired cards preserved in " + pool.Title + "/" + constraint);
                }
            }
            CardModel active = ModelDb.Card<DreamPigment>();
            Check(active.ShouldShowInCardLibrary && active.CanBeGeneratedInCombat && active.CanBeGeneratedByModifiers,
                "replacement is independently obtainable");
            Check(CardFactory.FilterForCombat([active, .. retired]).SequenceEqual([active]), "native mixed candidate filter");
            CardModel vanilla = ModelDb.Card<Bash>();
            Check(RetiredCardCatalog.Obtainable([vanilla, active, .. retired]).SequenceEqual([vanilla, active]),
                "vanilla and active models unaffected");
            Check(!ModelDb.Enchantment<EnergyOverloadEnchantment>().CanEnchant(active.ToMutable()),
                "retired enchantment rejects new applications");
            string result = $"[DS27RetiredTest] PASS {checks} native model/candidate/save assertions; no legacy effects executed.";
            MaidenSuccubusMod.Logger.Info(result);
            return new CmdResult(true, result);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27RetiredTest] " + ex);
            return new CmdResult(false, $"Failed after {checks} assertions: {ex.Message}");
        }
    }
}
#endif
