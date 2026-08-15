using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MaidenSuccubus.Core.Control;

namespace MaidenSuccubus.Core.Intents;

public static class VanillaIntentAdapters
{
    public static void Register()
    {
        // First conservative adapter used to prove that an original state machine
        // can opt in without changing its model or affecting unregistered enemies.
        IntentAdapterRegistry.Register<TwigSlimeS>(new TwigSlimeSAdapter());
    }

    private sealed class TwigSlimeSAdapter :
        IControlIntentProvider,
        IInvasionIntentProvider,
        IDesireIntentProvider
    {
        public ControlIntentSpec GetControlIntent(MonsterModel monster) =>
            new(5, ControlType.Skill, 3);

        public InvasionIntentSpec GetInvasionIntent(MonsterModel monster) =>
            new(6);

        public DesireIntentSpec GetDesireIntent(MonsterModel monster) =>
            new(1, MaxUsesPerCombat: 1);
    }
}

