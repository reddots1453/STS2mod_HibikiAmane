using MaidenSuccubus.Core.Cards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

internal sealed record Extraction(string Card, HumilityEffectProgram? Program, string? Error, int Line);

/// <summary>Static call slicing only: never invokes card methods or removed effects.</summary>
internal static class CallExtractor
{
    internal static IReadOnlyList<Extraction> Extract(string source, IReadOnlyList<MethodDeclarationSyntax>? methods = null)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var syntaxErrors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (syntaxErrors.Length != 0)
            return [new("<source>", null, string.Join("; ", syntaxErrors.Select(d => d.ToString())), 1)];
        List<Extraction> results = [];
        foreach (var type in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var method = type.Members.OfType<MethodDeclarationSyntax>().SingleOrDefault(m => m.Identifier.Text == "OnPlay");
            if (method == null) continue;
            string ns = string.Join(".", type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()));
            string name = string.IsNullOrEmpty(ns) ? type.Identifier.Text : ns + "." + type.Identifier.Text;
            int line = tree.GetLineSpan(method.Span).StartLinePosition.Line + 1;
            try
            {
                var expanded = (MethodDeclarationSyntax)new HelperExpansion(methods ?? HelperExpansion.Index([source]), method).Visit(method)!;
                // Expression-bodied OnPlay must also be exposed to statement expansion.
                if (expanded.ExpressionBody != null)
                    expanded = expanded.WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(expanded.ExpressionBody.Expression))).WithExpressionBody(null);
                expanded = (MethodDeclarationSyntax)new HelperExpansion(methods ?? HelperExpansion.Index([source]), method).Visit(expanded)!;
                results.Add(new(name, Slice(expanded), null, line));
            }
            catch (NotSupportedException error) { results.Add(new(name, null, error.Message, line)); }
        }
        return results;
    }

    private static bool IsCall(InvocationExpressionSyntax call, string owner, string name) =>
        call.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.Text == name
        && member.Expression.ToString().Split('.').Last() == owner;

    private static string MethodName(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
        IdentifierNameSyntax name => name.Identifier.Text,
        _ => call.Expression.ToString(),
    };

    private static bool IsEffect(InvocationExpressionSyntax call) =>
        IsCall(call, "DamageCmd", "Attack") || IsCall(call, "CreatureCmd", "GainBlock");

    private static HumilityEffectProgram Slice(MethodDeclarationSyntax method)
    {
        var all = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var calls = all.Where(IsEffect).OrderBy(c => c.SpanStart).ToArray();
        List<(int Position, HumilityEffect Effect)> effects = [];
        HashSet<InvocationExpressionSyntax> retainedChains = [];
        foreach (var call in calls)
        {
            if (call.Ancestors().TakeWhile(n => n != method).Any(n => n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                throw Unsupported(call, "damage/block inside callback requires explicit extraction support");
            HumilityValue amount;
            HumilityValue repeats = HumilityValue.Number(1);
            HumilityTarget target;
            if (IsCall(call, "CreatureCmd", "GainBlock"))
            {
                if (call.ArgumentList.Arguments.Count < 2) throw Unsupported(call, "GainBlock arguments missing");
                target = Target(call.ArgumentList.Arguments[0].Expression);
                amount = Value(call.ArgumentList.Arguments[1].Expression, method, call.SpanStart, []);
                effects.Add((call.SpanStart, new(HumilityEffectKind.Block, target, amount, repeats)));
                continue;
            }
            amount = Value(call.ArgumentList.Arguments.Single().Expression, method, call.SpanStart, []);
            HumilityTarget? attackTarget = null;
            InvocationExpressionSyntax? targetCall = null;
            HumilityAttackSource source = HumilityAttackSource.Card;
            bool sourceSet = false, countSet = false;
            int executePosition = -1;
            void ReadChain(InvocationExpressionSyntax first, bool includeFirst)
            {
                SyntaxNode current = first;
                var chain = new List<InvocationExpressionSyntax>();
                if (includeFirst) chain.Add(first);
                while (current.Parent is MemberAccessExpressionSyntax link && link.Expression == current
                    && link.Parent is InvocationExpressionSyntax nextLink)
                { chain.Add(nextLink); current = nextLink; }
                foreach (var next in chain)
                {
                    if (executePosition >= 0) throw Unsupported(next, "attack builder configured or executed after Execute");
                    retainedChains.Add(next);
                    string name = MethodName(next);
                    HumilityTarget? nextTarget = name switch
                    {
                        "Targeting" => Target(next.ArgumentList.Arguments.Single().Expression),
                        "TargetingAllOpponents" => HumilityTarget.AllEnemies,
                        "TargetingRandomOpponents" => HumilityTarget.RandomEnemy,
                        _ => null,
                    };
                    if (nextTarget.HasValue)
                    {
                        if (attackTarget.HasValue && attackTarget != nextTarget)
                        {
                            if (attackTarget is not (HumilityTarget.Selected or HumilityTarget.AllEnemies or HumilityTarget.CurrentCardTarget)
                                || nextTarget is not (HumilityTarget.Selected or HumilityTarget.AllEnemies)
                                || targetCall == null || !ExclusiveBranches(targetCall, next))
                                throw Unsupported(next, "conflicting attack targets");
                            attackTarget = HumilityTarget.CurrentCardTarget;
                        }
                        else attackTarget = nextTarget;
                        targetCall = next;
                        continue;
                    }
                    switch (name)
                    {
                    case "WithHitCount":
                        var count = Value(next.ArgumentList.Arguments.Single().Expression, method, next.SpanStart, []);
                        if (countSet && repeats != count) throw Unsupported(next, "conflicting attack counts");
                        repeats = count; countSet = true; break;
                    case "Execute":
                        if (executePosition >= 0) throw Unsupported(next, "multiple executions of one attack builder");
                        executePosition = next.SpanStart; break;
                    case "FromCard":
                        if (sourceSet && source != HumilityAttackSource.Card) throw Unsupported(next, "conflicting attack sources");
                        source = HumilityAttackSource.Card; sourceSet = true; break;
                    case "FromOsty":
                        if (next.ArgumentList.Arguments.Count < 2
                            || Unwrap(next.ArgumentList.Arguments[0].Expression).ToString() is not ("Owner.Osty" or "base.Owner.Osty" or "this.Owner.Osty"))
                            throw Unsupported(next, "unsupported Osty source expression");
                        if (sourceSet && source != HumilityAttackSource.Osty) throw Unsupported(next, "conflicting attack sources");
                        source = HumilityAttackSource.Osty; sourceSet = true;
                        break;
                    case "BeforeDamage":
                        if (next.ArgumentList.Arguments.Count != 1 || !RemovedCalls.VisualCallback(next.ArgumentList.Arguments[0].Expression, method))
                            throw Unsupported(next, "nonvisual BeforeDamage callback");
                        break;
                    case "WithHitFx": case "WithAttackerAnim": case "WithHitVfxNode":
                    case "WithHitVfxSpawnedAtBase": case "SpawningHitVfxOnEachCreature": case "WithNoAttackerAnim": case "WithAttackerFx": case "OnlyPlayAnimOnce":
                        break; // Visual callbacks are not executed by the rewritten program.
                    default: throw Unsupported(next, "unsupported attack-chain method " + name);
                    }
                }
            }
            ReadChain(call, false);
            if (executePosition < 0)
            {
                var declaration = call.Ancestors().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
                if (declaration == null) throw Unsupported(call, "attack builder must be a direct chain or unique local");
                string localName = declaration.Identifier.Text;
                if (method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Count(v => v.Identifier.Text == localName) != 1)
                    throw Unsupported(declaration, "ambiguous attack builder local");
                var continuations = all.Where(c => c.SpanStart > call.SpanStart && c.Expression is MemberAccessExpressionSyntax m
                    && m.Expression is IdentifierNameSyntax id && id.Identifier.Text == localName).OrderBy(c => c.SpanStart).ToArray();
                foreach (var continuation in continuations)
                {
                    if (continuation.Ancestors().TakeWhile(n => n != method).Any(n => n is AnonymousFunctionExpressionSyntax))
                        throw Unsupported(continuation, "deferred attack builder use");
                    ReadChain(continuation, true);
                }
                foreach (var assignment in method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(a => a.Left.ToString() == localName))
                    if (!assignment.Right.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(retainedChains.Contains))
                        throw Unsupported(assignment, "attack builder reassigned to unknown value");
            }
            if (executePosition < 0 || attackTarget == null) throw Unsupported(call, "attack chain must include explicit target and Execute");
            effects.Add((executePosition, new(HumilityEffectKind.Damage, attackTarget.Value, amount, repeats, source)));
        }
        // Unknown helpers may contain a hidden attack. Do not silently label them an
        // empty program, or keep only a direct attack while dropping a helper's damage.
        IEnumerable<ExpressionSyntax> statements = method.DescendantNodes().OfType<ExpressionStatementSyntax>().Select(s => s.Expression);
        statements = statements.Concat(method.DescendantNodes().OfType<ReturnStatementSyntax>().Where(s => s.Expression != null).Select(s => s.Expression!));
        if (method.ExpressionBody != null) statements = statements.Append(method.ExpressionBody.Expression);
        foreach (var statement in statements)
        {
            if (statement.Ancestors().TakeWhile(n => n != method).Any(n => n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)) continue;
            var expression = statement is AwaitExpressionSyntax awaitExpression ? awaitExpression.Expression : statement;
            if (expression is not InvocationExpressionSyntax invocation || invocation.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(IsEffect)) continue;
            if (retainedChains.Contains(invocation) || RemovedCalls.Statement(invocation, method)) continue;
            if (invocation.Expression is MemberAccessExpressionSyntax member
                && member.Expression.ToString() is "CardPileCmd" or "CardCmd" or "PowerCmd" or "PlayerCmd" or "OrbCmd" or "CreatureCmd" or "ArgumentNullException" or "Cmd" or "SfxCmd" or "VfxCmd" or "CombatEnchantmentCmd") continue;
            if (IsCall(invocation, "ForgeCmd", "Forge") || IsCall(invocation, "OstyCmd", "Summon")) continue;
            throw Unsupported(invocation, "helper statement may hide damage/block: " + MethodName(invocation));
        }
        return new(effects.OrderBy(e => e.Position).Select(e => e.Effect));
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax p => Unwrap(p.Expression),
        CastExpressionSyntax c => Unwrap(c.Expression),
        PostfixUnaryExpressionSyntax p when p.IsKind(SyntaxKind.SuppressNullableWarningExpression) => Unwrap(p.Operand),
        _ => expression,
    };

    private static bool ExclusiveBranches(SyntaxNode first, SyntaxNode second) => first.Ancestors().OfType<IfStatementSyntax>()
        .Any(branch => branch.Else != null &&
            (branch.Statement.Span.Contains(first.Span) && branch.Else.Statement.Span.Contains(second.Span)
             || branch.Else.Statement.Span.Contains(first.Span) && branch.Statement.Span.Contains(second.Span)));

    private static HumilityTarget Target(ExpressionSyntax expression)
    {
        string text = Unwrap(expression).ToString();
        if (text is "Owner.Creature" or "base.Owner.Creature" or "this.Owner.Creature") return HumilityTarget.Self;
        if (text is "play.Target" or "cardPlay.Target") return HumilityTarget.Selected;
        throw Unsupported(expression, "unsupported target expression");
    }

    private static bool DynamicVars(ExpressionSyntax expression) => Unwrap(expression).ToString() is "DynamicVars" or "base.DynamicVars" or "this.DynamicVars";

    private static HumilityValue Value(ExpressionSyntax expression, MethodDeclarationSyntax method, int before, HashSet<string> resolving)
    {
        expression = Unwrap(expression);
        // Upgrade selection is part of the numeric argument, not a surrounding
        // trigger condition. Bind it live so upgrading the rewritten card still works.
        if (expression is ConditionalExpressionSyntax upgrade
            && Unwrap(upgrade.Condition).ToString() is "IsUpgraded" or "base.IsUpgraded" or "this.IsUpgraded")
        {
            HumilityValue yes = Value(upgrade.WhenTrue, method, before, resolving);
            HumilityValue no = Value(upgrade.WhenFalse, method, before, resolving);
            return HumilityValue.Binary(HumilityValueKind.Add, no,
                HumilityValue.Binary(HumilityValueKind.Multiply, HumilityValue.Named("$upgraded"),
                    HumilityValue.Binary(HumilityValueKind.Add, yes,
                        HumilityValue.Binary(HumilityValueKind.Multiply, HumilityValue.Number(-1), no))));
        }
        if (expression is LiteralExpressionSyntax literal && literal.Token.Value is IConvertible number
            && literal.IsKind(SyntaxKind.NumericLiteralExpression))
            return HumilityValue.Number(Convert.ToDecimal(number, System.Globalization.CultureInfo.InvariantCulture));
        if (expression is PrefixUnaryExpressionSyntax minus && minus.IsKind(SyntaxKind.UnaryMinusExpression))
            return HumilityValue.Binary(HumilityValueKind.Multiply, HumilityValue.Number(-1), Value(minus.Operand, method, before, resolving));
        if (expression is BinaryExpressionSyntax binary && binary.Kind() is SyntaxKind.AddExpression or SyntaxKind.MultiplyExpression)
            return HumilityValue.Binary(binary.IsKind(SyntaxKind.AddExpression) ? HumilityValueKind.Add : HumilityValueKind.Multiply,
                Value(binary.Left, method, before, resolving), Value(binary.Right, method, before, resolving));
        if (expression is MemberAccessExpressionSyntax member)
        {
            if (DynamicVars(member.Expression)) return HumilityValue.Named(member.Name.Identifier.Text);
            if (member.Name.Identifier.Text is "BaseValue" or "IntValue") return Value(member.Expression, method, before, resolving);
            if (expression.ToString() is "play.Resources.EnergyValue" or "cardPlay.Resources.EnergyValue") return HumilityValue.X(HumilityValueKind.EnergyX);
            if (expression.ToString() is "Owner.Creature.Block" or "base.Owner.Creature.Block") return HumilityValue.Named("$block");
            if (expression.ToString() is "Owner.Creature.CurrentHp" or "base.Owner.Creature.CurrentHp") return HumilityValue.Named("$hp");
        }
        if (expression is ElementAccessExpressionSyntax element && DynamicVars(element.Expression)
            && element.ArgumentList.Arguments.Single().Expression is LiteralExpressionSyntax key && key.Token.Value is string variableName)
            return HumilityValue.Named(variableName);
        if (expression is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax access)
        {
            if (access.Name.Identifier.Text == "Value" && invocation.ArgumentList.Arguments.Count == 1
                && invocation.ArgumentList.Arguments[0].Expression.ToString() == "DesireResource.Id"
                && access.Expression is InvocationExpressionSyntax ledger && ledger.ArgumentList.Arguments.Count == 0
                && ledger.Expression is MemberAccessExpressionSyntax ledgerAccess
                && ledgerAccess.Name.Identifier.Text == "SecondaryResources"
                && ledgerAccess.Expression.ToString() is "play" or "cardPlay")
                return HumilityValue.X(HumilityValueKind.SecondaryX);
            if (access.Name.Identifier.Text == "Calculate") return Value(access.Expression, method, before, resolving);
            if (access.Expression.ToString() is "base" or "this" && invocation.ArgumentList.Arguments.Count == 0
                && access.Name.Identifier.Text is "ResolveEnergyXValue" or "ResolveStarXValue")
                return HumilityValue.X(access.Name.Identifier.Text == "ResolveEnergyXValue" ? HumilityValueKind.EnergyX : HumilityValueKind.StarX);
            if (access.Expression.ToString() == "Math" && access.Name.Identifier.Text is "Min" or "Max" && invocation.ArgumentList.Arguments.Count == 2)
                return HumilityValue.Binary(access.Name.Identifier.Text == "Min" ? HumilityValueKind.Min : HumilityValueKind.Max,
                    Value(invocation.ArgumentList.Arguments[0].Expression, method, before, resolving),
                    Value(invocation.ArgumentList.Arguments[1].Expression, method, before, resolving));
        }
        if (expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax xMethod } xCall
            && xCall.ArgumentList.Arguments.Count == 0 && xMethod.Identifier.Text is "ResolveEnergyXValue" or "ResolveStarXValue")
            return HumilityValue.X(xMethod.Identifier.Text == "ResolveEnergyXValue" ? HumilityValueKind.EnergyX : HumilityValueKind.StarX);
        if (expression is IdentifierNameSyntax local)
        {
            string name = local.Identifier.Text;
            if (!resolving.Add(name)) throw Unsupported(expression, "cyclic local value");
            try
            {
                var declarations = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                    .Where(v => v.Identifier.Text == name && v.SpanStart < before).ToArray();
                bool assigned = method.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                    .Any(a => a.Left.ToString() == name && a.SpanStart < before);
                bool mutated = method.DescendantNodes().Any(n => n.SpanStart < before &&
                    (n is PostfixUnaryExpressionSyntax p && p.Operand.ToString() == name ||
                     n is PrefixUnaryExpressionSyntax p2 && p2.Operand.ToString() == name &&
                     p2.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression));
                if (assigned || mutated || declarations.Length != 1 || declarations[0].Initializer == null)
                    throw Unsupported(expression, "local requires unsupported assignment/control-flow slicing");
                return Value(declarations[0].Initializer!.Value, method, declarations[0].SpanStart, resolving);
            }
            finally { resolving.Remove(name); }
        }
        throw Unsupported(expression, "unsupported numeric expression (not executed)");
    }

    private static NotSupportedException Unsupported(SyntaxNode node, string reason) =>
        new($"{reason} at line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {node}");
}
