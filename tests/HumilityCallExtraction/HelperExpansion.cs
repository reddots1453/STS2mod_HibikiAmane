using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

/// <summary>Inline source-resolvable statement helpers, without invoking them.</summary>
internal sealed class HelperExpansion : CSharpSyntaxRewriter
{
    private readonly IReadOnlyList<MethodDeclarationSyntax> _methods;
    private readonly string _owner;
    private readonly MethodDeclarationSyntax _method;
    private readonly HashSet<string> _active;
    private readonly Counter _counter;
    private sealed class Counter { internal int Value; }

    internal static IReadOnlyList<MethodDeclarationSyntax> Index(IEnumerable<string> sources) => sources
        .SelectMany(source => CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()).ToArray();

    internal HelperExpansion(IReadOnlyList<MethodDeclarationSyntax> methods, MethodDeclarationSyntax owner)
        : this(methods, owner, [], new Counter()) { }
    private HelperExpansion(IReadOnlyList<MethodDeclarationSyntax> methods, MethodDeclarationSyntax owner, HashSet<string> active, Counter counter)
        => (_methods, _owner, _method, _active, _counter) = (methods, ClassName(owner), owner, active, counter);

    private static string ClassName(MethodDeclarationSyntax method)
    {
        var type = method.Ancestors().OfType<TypeDeclarationSyntax>().First();
        return string.Join(".", type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString())
            .Concat(new[] { type.Identifier.Text }));
    }

    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node) => Expand(node.Expression) ?? base.VisitExpressionStatement(node);
    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node) => node.Expression == null ? node : Expand(node.Expression) ?? base.VisitReturnStatement(node);
    public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) => node;
    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => node;
    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => node;
    public override SyntaxNode? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) => node;

    private BlockSyntax? Expand(ExpressionSyntax expression)
    {
        if (expression is AwaitExpressionSyntax awaiting) expression = awaiting.Expression;
        if (expression is not InvocationExpressionSyntax call) return null;
        if (RemovedCalls.Boundary(call)) return null;
        if (call.Expression is MemberAccessExpressionSyntax instanceMember
            && RemovedCalls.LocalType(instanceMember.Expression, _method) is { } localType
            && _owner.EndsWith("." + localType, StringComparison.Ordinal))
        {
            var localMethods = _methods.Where(m => ClassName(m) == _owner
                && m.Identifier.Text == instanceMember.Name.Identifier.Text && ArgumentsFit(m, call)).ToArray();
            // A helper that only mutates another card's fields/vars is an effect to
            // delete, not an attack using the current card as the wrong receiver.
            if (localMethods.Length == 1 && localMethods[0].Body is { } mutation
                && !mutation.DescendantNodes().Any(n => n is InvocationExpressionSyntax or ObjectCreationExpressionSyntax
                    or ImplicitObjectCreationExpressionSyntax or AwaitExpressionSyntax))
                return SyntaxFactory.Block();
        }
        // Returned selection of a helper's collection does not change its effects.
        if (call.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "FirstOrDefault" } selected
            && call.ArgumentList.Arguments.Count == 0 && selected.Expression is ParenthesizedExpressionSyntax { Expression: AwaitExpressionSyntax collection })
            return Expand(collection.Expression);
        string name, receiver;
        bool instance;
        switch (call.Expression)
        {
            case SimpleNameSyntax simple: name = simple.Identifier.Text; receiver = _owner; instance = true; break;
            case MemberAccessExpressionSyntax member:
                name = member.Name.Identifier.Text;
                instance = member.Expression is ThisExpressionSyntax;
                receiver = instance ? _owner : member.Expression.ToString();
                break;
            default: return null;
        }
        var matches = _methods.Where(m => m.Identifier.Text == name &&
            (ClassName(m) == receiver || !instance && ClassName(m).EndsWith("." + receiver, StringComparison.Ordinal)) &&
            (instance || m.Modifiers.Any(SyntaxKind.StaticKeyword))).ToArray();
        if (matches.Length == 0) return null;
        if (matches.Length > 1)
            matches = matches.Where(m => ArgumentsFit(m, call)).ToArray();
        if (matches.Length != 1) throw new NotSupportedException("Ambiguous helper overload: " + receiver + "." + name);
        var helper = matches[0];
        string identity = ClassName(helper) + "." + name + helper.ParameterList;
        if (_active.Count >= 16 || !_active.Add(identity)) throw new NotSupportedException("Recursive helper extraction: " + identity);
        try
        {
            var values = Bind(helper, call);
            string prefix = "__humility_" + (++_counter.Value) + "_";
            // Rename helper locals to prevent collisions with callers or another inlining.
            foreach (var local in helper.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                values.TryAdd(local.Identifier.Text, SyntaxFactory.IdentifierName(prefix + local.Identifier.Text));
            BlockSyntax body = helper.Body ?? (helper.ExpressionBody != null
                ? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(helper.ExpressionBody.Expression))
                : throw new NotSupportedException("Helper has no source body: " + identity));
            body = (BlockSyntax)new Substitute(values).Visit(body)!;
            return (BlockSyntax)new HelperExpansion(_methods, helper, _active, _counter).Visit(body)!;
        }
        finally { _active.Remove(identity); }
    }

    private bool ArgumentsFit(MethodDeclarationSyntax method, InvocationExpressionSyntax call)
    {
        Dictionary<string, ExpressionSyntax> args;
        try { args = Bind(method, call); }
        catch (NotSupportedException) { return false; }
        foreach (var parameter in method.ParameterList.Parameters)
        {
            ExpressionSyntax argument = args[parameter.Identifier.Text];
            string expected = parameter.Type?.ToString().TrimEnd('?') ?? "";
            string? actual = argument switch
            {
                MemberAccessExpressionSyntax member when member.Name.Identifier.Text == "IntValue" => "int",
                MemberAccessExpressionSyntax member when member.Name.Identifier.Text == "Owner" => "Player",
                MemberAccessExpressionSyntax member when member.Name.Identifier.Text == "CombatState" => "ICombatState",
                _ => RemovedCalls.LocalType(argument, _method),
            };
            // Only use unambiguous game API types. Numeric literals can convert to
            // several overloads, so they deliberately do not resolve ambiguity.
            if (actual != null && expected != actual
                && (actual is "Player" or "ICombatState" || expected is "Player" or "ICombatState")) return false;
        }
        return true;
    }

    private static Dictionary<string, ExpressionSyntax> Bind(MethodDeclarationSyntax method, InvocationExpressionSyntax call)
    {
        var parameters = method.ParameterList.Parameters;
        var values = new Dictionary<string, ExpressionSyntax>(StringComparer.Ordinal);
        int positional = 0;
        foreach (var argument in call.ArgumentList.Arguments)
        {
            if (!argument.RefKindKeyword.IsKind(SyntaxKind.None)) throw new NotSupportedException("ref/out/in helper argument is not a pure value");
            string? name = argument.NameColon?.Name.Identifier.Text;
            if (name == null)
            {
                if (positional >= parameters.Count) throw new NotSupportedException("Too many helper arguments");
                name = parameters[positional++].Identifier.Text;
            }
            if (!parameters.Any(p => p.Identifier.Text == name) || !values.TryAdd(name, argument.Expression))
                throw new NotSupportedException("Invalid or repeated helper argument: " + name);
        }
        foreach (var parameter in parameters)
        {
            if (parameter.Modifiers.Any()) throw new NotSupportedException("Unsupported helper parameter modifier");
            if (!values.ContainsKey(parameter.Identifier.Text))
                values.Add(parameter.Identifier.Text, parameter.Default?.Value
                    ?? throw new NotSupportedException("Missing helper argument: " + parameter.Identifier.Text));
        }
        return values;
    }

    private sealed class Substitute(Dictionary<string, ExpressionSyntax> values) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (node.Parent is MemberAccessExpressionSyntax access && access.Name == node || node.Parent is NameColonSyntax) return node;
            return values.TryGetValue(node.Identifier.Text, out var value) ? value.WithTriviaFrom(node) : node;
        }
        public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var updated = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node)!;
            return values.TryGetValue(node.Identifier.Text, out var value) && value is IdentifierNameSyntax identifier
                ? updated.WithIdentifier(identifier.Identifier) : updated;
        }
    }
}
