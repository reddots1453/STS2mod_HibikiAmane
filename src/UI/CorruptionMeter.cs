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
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.UI;

// 顶栏天平刻度条：显示堕落值 (-5 到 +5)
[RegisterNodeAttachment(
    typeof(NTopBar),
    "corruption_meter",
    NodeName = "CorruptionMeter",
    DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName)]
public sealed partial class CorruptionMeter : Control, INodeAttachmentSetup
{
    private const int SegmentCount = 11;
    private const float SegmentSize = 14f;
    private const float SegmentGap = 2f;
    private const float MeterWidth = SegmentCount * SegmentSize + (SegmentCount - 1) * SegmentGap;
    private const float MeterHeight = SegmentSize;
    private const float MeterYOffset = 8f;

    private int _displayedValue = int.MaxValue;
    private bool _isShown = false;
    private NTopBar? _topBar;
    private readonly HoverTip _hoverTip = new(
        new LocString(
            "static_hover_tips",
            "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION.title"),
        new LocString(
            "static_hover_tips",
            "MAIDENSUCCUBUS_TOPBARBUTTON_CORRUPTION.description"));

    // 诊断标记（每个关键路径首次触发时 log 一次）
    private bool _loggedSetup;
    private bool _loggedNullRunState;
    private bool _loggedNonMsChar;
    private bool _loggedShow;
    private bool _loggedDraw;
    private string? _lastCharType;

    public override void _EnterTree()
    {
        CorruptionEvents.Changed += OnCorruptionChanged;
    }

    public override void _ExitTree()
    {
        CorruptionEvents.Changed -= OnCorruptionChanged;
    }

    public void Setup(Node parent, Node node)
    {
        if (!_loggedSetup)
        {
            MaidenSuccubusMod.Logger.Info($"[CorruptionMeter] Setup called, parent={parent?.Name} ({parent?.GetType().Name})");
            _loggedSetup = true;
        }

        // Boss 图标与计时器之间是弹性空白区，不应把计量条交给左侧 HBox
        // 自动排版。直接挂到 NTopBar，并按两者的全局边界计算空白区中心。
        if (parent is NTopBar topBar
            && topBar.BossIcon != null
            && GodotObject.IsInstanceValid(topBar.BossIcon)
            && topBar.Timer != null
            && GodotObject.IsInstanceValid(topBar.Timer))
        {
            _topBar = topBar;
            // RitsuLib 的默认 SetupTiming 是 BeforeAdd：此刻本节点还没有 parent，
            // 直接 Reparent 会报 "Node needs a parent" 并中断剩余初始化。
            // 延迟到下一帧，等待 attachment runtime 完成 AddChild。
            CallDeferred(Node.MethodName.Reparent, topBar);
        }

        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = Vector2.Zero;
        CustomMinimumSize = new Vector2(MeterWidth, MeterHeight + MeterYOffset);
        Size = CustomMinimumSize;
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 100;
        MouseEntered += ShowHoverTip;
        MouseExited += ClearHoverTip;

        // 使用原生 CanvasItem 子节点作为可靠的首帧渲染路径，不依赖动态
        // C# 节点的 _Draw 是否已被 Godot script bridge 正确接管。
        if (GetChildCount() == 0)
        {
            for (int i = 0; i < SegmentCount; i++)
            {
                int value = i - 5;
                Color baseColor = GetSegmentColor(value);
                var segment = new ColorRect
                {
                    Name = $"Segment{value:+0;-0;0}",
                    Position = new Vector2(i * (SegmentSize + SegmentGap), 0),
                    Size = new Vector2(SegmentSize, SegmentSize),
                    Color = value == Corruption.Neutral ? baseColor : baseColor.Darkened(0.55f),
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                AddChild(segment);
            }

            float markerX = 5 * (SegmentSize + SegmentGap) + SegmentSize / 2;
            var marker = new Polygon2D
            {
                Name = "CurrentValueMarker",
                Position = new Vector2(markerX, MeterHeight + 3),
                Polygon = new Vector2[]
                {
                    new(-4f, 0),
                    new(4f, 0),
                    new(0, 5.6f),
                },
                Color = new Color(1, 1, 1, 0.9f),
            };
            AddChild(marker);
        }

        // 动态挂载的 C# 节点在部分环境中不会可靠收到 _Process，因此使用
        // Godot 原生 Timer 驱动角色归属检查。默认隐藏，只有确认当前玩家是
        // 本 Mod 角色后才显示，避免其他角色短暂或永久看到堕落条。
        var visibilityTimer = GetNodeOrNull<Godot.Timer>("VisibilityPollTimer");
        if (visibilityTimer == null)
        {
            visibilityTimer = new Godot.Timer
            {
                Name = "VisibilityPollTimer",
                WaitTime = 0.25,
                OneShot = false,
                Autostart = true,
                ProcessCallback = Godot.Timer.TimerProcessCallback.Idle,
            };
            visibilityTimer.Timeout += RefreshVisibility;
            AddChild(visibilityTimer);
        }

        ProcessMode = ProcessModeEnum.Always;
        SetProcess(true);
        _displayedValue = Corruption.Neutral;
        Visible = false;
        _isShown = false;
        CallDeferred(nameof(RefreshVisibility));
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        UpdatePosition();

        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            if (!_loggedNullRunState)
            {
                MaidenSuccubusMod.Logger.Info("[CorruptionMeter] _Process: runState is null (RunManager not ready or not in run)");
                _loggedNullRunState = true;
            }
            HideIfNeeded();
            return;
        }
        _loggedNullRunState = false;

        var player = LocalContext.GetMe(runState);
        var charType = player?.Character?.GetType().Name ?? "null";
        if (charType != _lastCharType)
        {
            MaidenSuccubusMod.Logger.Info($"[CorruptionMeter] _Process: character type = {charType}");
            _lastCharType = charType;
        }

        if (player?.Character is not MaidenSuccubusCharacter)
        {
            if (!_loggedNonMsChar)
            {
                MaidenSuccubusMod.Logger.Info($"[CorruptionMeter] _Process: not MS character ({charType}), hiding");
                _loggedNonMsChar = true;
            }
            HideIfNeeded();
            return;
        }
        _loggedNonMsChar = false;

        ShowIfNeeded();

        int currentValue = CorruptionQuery.Get(runState);
        if (currentValue != _displayedValue)
        {
            MaidenSuccubusMod.Logger.Info($"[CorruptionMeter] value changed: {_displayedValue} -> {currentValue}, QueueRedraw");
            _displayedValue = currentValue;
            QueueRedraw();
        }
    }

