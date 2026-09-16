using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Intents;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Core.Intents;

public readonly record struct EroticIntentVisual(
    Creature Target,
    EroticIntentKind Kind);

public static class EroticIntentVisualEvents
{
    public static event Action<EroticIntentVisual>? Triggered;

    internal static void Publish(Creature target, EroticIntentKind kind)
    {
        foreach (Action<EroticIntentVisual> handler in
            Triggered?.GetInvocationList().Cast<Action<EroticIntentVisual>>()
            ?? [])
        {
            try
            {
                handler(new EroticIntentVisual(target, kind));
            }
            catch (Exception ex)
            {
                MaidenSuccubusMod.Logger.Warn(
                    $"Erotic intent visual listener failed: {ex.Message}");
            }
        }
    }
}

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
    public override string GetAnimation(
        IEnumerable<Creature> targets,
        Creature owner) => IntentAnimData.cardDebuff;

    public override Texture2D? GetTexture(
        IEnumerable<Creature> targets,
        Creature owner) => MaidenIntentIconAssets.Get(this)
            ?? base.GetTexture(targets, owner);
    public IReadOnlyList<ExtraIconAmountLabelSpec>
        GetIntentExtraCornerAmountLabelSpecs() =>
        [
            new ExtraIconAmountLabelSpec(
                BlockRequired.ToString(),
                ExtraIconAmountLabelCorner.BottomLeft,
                default,
                new Color(0.30f, 0.78f, 0.95f),
                Colors.Black),
            new ExtraIconAmountLabelSpec(
                EscapeRequired.ToString(),
                ExtraIconAmountLabelCorner.BottomRight,
                default,
                new Color(0.96f, 0.42f, 0.42f),
                Colors.Black),
        ];

    protected override LocString GetIntentDescription(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        description.Add("Effect", EroticIntentDisplayText.Format(EffectText));
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

    public override string GetAnimation(
        IEnumerable<Creature> targets,
        Creature owner) => GetTotalDamage(targets, owner) switch
        {
            < 5 => IntentAnimData.attack1,
            < 10 => IntentAnimData.attack2,
            < 20 => IntentAnimData.attack3,
            < 40 => IntentAnimData.attack4,
            _ => IntentAnimData.attack5,
        };

    public override Texture2D GetTexture(
        IEnumerable<Creature> targets,
        Creature owner) => MaidenIntentIconAssets.Get(this)
            ?? base.GetTexture(targets, owner);

    protected override LocString GetIntentDescription(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        description.Add("Effect", EroticIntentDisplayText.Format(EffectText));
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

    public override string GetAnimation(
        IEnumerable<Creature> targets,
        Creature owner) => IntentAnimData.debuff;

    public override Texture2D? GetTexture(
        IEnumerable<Creature> targets,
        Creature owner) => MaidenIntentIconAssets.Get(this)
            ?? base.GetTexture(targets, owner);

    public IReadOnlyList<ExtraIconAmountLabelSpec>
        GetIntentExtraCornerAmountLabelSpecs() =>
        [
            // RitsuLib reserves BottomRight for the vanilla value label. Center
            // the desire amount inside this intent's own value band so it does
            // not lean into an adjacent supplemental intent icon.
            ExtraIconAmountLabelSpec.PlainCustom(
                $"+{Amount}",
                2f,
                40f,
                64f,
                63f),
        ];

    protected override LocString GetIntentDescription(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        description.Add("Effect", EroticIntentDisplayText.Format(EffectText));
        return description;
    }
}

public sealed class TearClothingIntent :
    AbstractIntent,
    IIntentExtraCornerAmountLabelSpecsProvider
{
    public override IntentType IntentType => IntentType.DebuffStrong;
    protected override string IntentPrefix => "MAIDENSUCCUBUS_TEAR_CLOTHING";
    protected override string SpritePath =>
        "atlases/intent_atlas.sprites/intent_card_debuff.tres";

    public override string GetAnimation(
        IEnumerable<Creature> targets,
        Creature owner) => IntentAnimData.cardDebuff;

    public IReadOnlyList<ExtraIconAmountLabelSpec>
        GetIntentExtraCornerAmountLabelSpecs() =>
        [ExtraIconAmountLabelSpec.PlainCustom("1", 2f, 40f, 64f, 63f)];
}

public sealed class ClothingHazardIntent : AbstractIntent
{
    private readonly string _cardName;

    public ClothingHazardIntent(string cardName) => _cardName = cardName;

    public override IntentType IntentType => IntentType.StatusCard;
    protected override LocString IntentLabelFormat =>
        new("intents", "FORMAT_STATUS_CARD_COUNT");
    protected override string IntentPrefix => "MAIDENSUCCUBUS_CLOTHING_HAZARD";
    protected override string SpritePath =>
        "atlases/intent_atlas.sprites/intent_status_card.tres";

    public override string GetAnimation(
        IEnumerable<Creature> targets,
        Creature owner) => IntentAnimData.debuff;

    public override LocString GetIntentLabel(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString label = IntentLabelFormat;
        label.Add("CardCount", 1);
        return label;
    }

    protected override LocString GetIntentDescription(
        IEnumerable<Creature> targets,
        Creature owner)
    {
        LocString description = base.GetIntentDescription(targets, owner);
        description.Add("CardName", _cardName);
        return description;
    }
}

internal static partial class EroticIntentDisplayText
{
    // Keep the effect wording from EROTIC_ATTACK_INTENTS.md intact. This helper
    // only adds the same semantic emphasis used by vanilla intent hover tips.
    private static readonly string[] HighlightedTerms =
    [
        "攻击牌", "技能牌", "能力牌", "挣脱值", "最大生命值",
        "格挡", "拘束", "欲望", "伤害", "力量", "敏捷",
        "虚弱", "易伤", "脆弱", "燃烧", "发情", "晕眩", "黏液",
    ];

    public static string Format(string effectText)
    {
        string formatted = effectText;
        foreach (string term in HighlightedTerms)
            formatted = formatted.Replace(term, $"[gold]{term}[/gold]", StringComparison.Ordinal);
        formatted = QuotedValueRegex().Replace(formatted, "[gold]“${Value}”[/gold]");
        return NumberRegex().Replace(formatted, "[blue]$&[/blue]");
    }

    [GeneratedRegex("“(?<Value>[^”]+)”", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedValueRegex();

    [GeneratedRegex(@"\d+", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();
}
