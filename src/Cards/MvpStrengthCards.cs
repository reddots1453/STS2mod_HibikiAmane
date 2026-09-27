using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class DarkStorm : MSCorruptCard
{
    // Legacy save field only: pre-DS27 versions granted Glam on pickup.
    // Do not replay that behavior or erase enchantments from existing saves.
    [SavedProperty]
    public bool EnchantedOnPickup { get; set; }

    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<Glam>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move), new PowerVar<VulnerablePower>(2)];

    public DarkStorm() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);
        foreach (var enemy in CombatState.HittableEnemies.ToArray())
        {
            await PowerCmd.Apply<VulnerablePower>(
                context,
                enemy,
                DynamicVars["VulnerablePower"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        // This also runs on ownerless upgrade previews and during load. Keep
        // it instance-local and deterministic: no history, VFX or DeckVersion.
        using (ControlQuery.SuppressPresentation())
        {
            if (Enchantment != null) return;
            var glam = ModelDb.Enchantment<Glam>().ToMutable();
            EnchantInternal(glam, 1);
            glam.ModifyCard();
        }
    }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class BerserkerMask : MSCorruptCard
{
    public BerserkerMask() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<BerserkerMaskPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class CurseWedge : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<CurseWedgePower>(1)];
    public CurseWedge() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<CurseWedgePower>(context, Owner.Creature,
            DynamicVars["CurseWedgePower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() =>
        DynamicVars["CurseWedgePower"].UpgradeValueBy(1);
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class SharpForge : MSCorruptCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        HoverTipFactory.FromEnchantment<Sharp>(DynamicVars["Sharp"].IntValue);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move), new DynamicVar("Sharp", 2)];

    public SharpForge() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);

        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards
            .Where(card => card.Type == CardType.Attack
                && MaidenSuccubus.Enchantments.LayeredEnchantments.HasOpenSlot(card)
                && ModelDb.Enchantment<Sharp>().CanEnchant(card))
            .ToList()
            .StableShuffle(Owner.RunState.Rng.CombatCardSelection)
            .Take(2)
            .ToList();

        foreach (CardModel card in candidates)
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(card, DynamicVars["Sharp"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["Sharp"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(MSCorruptCardPool))]
public sealed class FinalSlash : MSCorruptCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move), new CardsVar(1)];

    public FinalSlash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(context);

        CardPile draw = PileType.Draw.GetPile(Owner);
        if (draw.Cards.Count == 0)
            return;

        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            context,
            draw,
            Owner,
            new CardSelectorPrefs(
                new LocString("card_selection", "MAIDEN_SUCCUBUS_TO_DISCARD_FROM_DRAW"),
                DynamicVars.Cards.IntValue));
        foreach (CardModel card in selected)
            await CardPileCmd.Add(card, PileType.Discard);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
