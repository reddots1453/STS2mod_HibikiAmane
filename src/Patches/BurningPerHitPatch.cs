using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Powers;

namespace MaidenSuccubus.Patches;

/// <summary>
/// Wrap only AttackCommand's per-hit bulk damage call, not general damage.
/// Keeps native hit-count modifiers, target selection, choices and after-attack
/// hooks. The legacy wrapper also supports the six-argument 0.107.1 call.
/// </summary>
[HarmonyPatch]
internal static class BurningPerHitPatch
{
    private delegate Task<IEnumerable<DamageResult>> LegacyDamage(
        PlayerChoiceContext context, IEnumerable<Creature>? targets,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource);

    private static LegacyDamage? _legacyDamage;

    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute))
            .GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
            .GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new MissingMethodException("AttackCommand.Execute.MoveNext");

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.Select(code => new CodeInstruction(code)).ToList();
        var calls = codes.Where(code => code.opcode == OpCodes.Call
            && code.operand is MethodInfo method && method.DeclaringType == typeof(CreatureCmd)
            && method.Name == nameof(CreatureCmd.Damage)
            && method.GetParameters() is var args && args.Length is 6 or 7
            && args[1].ParameterType == typeof(IEnumerable<Creature>)
            && args[2].ParameterType == typeof(decimal)).ToArray();
        if (calls.Length != 1)
            throw new InvalidOperationException($"Expected one per-hit damage call, found {calls.Length}.");

        MethodInfo native = (MethodInfo)calls[0].operand;
        bool legacy = native.GetParameters().Length == 6;
        if (legacy) _legacyDamage = native.CreateDelegate<LegacyDamage>();
        calls[0].operand = AccessTools.Method(typeof(BurningPerHitPatch),
            legacy ? nameof(DamageHitLegacy) : nameof(DamageHit));
        MaidenSuccubusMod.Logger.Info($"[BurningPerHit] Installed native hit wrapper ({native.GetParameters().Length} args).");
        return codes;
    }

    private static async Task<IEnumerable<DamageResult>> DamageHit(
        PlayerChoiceContext context, IEnumerable<Creature>? targets,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer is { Side: CombatSide.Enemy, IsDead: false }
            && dealer.GetPower<BurningPower>() is { } burning)
        {
            await burning.ResolveBeforeHit(context);
            if (dealer.IsDead) return [];
            targets = targets?.Where(target => target.IsAlive).ToArray();
        }
        return await CreatureCmd.Damage(context, targets, amount, props, dealer, cardSource, cardPlay);
    }

    private static async Task<IEnumerable<DamageResult>> DamageHitLegacy(
        PlayerChoiceContext context, IEnumerable<Creature>? targets,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer is { Side: CombatSide.Enemy, IsDead: false }
            && dealer.GetPower<BurningPower>() is { } burning)
        {
            await burning.ResolveBeforeHit(context);
            if (dealer.IsDead) return [];
            targets = targets?.Where(target => target.IsAlive).ToArray();
        }
        return await (_legacyDamage ?? throw new InvalidOperationException("Missing legacy attack damage delegate."))
            (context, targets, amount, props, dealer, cardSource);
    }
}
