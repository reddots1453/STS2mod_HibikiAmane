using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;
using STS2RitsuLib.Interop.AutoRegistration;
using CorruptionData = MaidenSuccubus.Data.Corruption;

namespace MaidenSuccubus.Acts;

// Character models are combat subscribers, not run subscribers. The act-entry
// reward must therefore live on a run listener and remain independent of the
// optional goddess-trial setting.
[RegisterSingleton]
public sealed class CorruptionActLifecycle : SingletonModel
{
    public override bool ShouldReceiveCombatHooks => false;
    private static readonly ConditionalWeakTable<RunState, CorruptionActLifecycle> Instances = new();
    private RunState _run = null!;

    public CorruptionActLifecycle() { }

    internal static IEnumerable<AbstractModel> Listeners(RunState run)
    {
        if (!run.Players.Any(player => player.IsActiveForHooks
            && player.Character is MaidenSuccubusCharacter)) return [];
        return [Instances.GetValue(run, state =>
        {
            var listener = (CorruptionActLifecycle)ModelDb.Singleton<CorruptionActLifecycle>().MutableClone();
            listener._run = state;
            return listener;
        })];
    }

    public override Task AfterActEntered()
    {
        if (_run.CurrentActIndex > 0
            && CorruptionData.Handle.Get(_run).VirginMark
            && CorruptionCmd.TryTriggerOnce(_run,
                $"SYS-CORRUPTION-VIRGIN-ACT-{_run.CurrentActIndex}"))
            CorruptionCmd.Modify(_run, -1, CorruptionChangeSource.VirginAct);
        return Task.CompletedTask;
    }
}
