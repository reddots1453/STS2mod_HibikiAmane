using MaidenSuccubus.Core.Cards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

internal static class DirectDamageQueries
{
    private static string Compact(SyntaxNode node) => string.Concat(node.ToString().Where(c => !char.IsWhiteSpace(c)));
    internal static bool IsDamage(InvocationExpressionSyntax call) => call.Expression.ToString() == "CreatureCmd.Damage"
        && !IsHpLoss(call);
    // HpLoss is a cost/other effect, not the card's retained attack damage.
    private static bool IsHpLoss(InvocationExpressionSyntax call) => call.ArgumentList.Arguments.Count >= 3
        && call.ArgumentList.Arguments[1].Expression.ToString() is "Owner.Creature" or "base.Owner.Creature"
        && call.ArgumentList.Arguments[2].Expression.ToString() is "DynamicVars.HpLoss.BaseValue" or "base.DynamicVars.HpLoss.BaseValue";

    internal static bool ValidateContext(MethodDeclarationSyntax method)
    {
        var contexts = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v =>
            v.Initializer?.Value is AwaitExpressionSyntax { Expression: InvocationExpressionSyntax call }
            && call.Expression.ToString() == "AttackCommand.CreateContextAsync").ToArray();
        if (contexts.Length == 0) return false;
        if (contexts.Length != 1 || contexts[0].Parent?.Parent is not LocalDeclarationStatementSyntax declaration
            || declaration.Parent != method.Body || declaration.AwaitKeyword.IsKind(SyntaxKind.None)
            || declaration.UsingKeyword.IsKind(SyntaxKind.None)
            || method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(c => c.Expression.ToString() == "DamageCmd.Attack"))
            throw new NotSupportedException("Direct damage needs one method-scoped native attack context without mixed attack builders");
        var create = (InvocationExpressionSyntax)((AwaitExpressionSyntax)contexts[0].Initializer!.Value).Expression;
        if (create.ArgumentList.Arguments.Count != 3
            || create.ArgumentList.Arguments[0].Expression.ToString() is not ("CombatState" or "base.CombatState")
            || create.ArgumentList.Arguments[2].Expression.ToString() is not ("play" or "cardPlay"))
            throw new NotSupportedException("Unsupported native attack context arguments");
        foreach (var call in method.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(c =>
            c.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "AddHit" }))
            if (call.Expression is not MemberAccessExpressionSyntax member || !ContextAlias(member.Expression, contexts[0].Identifier.Text, method, []))
                throw new NotSupportedException("AddHit receiver is not the retained native context");
        return true;
    }

    private static bool ContextAlias(ExpressionSyntax expression, string context, MethodDeclarationSyntax method, HashSet<string> seen)
    {
        if (expression is not IdentifierNameSyntax name || !seen.Add(name.Identifier.Text)) return false;
        if (name.Identifier.Text == context) return !method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == context);
        var locals = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v => v.Identifier.Text == name.Identifier.Text).ToArray();
        return locals.Length == 1 && locals[0].Initializer is { } init
            && !method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == name.Identifier.Text)
            && ContextAlias(init.Value, context, method, seen);
    }

    internal static HumilityAttackSource Source(InvocationExpressionSyntax call, bool grouped)
    {
        var args = call.ArgumentList.Arguments;
        if (args.Count is not (5 or 6 or 7) || args[^2].Expression.ToString() != "this"
            || args[^1].Expression.ToString() is not ("play" or "cardPlay" or "null"))
            throw new NotSupportedException("Unknown direct damage overload/source");
        if (args.Count == 5 && !grouped && args[2].Expression.ToString() is "DynamicVars.Damage" or "base.DynamicVars.Damage")
            return HumilityAttackSource.DirectDamageVar;
        string props = Compact(args[3].Expression);
        if (args.Count == 6 && props is "Owner.Creature" or "base.Owner.Creature"
            && args[2].Expression.ToString() is "DynamicVars.Damage" or "base.DynamicVars.Damage")
            return grouped ? HumilityAttackSource.ContextCard : HumilityAttackSource.DirectDamageVar;
        if (args.Count == 7 && args[4].Expression.ToString() is not ("Owner.Creature" or "base.Owner.Creature"))
            throw new NotSupportedException("Unknown direct damage dealer");
        return props switch
        {
            "ValueProp.Move" => grouped ? HumilityAttackSource.ContextCard : HumilityAttackSource.DirectCard,
            "ValueProp.Unpowered|ValueProp.Move" or "ValueProp.Move|ValueProp.Unpowered" => grouped ? HumilityAttackSource.ContextUnpowered : HumilityAttackSource.DirectUnpowered,
            "ValueProp.Unblockable|ValueProp.Unpowered|ValueProp.Move" when !grouped => HumilityAttackSource.DirectUnblockable,
            _ => throw new NotSupportedException("Unknown direct damage value props"),
        };
    }

    internal static int? FirstResult(ExpressionSyntax expression, MethodDeclarationSyntax method, int before)
    {
        if (method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(c => c.Expression.ToString() == "DamageCmd.Attack")) return null;
        if (expression is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression, Left: MemberAccessExpressionSyntax left, Right: MemberAccessExpressionSyntax right }
            || left.Name.Identifier.Text != "TotalDamage" || right.Name.Identifier.Text != "OverkillDamage"
            || left.Expression.ToString() != right.Expression.ToString() || left.Expression is not IdentifierNameSyntax local) return null;
        var result = FindInitializer(local.Identifier.Text, method, before);
        if (result is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "FirstOrDefault", Expression: IdentifierNameSyntax list } } first
            || first.ArgumentList.Arguments.Count != 0) return null;
        var input = FindInitializer(list.Identifier.Text, method, before);
        if (input is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ToList", Expression: ParenthesizedExpressionSyntax { Expression: AwaitExpressionSyntax { Expression: InvocationExpressionSyntax damage } } } }
            || !IsDamage(damage)) return null;
        return method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Count(c => (IsDamage(c) || c.Expression.ToString() == "CreatureCmd.GainBlock") && c.SpanStart < damage.SpanStart);
    }

    internal static bool OtherEnemies(ExpressionSyntax expression, MethodDeclarationSyntax method, int before)
    {
        if (expression is not IdentifierNameSyntax local) return false;
        var input = FindInitializer(local.Identifier.Text, method, before);
        if (input is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ToList", Expression: ParenthesizedExpressionSyntax { Expression: QueryExpressionSyntax query } } }) return false;
        string item = query.FromClause.Identifier.Text;
        if (query.Body.Clauses.Count != 1 || query.Body.Clauses[0] is not WhereClauseSyntax where
            || Compact(where.Condition) != item + ".IsHittable" || query.Body.SelectOrGroup is not SelectClauseSyntax select
            || select.Expression.ToString() != item) return false;
        // The receiver of the first selected-target result is that target. Prove
        // its origin before replacing the teammates-minus-selected query.
        foreach (var result in method.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            var test = SyntaxFactory.ParseExpression(result.Identifier.Text + ".TotalDamage + " + result.Identifier.Text + ".OverkillDamage");
            if (FirstResult(test, method, before) is not { } index) continue;
            var damages = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(c => IsDamage(c) || c.Expression.ToString() == "CreatureCmd.GainBlock").ToArray();
            if (damages[index].ArgumentList.Arguments[1].Expression.ToString() is not ("play.Target" or "cardPlay.Target")) continue;
            string from = Compact(query.FromClause.Expression);
            foreach (string play in new[] { "play", "cardPlay" })
                if (from == $"base.CombatState.GetTeammatesOf({result.Identifier.Text}.Receiver).Except(newglobal::_003C_003Ez__ReadOnlySingleElementList<Creature>({play}.Target))") return true;
        }
        return false;
    }

    private static ExpressionSyntax? FindInitializer(string name, MethodDeclarationSyntax method, int before)
    {
        var declarations = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v => v.Identifier.Text == name && v.SpanStart < before).ToArray();
        if (declarations.Length != 1 || method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == name)) return null;
        return declarations[0].Initializer?.Value;
    }
}
