using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

/// <summary>Recognized non-damage operations. Names alone are not a blanket helper exemption.</summary>
internal static class RemovedCalls
{
    // Effect boundaries are APIs, not card identities. Do not inline a deleted
    // power/selection/transformation and accidentally resurrect its inner effects.
    internal static bool Boundary(InvocationExpressionSyntax call)
    {
        if (call.Expression is not MemberAccessExpressionSyntax member) return false;
        string receiver = member.Expression.ToString();
        string name = member.Name.Identifier.Text;
        if (receiver is "CardCmd" or "CardPileCmd" or "CardSelectCmd" or "PowerCmd" or "PlayerCmd" or "OrbCmd"
            or "CombatEnchantmentCmd" or "GeneratedCardCostCmd" or "CondemnationCmd" or "TransformationCmd" or "ScriptureCmd") return true;
        return (receiver, name) is ("ControlCmd", "Escape") or ("IntentMoveFactory", "Stun")
            or ("Temptation", "Modify") or ("Data.Desire", "Modify") or ("TemperancePileCmd", "Play")
            or ("Hook", "AfterPreventingDraw") or ("Array", "Empty")
            or ("HumilityExtractedCards", "Apply")
            or ("DoomPower", "DoomKill")
            or ("CombatManager.Instance.History", "CardDrawn")
            or ("Hook", "AfterCardDrawn") or ("IntentMoveFactory", "TryForceErotic")
            or ("Desire", "Modify")
            || name == "GetPile" && receiver.StartsWith("PileType.", StringComparison.Ordinal);
    }

    internal static bool Statement(InvocationExpressionSyntax call, MethodDeclarationSyntax method)
    {
        if (Boundary(call) || Visual(call, method)) return true;
        if (call.Expression is not MemberAccessExpressionSyntax member) return false;
        string name = member.Name.Identifier.Text;
        string receiver = member.Expression.ToString();
        if (receiver is "PotionCmd" or "Log" || receiver == "Godot.GD" && name == "PushWarning"
            || receiver == "ThinkCmd" && name == "Play") return true;
        if (member.Expression is MemberAccessExpressionSyntax cost && cost.Name.Identifier.Text == "EnergyCost"
            && name is "SetThisCombat" or "AddThisCombat" or "SetThisTurn" or "AddThisTurn" or "SetThisTurnOrUntilPlayed") return true;
        string? type = LocalType(member.Expression, method);
        if (type == "CardModel" && name is "SetToFreeThisTurn" or "SetToFreeThisCombat" or "AddKeyword" or "RemoveKeyword" or "FinalizeUpgradeInternal" or "InvokeDrawn") return true;
        if (type == "CardPile" && name == "MoveToTopInternal") return true;
        if (type == "CombatRoom" && name == "AddExtraReward") return true;
        if (type == "EnchantmentChoiceCard" && name == "Configure") return true;
        if (type?.EndsWith("Power", StringComparison.Ordinal) == true && name is "Schedule" or "SetDamage" or "SetSelectedCard" or "Trigger") return true;
        if (receiver.EndsWith(".PendingPostCombatCards", StringComparison.Ordinal) && name == "Add") return true;
        if (type != null && (type.StartsWith("List<", StringComparison.Ordinal) || type.StartsWith("HashSet<", StringComparison.Ordinal))
            && name is "Add" or "Remove" or "RemoveAt" or "Clear" or "AddRange") return true;
        return false;
    }

