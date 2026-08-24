using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MaidenSuccubus.Core.Transformation;

namespace MaidenSuccubus.UI;

/// <summary>
/// Lightweight layered combat visuals.  All source layers share the same
/// 922x922 canvas, so changing armour only swaps the clothing texture.
/// </summary>
public sealed partial class MaidenSuccubusCreatureVisuals : NCreatureVisuals
{
    private const string RootPath = "res://MaidenSuccubus/images/character/";
    private static readonly Vector2 RestPosition = new(0f, -300f);
    private static readonly Vector2 RestScale = Vector2.One * 0.66f;

    private Node2D? _visualRoot;
    private Sprite2D? _clothing;
    private int _shownArmor = int.MinValue;
    private Tween? _feedbackTween;
    private bool _feedbackActive;
    private bool _dead;
    private double _idleTime;

    /// <summary>
    /// Creates the layered visuals only when every indispensable base layer
    /// can be loaded.  The character model falls back to its original scene
    /// when this returns null, preventing a missing loose asset from producing
    /// an invisible player.
    /// </summary>
    public static MaidenSuccubusCreatureVisuals? TryCreate()
    {
        Texture2D? body = LoadTexture("body.png");
        Texture2D? clothing = LoadTexture("cloth_0001.png");
        Texture2D? face = LoadTexture("face.png");
        if (body == null || clothing == null || face == null)
        {
            MaidenSuccubusMod.Logger.Warn(
                "Layered character visuals are incomplete; using the fallback character scene.");
            return null;
        }

        var root = new MaidenSuccubusCreatureVisuals
        {
            Name = "MaidenSuccubusCreatureVisuals",
        };
        var visualRoot = new Node2D
        {
            Name = "Visuals",
            UniqueNameInOwner = true,
            Position = RestPosition,
            Scale = RestScale,
        };
        root.AddChild(visualRoot);
        visualRoot.Owner = root;

        AddLayer(visualRoot, "BodyLayer", body, 0);
        root._clothing = AddLayer(visualRoot, "ClothingLayer", clothing, 1);
        AddLayer(visualRoot, "FaceLayer", face, 2);
        root._visualRoot = visualRoot;

        AddUnique(root, new Control
        {
            Name = "Bounds",
            Position = new Vector2(-165f, -610f),
            Size = new Vector2(330f, 610f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        AddUnique(root, new Marker2D
        {
            Name = "IntentPos",
            Position = new Vector2(0f, -650f),
        });
        AddUnique(root, new Marker2D
        {
            Name = "CenterPos",
            Position = new Vector2(0f, -320f),
        });
        AddUnique(root, new Marker2D
        {
            Name = "OrbPos",
            Position = new Vector2(-190f, -360f),
        });
        AddUnique(root, new Marker2D
        {
            Name = "TalkPos",
            Position = new Vector2(0f, -610f),
        });
        return root;
    }

    public override void _Ready()
    {
        base._Ready();
        SetProcess(true);
        RefreshClothing();
    }

    public override void _Process(double delta)
    {
        RefreshClothing();
        if (_visualRoot == null || _feedbackActive || _dead)
        {
            return;
        }

        _idleTime += delta;
        _visualRoot.Position = new Vector2(
            RestPosition.X,
            RestPosition.Y + Mathf.Sin((float)_idleTime * 2.1f) * 4f);
        _visualRoot.Rotation = Mathf.Sin((float)_idleTime * 1.3f) * 0.006f;
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
                new Vector2(-26f, -300f),
                -0.04f,
                RestScale,
                new Color(1f, 0.45f, 0.45f),
                0.18f);
        }
        else if (normalized.Contains("cast") || normalized.Contains("skill"))
        {
            PlayTransient(
                new Vector2(0f, -316f),
                -0.025f,
                Vector2.One * 0.72f,
                new Color(0.85f, 0.65f, 1f),
                0.3f);
        }
        else if (normalized.Contains("attack"))
        {
            PlayTransient(
                new Vector2(34f, -296f),
                0.045f,
                Vector2.One * 0.68f,
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
            _visualRoot, "position", new Vector2(-25f, -270f), 0.7f);
        _feedbackTween.TweenProperty(_visualRoot, "rotation", -0.28f, 0.7f);
        _feedbackTween.TweenProperty(
            _visualRoot, "scale", Vector2.One * 0.62f, 0.7f);
        _feedbackTween.TweenProperty(
            _visualRoot, "modulate", new Color(0.45f, 0.45f, 0.52f, 0.25f), 0.7f);
    }

    private void RefreshClothing()
    {
        if (_clothing == null || GetParent() is not NCreature node)
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
            >= 3 => "cloth_0021.png",
            2 => "cloth_0022.png",
            1 => "cloth_0023.png",
            _ => "cloth_0001.png",
        };
        Texture2D? nextTexture = LoadTexture(file);
        if (nextTexture == null)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Unable to switch character clothing to '{file}'; keeping the current layer.");
            return;
        }

        _clothing.Texture = nextTexture;
        _shownArmor = armor;
    }

    private static Sprite2D AddLayer(
        Node2D parent,
        string name,
        Texture2D texture,
        int zIndex)
    {
        var sprite = new Sprite2D
        {
            Name = name,
            Texture = texture,
            ZIndex = zIndex,
        };
        parent.AddChild(sprite);
        return sprite;
    }

    private static Texture2D? LoadTexture(string file)
    {
        string resourcePath = RootPath + file;
        Texture2D? imported = GD.Load<Texture2D>(resourcePath);
        if (imported != null)
        {
            return imported;
        }

        // Debug/hot-reload installs deliberately do not carry a PCK.  Load
        // mirrored loose PNG files directly when Godot has not imported them.
        string absolutePath = ProjectSettings.GlobalizePath(resourcePath);
        Image image = Image.LoadFromFile(absolutePath);
        if (image.IsEmpty())
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Unable to load character layer '{absolutePath}'.");
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static void AddUnique(Node root, Node child)
    {
        child.UniqueNameInOwner = true;
        root.AddChild(child);
        child.Owner = root;
    }
}
