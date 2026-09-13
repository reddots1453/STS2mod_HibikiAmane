using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Intents;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Powers;

public abstract class EroticIntentThresholdPower : MaidenSuccubusPowerTemplate
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Single;

    internal static async Task ApplyAll(
        MonsterModel monster,
        EroticMonsterSpec spec)
    {
        var context = new ThrowingPlayerChoiceContext();
        if (spec.DesireThreshold > 0
            && !monster.Creature.HasPower<DesireIntentThresholdPower>())
        {
            await PowerCmd.Apply<DesireIntentThresholdPower>(
                context, monster.Creature, spec.DesireThreshold,
                monster.Creature, null, silent: true);
        }
        if (spec.ControlThreshold > 0
            && !monster.Creature.HasPower<ControlIntentThresholdPower>())
        {
            await PowerCmd.Apply<ControlIntentThresholdPower>(
                context, monster.Creature, spec.ControlThreshold,
                monster.Creature, null, silent: true);
        }
        if (spec.InvasionThreshold > 0
            && !monster.Creature.HasPower<InvasionIntentThresholdPower>())
        {
            await PowerCmd.Apply<InvasionIntentThresholdPower>(
                context, monster.Creature, spec.InvasionThreshold,
                monster.Creature, null, silent: true);
        }
    }
}

[RegisterPower]
public sealed class DesireIntentThresholdPower : EroticIntentThresholdPower;

[RegisterPower]
public sealed class ControlIntentThresholdPower : EroticIntentThresholdPower;

[RegisterPower]
public sealed class InvasionIntentThresholdPower : EroticIntentThresholdPower;
