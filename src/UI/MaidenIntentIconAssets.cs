using Godot;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MaidenSuccubus.Core.Intents;

namespace MaidenSuccubus.UI;

public static class MaidenIntentIconAssets
{
    public static Texture2D? Get(AbstractIntent intent)
    {
        string? basename = intent switch
        {
            ControlIntent => "restraint",
            InvasionIntent => "violation",
            DesireGainIntent => "desire_attack",
            _ => null,
        };
        return basename == null
            ? null
            : RuntimeTextureAssets.Load($"powers/64x64/{basename}_power.png");
    }
}
