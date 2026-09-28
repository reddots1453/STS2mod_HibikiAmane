using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

// Prove the query from source, not from the helper or card's name. No user code runs.
internal sealed class NumericQueries(IReadOnlyList<ClassDeclarationSyntax> chain) : CSharpSyntaxRewriter
{
    private static string Compact(SyntaxNode node) => string.Concat(node.ToString().Where(c => !char.IsWhiteSpace(c)));

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is IdentifierNameSyntax helper && node.ArgumentList.Arguments.Count == 1
            && node.ArgumentList.Arguments[0].Expression.ToString() is "Owner.Creature" or "base.Owner.Creature" or "this.Owner.Creature")
        {
            var methods = chain.SelectMany(c => c.Members.OfType<MethodDeclarationSyntax>())
                .Where(m => m.Identifier.Text == helper.Identifier.Text).ToArray();
            if (methods.Length == 1 && methods[0] is { } method && method.ReturnType.ToString() == "bool"
                && method.ParameterList.Parameters.Count == 1 && method.ParameterList.Parameters[0].Type?.ToString() == "Creature")
            {
                ExpressionSyntax? body = method.ExpressionBody?.Expression;
                if (method.Body?.Statements.Count == 1 && method.Body.Statements[0] is ReturnStatementSyntax ret) body = ret.Expression;
                if (body != null)
                {
                    string parameter = method.ParameterList.Parameters[0].Identifier.Text;
                    var lambdas = body.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().ToArray();
                    if (lambdas.Length == 1 && lambdas[0].ParameterList.Parameters.Count == 1
                        && lambdas[0].ParameterList.Parameters[0].Type?.ToString() == "DamageReceivedEntry")
                    {
                        string entry = lambdas[0].ParameterList.Parameters[0].Identifier.Text;
                        string expected = $"CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>().Any((DamageReceivedEntry {entry})=>{entry}.HappenedThisTurn({parameter}.CombatState)&&{entry}.Receiver=={parameter}&&{entry}.Result.UnblockedDamage>0)";
                        if (Compact(body) == string.Concat(expected.Where(c => !char.IsWhiteSpace(c))))
                            return SyntaxFactory.IdentifierName("__humilityOwnerLostHpThisTurn");
                    }
                }
            }
        }
        return base.VisitInvocationExpression(node);
    }

    internal static int? AttackResult(ExpressionSyntax expression, MethodDeclarationSyntax method, int before)
    {
        if (expression is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Sum" } sum } sumCall
            || sumCall.ArgumentList.Arguments.Count != 1
            || sumCall.ArgumentList.Arguments[0].Expression is not ParenthesizedLambdaExpressionSyntax sumLambda
            || sumLambda.ParameterList.Parameters.Count != 1
            || sumLambda.ParameterList.Parameters[0].Type?.ToString() != "DamageResult") return null;
        string result = sumLambda.ParameterList.Parameters[0].Identifier.Text;
        if (Compact(sumLambda.Body) != result + ".TotalDamage+" + result + ".OverkillDamage") return null;
        if (sum.Expression is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "SelectMany" } many } manyCall
            || manyCall.ArgumentList.Arguments.Count != 1
            || manyCall.ArgumentList.Arguments[0].Expression is not ParenthesizedLambdaExpressionSyntax manyLambda
            || manyLambda.ParameterList.Parameters.Count != 1
            || manyLambda.ParameterList.Parameters[0].Type is not { } manyType || Compact(manyType) != "List<DamageResult>"
            || manyLambda.Body.ToString() != manyLambda.ParameterList.Parameters[0].Identifier.Text
            || many.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Results", Expression: IdentifierNameSyntax local }) return null;
        var declarations = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Identifier.Text == local.Identifier.Text && v.SpanStart < before).ToArray();
        if (declarations.Length != 1 || declarations[0].Initializer?.Value is not AwaitExpressionSyntax { Expression: InvocationExpressionSyntax execution }
            || execution.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Execute" }
            || method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == local.Identifier.Text)) return null;
        var roots = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsEffect).ToArray();
        // A split builder's declaration order is not its execution order. Leave
        // that aggregate diagnostic until the common ordered-effect binding handles it.
        if (roots.Any(other => IsAttack(other) && !other.Ancestors().OfType<InvocationExpressionSyntax>()
            .Any(c => c.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Execute" }))) return null;
        var root = execution.DescendantNodes().OfType<InvocationExpressionSyntax>().SingleOrDefault(IsAttack);
        if (root == null) return null;
        // Selected-target result preview is native. Other aggregate targets need a
        // per-target preview adapter before accepting their result-dependent block.
        var targets = execution.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(c => c.Expression is MemberAccessExpressionSyntax m && m.Name.Identifier.Text.StartsWith("Targeting", StringComparison.Ordinal)).ToArray();
        if (targets.Length != 1 || targets[0].Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Targeting" }
            || targets[0].ArgumentList.Arguments.Count != 1
            || targets[0].ArgumentList.Arguments[0].Expression.ToString() is not ("play.Target" or "cardPlay.Target")) return null;
        // Result use after subsequent mutation of the builder/results is not supported.
        if (method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call => call.SpanStart > execution.SpanStart
            && call.SpanStart < before && call.Expression is MemberAccessExpressionSyntax m
            && (m.Expression.ToString() == local.Identifier.Text || m.Expression.ToString().StartsWith(local.Identifier.Text + ".Results")))) return null;
        return roots.Count(other => Position(other) < Position(root));
    }

    private static bool IsAttack(InvocationExpressionSyntax call) => call.Expression.ToString() == "DamageCmd.Attack";
    private static bool IsEffect(InvocationExpressionSyntax call) => IsAttack(call) || call.Expression.ToString() == "CreatureCmd.GainBlock";
    private static int Position(InvocationExpressionSyntax call)
    {
        if (!IsAttack(call)) return call.SpanStart;
        return call.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault(c =>
            c.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Execute" })?.SpanStart ?? call.SpanStart;
    }
}
