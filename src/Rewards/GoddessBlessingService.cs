using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Relics;

namespace MaidenSuccubus.Rewards;

public enum GoddessBlessingSide
{
    Light,
    Dark,
}

public static class GoddessBlessingRegistry
{
    private static readonly Dictionary<GoddessBlessingSide, List<Func<RelicModel>>> Pools = new()
    {
        [GoddessBlessingSide.Light] = [],
        [GoddessBlessingSide.Dark] = [],
    };

    public static void Register<T>(GoddessBlessingSide side)
        where T : RelicModel =>
        Pools[side].Add(() => ModelDb.Relic<T>().ToMutable());

    public static RelicModel CreateReward(
        GoddessBlessingSide side,
        Player player)
    {
        // 正式内容加入池后使用 Run RNG；空池必须安全回退，不能阻塞流程。
        if (Pools[side].Count == 0)
        {
            bool isLight = side == GoddessBlessingSide.Light;
            string placeholder = isLight
                ? nameof(LightBlessingPlaceholder)
                : nameof(DarkBlessingPlaceholder);
            MaidenSuccubusMod.Logger.Warn(
                $"Goddess blessing pool '{side}' is empty; "
                + $"using placeholder {placeholder}.");
            return isLight
                ? ModelDb.Relic<LightBlessingPlaceholder>().ToMutable()
                : ModelDb.Relic<DarkBlessingPlaceholder>().ToMutable();
        }

        Func<RelicModel> selected =
            player.PlayerRng.Rewards.NextItem(Pools[side])
            ?? Pools[side][0];
        return selected();
    }
}

public sealed class GoddessBlessingReward : Reward
{
    private readonly GoddessBlessingSide _side;
    private readonly int _corruptionDelta;
    private readonly RelicModel _relic;

    protected override RewardType RewardType => RewardType.None;
    public override int RewardsSetIndex => 3;
    public override LocString Description => _relic.Title;
    public override bool IsPopulated => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => _relic.HoverTips;

    public GoddessBlessingReward(
        GoddessBlessingSide side,
        RelicModel relic,
        Player player)
        : base(player)
    {
        _side = side;
        _corruptionDelta = side == GoddessBlessingSide.Light ? -1 : 1;
        _relic = relic;
    }

    public override void Populate()
    {
    }

    public override TextureRect CreateIcon()
    {
        var icon = new TextureRect
        {
            Texture = _relic.BigIcon,
            Material = (ShaderMaterial)PreloadManager.Cache
                .GetMaterial("res://materials/ui/relic_mat.tres")
                .Duplicate(deep: true),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        };
        _relic.UpdateTexture(icon);
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return icon;
    }

    protected override async Task<bool> OnSelect()
    {
        await RelicCmd.Obtain(_relic, Player);
        CorruptionCmd.Modify(
            (RunState)Player.RunState,
            _corruptionDelta,
            new CorruptionChangeSource($"boss_blessing.{_side.ToString().ToLowerInvariant()}"));
        MaidenSuccubusMod.Logger.Info(
            $"Selected {_side} goddess blessing: {_relic.Id.Entry}, "
            + $"corruptionDelta={_corruptionDelta}.");
        return true;
    }

    public override void MarkContentAsSeen() =>
        SaveManager.Instance.MarkRelicAsSeen(_relic);
}

public static class GoddessBlessingService
{
    public static Task Offer(Player player)
    {
        var light = new GoddessBlessingReward(
            GoddessBlessingSide.Light,
            GoddessBlessingRegistry.CreateReward(GoddessBlessingSide.Light, player),
            player);
        var dark = new GoddessBlessingReward(
            GoddessBlessingSide.Dark,
            GoddessBlessingRegistry.CreateReward(GoddessBlessingSide.Dark, player),
            player);
        var linked = new LinkedRewardSet([light, dark], player);
        return RewardsCmd.OfferCustom(player, [linked]);
    }
}
