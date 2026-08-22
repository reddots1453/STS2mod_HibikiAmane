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

        // At the start of each player turn, protection is refreshed to the
        // exact current armor amount. Multi-hit attacks can therefore consume
        // more than one layer during the same turn.
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
        VfxCmd.PlayOnCreatureCenter(Owner, "vfx/vfx_block");
        await CreatureCmd.TriggerAnim(Owner, "Hit", 0f);
        await PowerCmd.Decrement(this);

        MagicArmorPower? armor = Owner.GetPower<MagicArmorPower>();
        if (armor is not null)
        {
            await PowerCmd.Decrement(armor);
        }

        // Reaching zero armor ends transformation immediately.
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

    public static async Task Transform(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? source = null)
    {
        MagicArmorPower? previousArmor =
            player.Creature.GetPower<MagicArmorPower>();
        bool wasAlreadyTransformed = previousArmor is not null;

        await PowerCmd.Apply<MagicArmorPower>(
            choiceContext,
            player.Creature,
            3,
            player.Creature,
            source);

        // The initial transformation must be protective immediately. Gaining
        // more armor while already transformed does not replenish protection
        // generated for the current turn.
        if (wasAlreadyTransformed)
        {
            return;
        }

        int armorAmount = GetAmount(player);
        MagicArmorProtectionPower? protection =
            player.Creature.GetPower<MagicArmorProtectionPower>();
        if (protection is null)
        {
            await PowerCmd.Apply<MagicArmorProtectionPower>(
                choiceContext,
                player.Creature,
                armorAmount,
                player.Creature,
                source);
        }
        else if (protection.Amount != armorAmount)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                protection,
                armorAmount - protection.Amount,
                player.Creature,
                source);
        }
    }

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
