using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.UI;

/// <summary>Top-bar corruption balance rendered from eleven reviewed states.</summary>
[RegisterNodeAttachment(
    typeof(NTopBar),
    "corruption_meter",
    NodeName = "CorruptionMeter",
    DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName)]
public sealed partial class CorruptionMeter : Control, INodeAttachmentSetup
{
    private const float MeterWidth = 256f;
    private const float MeterHeight = 85f;
    // Pixel coordinates in the reviewed 2172x724 balance artwork, not eleven
    // equal slices of its canvas (which includes transparent side margins).
    private static readonly (int Value, float ArtworkX)[] HoverTicks =
    [
        (-5, 560f), (-3, 770f), (0, 1086f), (3, 1401f), (5, 1611f),
    ];
    private const float HoverWidth = 24f;
    private const float HoverTop = 400f / 724f * MeterHeight;
    private const float HoverHeight = 220f / 724f * MeterHeight;

    private TextureRect? _meterTexture;
    private int _displayedValue = int.MaxValue;
    private bool _isShown;
    private NTopBar? _topBar;
    private int _initialRefreshAttempts;
    private RunState? _lastReportedRun;
    private int _lastReportedValue = int.MaxValue;

    public override void _EnterTree()
    {
        CorruptionEvents.Changed += OnCorruptionChanged;
        RunUiRefreshEvents.CombatVisibilityChanged += OnCombatVisibilityChanged;
    }

    public override void _ExitTree()
    {
        CorruptionEvents.Changed -= OnCorruptionChanged;
        RunUiRefreshEvents.CombatVisibilityChanged -= OnCombatVisibilityChanged;
        ClearHoverTip();
    }

    public void Setup(Node parent, Node node)
    {
        if (parent is NTopBar topBar)
        {
            _topBar = topBar;
            CallDeferred(Node.MethodName.Reparent, topBar);
        }

        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = Vector2.Zero;
        CustomMinimumSize = new Vector2(MeterWidth, MeterHeight);
        Size = CustomMinimumSize;
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 0;
        MouseEntered += ShowHoverTip;
        MouseExited += ClearHoverTip;

        // BuildVisuals immediately selects a texture. Seed it with the real
        // neutral value instead of the field's invalidation sentinel; leaving
        // the +5 clamped texture here made a zero-value scale look tilted.
        _displayedValue = Corruption.Neutral;
        BuildVisuals();
        // The top bar is created before the first map coordinate is visited;
        // refresh when that lifecycle boundary changes, not only on combat.
        SetProcess(true);
        Visible = false;
        CallDeferred(nameof(RefreshVisibility));
    }

    public override void _Process(double delta) => RefreshVisibility();

    private void BuildVisuals()
    {
        if (GetNodeOrNull<TextureRect>("Balance") != null)
        {
            return;
        }

        _meterTexture = new TextureRect
        {
            Name = "Balance",
            Size = new Vector2(MeterWidth, MeterHeight),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_meterTexture);

        foreach (var tick in HoverTicks)
        {
            int value = tick.Value;
            float centerX = tick.ArtworkX / 2172f * MeterWidth;
            Control zone = new()
            {
                Name = $"Hover{value:+0;-0;0}",
                Position = new Vector2(centerX - HoverWidth / 2f, HoverTop),
                Size = new Vector2(HoverWidth, HoverHeight),
                MouseFilter = MouseFilterEnum.Stop,
            };
            zone.MouseEntered += () => ShowHoverTipForValue(value);
            zone.MouseExited += ClearHoverTip;
            AddChild(zone);
        }
        UpdateTexture();
    }

    private void RefreshVisibility()
    {
        UpdatePosition();

        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            HideIfNeeded();
            if (_initialRefreshAttempts++ < 4)
            {
                CallDeferred(nameof(RefreshVisibility));
            }
            return;
        }
        _initialRefreshAttempts = 0;

