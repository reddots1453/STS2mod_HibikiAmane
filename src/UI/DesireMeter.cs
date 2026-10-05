using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.UI;

/// <summary>
/// Persistent desire meter displayed along the left side in and out of combat,
/// using the eleven reviewed visual states.
/// </summary>
[RegisterNodeAttachment(
    typeof(NTopBar),
    "desire_meter",
    NodeName = "DesireMeter",
    DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName)]
public sealed partial class DesireMeter : Control, INodeAttachmentSetup
{
    private static readonly Vector2 MeterSize = new(64f, 208f);
    private const float MeterArtworkHeight = 344f;
    private const float GaugeBottom = 330f;
    private const float GaugeHeight = 257.5f;
    private ShaderMaterial? _thresholdMaterial;

    // Coordinates are in source texels (AtlasTexture keeps the original UVs).
    // Replace only the baked pink stripe, then draw the live threshold in the same gauge.
    private const string ThresholdShader = """
        shader_type canvas_item;
        uniform float marker_y = 124.0;
        varying vec4 vertex_tint;
        void vertex() { vertex_tint = COLOR; }
        void fragment() {
            vec2 pixel = UV / TEXTURE_PIXEL_SIZE;
            vec4 color = texture(TEXTURE, UV);
            if (pixel.x >= 42.0 && pixel.x < 86.0) {
                if (pixel.y >= 118.0 && pixel.y <= 129.0) {
                    color = texture(TEXTURE, vec2(UV.x, 130.0 * TEXTURE_PIXEL_SIZE.y));
                }
                float distance_y = abs(pixel.y - marker_y);
                float glow = 1.0 - smoothstep(0.5, 4.0, distance_y);
                float core = 1.0 - smoothstep(0.5, 1.5, distance_y);
                color.rgb = mix(color.rgb, vec3(1.0, 0.42, 0.75), glow * 0.55);
                color.rgb = mix(color.rgb, vec3(1.0, 0.49, 0.83), core);
                color.a = max(color.a, core);
            }
            COLOR = color * vertex_tint;
        }
        """;

    private TextureRect? _meterTexture;
    private CpuParticles2D? _bubbles;
    private Label? _valueLabel;
    private NTopBar? _topBar;
    private MegaCrit.Sts2.Core.Entities.Players.Player? _player;
    private int _displayedValue = int.MinValue;
    private int _displayedMaximum = int.MinValue;
    private int _displayedThreshold = int.MinValue;
    private IDisposable? _loadSubscription;

    public override void _EnterTree()
    {
        DesireEvents.Changed += OnDesireChanged;
        CorruptionEvents.Changed += OnCorruptionChanged;
        _loadSubscription = STS2RitsuLib.RitsuLibFramework.SubscribeLifecycle<STS2RitsuLib.RunLoadedEvent>(
            _ => CallDeferred(nameof(Refresh)), replayCurrentState: true);
        VisibilityChanged += OnVisibilityChanged;
        RunUiRefreshEvents.CombatVisibilityChanged += OnCombatVisibilityChanged;
    }

    public override void _ExitTree()
    {
        DesireEvents.Changed -= OnDesireChanged;
        CorruptionEvents.Changed -= OnCorruptionChanged;
        VisibilityChanged -= OnVisibilityChanged;
        _loadSubscription?.Dispose();
        _loadSubscription = null;
        RunUiRefreshEvents.CombatVisibilityChanged -= OnCombatVisibilityChanged;
        ClearHoverTip();
    }

    public void Setup(Node parent, Node node)
    {
        if (parent is NTopBar topBar)
        {
            _topBar = topBar;
            CallDeferred(nameof(AttachToRail));
        }

        SetAnchorsPreset(LayoutPreset.TopLeft);
        CustomMinimumSize = MeterSize;
        Size = MeterSize;
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 2;
        Visible = false;
        MouseEntered += ShowHoverTip;
        MouseExited += ClearHoverTip;

        BuildMeter();
        SetProcess(false);
        CallDeferred(nameof(Refresh));
    }

