using Godot;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Intents;

namespace MaidenSuccubus.UI;

public static class MaidenIntentIconAssets
{
    public static Texture2D? Get(AbstractIntent intent)
    {
        string? fileName = intent switch
        {
            ControlIntent { ControlType: ControlType.Attack } =>
                "restraint_attack_intent.png",
            ControlIntent { ControlType: ControlType.Skill } =>
                "restraint_skill_intent.png",
            ControlIntent { ControlType: ControlType.Power } =>
                "restraint_power_intent.png",
            DesireGainIntent => "desire_gain_intent.png",
            TearClothingIntent => "tear_clothing_intent.png",
            // Invasion keeps its reviewed Power icon until a dedicated formal
            // intent icon is accepted.
            InvasionIntent => null,
            _ => null,
        };
        if (fileName != null)
        {
            return RuntimeTextureAssets.Load($"intents/64x64/{fileName}");
        }
        return intent is InvasionIntent
            ? RuntimeTextureAssets.Load("powers/64x64/violation_power.png")
            : null;
    }
}
