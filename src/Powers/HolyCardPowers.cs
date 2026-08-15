using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Scriptures;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class RestoreDexterityAtTurnEndPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }
        decimal amount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner,
            -amount,
            Owner,
            null);
    }
}

[RegisterPower]
public sealed class PreventNextDesireGainPower :
    ModPowerTemplate,
    ISecondaryResourceHookListener
{
    private bool _consumed;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public bool ShouldGainSecondaryResource(
        SecondaryResourceContext context,
        decimal amount)
    {
        if (_consumed
            || amount <= 0
            || context.Definition.Id != DesireResource.Id
            || context.Player.Creature != Owner)
        {
            return true;
        }
        _consumed = true;
        Flash();
        TaskHelper.RunSafely(PowerCmd.Remove(this));
        return false;
    }
}

[RegisterPower]
public sealed class InwardDisciplinePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner || !Owner.HasPower<WeakPower>())
        {
            return 1m;
        }
        return 1m + Amount / 100m;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner || !Owner.HasPower<FrailPower>())
        {
            return 1m;
        }
        return 1m + Amount / 100m;
    }
}

[RegisterPower]
public sealed class HolyRadiancePower : ModPowerTemplate
{
    private bool _resolving;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_resolving
            || power.Owner != Owner
            || amount <= 0
            || power.TypeForCurrentAmount != PowerType.Buff
            || power == this
            || Owner.CombatState == null)
        {
            return;
        }
        _resolving = true;
        try
        {
            Flash();
            foreach (Creature enemy in Owner.CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<WeakPower>(
                    choiceContext,
                    enemy,
                    Amount,
                    Owner,
                    null);
            }
        }
        finally
        {
            _resolving = false;
        }
    }
}

[RegisterPower]
public sealed class HolyResonancePower :
    ModPowerTemplate,
    IScriptureTriggeredListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task AfterScriptureTriggered(ScriptureTriggered scripture)
    {
        if (scripture.Owner != Owner)
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(
            Owner,
            Amount,
            ValueProp.Move,
            null);
    }
}
