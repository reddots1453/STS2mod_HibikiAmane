using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Relics;

// RELIC-CHAR-002: the original curse is prevented before entering the deck.
[RegisterRelic(typeof(MSRelicPool))]
public sealed class InternalCondom : MSRelicTemplate
{
    private int _storedCount;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override RelicAssetProfile AssetProfile => RelicIconAssets.For(CorruptVariation
        ? "character_InternalCondom_Corrupt" : "character_InternalCondom");

    [SavedProperty]
    public int StoredCount
    {
        get => _storedCount;
        set
        {
            AssertMutable();
            _storedCount = Math.Max(0, value);
            InvokeDisplayAmountChanged();
        }
    }

    internal bool CorruptVariation => IsMutable && Owner is
        { Character: MaidenSuccubusCharacter, RunState: RunState run }
        && CorruptionQuery.Get(run) >= 3;

    public override bool ShouldAddToDeck(CardModel card) =>
        !IsMutable || HasBeenRemovedFromState || Owner?.Character is not MaidenSuccubusCharacter
        || !ReferenceEquals(card.Owner, Owner) || card is not MSInvasionCurseTemplate;

    public override async Task AfterAddToDeckPrevented(CardModel card)
    {
        if (card is not MSInvasionCurseTemplate || !ReferenceEquals(card.Owner, Owner)) return;
        StoredCount++;
        Flash();
        if (CorruptVariation) await CreatureCmd.GainMaxHp(Owner.Creature, 3);
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [new HoverTip(new LocString("relics", "MAIDEN_SUCCUBUS_RELIC_INTERNAL_CONDOM.title"),
            $"已存入{StoredCount}张精液类诅咒牌。")];
}
