#if DEBUG
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Patches;
using MaidenSuccubus.RestSite;

namespace MaidenSuccubus.ConsoleCommands;

/// <summary>Read-only inspection. Does not alter resources, decks, RNG or the open screen.</summary>
public sealed class DesignSealViewTestConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "ms_test_seal_view";
    public override string Args => "";
    public override string Description => "Read-only sealed-deck text, eligibility, installed patches and visible tint checks";
    public override bool IsNetworked => false;

    public override CmdResult Process(Player? player, string[] args)
    {
        if (args.Length != 0 || player?.Character is not MaidenSuccubusCharacter
            || !LocalContext.IsMe(player) || player.RunState is not RunState run)
            return new CmdResult(false, "Use ms_test_seal_view in a local Maiden run. Open the deck view first to also check visible holders.");
        int checks = 0, holders = 0, sealedHolders = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            checks++;
        }
        string Plain(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\[/?(?:gold|purple)\]", "");
        try
        {
            var deckBefore = player.Deck.Cards.ToArray();
            int corruption = CorruptionQuery.Get(run);
            string rngBefore = run.Rng.CombatCardGeneration.ToSerializable().ToString();
            foreach (CardModel card in deckBefore)
            {
                var route = RouteCardQuery.Get(card);
                bool expected = corruption >= 3 && route == RouteCardKind.Holy
                    || corruption <= -3 && route == RouteCardKind.Corrupt;
                string? key = SealPresentation.DescriptionKey(card);
                Check((key != null) == expected, "permanent instance eligibility: " + card.Id);
                Check(CombatSealQuery.IsSealed(run, card) == expected, "presentation and next combat seal agree");
                Check(SealPresentation.DescriptionKey((CardModel)card.MutableClone()) == null, "detached/preview clone cannot itself qualify");
                Check(SealPresentation.DescriptionKey(card.CanonicalInstance) == null, "canonical compendium card not sealed");
                if (key != null)
                {
                    string expectedText = route == RouteCardKind.Holy
                        ? "由于堕落值≥3，这张牌被封印了，战斗开始时不会进入抽牌堆。"
                        : "由于堕落值≤-3，这张牌被封印了，战斗开始时不会进入抽牌堆。";
                    Check(Plain(new LocString("static_hover_tips", key).GetFormattedText()) == expectedText, "exact direction-specific hover text");
                }
            }
            Check(new SacrificeRestSiteOption(player).Description.GetFormattedText()
                == "移除所有封印区卡牌，不会占用本次火堆行动。", "formal sacrifice description without obsolete growth");
            foreach (var (type, method, prefix) in new[]
            {
                (typeof(NGridCardHolder), "OnCardReassigned", false),
                (typeof(NDeckViewScreen), "DisplayCards", false),
                (typeof(NGridCardHolder), "OnFreedToPool", true),
                (typeof(NCardHolder), "CreateHoverTips", true),
            })
            {
                var info = Harmony.GetPatchInfo(AccessTools.Method(type, method));
                Check(info != null && (prefix ? info.Prefixes : info.Postfixes)
                    .Any(patch => patch.PatchMethod.DeclaringType == typeof(DeckSealVisualPatch)), "installed seal hook: " + method);
            }
            if (NRun.Instance is { } active)
            {
                var pending = new Stack<Node>();
                pending.Push(active.GetTree().Root);
                while (pending.TryPop(out var node))
                {
                    if (node is NGridCardHolder holder && holder.CardNode != null && holder.IsVisibleInTree())
                    {
                        bool expected = DeckSealVisualPatch.IsInDeckView(holder)
                            && SealPresentation.DescriptionKey(holder.CardModel) != null;
                        Check(DeckSealVisualPatch.HasSealTint(holder) == expected, "visible holder tint matches base-card eligibility");
                        holders++;
                        if (expected) sealedHolders++;
                    }
                    else foreach (Node child in node.GetChildren()) pending.Push(child);
                }
            }
            Check(player.Deck.Cards.SequenceEqual(deckBefore) && CorruptionQuery.Get(run) == corruption
                && run.Rng.CombatCardGeneration.ToSerializable().ToString() == rngBefore, "read-only command preserves live state");
            string result = $"[DS27SealViewTest] PASS {checks} assertions; visible holders={holders}, sealed={sealedHolders}. "
                + "Only the current route/screen was inspected; layout, other thresholds and screen transitions still need manual checks.";
            MaidenSuccubusMod.Logger.Info(result);
            return new CmdResult(true, result);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[DS27SealViewTest] FAIL " + ex);
            return new CmdResult(false, ex.Message);
        }
    }
}
#endif
