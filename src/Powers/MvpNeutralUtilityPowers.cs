using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class MentalUnityPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override Task BeforeAttack(AttackCommand command)
    {
        Creature? beneficiary = Applier;
        if (command.Attacker != Owner || beneficiary is null || beneficiary.IsDead)
            return Task.CompletedTask;
        return CreatureCmd.GainBlock(beneficiary, Amount, ValueProp.Unpowered, null);
    }
}

[RegisterPower]
public sealed class CounterDefensePower : ModPowerTemplate
{
    private Creature? _activeAttacker;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker?.Side != Owner.Side)
            _activeAttacker = command.Attacker;
        return Task.CompletedTask;
    }
    public override async Task AfterDamageReceived(
        PlayerChoiceContext context, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer is null || dealer != _activeAttacker)
            return;
        decimal damage = Amount;
        _activeAttacker = null;
        await PowerCmd.Remove(this);
        if (dealer.IsAlive)
            await CreatureCmd.Damage(context, dealer, damage, ValueProp.Unpowered | ValueProp.Move, Owner);
    }
    public override Task AfterAttack(PlayerChoiceContext context, AttackCommand command)
    {
        if (command.Attacker == _activeAttacker)
            _activeAttacker = null;
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class UltimateFlarePower : ModPowerTemplate
{
    public decimal Damage { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.CombatState is null) return;
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(),
            Owner.CombatState.GetOpponentsOf(Owner).Where(c => c.IsAlive),
            Damage, ValueProp.Move, Owner);
        await PowerCmd.Remove(this);
    }
}

[RegisterPower]
public sealed class MagicIndexPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterCardDrawnEarly(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature == Owner && card.Enchantment != null)
            await CardPileCmd.Draw(context, (int)Amount, card.Owner);
    }
}

[RegisterPower]
public sealed class ResonanceArmorPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || cardPlay.Card.Enchantment == null)
            return Task.CompletedTask;
        return CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, cardPlay);
    }
}

[RegisterPower]
public sealed class LullabyPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task BeforeFlush(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner) return;
        await CardPileCmd.AddGeneratedCardToCombat(
            player.RunState.CreateCard<DrowsyStatus>(player), PileType.Hand, player);
        int handSize = PileType.Hand.GetPile(player).Cards.Count;
        await CreatureCmd.GainBlock(Owner, handSize * Amount, ValueProp.Unpowered, null);
    }
}

[RegisterPower]
public sealed class TenaciousResistancePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
