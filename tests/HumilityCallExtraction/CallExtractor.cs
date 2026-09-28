using MaidenSuccubus.Core.Cards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

internal sealed record Extraction(string Card, HumilityEffectProgram? Program, string? Error, int Line, bool OnlyDamageAndBlock = false);

/// <summary>Static call slicing only: never invokes card methods or removed effects.</summary>
internal static class CallExtractor
{
    internal static IReadOnlyList<Extraction> Extract(string source, IReadOnlyList<MethodDeclarationSyntax>? methods = null, SourceHierarchy? hierarchy = null)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var syntaxErrors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (syntaxErrors.Length != 0)
            return [new("<source>", null, string.Join("; ", syntaxErrors.Select(d => d.ToString())), 1)];
        List<Extraction> results = [];
        methods ??= HelperExpansion.Index([source]);
        hierarchy ??= new SourceHierarchy(methods.Select(m => m.SyntaxTree).Distinct()
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Concat(root.DescendantNodes().OfType<ClassDeclarationSyntax>()));
        foreach (var type in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (type.Modifiers.Any(SyntaxKind.AbstractKeyword) || type.TypeParameterList != null) continue;
            string ns = string.Join(".", type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()));
            string name = string.IsNullOrEmpty(ns) ? type.Identifier.Text : ns + "." + type.Identifier.Text;
            int line = tree.GetLineSpan(type.Span).StartLinePosition.Line + 1;
            try
            {
                var chain = hierarchy.Chain(type);
                var method = chain.SelectMany(t => t.Members.OfType<MethodDeclarationSyntax>())
                    .FirstOrDefault(m => m.Identifier.Text == "OnPlay");
                if (method == null) continue;
                var expansion = new HelperExpansion(methods, method, chain);
                var expanded = (MethodDeclarationSyntax)expansion.Visit(method)!;
                // Expression-bodied OnPlay must also be exposed to statement expansion.
                if (expanded.ExpressionBody != null)
                    expanded = expanded.WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(expanded.ExpressionBody.Expression))).WithExpressionBody(null);
                var secondExpansion = new HelperExpansion(methods, method, chain);
                expanded = (MethodDeclarationSyntax)secondExpansion.Visit(expanded)!;
                expanded = (MethodDeclarationSyntax)new NumericQueries(chain).Visit(expanded)!;
                var program = Slice(expanded, methods, NumericQueries.TypeProperty(chain));
                bool pure = program.HasDamageOrBlock && !expansion.DeletedOtherEffect && !secondExpansion.DeletedOtherEffect
                    && chain.All(t => PurityClassifier.IsPure(t, expanded));
                results.Add(new(name, program, null, line, pure));
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
        IsCall(call, "DamageCmd", "Attack") || IsCall(call, "CreatureCmd", "GainBlock") || DirectDamageQueries.IsDamage(call);

    private static HumilityEffectProgram Slice(MethodDeclarationSyntax method, IReadOnlyList<MethodDeclarationSyntax> methods, string? typeProperty)
    {
        var all = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var calls = all.Where(IsEffect).OrderBy(c => c.SpanStart).ToArray();
        bool directDamage = calls.Any(DirectDamageQueries.IsDamage);
        bool groupedDamage = directDamage && DirectDamageQueries.ValidateContext(method);
        List<(int Position, HumilityEffect Effect)> effects = [];
        HashSet<InvocationExpressionSyntax> retainedChains = [];
        foreach (var call in calls)
        {
            if (call.Ancestors().TakeWhile(n => n != method).Any(n => n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                throw Unsupported(call, "damage/block inside callback requires explicit extraction support");
            HumilityValue amount;
            HumilityValue repeats = HumilityValue.Number(1);
            HumilityTarget target;
            if (DirectDamageQueries.IsDamage(call))
            {
                var directSource = DirectDamageQueries.Source(call, groupedDamage);
                target = Target(call.ArgumentList.Arguments[1].Expression, method, call.SpanStart, []);
                amount = Value(call.ArgumentList.Arguments[2].Expression, method, call.SpanStart, []);
                effects.Add((call.SpanStart, new(HumilityEffectKind.Damage, target, amount, repeats, directSource)));
                continue;
            }
            if (IsCall(call, "CreatureCmd", "GainBlock"))
            {
                if (call.ArgumentList.Arguments.Count < 2) throw Unsupported(call, "GainBlock arguments missing");
                target = Target(call.ArgumentList.Arguments[0].Expression, method, call.SpanStart, []);
                amount = Value(call.ArgumentList.Arguments[1].Expression, method, call.SpanStart, []);
                repeats = BlockRepetitions(call, method);
                effects.Add((call.SpanStart, new(HumilityEffectKind.Block, target, amount, repeats,
                    RequiredCardType: NumericQueries.RequiredType(call, typeProperty))));
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
                        "Targeting" => Target(next.ArgumentList.Arguments.Single().Expression, method, next.SpanStart, []),
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
                        if (next.ArgumentList.Arguments.Count != 1 || !RemovedCalls.VisualCallback(next.ArgumentList.Arguments[0].Expression, method, methods))
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
            effects.Add((executePosition, new(HumilityEffectKind.Damage, attackTarget.Value, amount, repeats, source,
                NumericQueries.RequiredType(call, typeProperty))));
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
            if (groupedDamage && MethodName(invocation) == "AddHit") continue; // Receiver verified by ValidateContext.
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

    private static HumilityValue BlockRepetitions(InvocationExpressionSyntax call, MethodDeclarationSyntax method)
    {
        var loop = call.Ancestors().TakeWhile(n => n != method).OfType<ForStatementSyntax>().FirstOrDefault();
        if (loop == null) return HumilityValue.Number(1);

        // A plain counted block command represents repeated block, not a removed
        // draw/kill loop. Keep its count without executing any surrounding behavior.
        var variables = loop.Declaration?.Variables;
        if (variables?.Count != 1 || loop.Initializers.Count != 0
            || variables.Value[0].Initializer?.Value.ToString() != "0"
            || loop.Condition is not BinaryExpressionSyntax condition || !condition.IsKind(SyntaxKind.LessThanExpression)
            || condition.Left is not IdentifierNameSyntax index || index.Identifier.Text != variables.Value[0].Identifier.Text
            || loop.Incrementors.Count != 1
            || loop.Incrementors[0] is not PostfixUnaryExpressionSyntax increment
            || !increment.IsKind(SyntaxKind.PostIncrementExpression) || increment.Operand.ToString() != index.Identifier.Text)
            throw Unsupported(loop, "unsupported block repetition counter");

        StatementSyntax body = loop.Statement;
        while (true)
        {
            if (body is BlockSyntax block && block.Statements.Count == 1) body = block.Statements[0];
            else if (body is IfStatementSyntax { Else: null } guard) body = guard.Statement;
            else break;
        }
        if (body is not ExpressionStatementSyntax { Expression: AwaitExpressionSyntax awaited }
            || awaited.Expression != call
            || loop.Ancestors().TakeWhile(n => n != method).Any(n => n is ForStatementSyntax))
            throw Unsupported(loop, "block loop requires a single retained block command");

        return Value(condition.Right, method, loop.SpanStart, []);
    }

    private static HumilityTarget Target(ExpressionSyntax expression, MethodDeclarationSyntax method, int before, HashSet<string> resolving)
    {
        expression = Unwrap(expression);
        string text = expression.ToString();
        if (text is "CombatState?.HittableEnemies" or "base.CombatState?.HittableEnemies" or "CombatState.HittableEnemies" or "base.CombatState.HittableEnemies") return HumilityTarget.AllEnemies;
        if (DirectDamageQueries.OtherEnemies(expression, method, before)) return HumilityTarget.OtherEnemies;
        if (text is "Owner.Creature" or "base.Owner.Creature" or "this.Owner.Creature") return HumilityTarget.Self;
        if (text is "play.Target" or "cardPlay.Target" or "play.Target.Player" or "cardPlay.Target.Player") return HumilityTarget.Selected;
        if (expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Creature" } creature)
            return Target(creature.Expression, method, before, resolving);
        if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "First" } first } firstCall
            && firstCall.ArgumentList.Arguments.Count == 0
            && first.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "OrderBy" } order } sort
            && order.Expression.ToString() is "CombatState.HittableEnemies" or "base.CombatState.HittableEnemies"
            && sort.ArgumentList.Arguments.Count == 1 && sort.ArgumentList.Arguments[0].Expression is SimpleLambdaExpressionSyntax lambda
            && lambda.Body.ToString() == lambda.Parameter.Identifier.Text + ".CurrentHp")
            return HumilityTarget.LowestHpEnemy;
        if (expression is IdentifierNameSyntax id)
        {
            if (!resolving.Add(id.Identifier.Text)) throw Unsupported(expression, "cyclic target alias");
            try
            {
                var local = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                    .Where(v => v.Identifier.Text == id.Identifier.Text && v.SpanStart < before).ToArray();
                if (local.Length == 1 && local[0].Initializer is { } init
                    && !method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == id.Identifier.Text && a.SpanStart < before))
                    return Target(init.Value, method, local[0].SpanStart, resolving);
                var loop = expression.Ancestors().OfType<ForEachStatementSyntax>().FirstOrDefault(f => f.Identifier.Text == id.Identifier.Text);
                if (loop?.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ToArray", Expression: InvocationExpressionSyntax filtered } }
                    && filtered.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Where" } filter
                    && filter.Expression.ToString() is "CombatState.HittableEnemies" or "base.CombatState.HittableEnemies"
                    && filtered.ArgumentList.Arguments.Count == 1 && filtered.ArgumentList.Arguments[0].Expression is SimpleLambdaExpressionSyntax other
                    && Compact(other.Body) is var predicate && (predicate == other.Parameter.Identifier.Text + "!=cardPlay.Target" || predicate == other.Parameter.Identifier.Text + "!=play.Target"))
                    return HumilityTarget.OtherEnemies;
                if (loop?.Expression is IdentifierNameSyntax collection)
                {
                    var query = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                        .SingleOrDefault(v => v.Identifier.Text == collection.Identifier.Text)?.Initializer?.Value as QueryExpressionSyntax;
                    if (query != null && query.FromClause.Expression.ToString() is "base.CombatState.GetTeammatesOf(base.Owner.Creature)" or "CombatState.GetTeammatesOf(Owner.Creature)"
                        && query.Body.Clauses.Count == 1
                        && query.Body.Clauses[0] is WhereClauseSyntax where
                        && where.Condition.ToString() == query.FromClause.Identifier.Text + " != null && " + query.FromClause.Identifier.Text + ".IsAlive && " + query.FromClause.Identifier.Text + ".IsPlayer"
                        && query.Body.SelectOrGroup is SelectClauseSyntax selected && selected.Expression.ToString() == query.FromClause.Identifier.Text)
                        return HumilityTarget.AllPlayers;
                }
            }
            finally { resolving.Remove(id.Identifier.Text); }
        }
        throw Unsupported(expression, "unsupported target expression");
    }

    private static bool DynamicVars(ExpressionSyntax expression) => Unwrap(expression).ToString() is "DynamicVars" or "base.DynamicVars" or "this.DynamicVars";

    private static HumilityValue Value(ExpressionSyntax expression, MethodDeclarationSyntax method, int before, HashSet<string> resolving)
    {
        expression = Unwrap(expression);
        if (DirectDamageQueries.FirstResult(expression, method, before) is { } firstIndex)
            return HumilityValue.Named("$effectFirstDamage:" + firstIndex);
        if (NumericQueries.AttackResult(expression, method, before) is { } resultIndex)
            return HumilityValue.Named("$effectDamage:" + resultIndex);
        if (expression is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } coalesce
            && coalesce.Right.ToString() == "0"
            && Unwrap(coalesce.Left) is ConditionalAccessExpressionSyntax powerAccess
            && powerAccess.WhenNotNull.ToString() == ".Amount"
            && powerAccess.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax powerCall } getPower
            && getPower.ArgumentList.Arguments.Count == 0
            && powerCall.Expression.ToString() is "Owner.Creature" or "base.Owner.Creature" or "play.Target" or "cardPlay.Target"
            && powerCall.Name is GenericNameSyntax { Identifier.Text: "GetPower" } powerName)
            return HumilityValue.Named((powerCall.Expression.ToString().EndsWith(".Target", StringComparison.Ordinal) ? "$targetPower:" : "$power:")
                + powerName.TypeArgumentList.Arguments.Single());
        // Predicates embedded in numeric arguments remain part of the value,
        // distinct from the enclosing trigger statements removed by slicing.
        if (expression is ConditionalExpressionSyntax upgrade)
        {
            HumilityValue condition = BooleanValue(upgrade.Condition, method, before, resolving);
            HumilityValue yes = Value(upgrade.WhenTrue, method, before, resolving);
            HumilityValue no = Value(upgrade.WhenFalse, method, before, resolving);
            return HumilityValue.Binary(HumilityValueKind.Add, no,
                HumilityValue.Binary(HumilityValueKind.Multiply, condition,
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
            if (expression.ToString() is "CombatState.HittableEnemies.Count" or "base.CombatState.HittableEnemies.Count"
                or "this.CombatState.HittableEnemies.Count") return HumilityValue.Named("$enemies");
            if (expression.ToString() == "CondemnationPower.DamagePerLayer") return HumilityValue.Named("$condemnationDamagePerLayer");
            if (member.Name.Identifier.Text == "BlockRequired"
                && member.Expression is IdentifierNameSyntax intentLocal)
            {
                var definitions = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                    .Where(v => v.Identifier.Text == intentLocal.Identifier.Text && v.SpanStart < before).ToArray();
                if (definitions.Length == 1 && definitions[0].Initializer is { } init
                    && !method.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                        .Any(a => a.Left.ToString() == intentLocal.Identifier.Text && a.SpanStart < before)
                    && Compact(init.Value) is
                        "play.Target?.Monster?.NextMove.Intents.OfType<ControlIntent>().FirstOrDefault()"
                        or "cardPlay.Target?.Monster?.NextMove.Intents.OfType<ControlIntent>().FirstOrDefault()")
                    return HumilityValue.Named("$targetControlBlock");
            }
            if (member.Name.Identifier.Text == "Count" && Pile(member.Expression, method, before, []) is { } pile)
                return HumilityValue.Named("$pile:" + pile);
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
            if (access.Expression.ToString() == "PowerLayerQuery" && access.Name.Identifier.Text == "CountBuffLayers"
                && invocation.ArgumentList.Arguments.Count == 1
                && invocation.ArgumentList.Arguments[0].Expression.ToString() is "Owner.Creature" or "base.Owner.Creature")
                return HumilityValue.Named("$buffLayers");
            if (access.Expression.ToString() == "DesireCombatSpending" && access.Name.Identifier.Text == "Get"
                && invocation.ArgumentList.Arguments.Count == 1 && invocation.ArgumentList.Arguments[0].Expression.ToString() is "Owner" or "base.Owner")
                return HumilityValue.Named("$spentSecondary");
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
                var assignments = method.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                    .Where(a => a.Left.ToString() == name && a.SpanStart < before).OrderBy(a => a.SpanStart).ToArray();
                bool mutated = method.DescendantNodes().Any(n => n.SpanStart < before &&
                    (n is PostfixUnaryExpressionSyntax p && p.Operand.ToString() == name ||
                     n is PrefixUnaryExpressionSyntax p2 && p2.Operand.ToString() == name &&
                     p2.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression));
                if (mutated || declarations.Length != 1 || declarations[0].Initializer == null)
                    throw Unsupported(expression, "local requires unsupported assignment/control-flow slicing");
                var value = Value(declarations[0].Initializer!.Value, method, declarations[0].SpanStart, resolving);
                foreach (var assignment in assignments)
                {
                    // Slice arithmetic dependencies, not the trigger around them.
                    // Loops/else arms and arbitrary reassignment require an explicit
                    // meaning; never guess a last assignment or execute the source.
                    if (assignment.Kind() is not (SyntaxKind.AddAssignmentExpression or SyntaxKind.MultiplyAssignmentExpression)
                        || assignment.SpanStart < declarations[0].SpanStart
                        || assignment.Ancestors().TakeWhile(n => n != method).Any(n =>
                            n is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax
                            or SwitchStatementSyntax or AnonymousFunctionExpressionSyntax
                            || n is IfStatementSyntax { Else: not null }))
                        throw Unsupported(assignment, "unsupported numeric reassignment or repeated dependency");
                    value = HumilityValue.Binary(assignment.IsKind(SyntaxKind.AddAssignmentExpression)
                        ? HumilityValueKind.Add : HumilityValueKind.Multiply, value,
                        Value(assignment.Right, method, assignment.SpanStart, resolving));
                }
                return value;
            }
            finally { resolving.Remove(name); }
        }
        throw Unsupported(expression, "unsupported numeric expression (not executed)");
    }

    private static string Compact(SyntaxNode node) => string.Concat(node.ToString().Where(c => !char.IsWhiteSpace(c)));

    private static HumilityValue BooleanValue(ExpressionSyntax expression, MethodDeclarationSyntax method, int before, HashSet<string> resolving)
    {
        expression = Unwrap(expression);
        if (expression.ToString() == "__humilityOwnerLostHpThisTurn")
            return HumilityValue.Named("$ownerLostHpThisTurn");
        if (expression.ToString() == "__humilityOwnerExhaustedThisTurn")
            return HumilityValue.Named("$ownerExhaustedThisTurn");
        if (expression is IdentifierNameSyntax enumMarker && enumMarker.Identifier.Text.StartsWith("__humilityEnum__", StringComparison.Ordinal))
        {
            string[] parts = enumMarker.Identifier.Text[16..].Split("__");
            if (parts.Length == 2) return HumilityValue.Named("$enumEquals:" + parts[0] + ":" + parts[1]);
        }
        if (expression.ToString() is "IsUpgraded" or "base.IsUpgraded" or "this.IsUpgraded")
            return HumilityValue.Named("$upgraded");
        if (Compact(expression) is "play.Target.CurrentHp*2<play.Target.MaxHp"
            or "cardPlay.Target.CurrentHp*2<cardPlay.Target.MaxHp")
            return HumilityValue.Named("$targetBelowHalf");
        if (expression is PrefixUnaryExpressionSyntax not && not.IsKind(SyntaxKind.LogicalNotExpression))
            return HumilityValue.Binary(HumilityValueKind.Add, HumilityValue.Number(1),
                HumilityValue.Binary(HumilityValueKind.Multiply, HumilityValue.Number(-1),
                    BooleanValue(not.Operand, method, before, resolving)));
        if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } call
            && member.Expression.ToString() is "play.Target" or "cardPlay.Target"
            && member.Name is GenericNameSyntax { Identifier.Text: "HasPower" } power && call.ArgumentList.Arguments.Count == 0)
            return HumilityValue.Named("$targetHasPower:" + power.TypeArgumentList.Arguments.Single());
        if (expression is IdentifierNameSyntax id && resolving.Add(id.Identifier.Text))
        {
            try
            {
                var definitions = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                    .Where(v => v.Identifier.Text == id.Identifier.Text && v.SpanStart < before).ToArray();
                if (definitions.Length == 1 && definitions[0].Initializer is { } init
                    && !method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == id.Identifier.Text && a.SpanStart < before))
                    return BooleanValue(init.Value, method, definitions[0].SpanStart, resolving);
            }
            finally { resolving.Remove(id.Identifier.Text); }
        }
        throw Unsupported(expression, "unsupported read-only numeric predicate");
    }

    private static string? Pile(ExpressionSyntax expression, MethodDeclarationSyntax method, int before, HashSet<string> seen)
    {
        expression = Unwrap(expression);
        if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ToList" } list })
            return Pile(list.Expression, method, before, seen);
        if (expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Cards", Expression: InvocationExpressionSyntax get }
            && get.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "GetPile", Expression: MemberAccessExpressionSyntax pile }
            && pile.Expression.ToString() == "PileType" && get.ArgumentList.Arguments.Count == 1
            && get.ArgumentList.Arguments[0].Expression.ToString() is "Owner" or "base.Owner")
            return pile.Name.Identifier.Text;
        if (expression is IdentifierNameSyntax id && seen.Add(id.Identifier.Text))
        {
            var locals = method.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .Where(v => v.Identifier.Text == id.Identifier.Text && v.SpanStart < before).ToArray();
            if (locals.Length == 1 && locals[0].Initializer is { } init
                && !method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == id.Identifier.Text && a.SpanStart < before))
                return Pile(init.Value, method, locals[0].SpanStart, seen);
        }
        return null;
    }

    private static NotSupportedException Unsupported(SyntaxNode node, string reason) =>
        new($"{reason} at line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {node}");
}
