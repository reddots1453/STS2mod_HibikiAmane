using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.ContentTemplates;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace MaidenSuccubus.Events;

[RegisterSharedEvent]
public sealed class SuspiciousShop : MSEventTemplate
{
    public override string CustomInitialPortraitPath => "res://images/events/whispering_hollow.png";

    public override bool IsAllowed(IRunState runState) =>
        runState.Players.All(player => player.Character is MaidenSuccubusCharacter);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new(this, Corruption >= 2 ? Work : null,
            InitialOptionKey(Corruption >= 2 ? "WORK" : "WORK_LOCKED")),
        new(this, Browse, InitialOptionKey("BROWSE")),
    ];

    internal static RelicModel RelicAt(int index) => index switch
    {
        0 => ModelDb.Relic<Blindfold>(),
        1 => ModelDb.Relic<Vibrator>(),
        2 => ModelDb.Relic<Milker>(),
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    private async Task Work()
    {
        if (Corruption < 2) return;
        CorruptionCmd.Modify((RunState)Owner!.RunState, 1,
            new CorruptionChangeSource("suspicious_shop.work"));
        await Data.Desire.Modify(Owner!, 2);
        await PlayerCmd.GainGold(Rng.NextInt(175, 201), Owner!);
        SetEventFinished(PageDescription("WORK"));
    }

    private async Task Browse()
    {
        await RelicCmd.Obtain(RelicAt(Rng.NextInt(3)).ToMutable(), Owner!);
        SetEventFinished(PageDescription("BROWSE"));
    }
}
