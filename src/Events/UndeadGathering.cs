using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Commands;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Pools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Events;

[RegisterSharedEvent]
public sealed class UndeadGathering : MSEventTemplate
{
    // Reuse a vanilla illustration until a dedicated event asset is supplied.
    public override string CustomInitialPortraitPath => "res://images/events/whispering_hollow.png";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HpLossVar(0)];
    public static int PrayerHpLoss(int maxHp) => Math.Max(0, maxHp / 10);

    public override bool IsAllowed(IRunState runState) =>
        runState.CurrentActIndex is 1 or 2
        && runState.Players.All(player => player.Character is MaidenSuccubusCharacter);

    public override void CalculateVars() =>
        DynamicVars.HpLoss.BaseValue = PrayerHpLoss(Owner?.Creature.MaxHp ?? 0);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool canEnchant = Owner!.Deck.Cards.Any(ModelDb.Enchantment<NecromancyEnchantment>().CanEnchant);
        EventOption[] options =
        [
            new EventOption(this, Listen, InitialOptionKey("LISTEN"), HoverTipFactory.FromCard<Soul>()),
            new EventOption(this, Pray, InitialOptionKey("PRAY"))
                .ThatDoesDamage(DynamicVars.HpLoss.IntValue),
            new EventOption(this, Corruption >= 3 && canEnchant ? Learn : null,
                InitialOptionKey(Corruption < 3 ? "LEARN_LOCKED" : canEnchant ? "LEARN" : "LEARN_NO_CARD"),
                HoverTipFactory.FromEnchantment<NecromancyEnchantment>()),
        ];
        foreach (EventOption option in options)
            DynamicVars.AddTo(option.Description);
        return options;
    }

    private async Task Listen()
    {
        for (int i = 0; i < 2; i++)
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(
                Owner!.RunState.CreateCard<Soul>(Owner), PileType.Deck), style: CardPreviewStyle.EventLayout);
        SetEventFinished(PageDescription("LISTEN"));
    }

    private async Task Pray()
    {
        // Use the same integer captured for the visible option, not a percentage
        // string or LoseMaxHp. Vanilla event damage is unblockable/unpowered.
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner!.Creature,
            DynamicVars.HpLoss.IntValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        if (Owner.Creature.IsDead) return;
        CardModel[] pool = ModelDb.CardPool<MSHolyCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
            .ToArray();
        IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckForTransformation(
            Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2));
        foreach (CardModel original in selected.ToArray())
            await CardCmd.Transform(original, Owner.RunState.CreateCard(
                Rng.NextItem(pool) ?? throw new InvalidOperationException("Holy transformation pool is empty."), Owner),
                CardPreviewStyle.EventLayout);
        CorruptionCmd.Modify((RunState)Owner.RunState, -1,
            new CorruptionChangeSource("undead_gathering.pray"));
        SetEventFinished(PageDescription("PRAY"));
    }

    private async Task Learn()
    {
        if (Corruption < 3) return;
        CardModel? selected = (await CardSelectCmd.FromDeckForEnchantment(
            Owner!, ModelDb.Enchantment<NecromancyEnchantment>(), 1,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1))).FirstOrDefault();
        if (selected == null) return;
        CardCmd.Enchant<NecromancyEnchantment>(selected, 1);
        EnchantmentVfxCmd.Preview(selected);
        await CardPileCmd.AddCurseToDeck<Normality>(Owner!);
        CorruptionCmd.Modify((RunState)Owner!.RunState, 1,
            new CorruptionChangeSource("undead_gathering.learn"));
        SetEventFinished(PageDescription("LEARN"));
    }
}
