using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Intents;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace MaidenSuccubus.Core.Intents;

public enum EroticBlockRecipient { Self, OtherEnemies, Summons }

/// <summary>Native defend sprite with a numeric preview of the executed block component.</summary>
public sealed class EroticBlockIntent(int amount, EroticBlockRecipient recipient) : DefendIntent
{
    public int Amount { get; } = amount;
    public EroticBlockRecipient Recipient { get; } = recipient;
    protected override string IntentPrefix => "MAIDENSUCCUBUS_BLOCK";
    public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) => IntentAnimData.defend;

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var label = new LocString("intents", "MAIDENSUCCUBUS_BLOCK.format");
        label.Add("BlockAmount", GetAmountText(owner));
        return label;
    }

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        string subject = Recipient switch
        {
            EroticBlockRecipient.OtherEnemies => "所有其他敌人将各获得",
            EroticBlockRecipient.Summons => "召唤物将各获得",
            _ => "这名敌人将获得",
        };
        description.Add("Effect", $"{subject}[blue]{GetAmountText(owner)}[/blue]点[gold]格挡[/gold]。");
        return description;
    }

    private string GetAmountText(Creature owner)
    {
        IEnumerable<Creature> recipients = Recipient == EroticBlockRecipient.Self ? [owner] :
            owner.CombatState?.Enemies.Where(enemy => enemy.IsAlive && !ReferenceEquals(enemy, owner)
                && (Recipient != EroticBlockRecipient.Summons || enemy.IsSecondaryEnemy)) ?? [];
        int[] values = recipients.Select(target => target.CombatState == null ? Amount :
            (int)Math.Max(0m, Hook.ModifyBlock(target.CombatState, target, Amount, ValueProp.Move, null, null, out _)))
            .DefaultIfEmpty(Amount).ToArray();
        int min = values.Min(), max = values.Max();
        return min == max ? min.ToString(CultureInfo.InvariantCulture) : $"{min}–{max}";
    }
}
