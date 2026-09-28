using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Core.Cards;

/// <summary>Combat-instance rewrite. Neither the canonical model nor DeckVersion is mutated.</summary>
[RegisterModelCapability(StableEntryStem = "humility_rewrite")]
public sealed class HumilityRewriteCapability : CardPlayCapability
{
    internal static bool HasRewrites { get; private set; }
    private HumilityEffectProgram? _program;
    internal HumilityEffectProgram Program => _program ?? throw new InvalidOperationException("Missing humility program.");

    internal static HumilityRewriteCapability? Find(CardModel card) =>
        card.IsMutable && card.Pile?.Type != PileType.Deck
        && ModelCapabilities.TryGet(card, out ModelCapabilitySet? set)
            ? set.Get<HumilityRewriteCapability>() : null;

    // Called only after an effect adapter has produced a complete program. Do not guess
    // a single-hit Damage/Block fallback for an unsupported card.
    internal static HumilityRewriteCapability Apply(CardModel card, HumilityEffectProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (!card.IsMutable || card.Pile?.IsCombatPile != true || card.CombatState == null
            || card.Type is not (CardType.Attack or CardType.Skill))
            throw new InvalidOperationException("Humility requires a mutable combat attack or skill.");
        HumilityNativeEffects.Validate(program);
        HumilityRewriteCapability? capability = Find(card);
        HumilityEffectProgram next = (capability?._program ?? program).DoubleAmounts();
        HasRewrites = true;
        if (capability == null)
        {
            capability = ModelCapabilityRegistry.Create<HumilityRewriteCapability>();
            capability._program = next;
            card.AddCapability(capability, allowMerge: false);
        }
        else capability._program = next;

        // Native enchantments can store their keywords locally. Never call ModifyCard
        // again: enchantments may also make non-idempotent cost/stat changes.
        RemoveIntrinsicRules(card);
        capability.MarkDirty();
        card.InvokeEnergyCostChanged(); // Native NCard reload notification; no cost mutation.
        return capability;
    }

    private static void RemoveIntrinsicRules(CardModel card)
    {
        HashSet<CardKeyword> kept = EnchantmentKeywords(card.Enchantment);
        foreach (CardKeyword keyword in card.GetKeywordsWithSources(KeywordSources.Local).ToArray())
            if (!kept.Contains(keyword)) card.RemoveKeyword(keyword);
        card.BaseReplayCount = 0; // Enchanted/global replay remains in the native pipeline.
    }

    protected override void OnOwnerCardUpgraded(CardModel card) => RemoveIntrinsicRules(card);
    protected override void OnOwnerCardDowngraded(CardModel card) => RemoveIntrinsicRules(card);

    internal static HashSet<CardKeyword> EnchantmentKeywords(EnchantmentModel? enchantment)
    {
        if (enchantment is LayeredEnchantment layered)
            return layered.Layers.SelectMany(layer => EnchantmentKeywords(layer)).ToHashSet();
        return enchantment switch
        {
            Steady => [CardKeyword.Retain],
            RoyallyApproved => [CardKeyword.Innate, CardKeyword.Retain],
            Goopy => [CardKeyword.Exhaust],
            TezcatarasEmber => [CardKeyword.Eternal],
            _ => [],
        };
    }

    protected override async Task<bool> BeforeOwnerCardOnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_program == null || Owner == null || ControlQuery.GetProjection(Owner) != null) return false;
        await using var sink = new HumilityNativeEffects(choiceContext, cardPlay);
        await _program.Execute(HumilityNativeEffects.XForPlay(cardPlay),
            sink.ResolveValue, sink);
        return true;
    }

    protected override Task OnOwnerCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;

    protected override JsonNode? SaveAdditionalState() => _program?.Save();
    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        _program = HumilityEffectProgram.Load(state);
        HumilityNativeEffects.Validate(_program);
        HasRewrites = true;
    }
}
