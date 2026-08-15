using MegaCrit.Sts2.Core.Entities.Creatures;
using MaidenSuccubus.Powers.Scriptures;

namespace MaidenSuccubus.Core.Scriptures;

public sealed record ScriptureTriggered(
    ScripturePowerTemplate Power,
    Creature Owner);

public interface IScriptureTriggeredListener
{
    Task AfterScriptureTriggered(ScriptureTriggered scripture);
}
