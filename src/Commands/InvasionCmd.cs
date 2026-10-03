using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Data;
using MaidenSuccubus.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace MaidenSuccubus.Commands;

public static class InvasionCmd
{
    public static bool IsRegisteredCurseName(string name) => name is
        "精液"
        or "腥臭黏液" or "腥臭粘液"
        or "催情液" or "孢子胶质" or "麻痹黏液" or "腐蚀黏液"
        or "虫卵" or "寄生卵" or "墨色体液" or "灼热体液"
        or "灵质残液" or "魔力残液" or "藤蔓种子" or "淤泥精液"
        or "深海黏液" or "实验药液" or "王家精华";

    public static async Task<bool> Resolve(
        PlayerChoiceContext choiceContext,
        MonsterModel source,
        Player target,
        InvasionIntentSpec spec,
        bool deferCompletion = false)
    {
        if (source.Creature.IsDead || target.Creature.IsDead
            || IntentAdapterRegistry.GetRuntime(source).ControlDisabled) return false;

        UI.CombatTextFeedback.Notify("invasion_intent_received", target.Creature, source.Creature, amount: spec.Damage);
        await DamageCmd.Attack(spec.Damage)
            .FromMonster(source)
            .WithNoAttackerAnim()
            .Execute(choiceContext);

        // Chastity Defense prevents natural invasion selection. If an invasion
        // was already selected before the protection appeared, its attack still
        // resolves, but the curse and recovery intent are cancelled.
        ChastityDefensePower? protection =
            target.Creature.GetPower<ChastityDefensePower>();
        if (protection != null)
        {
            protection.NotifyInvasionBlocked();
            return false;
        }

        MSInvasionCurseTemplate? curse = await AddCurse(target, spec.CurseName);
        if (curse == null)
        {
            return false;
        }
        curse.SourceMonsterId = source.Id.Entry;

        // Commit the combat restriction and release this source's bindings now,
        // not after an optional CG or extra effects that can await or fail.
        await OnCurseInserted(choiceContext, source, target);

        if (target.RunState is RunState runState)
        {
            bool firstInvasion = Corruption.Handle.Get(runState).VirginMark;
            if (firstInvasion)
            {
                Corruption.Handle.Modify(runState, state => state.VirginMark = false);
                CorruptionCmd.Modify(runState, 1, CorruptionChangeSource.FirstInvasion);
            }
        }

        if (!deferCompletion)
        {
            Complete(source);
        }
        return true;
    }

    private static async Task OnCurseInserted(
        PlayerChoiceContext context, MonsterModel source, Player target)
    {
        IntentAdapterRegistry.GetRuntime(source).ControlDisabled = true;
        int released = 0;
        IEnumerable<Player> players = source.CombatState?.Players ?? [target];
        foreach (Player player in players.Append(target).Distinct())
        {
            released += player.Creature.Powers.OfType<ControlPower>()
                .Count(power => ReferenceEquals(power.Applier, source.Creature));
            await ControlCmd.Release(context, player.Creature, source.Creature);
        }
        MaidenSuccubusMod.Logger.Info(
            $"[Invasion] Curse inserted source={source.Id.Entry}; control/invasion disabled; releasedBindings={released}");
    }

    public static void Complete(MonsterModel source)
    {
        IntentRuntimeState runtime = IntentAdapterRegistry.GetRuntime(source);
        runtime.ControlDisabled = true;
        IntentMoveFactory.ForceStun(source);
    }

    private static async Task<MSInvasionCurseTemplate?> AddCurse(
        Player target,
        string name) => name switch
        {
            "精液" => await Add<SemenCurse>(target),
            "腥臭黏液" or "腥臭粘液" =>
                await Add<FoulSlimeCurse>(target),
            "催情液" => await Add<AphrodisiacCurse>(target),
            "孢子胶质" => await Add<SporeMucusCurse>(target),
            "麻痹黏液" => await Add<ParalyticSlimeCurse>(target),
            "腐蚀黏液" => await Add<CorrosiveSlimeCurse>(target),
            "虫卵" => await Add<InsectEggCurse>(target),
            "寄生卵" => await Add<ParasiticEggCurse>(target),
            "墨色体液" => await Add<InkFluidCurse>(target),
            "灼热体液" => await Add<ScorchingFluidCurse>(target),
            "灵质残液" => await Add<EctoplasmResidueCurse>(target),
            "魔力残液" => await Add<MagicResidueCurse>(target),
            "藤蔓种子" => await Add<VineSeedCurse>(target),
            "淤泥精液" => await Add<SludgeSemenCurse>(target),
            "深海黏液" => await Add<DeepSeaSlimeCurse>(target),
            "实验药液" => await Add<ExperimentalLiquidCurse>(target),
            "王家精华" => await Add<RoyalEssenceCurse>(target),
            _ => throw new InvalidDataException(
                $"Unregistered invasion curse name: {name}"),
        };

    private static async Task<MSInvasionCurseTemplate?> Add<T>(Player target)
        where T : MSInvasionCurseTemplate =>
        await AddOrStore<T>(target);

    private static async Task<MSInvasionCurseTemplate?> AddOrStore<T>(Player target)
        where T : MSInvasionCurseTemplate
    {
        int before = target.Relics.OfType<InternalCondom>().Sum(relic => relic.StoredCount);
        MSInvasionCurseTemplate? added = await CardPileCmd.AddCurseToDeck<T>(target)
            as MSInvasionCurseTemplate;
        if (added != null) return added;
        // Hook.ShouldAddToDeck returned false because the relic stored the
        // curse. This is still a successful invasion, unlike other prevention.
        return target.Relics.OfType<InternalCondom>().Sum(relic => relic.StoredCount) > before
            ? target.RunState.CreateCard<T>(target) : null;
    }
}
