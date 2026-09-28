using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

/// <summary>Recognized non-damage operations. Names alone are not a blanket helper exemption.</summary>
internal static class RemovedCalls
{
    internal static bool Statement(InvocationExpressionSyntax call, MethodDeclarationSyntax method)
    {
        if (Visual(call, method)) return true;
        if (call.Expression is not MemberAccessExpressionSyntax member) return false;
        string name = member.Name.Identifier.Text;
        string receiver = member.Expression.ToString();
        if (receiver is "PotionCmd" or "Log") return true;
        if (member.Expression is MemberAccessExpressionSyntax cost && cost.Name.Identifier.Text == "EnergyCost"
            && name is "SetThisCombat" or "AddThisCombat" or "SetThisTurn" or "AddThisTurn") return true;
        string? type = LocalType(member.Expression, method);
        if (type == "CardModel" && name is "SetToFreeThisTurn" or "SetToFreeThisCombat" or "AddKeyword" or "RemoveKeyword") return true;
        if (type != null && (type.StartsWith("List<", StringComparison.Ordinal) || type.StartsWith("HashSet<", StringComparison.Ordinal))
            && name is "Add" or "Remove" or "RemoveAt" or "Clear" or "AddRange") return true;
        return false;
    }

    internal static bool VisualCallback(ExpressionSyntax expression, MethodDeclarationSyntax method)
    {
        // A method group or unknown callback is not assumed to be visual.
        if (expression is not AnonymousFunctionExpressionSyntax) return false;
        if (expression.DescendantNodes().Any(node => node is AssignmentExpressionSyntax or ThrowStatementSyntax or ThrowExpressionSyntax
            || node is PostfixUnaryExpressionSyntax post && post.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
            || node is PrefixUnaryExpressionSyntax pre && pre.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression)) return false;
        return expression.DescendantNodes().OfType<InvocationExpressionSyntax>().All(call => Visual(call, method));
    }

    private static bool Visual(InvocationExpressionSyntax call, MethodDeclarationSyntax method)
    {
        if (call.Expression is MemberBindingExpressionSyntax bound && bound.Name.Identifier.Text == "AddChildSafely"
            && call.Parent is ConditionalAccessExpressionSyntax conditional)
            return conditional.Expression.ToString().Contains("CombatVfxContainer", StringComparison.Ordinal);
        if (call.Expression is not MemberAccessExpressionSyntax member) return false;
        string name = member.Name.Identifier.Text;
        string receiver = member.Expression.ToString();
        if (receiver is "Cmd" && name == "Wait" || receiver is "SfxCmd" or "VfxCmd") return true;
        if (receiver.Split('.').Last().EndsWith("Vfx", StringComparison.Ordinal)
            && name is "Create" or "CreateNormal") return true;
        if (name == "AddChildSafely" && (receiver.Contains("CombatVfxContainer", StringComparison.Ordinal)
            || receiver == "NGame.Instance.CurrentRunNode.GlobalUi")) return true;
        if (name == "GetCreatureNode" && receiver.StartsWith("NCombatRoom.", StringComparison.Ordinal)) return true;
        if (LocalType(member.Expression, method)?.EndsWith("Vfx", StringComparison.Ordinal) == true) return true;
        // Pure collection reads inside a visual callback only. Nested predicates
        // are checked too, so an unknown invoked helper cannot slip through.
        if (call.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().Any()
            && name is "ToList" or "Last" or "LastOrDefault" or "Where") return true;
        return false;
    }

    private static string? LocalType(ExpressionSyntax expression, MethodDeclarationSyntax method)
    {
        if (expression is not IdentifierNameSyntax local) return null;
        string name = local.Identifier.Text;
        var declarations = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v => v.Identifier.Text == name).ToArray();
        if (declarations.Length == 1 && declarations[0].Parent is VariableDeclarationSyntax declaration)
        {
            if (declaration.Type.ToString() != "var") return declaration.Type.ToString();
            if (declarations[0].Initializer?.Value is ObjectCreationExpressionSyntax created) return created.Type.ToString();
        }
        var loops = method.DescendantNodes().OfType<ForEachStatementSyntax>().Where(f => f.Identifier.Text == name).ToArray();
        return loops.Length == 1 ? loops[0].Type.ToString() : null;
    }
}
