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
using MaidenSuccubus.Core.Temptation;

namespace MaidenSuccubus.UI;

[RegisterNodeAttachment(
    typeof(NTopBar),
    "temptation_meter",
    NodeName = "TemptationMeter",
    DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName)]
public sealed partial class TemptationMeter : Control, INodeAttachmentSetup
{
    private Label? _value;
    private NTopBar? _topBar;
    private MegaCrit.Sts2.Core.Entities.Players.Player? _player;

    public override void _EnterTree()
    {
        TemptationEvents.Changed += OnChanged;
        RunUiRefreshEvents.CombatVisibilityChanged += OnCombatVisibilityChanged;
    }

    public override void _ExitTree()
    {
        TemptationEvents.Changed -= OnChanged;
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
        Position = new Vector2(76f, 122f);
        CustomMinimumSize = new Vector2(70f, 58f);
        Size = CustomMinimumSize;
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 100;
        Visible = false;
        MouseEntered += ShowHoverTip;
        MouseExited += () => NHoverTipSet.Remove(this);

        var backing = new ColorRect
        {
            Color = new Color(0.18f, 0.035f, 0.14f, 0.90f),
            Size = CustomMinimumSize,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(backing);
        var title = new Label
        {
            Text = "诱惑度",
            Size = new Vector2(70f, 27f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        title.AddThemeColorOverride("font_color", new Color(1f, 0.68f, 0.88f));
        title.AddThemeColorOverride("font_outline_color", Colors.Black);
        title.AddThemeConstantOverride("outline_size", 4);
        AddChild(title);
        _value = new Label
        {
            Text = "10",
            Position = new Vector2(0f, 25f),
            Size = new Vector2(70f, 31f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _value.AddThemeColorOverride("font_color", Colors.White);
        _value.AddThemeColorOverride("font_outline_color", Colors.Black);
        _value.AddThemeConstantOverride("outline_size", 4);
        AddChild(_value);
        CallDeferred(nameof(Refresh));
    }

    private void Refresh()
    {
        if (_topBar != null && GodotObject.IsInstanceValid(_topBar))
        {
            Position = new Vector2(76f, 122f) - _topBar.GlobalPosition;
        }
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        _player = runState == null ? null : LocalContext.GetMe(runState);
        Visible = CombatManager.Instance.IsInProgress
            && _player?.Character is MaidenSuccubusCharacter;
        if (Visible && _value != null && _player != null)
        {
            _value.Text = Temptation.Get(_player).ToString();
        }
    }

    private void OnChanged(TemptationChanged change)
    {
        if (_player != null
            && ReferenceEquals(change.Player, _player)
            && _value != null)
        {
            _value.Text = change.NewValue.ToString();
        }
    }

    private void OnCombatVisibilityChanged(bool _) => CallDeferred(nameof(Refresh));

    private void ShowHoverTip()
    {
        if (!Visible || _player == null)
        {
            return;
        }
        LocString description = new(
            "static_hover_tips",
            "MAIDENSUCCUBUS_TEMPTATION.description");
        description.Add("Current", Temptation.Get(_player));
        description.Add("Base", Temptation.FormBaseValue(_player.Creature));
        description.Add("Armor", Temptation.ArmorModifier(_player.Creature));
        NHoverTipSet.CreateAndShow(
            this,
            new HoverTip(
                new LocString(
                    "static_hover_tips",
                    "MAIDENSUCCUBUS_TEMPTATION.title"),
                description),
            HoverTip.GetHoverTipAlignment(this));
    }
}
