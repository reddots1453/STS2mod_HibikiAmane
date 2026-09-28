using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HumilityCallExtraction;

/// <summary>Source-only base lookup; unknown external bases terminate the chain.</summary>
internal sealed class SourceHierarchy
{
    private readonly Dictionary<string, ClassDeclarationSyntax> _types;
    internal SourceHierarchy(IEnumerable<ClassDeclarationSyntax> types) =>
        _types = types.GroupBy(Key).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    internal static string Key(ClassDeclarationSyntax type) =>
        Namespace(type) + "." + type.Identifier.Text + "`" + (type.TypeParameterList?.Parameters.Count ?? 0);

    private static string Namespace(ClassDeclarationSyntax type) => string.Join(".",
        type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()));

    internal IReadOnlyList<ClassDeclarationSyntax> Chain(ClassDeclarationSyntax type)
    {
        List<ClassDeclarationSyntax> chain = [];
        HashSet<string> seen = [];
        while (true)
        {
            if (!seen.Add(Key(type))) throw new NotSupportedException("Cyclic source inheritance: " + Key(type));
            chain.Add(type);
            if (type.BaseList?.Types.FirstOrDefault()?.Type is not NameSyntax name) return chain;
            var simple = name.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().FirstOrDefault();
            if (name is QualifiedNameSyntax qualified) simple = qualified.Right;
            if (simple == null) return chain;
            string suffix = "." + simple.Identifier.Text + "`" + (simple is GenericNameSyntax generic ? generic.TypeArgumentList.Arguments.Count : 0);
            string prefix = name is QualifiedNameSyntax q ? q.Left.ToString() : Namespace(type);
            if (!_types.TryGetValue(prefix + suffix, out var parent))
            {
                var candidates = _types.Where(p => p.Key.EndsWith(suffix, StringComparison.Ordinal)).ToArray();
                if (candidates.Length > 1) throw new NotSupportedException("Ambiguous source base: " + name);
                if (candidates.Length == 0) return chain;
                parent = candidates[0].Value;
            }
            type = parent;
        }
    }
}
