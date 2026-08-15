using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

[RegisterPower]
public sealed class MagicArmorPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player.Creature != Owner || Amount <= 0)
        {
            return;
        }

        // Protection is snapshotted only at turn start. Gaining magic armor
        // later in the turn deliberately does not add protection charges.
        MagicArmorProtectionPower? current =
            Owner.GetPower<MagicArmorProtectionPower>();
        if (current is null)
        {
            await PowerCmd.Apply<MagicArmorProtectionPower>(
                choiceContext, Owner, Amount, Owner, null);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                current,
                Amount - current.Amount,
                Owner,
                null);
        }
    }
}

[RegisterPower]
public sealed class MagicArmorProtectionPower : ModPowerTemplate
{
    private bool _preventedThisHit;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _preventedThisHit = target == Owner
            && amount > 0
            && Amount > 0
            && dealer?.Side == CombatSide.Enemy;
        return _preventedThisHit ? 0 : amount;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (!_preventedThisHit)
        {
            return;
        }

        _preventedThisHit = false;
        Flash();
        await PowerCmd.Decrement(this);

        MagicArmorPower? armor = Owner.GetPower<MagicArmorPower>();
        if (armor is not null)
        {
            await PowerCmd.Decrement(armor);
        }

        // Reaching zero armor ends transformation immediately and invalidates
        // all unused charges from the start-of-turn snapshot.
        if (Owner.GetPower<MagicArmorPower>() is null)
        {
            await PowerCmd.Remove<MagicArmorProtectionPower>(Owner);
        }
    }
}

public static class MagicArmorCmd
{
    public static bool IsTransformed(Player player) =>
        player.Creature.GetPower<MagicArmorPower>() is { Amount: > 0 };

    public static int GetAmount(Player player) =>
        player.Creature.GetPower<MagicArmorPower>()?.Amount ?? 0;

    public static Task Transform(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? source = null) =>
        PowerCmd.Apply<MagicArmorPower>(
            choiceContext,
            player.Creature,
            3,
            player.Creature,
            source);

    public static async Task<bool> TrySpend(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount,
        CardModel? source = null)
    {
        MagicArmorPower? armor = player.Creature.GetPower<MagicArmorPower>();
        if (armor is null || amount <= 0 || armor.Amount < amount)
        {
            return false;
        }

        await PowerCmd.ModifyAmount(
            choiceContext, armor, -amount, player.Creature, source);
        if (player.Creature.GetPower<MagicArmorPower>() is null)
        {
            await PowerCmd.Remove<MagicArmorProtectionPower>(player.Creature);
        }
        return true;
    }
}