    private void BuildMeter()
    {
        if (GetNodeOrNull<TextureRect>("MeterTexture") != null)
        {
            return;
        }

        _meterTexture = new TextureRect
        {
            Name = "MeterTexture",
            Size = new Vector2(MeterSize.X, MeterArtworkHeight / 2f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _thresholdMaterial = new ShaderMaterial { Shader = new Shader { Code = ThresholdShader } };
        _meterTexture.Material = _thresholdMaterial;
        AddChild(_meterTexture);

        _valueLabel = new Label
        {
            Name = "Value",
            Text = "0",
            Position = new Vector2(11.5f, 175f),
            Size = new Vector2(41f, 28f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _valueLabel.AddThemeFontSizeOverride("font_size", 18);
        _valueLabel.AddThemeColorOverride("font_color", Colors.White);
        _valueLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _valueLabel.AddThemeConstantOverride("outline_size", 4);
        AddChild(_valueLabel);

        AddThresholdHover("Threshold10", new Rect2(6f, 0f, 52f, 34f), 10);
        AddThresholdHover("Threshold8", new Rect2(8f, 52f, 48f, 22f), 8);

        _bubbles = new CpuParticles2D
        {
            Name = "HighDesireBubbles",
            Position = new Vector2(32f, 166f),
            Amount = 14,
            Lifetime = 2.2,
            Emitting = false,
            LocalCoords = true,
            Direction = Vector2.Up,
            Spread = 24f,
            Gravity = new Vector2(0f, -8f),
            InitialVelocityMin = 18f,
            InitialVelocityMax = 34f,
            ScaleAmountMin = 0.45f,
            ScaleAmountMax = 1f,
            Color = new Color(1f, 0.42f, 0.72f, 0.82f),
            Texture = CreateBubbleTexture(),
            ZIndex = 3,
        };
        AddChild(_bubbles);
        UpdateTexture(0);
    }

    private void AddThresholdHover(string name, Rect2 rect, int value)
    {
        Control zone = new()
        {
            Name = name,
            Position = rect.Position,
            Size = rect.Size,
            MouseFilter = MouseFilterEnum.Stop,
        };
        zone.MouseEntered += () => ShowHoverTipForValue(value);
        zone.MouseExited += ClearHoverTip;
        AddChild(zone);
    }

    private void Refresh()
    {
        UpdatePosition();

        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            _player = null;
            Visible = false;
            return;
        }

        _player = LocalContext.GetMe(runState);
        if (_player?.Character is not MaidenSuccubusCharacter)
        {
            Visible = false;
            return;
        }

        Visible = true;
        UpdateValue(Desire.GetDisplayValue(_player));
    }

    private void OnVisibilityChanged()
    {
        // The rail follows native top-bar visibility. Binding may have run
        // before RunLoaded/local-player setup; refresh when it actually appears.
        if (IsVisibleInTree()) CallDeferred(nameof(Refresh));
    }

    private void OnDesireChanged(DesireChanged change)
    {
        if (_player == null) Refresh();
        if (_player != null && ReferenceEquals(change.Player, _player))
        {
            UpdateValue(change.NewValue);
        }
    }

    private void OnCorruptionChanged(CorruptionChanged change)
    {
        if (_player != null && ReferenceEquals(_player.RunState, change.RunState)) Refresh();
    }

    private void OnCombatVisibilityChanged(bool _)
    {
        // This persistent meter remains visible in combat; the RitsuLib counter
        // beside energy is a complementary combat-only presentation.
        CallDeferred(nameof(Refresh));
    }

    private void UpdateValue(int value)
    {
        int maximum = _player == null ? Desire.Max : Desire.GetMaximum(_player);
        int threshold = _player == null ? 8 : DesireRuleModifiers.GetControlBypassThreshold(_player);
        if (value == _displayedValue && maximum == _displayedMaximum && threshold == _displayedThreshold)
        {
            return;
        }

        _displayedValue = value;
        _displayedMaximum = maximum;
        _displayedThreshold = threshold;
        UpdateTexture(value);
        float markerY = Mathf.Clamp(GaugeBottom - GaugeHeight * threshold / Math.Max(1, maximum),
            GaugeBottom - GaugeHeight, GaugeBottom);
        _thresholdMaterial?.SetShaderParameter("marker_y", markerY);
        if (GetNodeOrNull<Control>("Threshold8") is Control thresholdZone)
            thresholdZone.Position = new Vector2(8f, markerY / 2f - thresholdZone.Size.Y / 2f);
        if (_valueLabel != null && _player != null)
        {
            _valueLabel.Text = value.ToString();
            _valueLabel.AddThemeColorOverride(
                "font_color",
                value >= 8
                    ? new Color(1f, 0.62f, 0.82f)
                    : Colors.White);
        }
        if (_bubbles != null)
        {
            _bubbles.Emitting = value >= 5;
        }
    }

    private void UpdateTexture(int value)
    {
        if (_meterTexture == null)
        {
            return;
        }
        int maximum = _player == null ? Desire.Max : Desire.GetMaximum(_player);
        // Reuse reviewed artwork as normalized fill; only the true maximum selects the full frame.
        int state = (int)Math.Clamp((long)value * 10 / Math.Max(1, maximum), 0L, 10L);
        Texture2D? artwork = RuntimeTextureAssets.Load(
            $"ui/desire_meter/desire_meter_{state:00}.png");
        // The reviewed 128x416 artwork includes a baked-in empty value frame
        // below y=344. Show only the gauge; the plain numeric label remains.
        _meterTexture.Texture = artwork == null ? null : new AtlasTexture
        {
            Atlas = artwork,
            Region = new Rect2(0f, 0f, artwork.GetWidth(), MeterArtworkHeight),
        };
    }

    private void ShowHoverTip() => ShowHoverTipForValue(null);

    private void ShowHoverTipForValue(int? value)
    {
        if (!Visible)
        {
            return;
        }
        string descriptionKey = value switch
        {
            8 => "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE.threshold8",
            10 => "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE.threshold10",
            _ => "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE.description",
        };
        LocString description = new("static_hover_tips", descriptionKey);
        if (value == 8)
            description.Add("Threshold", _player == null ? 8 : DesireRuleModifiers.GetControlBypassThreshold(_player));
        NHoverTipSet.CreateAndShow(
            this,
            new HoverTip(
                new LocString(
                    "static_hover_tips",
                    "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE.title"),
                description),
            HoverTip.GetHoverTipAlignment(this));
    }

    private void ClearHoverTip() => NHoverTipSet.Remove(this);

    private void UpdatePosition()
    {
        if (GetParent() is MaidenSidebarRail)
        {
            Position = MaidenSidebarRail.DesirePosition;
        }
        else
        {
            AttachToRail();
        }
    }

    private void AttachToRail()
    {
        if (_topBar == null || !GodotObject.IsInstanceValid(_topBar))
        {
            return;
        }

        MaidenSidebarRail rail = MaidenSidebarRail.GetOrCreate(_topBar);
        if (!ReferenceEquals(GetParent(), rail))
        {
            Reparent(rail);
        }
        Position = MaidenSidebarRail.DesirePosition;
    }

    private static Texture2D CreateBubbleTexture()
    {
        const int size = 20;
        using var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = Vector2.One * ((size - 1) / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2(x, y).DistanceTo(center);
                float alpha = Math.Clamp(1f - Math.Abs(radius - 7f), 0f, 1f);
                image.SetPixel(x, y, new Color(1f, 0.7f, 0.9f, alpha));
            }
        }
        return ImageTexture.CreateFromImage(image);
    }
}
