using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

/// <summary>Positive evidence about the ORIGINAL card, not the already sliced result.</summary>
internal static class PurityClassifier
{
    internal static bool IsPure(ClassDeclarationSyntax type, MethodDeclarationSyntax method)
    {
        if (type.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Modifiers.Any(SyntaxKind.OverrideKeyword)
            && m.Identifier.Text is not ("OnPlay" or "OnUpgrade" or "AfterDowngraded")
            && (m.Identifier.Text.StartsWith("After", StringComparison.Ordinal)
                || m.Identifier.Text.StartsWith("Before", StringComparison.Ordinal)
                || m.Identifier.Text.StartsWith("On", StringComparison.Ordinal)))) return false;
        if (type.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Modifiers.Any(SyntaxKind.OverrideKeyword)
            && p.Identifier.Text is "IsPlayable" or "HasTurnEndInHandEffect")) return false;

        var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var effects = calls.Where(IsEffect).ToArray();
        if (effects.Length == 0) return false;
        // Slice runs first and proves every counted block loop is a single block
        // command with a resolvable count. Repetition alone is not another effect.
        if (effects.Any(effect => effect.Ancestors().TakeWhile(n => n != method).Any(n => n is IfStatementSyntax
            or SwitchStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax
            or DoStatementSyntax or AnonymousFunctionExpressionSyntax
            || n is ForStatementSyntax && !IsBlock(effect)))) return false;

        var builderNames = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Initializer?.Value.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(IsEffect) == true)
            .Select(v => v.Identifier.Text).ToHashSet(StringComparer.Ordinal);
        foreach (var call in calls)
        {
            if (call.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().Any(c =>
                c.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(IsEffect))) continue;
            if (call.Expression is MemberAccessExpressionSyntax member)
            {
                string name = member.Name.Identifier.Text;
                string receiver = member.Expression.ToString();
                if (builderNames.Contains(receiver)) continue; // The complete chain was validated by Slice.
                if (receiver == "ArgumentNullException" && name == "ThrowIfNull") continue;
                if (receiver is "Math" or "Mathf" && name is "Min" or "Max") continue;
                if (receiver == "CreatureCmd" && name == "TriggerAnim") continue;
                if (receiver is "this" or "base" && name is "ResolveEnergyXValue" or "ResolveStarXValue") continue;
            }
            if (call.Expression is IdentifierNameSyntax x && x.Identifier.Text is "ResolveEnergyXValue" or "ResolveStarXValue") continue;
            if (RemovedCalls.IsVisual(call, method)) continue;
            return false;
        }
        var locals = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v => v.Identifier.Text).ToHashSet();
        // Field/stat growth is still another original effect, even without commands.
        if (method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a =>
            a.Left is not IdentifierNameSyntax id || !locals.Contains(id.Identifier.Text))) return false;
        if (method.DescendantNodes().Any(n => n is PostfixUnaryExpressionSyntax post
                && post.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
                && (post.Operand is not IdentifierNameSyntax id || !locals.Contains(id.Identifier.Text))
            || n is PrefixUnaryExpressionSyntax pre && pre.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression
                && (pre.Operand is not IdentifierNameSyntax id2 || !locals.Contains(id2.Identifier.Text)))) return false;
        return true;
    }

    private static bool IsBlock(InvocationExpressionSyntax call) => call.Expression is MemberAccessExpressionSyntax member
        && member.Expression.ToString().EndsWith("CreatureCmd", StringComparison.Ordinal) && member.Name.Identifier.Text == "GainBlock";

    private static bool IsEffect(InvocationExpressionSyntax call) => call.Expression is MemberAccessExpressionSyntax member
        && (member.Expression.ToString().EndsWith("DamageCmd", StringComparison.Ordinal) && member.Name.Identifier.Text == "Attack"
            || member.Expression.ToString().EndsWith("CreatureCmd", StringComparison.Ordinal) && member.Name.Identifier.Text == "GainBlock");
}
