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

    public ControlIntent(int blockRequired, ControlType controlType, int escapeRequired)
    {
        BlockRequired = blockRequired;
        ControlType = controlType;
        EscapeRequired = escapeRequired;
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
}

public sealed class InvasionIntent : SingleAttackIntent
{
    public InvasionIntent(int damage) : base(damage) { }
    protected override string IntentPrefix => "MAIDENSUCCUBUS_INVASION";
    protected override string SpritePath =>
        "atlases/intent_atlas.sprites/intent_attack.tres";
}

public sealed class DesireGainIntent :
    AbstractIntent,
    IIntentExtraCornerAmountLabelSpecsProvider
{
    public int Amount { get; }

    public DesireGainIntent(int amount) => Amount = amount;

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
}
