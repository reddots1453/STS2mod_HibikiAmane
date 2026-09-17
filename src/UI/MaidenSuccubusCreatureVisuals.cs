using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;

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
    private Sprite2D? _expressionSprite;
    private string? _shownAppearance;
    private string? _shownExpression;
    private Tween? _feedbackTween;
    private Tween? _edgeTween;
    private CanvasLayer? _edgeLayer;
    private Control? _edgeVisual;
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
        LoadTexture("character_corrupt_armor_1.png");
        LoadTexture("character_corrupt_armor_2.png");
        LoadTexture("character_corrupt_armor_3.png");
        LoadTexture("character_eternal_armor_1.png");
        LoadTexture("character_eternal_armor_2.png");
        LoadTexture("character_eternal_armor_3.png");
        PreloadExpressionTextures();
        LoadTexture("climax.jpg");

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
        root._expressionSprite = new Sprite2D
        {
            Name = "ExpressionSprite",
            FlipH = true,
            // Sibling order draws the face over the body at the same Z.
            // A higher Z escapes later-drawn screen dimmers (game over,
            // deck/map overlays), leaving bright floating eyes above them.
            ZIndex = root._characterSprite.ZIndex,
        };
        root._visualRoot.AddChild(root._expressionSprite);
        return root;
    }

    public override void _Ready()
    {
        base._Ready();
        SetProcess(false);
        TransformationEvents.Changed += OnTransformationChanged;
        DesireEvents.Changed += OnDesireChanged;
        CorruptionEvents.Changed += OnCorruptionChanged;
        EroticIntentVisualEvents.Triggered += OnEroticIntentVisual;
        BuildEdgeVisual();
        RefreshAppearance();
        RefreshExpressionAndEdge();
    }

    public override void _ExitTree()
    {
        TransformationEvents.Changed -= OnTransformationChanged;
        DesireEvents.Changed -= OnDesireChanged;
        CorruptionEvents.Changed -= OnCorruptionChanged;
        EroticIntentVisualEvents.Triggered -= OnEroticIntentVisual;
        _feedbackTween?.Kill();
        _edgeTween?.Kill();
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

    private void OnDesireChanged(DesireChanged change)
    {
        if (!IsOwnedCreature(change.Player.Creature))
        {
            return;
        }

        RefreshExpressionAndEdge();
        if (change.OldValue < Data.Desire.Max
            && change.NewValue >= Data.Desire.Max)
        {
            PlayClimaxCutIn();
        }
    }

    private void OnCorruptionChanged(CorruptionChanged change)
    {
        if (GetParent() is NCreature node
            && node.Entity.Player?.RunState is RunState runState
            && ReferenceEquals(change.RunState, runState))
        {
            RefreshExpressionAndEdge();
        }
    }

    private void OnEroticIntentVisual(EroticIntentVisual visual)
    {
        if (!IsOwnedCreature(visual.Target))
        {
            return;
        }

        switch (visual.Kind)
        {
            case EroticIntentKind.Desire:
                PlayPinkEdgeFlash();
                PlayHeartBubbles();
                PlayTransient(
                    new Vector2(-8f, -199f),
                    -0.018f,
                    Vector2.One * 0.365f,
                    new Color(1f, 0.68f, 0.86f),
                    0.32f);
                break;
            case EroticIntentKind.Control:
                PlayTransient(
                    new Vector2(-13f, -194f),
                    -0.045f,
                    RestScale,
                    new Color(0.82f, 0.55f, 0.92f),
                    0.4f);
                break;
            case EroticIntentKind.Invasion:
                PlayPinkEdgeFlash();
                PlayTransient(
                    new Vector2(-21f, -191f),
                    -0.075f,
                    Vector2.One * 0.355f,
                    new Color(1f, 0.38f, 0.62f),
                    0.48f);
                break;
        }
    }

    private bool IsOwnedCreature(
        MegaCrit.Sts2.Core.Entities.Creatures.Creature creature) =>
        GetParent() is NCreature node
        && ReferenceEquals(node.Entity, creature);

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

    private void RefreshExpressionAndEdge()
    {
        if (_expressionSprite == null
            || GetParent() is not NCreature node
            || node.Entity.Player is not { } player
            || player.RunState is not RunState runState)
        {
            return;
        }

        int desire = Data.Desire.Get(player);
        string route = CorruptionQuery.Get(runState) switch
        {
            >= 3 => "corrupt",
            <= -3 => "holy",
            _ => "neutral",
        };
        string range = desire switch
        {
            >= 10 => "10",
            >= 8 => "8_9",
            >= 5 => "5_7",
            _ => "0_4",
        };
        string file = $"expressions/{route}_desire_{range}.png";
        if (file != _shownExpression)
        {
            Texture2D? expression = LoadTexture(file);
            if (expression != null)
            {
                _expressionSprite.Texture = expression;
                AlignExpressionToAppearance();
                _shownExpression = file;
            }
        }

        SetPersistentPinkEdge(desire >= 8);
    }

    private void BuildEdgeVisual()
    {
        _edgeLayer = new CanvasLayer
        {
            Name = "HighDesireEdgeLayer",
            Layer = 80,
        };
        AddChild(_edgeLayer);

        _edgeVisual = new Control
        {
            Name = "PinkEdges",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = Colors.Transparent,
        };
        _edgeLayer.AddChild(_edgeVisual);
        _edgeVisual.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        AddEdgeRect("Top", 0f, 0f, 1f, 0f, 0f, 0f, 0f, 46f);
        AddEdgeRect("Bottom", 0f, 1f, 1f, 1f, 0f, -46f, 0f, 0f);
        AddEdgeRect("Left", 0f, 0f, 0f, 1f, 0f, 0f, 46f, 0f);
        AddEdgeRect("Right", 1f, 0f, 1f, 1f, -46f, 0f, 0f, 0f);
    }

    private void AddEdgeRect(
        string name,
        float anchorLeft,
        float anchorTop,
        float anchorRight,
        float anchorBottom,
        float offsetLeft,
        float offsetTop,
        float offsetRight,
        float offsetBottom)
    {
        if (_edgeVisual == null)
        {
            return;
        }

        var edge = new ColorRect
        {
            Name = name,
            Color = new Color(1f, 0.16f, 0.55f, 0.48f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = anchorLeft,
            AnchorTop = anchorTop,
            AnchorRight = anchorRight,
            AnchorBottom = anchorBottom,
            OffsetLeft = offsetLeft,
            OffsetTop = offsetTop,
            OffsetRight = offsetRight,
            OffsetBottom = offsetBottom,
        };
        _edgeVisual.AddChild(edge);
    }

    private void SetPersistentPinkEdge(bool enabled)
    {
        if (_edgeVisual == null)
        {
            return;
        }

        _edgeTween?.Kill();
        _edgeTween = null;
        if (!enabled)
        {
            _edgeVisual.Modulate = Colors.Transparent;
            return;
        }

        _edgeVisual.Modulate = new Color(1f, 1f, 1f, 0f);
        _edgeTween = CreateTween().SetLoops();
        _edgeTween.TweenInterval(0.8f);
        _edgeTween.TweenProperty(
            _edgeVisual, "modulate:a", 0.34f, 0.35f);
        _edgeTween.TweenInterval(0.35f);
        _edgeTween.TweenProperty(
            _edgeVisual, "modulate:a", 0f, 0.65f);
        _edgeTween.TweenInterval(1.25f);
    }

    private void PlayPinkEdgeFlash()
    {
        if (_edgeLayer == null)
        {
            return;
        }

        var flash = new ColorRect
        {
            Name = "EroticIntentPinkFlash",
            Color = new Color(1f, 0.18f, 0.56f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _edgeLayer.AddChild(flash);
        flash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        Tween tween = CreateTween();
        tween.TweenProperty(flash, "color:a", 0.19f, 0.12f);
        tween.TweenProperty(flash, "color:a", 0f, 0.42f);
        tween.TweenCallback(Callable.From(flash.QueueFree));
    }

    private void PlayHeartBubbles()
    {
        if (_visualRoot == null)
        {
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            var heart = new Label
            {
                Text = "♡",
                Position = new Vector2(-72f + i * 62f, -550f - i * 18f),
                Modulate = new Color(1f, 0.32f, 0.69f, 0.9f),
                Scale = Vector2.One * (2.2f + i * 0.25f),
                ZIndex = 5,
            };
            heart.AddThemeColorOverride(
                "font_outline_color", new Color(0.35f, 0.02f, 0.2f));
            heart.AddThemeConstantOverride("outline_size", 2);
            _visualRoot.AddChild(heart);
            Tween tween = CreateTween().SetParallel();
            tween.TweenProperty(
                heart, "position:y", heart.Position.Y - 105f, 0.85f);
            tween.TweenProperty(heart, "modulate:a", 0f, 0.85f);
            tween.Chain().TweenCallback(Callable.From(heart.QueueFree));
        }
    }

    private void PlayClimaxCutIn()
    {
        Texture2D? texture = LoadTexture("climax.jpg");
        if (texture == null)
        {
            return;
        }

        var layer = new CanvasLayer
        {
            Name = "ClimaxCutIn",
            Layer = 110,
        };
        AddChild(layer);
        var backdrop = new ColorRect
        {
            Color = new Color(0.08f, 0f, 0.05f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        layer.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var cutIn = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 0.85f, 0.95f, 0f),
        };
        layer.AddChild(cutIn);
        cutIn.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(backdrop, "color:a", 0.72f, 0.18f);
        tween.TweenProperty(cutIn, "modulate:a", 1f, 0.18f);
        tween.Chain().TweenInterval(0.72f);
        tween.Chain().TweenProperty(backdrop, "color:a", 0f, 0.35f);
        tween.Parallel().TweenProperty(cutIn, "modulate:a", 0f, 0.35f);
        tween.Chain().TweenCallback(Callable.From(layer.QueueFree));
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
        string prefix = node.Entity.HasPower<EternalRobePower>()
            ? "character_eternal_armor_"
            : node.Entity.HasPower<CorruptRobePower>()
                ? "character_corrupt_armor_"
                : "character_armor_";
        string file = armor switch
        {
            >= 3 => prefix + "3.png",
            2 => prefix + "2.png",
            1 => prefix + "1.png",
            _ => "character_normal.png",
        };
        if (file == _shownAppearance)
        {
            return;
        }
        Texture2D? nextTexture = LoadTexture(file);
        if (nextTexture == null)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Unable to switch character appearance to '{file}'; keeping the current texture.");
            return;
        }

        _characterSprite.Texture = nextTexture;
        AlignExpressionToAppearance();
        _shownAppearance = file;
    }

    private void AlignExpressionToAppearance()
    {
        if (_characterSprite?.Texture == null
            || _expressionSprite?.Texture == null)
        {
            return;
        }

        // Both bitmaps use the same original top-left artwork coordinates,
        // but expressions are 922x922 while bodies are 922x1250. Sprite2D is
        // centered by default, so compensate for the unequal canvas heights.
        _expressionSprite.Position =
            (_expressionSprite.Texture.GetSize()
                - _characterSprite.Texture.GetSize()) / 2f;
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
        Error error = file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? image.LoadJpgFromBuffer(pngBytes)
                : image.LoadPngFromBuffer(pngBytes);
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

    private static void PreloadExpressionTextures()
    {
        string[] routes = ["neutral", "holy", "corrupt"];
        string[] ranges = ["0_4", "5_7", "8_9", "10"];
        foreach (string route in routes)
        {
            foreach (string range in ranges)
            {
                LoadTexture($"expressions/{route}_desire_{range}.png");
            }
        }
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
