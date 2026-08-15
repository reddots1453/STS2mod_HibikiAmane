using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using MaidenSuccubus.Data;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Relics;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Keywords;
using MegaCrit.Sts2.Core.Rooms;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Characters.Starts;
using CorruptionData = MaidenSuccubus.Data.Corruption;

namespace MaidenSuccubus.Characters;

// 骨架阶段：使用 Ironclad 的视觉资源作为占位，后续替换为自定义场景
// 3 开局（普通/圣女/魅魔）将通过自定义角色选择 UI 实现，不在骨架范围内
[RegisterCharacter]
public class MaidenSuccubusCharacter
    : ModCharacterTemplate<MSNeutralCardPool, MSRelicPool, MSPotionPool>,
      IMaidenSuccubusStartProfile
{
    public virtual MaidenSuccubusStartProfileId StartProfileId =>
        MaidenSuccubusStartProfileId.Normal;
    public virtual int InitialCorruption => 0;
    public CharacterAssetProfile StartAssetProfile => AssetProfile;
    // 主题色：紫粉（圣女与魅魔的中间色）
    public override Color NameColor => new(0.85f, 0.5f, 0.85f);
    public override Color EnergyLabelOutlineColor => new(0.85f, 0.5f, 0.85f);
    public override Color MapDrawingColor => new(0.85f, 0.5f, 0.85f);

    public override CharacterGender Gender => CharacterGender.Feminine;

    public override int StartingHp => 60;
    public override int StartingGold => 99;

    // 占位：继承 Ironclad 的所有视觉资源（模型、能量表盘、商店、火堆等）
    // 后续将逐步替换为 res://MaidenSuccubus/scenes/... 自定义场景
    public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Ironclad();

    // 骨架阶段不实现时间线小故事
    public override bool RequiresEpochAndTimeline => false;

    // DOC-MVP-001：唯一普通开局，4打击、4防御、1张圣洁基础牌“变身”。
    [Obsolete("RitsuLib legacy override; migrate with the starter-registration milestone.")]
    protected override IEnumerable<StartingDeckEntry> StartingDeckEntries =>
    [
        new(typeof(MaidenStrike), 4),
        new(typeof(MaidenDefend), 4),
        new(typeof(Transform), 1),
    ];

    // 起始遗物占位：燃烧之血
    // 后续将替换为 MS 自有遗物（根据开局路线不同给予不同初始遗物）
    [Obsolete("RitsuLib legacy override; migrate with the starter-registration milestone.")]
    protected override IEnumerable<Type> StartingRelicTypes =>
    [
        typeof(TwinSoulChalice),
    ];

    // 攻击和施法动画延迟（骨架阶段用 0，后续根据动画调整）
    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;

    // 攻击建筑师的攻击特效列表（骨架阶段沿用 Ironclad 的特效）
    public override List<string> GetArchitectAttackVfx() => [
        "vfx/vfx_attack_blunt",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter"
    ];

    public override async Task AfterPlayerTurnStartEarly(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player.Character is not MaidenSuccubusCharacter)
        {
            return;
        }

        await Desire.ResolvePendingFirstTurnStun(choiceContext, player);
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        MegaCrit.Sts2.Core.Entities.Cards.PileType oldPileType,
        AbstractModel? source)
    {
        PortableHandTracker.ObservePileChange(card);
        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        Player? player = room.CombatState.Players.FirstOrDefault(
            candidate => ReferenceEquals(candidate.Character, this));
        if (player?.RunState is not RunState runState)
        {
            return;
        }

        int corruption = CorruptionQuery.Get(runState);
        if (corruption == CorruptionData.Min)
        {
            await Desire.Modify(player, -1);
        }
        else if (corruption == CorruptionData.Max)
        {
            await Desire.Modify(player, 1);
        }
    }

    // 自动转换人物场景（教程标准写法，复制即可）
    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);
}
