using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MaidenSuccubus.Core.Transformation;

namespace MaidenSuccubus.UI;

/// <summary>
/// Lightweight combat visuals backed by one precomposed sprite per form.
/// Keeping the original illustration layers out of the live scene prevents
/// their relative Z indices from bleeding through full-screen overlays.
/// </summary>
public sealed partial class MaidenSuccubusCreatureVisuals : NCreatureVisuals
{
    private const string RootPath = "res://MaidenSuccubus/images/character/";
    private const string ScenePath =
        "res://MaidenSuccubus/scenes/maiden_succubus_character.tscn";
    private static readonly Dictionary<string, Texture2D> TextureCache =
        new(StringComparer.Ordinal);
    private static readonly HashSet<string> FailedTextureLoads =
        new(StringComparer.Ordinal);
    // The whole-character textures are 922x1250 and retain the original
    // artwork coordinates through the knees.  This scale keeps the completed
    // figure close to the base-game character height, while the Y offset puts
    // the newly drawn shoe soles on the combat floor.
    private static readonly Vector2 RestPosition = new(0f, -194f);
    private static readonly Vector2 RestScale = Vector2.One * 0.36f;

    private Node2D? _visualRoot;
    private Sprite2D? _characterSprite;
    private int _shownArmor = int.MinValue;
    private Tween? _feedbackTween;
    private bool _feedbackActive;
    private bool _dead;

    /// <summary>
    /// Creates the visuals only when the default whole-character texture can
    /// be loaded. The character model falls back to its original scene when
    /// this returns null, preventing a missing loose asset from producing an
    /// invisible player.
    /// </summary>
    public static MaidenSuccubusCreatureVisuals? TryCreate()
    {
        Texture2D? appearance = LoadTexture("character_normal.png");
        if (appearance == null)
        {
            MaidenSuccubusMod.Logger.Warn(
                "Whole-character visuals are incomplete; using the fallback character scene.");
            return null;
        }

        // Decode the three alternate forms while the combat room is being
        // constructed. Form/armour changes can then swap an already resident
        // texture instead of synchronously reading and decoding a PNG in the
        // middle of an animation frame.
        LoadTexture("character_armor_1.png");
        LoadTexture("character_armor_2.png");
        LoadTexture("character_armor_3.png");

        PackedScene? scene = ResourceLoader.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Unable to load character layout '{ScenePath}'; using the fallback character scene.");
            return null;
        }

        Node2D scaffold = scene.Instantiate<Node2D>();
        var root = new MaidenSuccubusCreatureVisuals
        {
            Name = "MaidenSuccubusCreatureVisuals",
        };
        AdoptSceneChildren(scaffold, root);
        scaffold.Free();

