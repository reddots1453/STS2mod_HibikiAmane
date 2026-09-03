using Godot;
using MegaCrit.Sts2.Core.Combat;
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
using STS2RitsuLib.Combat.SecondaryResources;
using MaidenSuccubus.Core.Desire;

namespace MaidenSuccubus.UI;

/// <summary>
/// Character-only vertical desire meter displayed along the left side.
/// </summary>
[RegisterNodeAttachment(
    typeof(NTopBar),
    "desire_meter",
    NodeName = "DesireMeter",
    DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName)]
public sealed partial class DesireMeter : Control, INodeAttachmentSetup
{
    private const int SegmentCount = 10;
    private const float SegmentWidth = 24f;
    private const float SegmentHeight = 13f;
    private const float SegmentGap = 3f;
    private const float TitleHeight = 25f;
    private const float ValueHeight = 24f;
    private const float MeterHeight =
        SegmentCount * SegmentHeight + (SegmentCount - 1) * SegmentGap;
    private const float TotalHeight = TitleHeight + MeterHeight + ValueHeight;

    private readonly List<ColorRect> _segments = [];
    private Label? _valueLabel;
    private NTopBar? _topBar;
    private MegaCrit.Sts2.Core.Entities.Players.Player? _player;
    private int _displayedValue = int.MinValue;

    public override void _EnterTree()
    {
        DesireEvents.Changed += OnDesireChanged;
        RunUiRefreshEvents.CombatVisibilityChanged += OnCombatVisibilityChanged;
    }

    public override void _ExitTree()
    {
        DesireEvents.Changed -= OnDesireChanged;
        RunUiRefreshEvents.CombatVisibilityChanged -= OnCombatVisibilityChanged;
        NHoverTipSet.Remove(this);
    }

    public void Setup(Node parent, Node node)
    {
        if (parent is NTopBar topBar)
        {
            _topBar = topBar;
            CallDeferred(Node.MethodName.Reparent, topBar);
        }

        SetAnchorsPreset(LayoutPreset.TopLeft);
        CustomMinimumSize = new Vector2(54f, TotalHeight);
        Size = CustomMinimumSize;
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 100;
        Visible = false;
        MouseEntered += ShowHoverTip;
        MouseExited += ClearHoverTip;

        BuildMeter();
        SetProcess(false);
        CallDeferred(nameof(Refresh));
    }

    private void BuildMeter()
    {
        if (GetNodeOrNull<Control>("MeterVisuals") != null)
        {
            return;
        }

        var visuals = new Control
        {
            Name = "MeterVisuals",
            MouseFilter = MouseFilterEnum.Ignore,
            Size = CustomMinimumSize,
        };
        AddChild(visuals);

        var title = new Label
        {
            Name = "Title",
            Text = "欲望",
            Position = Vector2.Zero,
            Size = new Vector2(54f, TitleHeight),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        title.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.91f));
        title.AddThemeColorOverride("font_outline_color", new Color(0.12f, 0.02f, 0.13f));
        title.AddThemeConstantOverride("outline_size", 4);
        visuals.AddChild(title);

        var backing = new ColorRect
        {
            Name = "Backing",
            Position = new Vector2(12f, TitleHeight - 3f),
            Size = new Vector2(SegmentWidth + 6f, MeterHeight + 6f),
            Color = new Color(0.08f, 0.025f, 0.09f, 0.88f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        visuals.AddChild(backing);

        _segments.Clear();
        for (int i = 0; i < SegmentCount; i++)
        {
            int requiredValue = SegmentCount - i;
            var segment = new ColorRect
            {
                Name = $"Desire{requiredValue}",
                Position = new Vector2(
                    15f,
                    TitleHeight + i * (SegmentHeight + SegmentGap)),
                Size = new Vector2(SegmentWidth, SegmentHeight),
                Color = GetSegmentColor(requiredValue, filled: false),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            visuals.AddChild(segment);
            _segments.Add(segment);
        }

        _valueLabel = new Label
        {
            Name = "Value",
            Text = "0/10",
            Position = new Vector2(0f, TitleHeight + MeterHeight + 2f),
            Size = new Vector2(54f, ValueHeight),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _valueLabel.AddThemeColorOverride("font_color", Colors.White);
        _valueLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _valueLabel.AddThemeConstantOverride("outline_size", 4);
        visuals.AddChild(_valueLabel);
    }

    private void Refresh()
    {
        UpdatePosition();

        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            _player = null;
            Visible = false;
            return;
        }

        var player = LocalContext.GetMe(runState);
        _player = player;
        if (player?.Character is not MaidenSuccubusCharacter
            || (CombatManager.Instance.IsInProgress
                && player.Creature.CombatState != null))
        {
            Visible = false;
            return;
        }

        Visible = true;
        UpdateValue(Desire.Get(player));
    }

    private void OnDesireChanged(DesireChanged change)
    {
        if (_player == null || !ReferenceEquals(change.Player, _player))
        {
            return;
        }
        UpdateValue(change.NewValue);
    }

    private void OnCombatVisibilityChanged(bool inCombat)
    {
        if (_player?.Character is not MaidenSuccubusCharacter)
        {
            return;
        }
        Visible = !inCombat;
        if (!inCombat)
        {
            UpdateValue(Desire.Get(_player));
        }
    }

    private void UpdateValue(int value)
    {
        if (value == _displayedValue)
        {
            return;
        }

        _displayedValue = value;
        if (_valueLabel != null && _player != null)
        {
            int? max = SecondaryResourceCmd.GetMax(_player, DesireResource.Id);
            _valueLabel.Text = max is null or >= int.MaxValue
                ? value.ToString()
                : $"{value}/{max}";
        }

        for (int i = 0; i < _segments.Count; i++)
        {
            int requiredValue = SegmentCount - i;
            _segments[i].Color = GetSegmentColor(
                requiredValue,
                value >= requiredValue);
        }
    }

    private void ShowHoverTip()
    {
        if (!Visible)
        {
            return;
        }
        NHoverTipSet.CreateAndShow(
            this,
            new HoverTip(
                new LocString(
                    "static_hover_tips",
                    "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE.title"),
                new LocString(
                    "static_hover_tips",
                    "MAIDENSUCCUBUS_SECONDARY_RESOURCE_DESIRE.description")),
            HoverTip.GetHoverTipAlignment(this));
    }

    private void ClearHoverTip() => NHoverTipSet.Remove(this);

    private void UpdatePosition()
    {
        if (_topBar == null || !GodotObject.IsInstanceValid(_topBar))
        {
            return;
        }

        // Fixed to the safe left edge, below the horizontal top bar.
        Position = new Vector2(18f, 122f) - _topBar.GlobalPosition;
    }

    private static Color GetSegmentColor(int requiredValue, bool filled)
    {
        Color baseColor = requiredValue switch
        {
            >= 8 => new Color(0.95f, 0.18f, 0.32f),
            >= 5 => new Color(0.95f, 0.46f, 0.62f),
            _ => new Color(0.68f, 0.24f, 0.68f),
        };

        return filled
            ? baseColor
            : new Color(baseColor.R * 0.24f, baseColor.G * 0.24f, baseColor.B * 0.24f, 0.82f);
    }
}
