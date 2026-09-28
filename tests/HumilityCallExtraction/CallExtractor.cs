using MaidenSuccubus.Core.Cards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

internal sealed record Extraction(string Card, HumilityEffectProgram? Program, string? Error, int Line);

/// <summary>Static call slicing only: never invokes card methods or removed effects.</summary>
internal static class CallExtractor
{
    internal static IReadOnlyList<Extraction> Extract(string source)
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
            try { results.Add(new(name, Slice(method), null, line)); }
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
        List<HumilityEffect> effects = [];
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
                effects.Add(new(HumilityEffectKind.Block, target, amount, repeats));
                continue;
            }
            amount = Value(call.ArgumentList.Arguments.Single().Expression, method, call.SpanStart, []);
            HumilityTarget? attackTarget = null;
            bool execute = false;
            SyntaxNode current = call;
            while (current.Parent is MemberAccessExpressionSyntax member && member.Expression == current
                   && member.Parent is InvocationExpressionSyntax next)
            {
                switch (member.Name.Identifier.Text)
                {
                    case "WithHitCount": repeats = Value(next.ArgumentList.Arguments.Single().Expression, method, call.SpanStart, []); break;
                    case "Targeting": attackTarget = Target(next.ArgumentList.Arguments.Single().Expression); break;
                    case "TargetingAllOpponents": attackTarget = HumilityTarget.AllEnemies; break;
                    case "TargetingRandomOpponents": attackTarget = HumilityTarget.RandomEnemy; break;
                    case "Execute": execute = true; break;
                    case "FromCard": case "WithHitFx": case "WithAttackerAnim": case "WithHitVfxNode":
                        break; // Visual callbacks are not executed by the rewritten program.
                    default: throw Unsupported(next, "unsupported attack-chain method " + member.Name.Identifier.Text);
                }
                current = next;
            }
            if (!execute || attackTarget == null) throw Unsupported(call, "attack chain must include explicit target and Execute");
            effects.Add(new(HumilityEffectKind.Damage, attackTarget.Value, amount, repeats));
        }
        // Unknown helpers may contain a hidden attack. Do not silently label them an
        // empty program, or keep only a direct attack while dropping a helper's damage.
        IEnumerable<ExpressionSyntax> statements = method.DescendantNodes().OfType<ExpressionStatementSyntax>().Select(s => s.Expression);
        if (method.ExpressionBody != null) statements = statements.Append(method.ExpressionBody.Expression);
        foreach (var statement in statements)
        {
            if (statement.Ancestors().TakeWhile(n => n != method).Any(n => n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)) continue;
            var expression = statement is AwaitExpressionSyntax awaitExpression ? awaitExpression.Expression : statement;
            if (expression is not InvocationExpressionSyntax invocation || invocation.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(IsEffect)) continue;
            if (invocation.Expression is MemberAccessExpressionSyntax member
                && member.Expression.ToString() is "CardPileCmd" or "CardCmd" or "PowerCmd" or "PlayerCmd" or "OrbCmd" or "CreatureCmd" or "ArgumentNullException" or "Cmd" or "SfxCmd") continue;
            throw Unsupported(invocation, "helper statement may hide damage/block: " + MethodName(invocation));
        }
        return new(effects);
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax p => Unwrap(p.Expression),
        CastExpressionSyntax c => Unwrap(c.Expression),
        PostfixUnaryExpressionSyntax p when p.IsKind(SyntaxKind.SuppressNullableWarningExpression) => Unwrap(p.Operand),
        _ => expression,
    };

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