        root._visualRoot = root.GetNodeOrNull<Node2D>("Visuals");
        root._characterSprite = root.GetNodeOrNull<Sprite2D>(
            "Visuals/CharacterSprite");
        if (root._visualRoot == null || root._characterSprite == null)
        {
            root.Free();
            MaidenSuccubusMod.Logger.Warn(
                $"Character layout '{ScenePath}' lacks Visuals/CharacterSprite; using the fallback character scene.");
            return null;
        }
        root._characterSprite.Texture = appearance;
        return root;
    }

    public override void _Ready()
    {
        base._Ready();
        SetProcess(false);
        TransformationEvents.Changed += OnTransformationChanged;
        RefreshAppearance();
    }

    public override void _ExitTree()
    {
        TransformationEvents.Changed -= OnTransformationChanged;
        _feedbackTween?.Kill();
        base._ExitTree();
    }

    private void OnTransformationChanged(
        MegaCrit.Sts2.Core.Entities.Creatures.Creature creature)
    {
        if (GetParent() is NCreature node
            && ReferenceEquals(node.Entity, creature))
        {
            RefreshAppearance();
        }
    }

    public void PlayFeedback(string trigger)
    {
        if (_visualRoot == null || _dead)
        {
            return;
        }

        string normalized = trigger.ToLowerInvariant();
        if (normalized.Contains("dead") || normalized.Contains("death"))
        {
            PlayDeath();
        }
        else if (normalized.Contains("hurt") || normalized.Contains("hit"))
        {
            PlayTransient(
                new Vector2(-18f, -194f),
                -0.04f,
                RestScale,
                new Color(1f, 0.45f, 0.45f),
                0.18f);
        }
        else if (normalized.Contains("cast") || normalized.Contains("skill"))
        {
            PlayTransient(
                new Vector2(0f, -206f),
                -0.025f,
                Vector2.One * 0.39f,
                new Color(0.85f, 0.65f, 1f),
                0.3f);
        }
        else if (normalized.Contains("attack"))
        {
            PlayTransient(
                new Vector2(24f, -190f),
                0.045f,
                Vector2.One * 0.38f,
                Colors.White,
                0.2f);
        }
    }

    private void PlayTransient(
        Vector2 position,
        float rotation,
        Vector2 scale,
        Color modulate,
        float duration)
    {
        if (_visualRoot == null)
        {
            return;
        }

        _feedbackTween?.Kill();
        _feedbackActive = true;
        _feedbackTween = CreateTween().SetParallel();
        _feedbackTween.TweenProperty(_visualRoot, "position", position, duration * 0.35f);
        _feedbackTween.TweenProperty(_visualRoot, "rotation", rotation, duration * 0.35f);
        _feedbackTween.TweenProperty(_visualRoot, "scale", scale, duration * 0.35f);
        _feedbackTween.TweenProperty(_visualRoot, "modulate", modulate, duration * 0.35f);
        _feedbackTween.Chain().TweenProperty(
            _visualRoot, "position", RestPosition, duration * 0.65f);
        _feedbackTween.Parallel().TweenProperty(
            _visualRoot, "rotation", 0f, duration * 0.65f);
        _feedbackTween.Parallel().TweenProperty(
            _visualRoot, "scale", RestScale, duration * 0.65f);
        _feedbackTween.Parallel().TweenProperty(
            _visualRoot, "modulate", Colors.White, duration * 0.65f);
        _feedbackTween.Chain().TweenCallback(Callable.From(() =>
            _feedbackActive = false));
    }

    private void PlayDeath()
    {
        if (_visualRoot == null)
        {
            return;
        }

        _feedbackTween?.Kill();
        _dead = true;
        _feedbackActive = true;
        _feedbackTween = CreateTween().SetParallel();
        _feedbackTween.TweenProperty(
            _visualRoot, "position", new Vector2(-18f, -174f), 0.7f);
        _feedbackTween.TweenProperty(_visualRoot, "rotation", -0.28f, 0.7f);
        _feedbackTween.TweenProperty(
            _visualRoot, "scale", Vector2.One * 0.34f, 0.7f);
        _feedbackTween.TweenProperty(
            _visualRoot, "modulate", new Color(0.45f, 0.45f, 0.52f, 0.25f), 0.7f);
    }

    private void RefreshAppearance()
    {
        if (_characterSprite == null || GetParent() is not NCreature node)
        {
            return;
        }

        int armor = TransformationCmd.IsTransformed(node.Entity)
            ? TransformationCmd.GetArmor(node.Entity)?.Amount ?? 0
            : 0;
        if (armor == _shownArmor)
        {
            return;
        }

        string file = armor switch
        {
            >= 3 => "character_armor_3.png",
            2 => "character_armor_2.png",
            1 => "character_armor_1.png",
            _ => "character_normal.png",
        };
        Texture2D? nextTexture = LoadTexture(file);
        if (nextTexture == null)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Unable to switch character appearance to '{file}'; keeping the current texture.");
            return;
        }

        _characterSprite.Texture = nextTexture;
        _shownArmor = armor;
    }

    private static Texture2D? LoadTexture(string file)
    {
        if (TextureCache.TryGetValue(file, out Texture2D? cached)
            && GodotObject.IsInstanceValid(cached))
        {
            return cached;
        }
        if (FailedTextureLoads.Contains(file))
        {
            return null;
        }

        string resourcePath = RootPath + file;
        byte[] pngBytes = Godot.FileAccess.GetFileAsBytes(resourcePath);
        if (pngBytes.Length == 0)
        {
            FailedTextureLoads.Add(file);
            MaidenSuccubusMod.Logger.Warn(
                $"Unable to read character texture '{resourcePath}'.");
            return null;
        }

        // These assets are deliberately deployed as loose PNG files for hot
        // reload. ResourceLoader has no importer for them at runtime and logs
        // two full error stacks for every failed GD.Load call. Decode the PNG
        // bytes directly instead; FileAccess also works when assets are packed.
        using var image = new Image();
        Error error = image.LoadPngFromBuffer(pngBytes);
        if (error != Error.Ok || image.IsEmpty())
        {
            FailedTextureLoads.Add(file);
            MaidenSuccubusMod.Logger.Warn(
                $"Unable to decode character texture '{resourcePath}' ({error}).");
            return null;
        }

        Texture2D texture = ImageTexture.CreateFromImage(image);
        TextureCache[file] = texture;
        return texture;
    }

    private static void AdoptSceneChildren(Node source, Node destination)
    {
        foreach (Node child in source.GetChildren())
        {
            ClearOwnerRecursive(child);
            source.RemoveChild(child);
            destination.AddChild(child);
            SetOwnerRecursive(child, destination);
        }
    }

    private static void ClearOwnerRecursive(Node node)
    {
        node.Owner = null;
        foreach (Node child in node.GetChildren())
        {
            ClearOwnerRecursive(child);
        }
    }

    private static void SetOwnerRecursive(Node node, Node owner)
    {
        node.Owner = owner;
        foreach (Node child in node.GetChildren())
        {
            SetOwnerRecursive(child, owner);
        }
    }
}
