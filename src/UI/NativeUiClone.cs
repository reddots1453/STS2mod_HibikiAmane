using Godot;

namespace MaidenSuccubus.UI;

/// <summary>Restore scene-local % lookups before a duplicated native control enters the tree.</summary>
internal static class NativeUiClone
{
    internal static void RestoreOwners(Node root)
    {
        root.Owner = null;
        foreach (Node child in root.GetChildren()) Restore(child, root);
    }

    private static void Restore(Node node, Node root)
    {
        bool unique = node.UniqueNameInOwner;
        node.UniqueNameInOwner = false;
        node.Owner = root;
        node.UniqueNameInOwner = unique;
        foreach (Node child in node.GetChildren()) Restore(child, root);
    }
}