        var player = LocalContext.GetMe(runState);
        if (player?.Character is not MaidenSuccubusCharacter
            || !runState.CurrentMapCoord.HasValue
            || _topBar == null || !_topBar.IsVisibleInTree()
            || _topBar.FocusBehaviorRecursive == Control.FocusBehaviorRecursiveEnum.Disabled
            || _topBar.Position.Y < -0.5f
            || _topBar.Deck?.IsVisibleInTree() != true
            || _topBar.Map?.IsVisibleInTree() != true
            || NModalContainer.Instance?.OpenModal != null
            || runState.CurrentRoom is EventRoom { CanonicalEvent: Neow })
        {
            HideIfNeeded();
            return;
        }

        ShowIfNeeded();
        int runValue = CorruptionQuery.Get(runState);
        SetValue(runValue);
        if (!ReferenceEquals(_lastReportedRun, runState) || _lastReportedValue != runValue)
        {
            _lastReportedRun = runState;
            _lastReportedValue = runValue;
            Util.Safe.Run(() => MaidenSuccubusMod.Logger.Info(
                $"[CorruptionUI] runValue={runValue}; displayed={_displayedValue}; textureLoaded={_meterTexture?.Texture != null}"),
                "CorruptionUI.LogDisplayedValue");
        }
    }

    private void OnCorruptionChanged(CorruptionChanged change)
    {
        if (RunManager.Instance?.DebugOnlyGetState() == change.RunState)
        {
            SetValue(change.NewValue);
        }
    }

    private void OnCombatVisibilityChanged(bool _) => RefreshVisibility();

    private void SetValue(int value)
    {
        int clamped = Math.Clamp(value, Corruption.Min, Corruption.Max);
        if (_displayedValue == clamped)
        {
            return;
        }
        _displayedValue = clamped;
        UpdateTexture();
    }

    private void UpdateTexture()
    {
        if (_meterTexture == null)
        {
            return;
        }
        int value = Math.Clamp(_displayedValue, Corruption.Min, Corruption.Max);
        string state = value switch
        {
            < 0 => $"neg{-value}",
            > 0 => $"pos{value}",
            _ => "zero",
        };
        _meterTexture.Texture = RuntimeTextureAssets.Load(
            $"ui/corruption/corruption_balance_{state}.png");
    }

    private void UpdatePosition()
    {
        if (_topBar == null
            || !GodotObject.IsInstanceValid(_topBar)
            || _topBar.BossIcon == null
            || !GodotObject.IsInstanceValid(_topBar.BossIcon))
        {
            return;
        }

        const float gap = 12f;
        float left = _topBar.BossIcon.GlobalPosition.X
            + _topBar.BossIcon.Size.X + gap;
        float right = _topBar.Timer != null && GodotObject.IsInstanceValid(_topBar.Timer)
            ? _topBar.Timer.GlobalPosition.X - gap
            : left + MeterWidth;
        float targetGlobalX = right - left >= MeterWidth
            ? left + (right - left - MeterWidth) / 2f
            : left;
        float targetGlobalY = _topBar.BossIcon.GlobalPosition.Y
            + (_topBar.BossIcon.Size.Y - MeterHeight) / 2f;
        Position = new Vector2(targetGlobalX, targetGlobalY) - _topBar.GlobalPosition;
    }

    internal void SuspendNativeUi()
    {
        HideIfNeeded();
        Visible = false;
    }

    private void HideIfNeeded()
    {
        if (!_isShown)
        {
            return;
        }
        ClearHoverTip();
        _isShown = false;
        Visible = false;
    }

    private void ShowIfNeeded()
    {
        if (_isShown)
        {
            return;
        }
        _isShown = true;
        Visible = true;
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
            3 => "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION.plus3",
            5 => "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION.plus5",
            -3 => "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION.minus3",
            -5 => "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION.minus5",
            _ => "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION.description",
        };
        LocString description = new("static_hover_tips", descriptionKey);
        description.Add("Current", _displayedValue);
        NHoverTipSet.CreateAndShow(
            this,
            new HoverTip(
                new LocString(
                    "static_hover_tips",
                    "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION.title"),
                description),
            HoverTip.GetHoverTipAlignment(this));
    }

    private void ClearHoverTip() => NHoverTipSet.Remove(this);
}
