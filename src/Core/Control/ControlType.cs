using MegaCrit.Sts2.Core.Entities.Cards;

namespace MaidenSuccubus.Core.Control;

public enum ControlType
{
    Attack,
    Skill,
    Power,
}

public static class ControlTypeExtensions
{
    public static bool Matches(this ControlType controlType, CardType cardType) =>
        controlType switch
        {
            ControlType.Attack => cardType == CardType.Attack,
            ControlType.Skill => cardType == CardType.Skill,
            ControlType.Power => cardType == CardType.Power,
            _ => false,
        };

    public static string LocalizedName(this ControlType controlType) =>
        controlType switch
        {
            ControlType.Attack => "攻击",
            ControlType.Skill => "技能",
            ControlType.Power => "能力",
            _ => controlType.ToString(),
        };

    public static string ShortLabel(this ControlType controlType) =>
        controlType switch
        {
            ControlType.Attack => "攻",
            ControlType.Skill => "技",
            ControlType.Power => "能",
            _ => "?",
        };
}
