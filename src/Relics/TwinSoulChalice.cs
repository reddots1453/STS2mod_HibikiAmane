using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Data;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Acts;
using MaidenSuccubus.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MaidenSuccubus.Relics;
using MaidenSuccubus.RestSite;
using MaidenSuccubus.Patches;

namespace MaidenSuccubus.Relics;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class TwinSoulChalice : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(5), new MaxHpVar(5), new CardsVar(1)];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/neows_lament.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/neows_lament.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/neows_lament.tres");

    public override async Task BeforeCombatStart()
    {
        if (Owner.RunState is not RunState runState) return;
        if (!FourthRouteProgressService.TryGetQuest(runState, out _))
        {
            var rng = Owner.RunState.Rng.Niche;
            FourthRouteQuest dark = FourthRouteProgressService.DarkQuests[rng.NextInt(7)];
            FourthRouteQuest light = FourthRouteProgressService.LightQuests[rng.NextInt(7)];
            List<CardModel> choices = [CreateQuestChoice(dark), CreateQuestChoice(light)];
            FourthRouteQuestChoice? selected = await CardSelectCmd.FromChooseACardScreen(
                new BlockingPlayerChoiceContext(), choices, Owner, canSkip: false) as FourthRouteQuestChoice;
            if (selected != null && Enum.TryParse(selected.QuestId, out FourthRouteQuest quest))
                FourthRouteProgressService.SelectQuest(runState, quest);
        }
        await FourthRouteProgressService.CheckThresholdQuest(Owner);
    }

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead) return;
        Flash();
        if (CorruptionQuery.Get((RunState)Owner.RunState) >= 4)
            await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
        else
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);

        if (Owner.RunState is RunState runState
            && FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest))
        {
            if (quest == FourthRouteQuest.Pride && _.RoomType == RoomType.Elite)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            else if (quest == FourthRouteQuest.Lust && Data.Desire.Get(Owner) >= 5)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            else if (quest == FourthRouteQuest.Chastity && Data.Desire.Get(Owner) <= 2)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            else if (quest == FourthRouteQuest.Wrath && _.CombatState.RoundNumber <= 3)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            else if (quest == FourthRouteQuest.Patience && _.RoomType == RoomType.Monster)
                await FourthRouteProgressService.AddProgress(Owner, quest);
            await FourthRouteProgressService.CheckThresholdQuest(Owner);

            M5ProgressState progress = M5Progress.Handle.Get(runState);
            if (_.RoomType == RoomType.Boss && runState.CurrentActIndex == 2
                && progress.FourthRouteRelicStage == 3)
            {
                await FourthRouteProgressService.AdvanceStage(Owner, 3);
                FourthActRunAdapter.EnsurePresent(runState);
            }
        }
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost) =>
        player == Owner && entry is MerchantRelicEntry { Model: FourthRouteFragmentRelic } ? 199m : cost;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || player.RunState is not RunState runState) return false;
        M5ProgressState state = M5Progress.Handle.Get(runState);
        if (state.FourthRouteRelicStage != 2 || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest))
            return false;
        bool dark = FourthRouteProgressService.AlignmentOf(quest) == FourthRouteAlignment.Dark;
        if (!player.Deck.Cards.Any(card => dark ? Core.Routes.RouteCardQuery.IsHoly(card) : Core.Routes.RouteCardQuery.IsCorrupt(card)))
            return false;
        options.Add(new FourthRouteSacrificeOption(player));
        return true;
    }

    public override Task AfterPotionUsed(PotionModel potion, MegaCrit.Sts2.Core.Entities.Creatures.Creature? target) =>
        TrackSimple(FourthRouteQuest.Gluttony);

    public override Task AfterRestSiteHeal(Player player, bool isMimicked) =>
        player == Owner ? TrackSimple(FourthRouteQuest.Sloth) : Task.CompletedTask;

    public override async Task AfterRestSiteSmith(Player player)
    {
        if (player != Owner) return;
        if (Owner.RunState is RunState runState && FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
            && quest == FourthRouteQuest.Diligence)
            await FourthRouteProgressService.AddProgress(Owner, quest);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Owner != Owner || Owner.RunState is not RunState runState
            || !FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)) return;
        if (quest == FourthRouteQuest.Envy && oldPileType == PileType.Deck && card.Pile?.Type != PileType.Deck)
            await FourthRouteProgressService.AddProgress(Owner, quest);
        else if (quest == FourthRouteQuest.Benevolence && oldPileType == PileType.None && card.Pile?.Type == PileType.Deck)
            await FourthRouteProgressService.AddProgress(Owner, quest);
    }

    private Task TrackSimple(FourthRouteQuest expected)
    {
        if (Owner.RunState is RunState runState && FourthRouteProgressService.TryGetQuest(runState, out FourthRouteQuest quest)
            && quest == expected)
            return FourthRouteProgressService.AddProgress(Owner, quest);
        return Task.CompletedTask;
    }

    private FourthRouteQuestChoice CreateQuestChoice(FourthRouteQuest quest)
    {
        FourthRouteQuestChoice choice = Owner.RunState.CreateCard<FourthRouteQuestChoice>(Owner);
        choice.Configure(quest.ToString(), QuestName(quest), QuestText(quest));
        return choice;
    }

    private static string QuestName(FourthRouteQuest quest) => quest switch
    {
        FourthRouteQuest.Pride => "傲慢", FourthRouteQuest.Greed => "贪婪", FourthRouteQuest.Lust => "色欲",
        FourthRouteQuest.Envy => "嫉妒", FourthRouteQuest.Gluttony => "暴食", FourthRouteQuest.Wrath => "愤怒",
        FourthRouteQuest.Sloth => "懒惰", FourthRouteQuest.Humility => "谦逊", FourthRouteQuest.Generosity => "慷慨",
        FourthRouteQuest.Chastity => "贞洁", FourthRouteQuest.Benevolence => "仁爱", FourthRouteQuest.Temperance => "节制",
        FourthRouteQuest.Patience => "耐心", _ => "勤勉"
    };

    private static string QuestText(FourthRouteQuest quest) => quest switch
    {
        FourthRouteQuest.Pride => "进行3场精英战斗", FourthRouteQuest.Greed => "持有至少300金币",
        FourthRouteQuest.Lust => "欲望不低于5时结束3场战斗", FourthRouteQuest.Envy => "移除2张牌",
        FourthRouteQuest.Gluttony => "使用3瓶药水", FourthRouteQuest.Wrath => "第3回合结束前赢得4场战斗",
        FourthRouteQuest.Sloth => "在火堆休息2次", FourthRouteQuest.Humility => "升级2张初始牌",
        FourthRouteQuest.Generosity => "跳过1个宝箱", FourthRouteQuest.Chastity => "欲望不高于2时结束3场战斗",
        FourthRouteQuest.Benevolence => "向牌组加入5张牌", FourthRouteQuest.Temperance => "跳过2次卡牌奖励",
        FourthRouteQuest.Patience => "进行5场普通战斗", _ => "在火堆锻造3次"
    };

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner
            || room is not CombatRoom
            || CorruptionQuery.Get((RunState)player.RunState) > -4)
            return false;

        rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, Owner));
        Flash();
        return true;
    }
}
