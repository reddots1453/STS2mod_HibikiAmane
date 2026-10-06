using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.UI;

/// <summary>Immediate, causal feedback even while an event hides the top bar.</summary>
internal static class CorruptionChangeFeedback
{
    private static CanvasLayer? _layer;
    private static VBoxContainer? _stack;
    private static bool _subscribed;

    internal static void Initialize()
    {
        if (_subscribed) return;
        _subscribed = true;
        CorruptionEvents.Changed += OnChanged;
    }

    private static void OnChanged(CorruptionChanged change)
    {
        if (change.Delta == 0 || change.Source == CorruptionChangeSource.Debug
            || RunManager.Instance?.DebugOnlyGetState() != change.RunState
            || LocalContext.GetMe(change.RunState)?.Character is not MaidenSuccubusCharacter)
            return;

        NGame? game = NGame.Instance;
        if (game == null || !GodotObject.IsInstanceValid(game)) return;
        VBoxContainer stack = EnsureStack(game);
        Color accent = change.Delta > 0
            ? new Color("#d88bd8") : new Color("#f0d878");

        var card = new PanelContainer
        {
            Name = "CorruptionChange",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(680f, 58f),
            Modulate = new Color(1f, 1f, 1f, 0f),
        };
        var background = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.07f, 0.13f, 0.92f),
            BorderColor = accent,
            BorderWidthLeft = 3,
            BorderWidthTop = 2,
            BorderWidthRight = 3,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        };
        card.AddThemeStyleboxOverride("panel", background);
        var text = new Label
        {
            Text = $"{CorruptionFeedbackReason.Describe(change.Source, change.Delta)}使堕落值 {change.Delta:+0;-0;0}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        text.AddThemeFontSizeOverride("font_size", 25);
        text.AddThemeColorOverride("font_color", accent);
        card.AddChild(text);
        stack.AddChild(card);
        Tween tween = card.CreateTween();
        tween.TweenProperty(card, "modulate:a", 1f, 0.15f);
        tween.TweenInterval(1.35f);
        tween.TweenProperty(card, "modulate:a", 0f, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(card)) card.QueueFree();
        }));
    }

    private static VBoxContainer EnsureStack(NGame game)
    {
        if (_stack != null && GodotObject.IsInstanceValid(_stack)
            && _layer != null && GodotObject.IsInstanceValid(_layer)
            && _layer.GetParent() == game)
            return _stack;

        _layer = new CanvasLayer { Name = "MaidenCorruptionFeedback", Layer = 130 };
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _layer.AddChild(root);
        game.AddChild(_layer);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _stack = new VBoxContainer
        {
            Name = "Messages",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            OffsetLeft = -350f,
            OffsetRight = 350f,
            OffsetTop = 76f,
            OffsetBottom = 310f,
        };
        root.AddChild(_stack);
        return _stack;
    }
}

internal static class CorruptionFeedbackReason
{
    internal static string Describe(CorruptionChangeSource source, int delta) => source.Id switch
    {
        "desire.first_maximum" => "首次欲望满值",
        "invasion.first" => "首次受到侵犯",
        "rest_site.first_masturbation" => "首次火堆自慰",
        "rest_site.first_prayer" => "首次火堆祈祷",
        "virgin.act" => "保持纯洁进入下一幕",
        "boss_blessing.light" => "领取光明女神祝福",
        "boss_blessing.dark" => "领取黑暗女神祝福",
        "start.holymaiden" => "选择圣洁少女开局",
        "start.succubus" => "选择魅魔开局",
        "undead_gathering.pray" => "亡灵集会·祈祷",
        "undead_gathering.learn" => "亡灵集会·学习禁忌知识",
        "suspicious_shop.work" => "可疑商店·工作",
        "massage_shop.second.special" => "按摩店·接受特别服务",
        "massage_shop.second.refuse" => "按摩店·拒绝服务",
        "massage_shop.third.special" => "按摩店·继续特别服务",
        "vanilla_event.ABYSSAL_BATHS.pages.INITIAL.options.IMMERSE" => "深渊浴池·沉浸",
        "vanilla_event.ABYSSAL_BATHS.pages.INITIAL.options.ABSTAIN" => "深渊浴池·克制",
        "vanilla_event.AROMA_OF_CHAOS.pages.INITIAL.options.LET_GO" => "混沌香气·放任",
        "vanilla_event.AROMA_OF_CHAOS.pages.INITIAL.options.MAINTAIN_CONTROL" => "混沌香气·保持克制",
        "vanilla_event.DOORS_OF_LIGHT_AND_DARK.pages.INITIAL.options.DARK" => "光暗之门·选择黑暗",
        "vanilla_event.DOORS_OF_LIGHT_AND_DARK.pages.INITIAL.options.LIGHT" => "光暗之门·选择光明",
        "vanilla_event.FIELD_OF_MAN_SIZED_HOLES.pages.INITIAL.options.ENTER_YOUR_HOLE" => "人形洞穴·进入洞穴",
        "vanilla_event.FIELD_OF_MAN_SIZED_HOLES.pages.INITIAL.options.RESIST" => "人形洞穴·抵抗",
        "vanilla_event.SPIRIT_GRAFTER.pages.INITIAL.options.LET_IT_IN" => "灵体嫁接·接纳灵体",
        "vanilla_event.SPIRIT_GRAFTER.pages.INITIAL.options.REJECTION" => "灵体嫁接·拒绝灵体",
        "vanilla_event.SYMBIOTE.pages.INITIAL.options.APPROACH" => "共生体·靠近",
        "vanilla_event.SYMBIOTE.pages.INITIAL.options.KILL_WITH_FIRE" => "共生体·用火焚烧",
        "vanilla_event.WATERLOGGED_SCRIPTORIUM.pages.INITIAL.options.TENTACLE_QUILL" => "水浸写经室·使用触手羽毛笔",
        "vanilla_event.WELLSPRING.pages.INITIAL.options.BATHE" => "泉水·沐浴",
        "vanilla_event.WHISPERING_HOLLOW.pages.INITIAL.options.HUG" => "低语空谷·拥抱树木",
        _ when source.Id.StartsWith("fourth_route.", StringComparison.Ordinal) => "完成女神试炼并领取遗物",
        _ when source.Id.StartsWith("act_alignment.", StringComparison.Ordinal) =>
            delta > 0 ? "进入下一幕后选择堕落" : "进入下一幕后选择圣洁",
        _ => $"未登记的行为（{source.Id}）",
    };
}
