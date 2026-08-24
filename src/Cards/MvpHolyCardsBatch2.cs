using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Cards;

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class Consecration : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        ScriptureCardPreview.All();
    public Consecration() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<ConsecrationPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class HolyPunishment : MSHolyCard
{
    protected override IEnumerable<IHoverTip> CardSpecificHoverTips =>
        ScriptureCardPreview.All();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move)];
    public HolyPunishment() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);
        CardPile draw = PileType.Draw.GetPile(Owner);
        if (draw.Cards.Count == 0) return;
        CardModel? card = (await CardSelectCmd.FromCombatPile(context, draw, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
            candidate => candidate.IsTransformable)).FirstOrDefault();
        if (card != null) await ScriptureCmd.TransformToRandomScripture(card);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class SoulPurification : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    public SoulPurification() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<SoulPurificationPower>(context, Owner.Creature,
            DynamicVars.Cards.BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class ExternalPowerSkeleton : MSHolyCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];
    public ExternalPowerSkeleton() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        return DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .TargetingAllOpponents(CombatState).WithHitFx("vfx/vfx_attack_slash").Execute(context);
    }
    public override async Task BeforeFlush(PlayerChoiceContext context, Player player)
    {
        if (player == Owner && Pile?.Type == PileType.Discard)
            await CardCmd.AutoPlay(context, this, null);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class AutoReactionArmor : MSHolyCard
{
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];
    public AutoReactionArmor() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    public override async Task BeforeFlush(PlayerChoiceContext context, Player player)
    {
        if (player == Owner && Pile?.Type == PileType.Discard)
            await CardCmd.AutoPlay(context, this, null);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class MemoryImprint : MSHolyCard
{
    public MemoryImprint() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        PowerCmd.Apply<MemoryImprintPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

[RegisterCard(typeof(MSHolyCardPool))]
public sealed class TacticalAnalyzer : MSHolyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    public TacticalAnalyzer() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
        CardModel? card = (await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1),
            candidate => candidate.IsUpgradable, this)).FirstOrDefault();
        if (card != null)
        {
            CardCmd.Upgrade(card);
            CardCmd.ApplyKeyword(card, CardKeyword.Retain);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
