using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using MaidenSuccubus.Core.Control;

namespace MaidenSuccubus.Core.Intents;

public sealed class ControlIntent :
    AbstractIntent,
    IIntentExtraCornerAmountLabelSpecsProvider
{
    public int BlockRequired { get; }
    public ControlType ControlType { get; }
    public int EscapeRequired { get; }
    public string DisplayName { get; }
    public string EffectText { get; }

    public ControlIntent(
        int blockRequired,
        ControlType controlType,
        int escapeRequired,
        string displayName,
        string effectText)
    {
        BlockRequired = blockRequired;
        ControlType = controlType;
        EscapeRequired = escapeRequired;
        DisplayName = displayName;
        EffectText = effectText;
    }

    public override IntentType IntentType => IntentType.DebuffStrong;
    protected override string IntentPrefix => "MAIDENSUCCUBUS_CONTROL";
    protected override string SpritePath =>
        "atlases/intent_atlas.sprites/intent_card_debuff.tres";
    protected override LocString IntentLabelFormat =>
        new("intents", "FORMAT_DAMAGE_SINGLE");

    public override LocString GetIntentLabel(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString label = IntentLabelFormat;
        label.Add("Damage", BlockRequired);
        return label;
    }

    public IReadOnlyList<ExtraIconAmountLabelSpec>
        GetIntentExtraCornerAmountLabelSpecs() =>
        [
            ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.TopRight,
                ControlType.ShortLabel()),
            ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.BottomRight,
                EscapeRequired.ToString()),
        ];

    protected override LocString GetIntentDescription(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        description.Add("Name", DisplayName);
        description.Add("Effect", EffectText);
        return description;
    }
}

public sealed class InvasionIntent : SingleAttackIntent
{
    public string DisplayName { get; }
    public string EffectText { get; }

    public InvasionIntent(int damage, string displayName, string effectText)
        : base(damage)
    {
        DisplayName = displayName;
        EffectText = effectText;
    }

    protected override string IntentPrefix => "MAIDENSUCCUBUS_INVASION";
    protected override string SpritePath =>
        "atlases/intent_atlas.sprites/intent_attack.tres";

    protected override LocString GetIntentDescription(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        description.Add("Name", DisplayName);
        description.Add("Effect", EffectText);
        return description;
    }
}

public sealed class DesireGainIntent :
    AbstractIntent,
    IIntentExtraCornerAmountLabelSpecsProvider
{
    public int Amount { get; }
    public string DisplayName { get; }
    public string EffectText { get; }

    public DesireGainIntent(int amount, string displayName, string effectText)
    {
        Amount = amount;
        DisplayName = displayName;
        EffectText = effectText;
    }

    public override IntentType IntentType => IntentType.Debuff;
    protected override string IntentPrefix => "MAIDENSUCCUBUS_DESIRE";
    protected override string SpritePath =>
        "atlases/intent_atlas.sprites/intent_debuff.tres";

    public IReadOnlyList<ExtraIconAmountLabelSpec>
        GetIntentExtraCornerAmountLabelSpecs() =>
        [
            ExtraIconAmountLabelSpec.Plain(
                ExtraIconAmountLabelCorner.BottomRight,
                $"+{Amount}"),
        ];

    protected override LocString GetIntentDescription(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        description.Add("Name", DisplayName);
        description.Add("Effect", EffectText);
        return description;
    }
}