    internal static bool VisualCallback(ExpressionSyntax expression, MethodDeclarationSyntax method,
        IReadOnlyList<MethodDeclarationSyntax>? methods = null)
    {
        // A method group or unknown callback is not assumed to be visual.
        if (expression is not AnonymousFunctionExpressionSyntax) return false;
        if (expression.DescendantNodes().Any(node => node is ThrowStatementSyntax or ThrowExpressionSyntax
            || node is PostfixUnaryExpressionSyntax post && post.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
            || node is PrefixUnaryExpressionSyntax pre && pre.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression)) return false;
        foreach (var assignment in expression.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left is MemberAccessExpressionSyntax property
                && property.Name.Identifier.Text is "Scale" or "Position" or "Modulate" or "Rotation"
                && LocalType(property.Expression, method)?.EndsWith("Vfx", StringComparison.Ordinal) == true)
                continue;
            if (assignment.Left is not IdentifierNameSyntax local) return false;
            var definitions = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .Where(v => v.Identifier.Text == local.Identifier.Text).ToArray();
            if (definitions.Length != 1 || definitions[0].Parent is not VariableDeclarationSyntax definition
                || definition.Type.ToString() is not ("float" or "double" or "Vector2" or "Vector3" or "Color"))
                return false;
            // A captured animation variable must have no uses in gameplay expressions.
            if (method.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Any(id => id.Identifier.Text == local.Identifier.Text && !expression.Span.Contains(id.Span)))
                return false;
        }
        return expression.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .All(call => Visual(call, method) || VisualQuery(call, methods));
    }

    private static bool VisualQuery(InvocationExpressionSyntax call, IReadOnlyList<MethodDeclarationSyntax>? methods)
    {
        if (methods == null || call.Expression is not IdentifierNameSyntax name) return false;
        var candidates = methods.Where(m => m.Identifier.Text == name.Identifier.Text
            && m.ParameterList.Parameters.Count == call.ArgumentList.Arguments.Count).ToArray();
        if (candidates.Length != 1) return false;
        var helper = candidates[0];
        if (!helper.ReturnType.ToString().TrimEnd('?').EndsWith("Vfx", StringComparison.Ordinal)
            || helper.DescendantNodes().Any(n => n is AssignmentExpressionSyntax or AwaitExpressionSyntax
                or ThrowStatementSyntax or ObjectCreationExpressionSyntax
                or PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax))
            return false;
        var calls = helper.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        return calls.Any(c => c.Expression is MemberBindingExpressionSyntax { Name.Identifier.Text: "GetCreatureNode" }
                && c.Ancestors().OfType<ConditionalAccessExpressionSyntax>()
                    .Any(a => a.Expression.ToString() == "NCombatRoom.Instance"))
            && calls.All(c => c.Expression switch
            {
                MemberBindingExpressionSyntax b => b.Name.Identifier.Text is "GetCreatureNode" or "GetChildren",
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text is "OfType" or "FirstOrDefault" or "GetChildren",
                _ => false,
            });
    }

    internal static bool IsVisual(InvocationExpressionSyntax call, MethodDeclarationSyntax method) => Visual(call, method);

    private static bool Visual(InvocationExpressionSyntax call, MethodDeclarationSyntax method)
    {
        if (call.Expression is MemberBindingExpressionSyntax { Name.Identifier.Text: "GetCreatureNode" }
            && call.Ancestors().OfType<ConditionalAccessExpressionSyntax>()
                .Any(a => a.Expression.ToString() == "NCombatRoom.Instance")) return true;
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
        if (name == "AddChildSafely" && member.Expression is IdentifierNameSyntax id)
        {
            var definitions = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .Where(v => v.Identifier.Text == id.Identifier.Text).ToArray();
            if (definitions.Length == 1
                && definitions[0].Initializer?.Value.ToString() == "NCombatRoom.Instance?.CombatVfxContainer"
                && !method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == id.Identifier.Text))
                return true;
        }
        if (name == "AddChildSafely" && receiver.EndsWith("GlobalUi", StringComparison.Ordinal)
            && call.Ancestors().OfType<ConditionalAccessExpressionSyntax>().Any(a => a.Expression.ToString() is "NRun.Instance" or "NGame.Instance")) return true;
        if (name == "GetCreatureNode" && receiver.StartsWith("NCombatRoom.", StringComparison.Ordinal)) return true;
        if (LocalType(member.Expression, method)?.EndsWith("Vfx", StringComparison.Ordinal) == true) return true;
        // Pure collection reads inside a visual callback only. Nested predicates
        // are checked too, so an unknown invoked helper cannot slip through.
        if (call.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().Any()
            && name is "ToList" or "Last" or "LastOrDefault" or "Where") return true;
        return false;
    }

    internal static string? LocalType(ExpressionSyntax expression, MethodDeclarationSyntax method)
    {
        expression = Unwrap(expression);
        if (expression is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax member)
        {
            if (member.Expression.ToString() == "PowerCmd" && member.Name is GenericNameSyntax { Identifier.Text: "Apply" } generic)
                return generic.TypeArgumentList.Arguments.Single().ToString();
            if (member.Name.Identifier.Text == "FirstOrDefault")
            {
                var selection = Unwrap(member.Expression);
                if (selection is InvocationExpressionSyntax select && select.Expression is MemberAccessExpressionSyntax access
                    && access.Expression.ToString() == "CardSelectCmd") return "CardModel";
            }
            if (member.Name.Identifier.Text == "GetPile" && member.Expression.ToString().StartsWith("PileType.", StringComparison.Ordinal)) return "CardPile";
        }
        if (expression is not IdentifierNameSyntax local) return null;
        string name = local.Identifier.Text;
        var declarations = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v => v.Identifier.Text == name).ToArray();
        if (declarations.Length > 1)
        {
            var types = declarations.Select(v => (v.Parent as VariableDeclarationSyntax)?.Type.ToString().TrimEnd('?'))
                .Distinct().ToArray();
            if (types.Length == 1 && types[0] is not (null or "var")) return types[0];
        }
        if (declarations.Length == 1 && declarations[0].Parent is VariableDeclarationSyntax declaration)
        {
            if (declaration.Type.ToString() != "var") return declaration.Type.ToString().TrimEnd('?');
            if (declarations[0].Initializer?.Value is ObjectCreationExpressionSyntax created) return created.Type.ToString();
            // Only infer direct API return types; do not chase arbitrary alias cycles.
            if (declarations[0].Initializer?.Value is { } value && Unwrap(value) is not IdentifierNameSyntax)
                return LocalType(value, method);
        }
        var loops = method.DescendantNodes().OfType<ForEachStatementSyntax>().Where(f => f.Identifier.Text == name)
            .Select(f => f.Type.ToString().TrimEnd('?')).Distinct().ToArray();
        if (loops.Length == 1) return loops[0];
        var patterns = method.DescendantNodes().OfType<DeclarationPatternSyntax>()
            .Where(p => p.Designation.ToString() == name).Select(p => p.Type.ToString()).Distinct().ToArray();
        return patterns.Length == 1 ? patterns[0] : null;
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax p => Unwrap(p.Expression),
        AwaitExpressionSyntax a => Unwrap(a.Expression),
        _ => expression,
    };
}
