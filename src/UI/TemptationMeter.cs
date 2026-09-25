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
using MaidenSuccubus.Core.Temptation;

namespace MaidenSuccubus.UI;

[RegisterNodeAttachment(
    typeof(NTopBar),
    "temptation_meter",
    NodeName = "TemptationMeter",
    DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName)]
public sealed partial class TemptationMeter : Control, INodeAttachmentSetup
{
    private static readonly Vector2 MeterSize = new(64f, 72f);

    private Label? _value;
    private NTopBar? _topBar;
    private MegaCrit.Sts2.Core.Entities.Players.Player? _player;
    private int _initialRefreshAttempts;

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
            CallDeferred(nameof(AttachToRail));
        }
        SetAnchorsPreset(LayoutPreset.TopLeft);
        CustomMinimumSize = MeterSize;
        Size = MeterSize;
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 2;
        Visible = false;
        MouseEntered += ShowHoverTip;
        MouseExited += () => NHoverTipSet.Remove(this);

        BuildMeter();
        CallDeferred(nameof(Refresh));
    }

    private void BuildMeter()
    {
        if (GetNodeOrNull<TextureRect>("TemptationIcon") != null)
        {
            return;
        }

        var title = new Label
        {
            Name = "Title",
            Text = new LocString(
                "static_hover_tips",
                "MAIDENSUCCUBUS_TEMPTATION.title").GetFormattedText(),
            Position = Vector2.Zero,
            Size = new Vector2(64f, 20f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.38f));
        title.AddThemeColorOverride("font_outline_color", Colors.Black);
        title.AddThemeConstantOverride("outline_size", 3);
        AddChild(title);

        var icon = new TextureRect
        {
            Name = "TemptationIcon",
            Texture = RuntimeTextureAssets.Load(
                "ui/temptation/temptation_lipstick_64.png"),
            Position = new Vector2(-1f, 19f),
            Size = new Vector2(48f, 48f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(icon);

        var badge = new Panel
        {
            Name = "ValueBadge",
            Position = new Vector2(35f, 29f),
            Size = new Vector2(29f, 29f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var badgeStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.035f, 0.11f, 0.94f),
            BorderColor = new Color(1f, 0.38f, 0.7f, 0.92f),
            ShadowColor = new Color(0f, 0f, 0f, 0.48f),
            ShadowSize = 3,
            ShadowOffset = new Vector2(1f, 2f),
        };
        badgeStyle.SetBorderWidthAll(2);
        badgeStyle.SetCornerRadiusAll(15);
        badge.AddThemeStyleboxOverride("panel", badgeStyle);
        AddChild(badge);

        _value = new Label
        {
            Name = "Value",
            Text = "10",
            Size = badge.Size,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _value.AddThemeFontSizeOverride("font_size", 17);
        _value.AddThemeColorOverride("font_color", Colors.White);
        _value.AddThemeColorOverride("font_outline_color", Colors.Black);
        _value.AddThemeConstantOverride("outline_size", 3);
        badge.AddChild(_value);
    }

    private void Refresh()
    {
        AttachToRail();
        RunState? runState = RunManager.Instance?.DebugOnlyGetState();
        _player = runState == null ? null : LocalContext.GetMe(runState);
        Visible = _player?.Character is MaidenSuccubusCharacter;
        if (_player == null && _initialRefreshAttempts++ < 4)
        {
            // The top-bar attachment can precede publication of the local
            // RunState player during a room transition.
            CallDeferred(nameof(Refresh));
        }
        else if (_player != null)
        {
            _initialRefreshAttempts = 0;
        }
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

    private void OnCombatVisibilityChanged(bool _)
    {
        _initialRefreshAttempts = 0;
        CallDeferred(nameof(Refresh));
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
        Position = MaidenSidebarRail.TemptationPosition;
    }

    private void ShowHoverTip()
    {
        if (!Visible || _player == null)
        {
            return;
        }
        LocString description = new(
            "static_hover_tips",
            "MAIDENSUCCUBUS_TEMPTATION.description");
        int current = Temptation.Get(_player);
        int baseValue = Temptation.FormBaseValue(_player.Creature);
        int armor = Temptation.ArmorModifier(_player.Creature);
        // Includes the saved card/relic modifier and hand-dependent effects
        // such as TransparentOutfitCurse. Use the actual total so the displayed
        // breakdown cannot omit a source that participates in the calculation.
        int other = current - baseValue - armor;
        description.Add("Current", current);
        description.Add("Base", baseValue);
        description.Add("Armor", armor.ToString("+0;-0;0"));
        description.Add("Other", other.ToString("+0;-0;0"));
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