    private void OnCorruptionChanged(CorruptionChanged change)
    {
        if (RunManager.Instance?.DebugOnlyGetState() != change.RunState)
        {
            return;
        }

        _displayedValue = change.NewValue;
        QueueRedraw();
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

        const float bossRightGap = 12f;
        float targetGlobalX = _topBar.BossIcon.GlobalPosition.X
            + _topBar.BossIcon.Size.X + bossRightGap;
        float targetGlobalY = _topBar.BossIcon.GlobalPosition.Y
            + (_topBar.BossIcon.Size.Y - (MeterHeight + MeterYOffset)) / 2f;
        Position = new Vector2(targetGlobalX, targetGlobalY) - _topBar.GlobalPosition;
    }

    private void HideIfNeeded()
    {
        if (_isShown)
        {
            ClearHoverTip();
            _isShown = false;
            Visible = false;
        }
    }

    private void ShowIfNeeded()
    {
        if (!_isShown)
        {
            _isShown = true;
            Visible = true;
            if (!_loggedShow)
            {
                MaidenSuccubusMod.Logger.Info("[CorruptionMeter] Show triggered (Visible=true)");
                _loggedShow = true;
            }
        }
    }

    public override void _Draw()
    {
        if (_displayedValue == int.MaxValue) return;

        if (!_loggedDraw)
        {
            MaidenSuccubusMod.Logger.Info($"[CorruptionMeter] _Draw called, value={_displayedValue}, size={Size}");
            _loggedDraw = true;
        }

        for (int i = 0; i < SegmentCount; i++)
        {
            int value = i - 5;
            float x = i * (SegmentSize + SegmentGap);

            Color baseColor = GetSegmentColor(value);
            Color drawColor = value == _displayedValue
                ? baseColor
                : baseColor.Darkened(0.55f);

            DrawRect(new Rect2(x, 0, SegmentSize, SegmentSize), drawColor, filled: true);
            DrawRect(new Rect2(x, 0, SegmentSize, SegmentSize), new Color(0, 0, 0, 0.85f), filled: false, width: 1f);
        }

        float markerX = (_displayedValue + 5) * (SegmentSize + SegmentGap) + SegmentSize / 2;
        DrawDownTriangle(new Vector2(markerX, MeterHeight + 3), 4f, new Color(1, 1, 1, 0.85f));
    }

    private static Color GetSegmentColor(int value)
    {
        if (value < 0) return new Color(0.5f, 0.7f, 1.0f);
        if (value > 0) return new Color(0.85f, 0.5f, 0.85f);
        return new Color(0.7f, 0.7f, 0.7f);
    }

    private void DrawDownTriangle(Vector2 topCenter, float halfWidth, Color color)
    {
        var points = new Vector2[]
        {
            topCenter + new Vector2(-halfWidth, 0),
            topCenter + new Vector2(halfWidth, 0),
            topCenter + new Vector2(0, halfWidth * 1.4f),
        };
        DrawPolygon(points, new Color[] { color });
    }

    private void ShowHoverTip()
    {
        if (!Visible)
        {
            return;
        }

        NHoverTipSet.CreateAndShow(
            this,
            _hoverTip,
            HoverTip.GetHoverTipAlignment(this));
    }

    private void ClearHoverTip()
    {
        NHoverTipSet.Remove(this);
    }
}
