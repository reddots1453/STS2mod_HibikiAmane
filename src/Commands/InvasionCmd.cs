using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Core.Intents;

namespace MaidenSuccubus.Commands;

public static class InvasionCmd
{
    public static async Task Resolve(
        PlayerChoiceContext choiceContext,
        MonsterModel source,
        Player target,
        InvasionIntentSpec spec)
    {
        if (source.Creature.IsDead || target.Creature.IsDead) return;

        await DamageCmd.Attack(spec.Damage)
            .FromMonster(source)
            .Targeting(target.Creature)
            .WithNoAttackerAnim()
            .Execute(choiceContext);

        SemenCurse? curse = await CardPileCmd.AddCurseToDeck<SemenCurse>(target)
            as SemenCurse;
        if (curse != null)
        {
            curse.SourceMonsterId = source.Id.Entry;
        }

        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(source);
        runtime.HasInvaded = true;
        runtime.ControlDisabled = !spec.MayControlAgain;
        IntentMoveFactory.ForceStun(source);
    }
}

