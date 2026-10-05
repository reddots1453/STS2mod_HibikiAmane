using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
using MegaCrit.Sts2.Core.Combat;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Characters.Starts;
using MaidenSuccubus.UI;
using MaidenSuccubus.Core.Temptation;
using CorruptionData = MaidenSuccubus.Data.Corruption;

namespace MaidenSuccubus.Characters;

// 骨架阶段：使用 Ironclad 的视觉资源作为占位，后续替换为自定义场景
// 3 开局（普通/圣女/魅魔）将通过自定义角色选择 UI 实现，不在骨架范围内
[RegisterCharacter]
public class MaidenSuccubusCharacter
    : ModCharacterTemplate<MSNeutralCardPool, MSRelicPool, MSPotionPool>,
      IMaidenSuccubusStartProfile
{
    private const string TopBarIconFallback =
        "res://scenes/ui/character_icons/ironclad_icon.tscn";
    private const string CharacterVisualsPath =
        "res://MaidenSuccubus/scenes/maiden_succubus_character.tscn";
    private const string MerchantScenePath =
        "res://MaidenSuccubus/scenes/maiden_succubus_merchant.tscn";
    private const string RestSiteScenePath =
        "res://MaidenSuccubus/scenes/maiden_succubus_rest_site.tscn";
    private const string CharacterSelectBgFallback =
        "res://scenes/screens/char_select/char_select_bg_ironclad.tscn";
    private static CharacterAssetProfile? _assetProfile;

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

    // 未替换的视觉资源继续回退到 Ironclad。顶栏读取 CharacterModel.Icon
    // （不是 IconTexture），因此通过 RitsuLib 的 Ui.IconPath 正式资产通道
    // 提供可加载的 Texture2D resource，由其运行时工厂生成 Icon Control。
    public override CharacterAssetProfile AssetProfile =>
        _assetProfile ??= CreateAssetProfile();

    private static CharacterAssetProfile CreateAssetProfile()
    {
        string iconPath = RuntimeTextureAssets.PrepareResource(
            "ui/core/hibiki_amane_character_icon_128.png",
            "user://maiden_succubus_character_icon.res",
            TopBarIconFallback);
        string outlinePath = RuntimeTextureAssets.PrepareResource(
            "ui/core/hibiki_amane_character_icon_outline_128.png",
            "user://maiden_succubus_character_icon_outline.res",
            iconPath);
        string characterSelectBgPath = RuntimeTextureAssets.PrepareResource(
            "ui/character_select/hibiki_amane_select_bg_v04_2561x1201.png",
            "user://maiden_succubus_character_select_bg_v04.res",
            CharacterSelectBgFallback);
        string characterSelectIconPath = RuntimeTextureAssets.PrepareResource(
            "ui/character_select/hibiki_amane_select_normal_v04_132x195.png",
            "user://maiden_succubus_character_select_icon_v04.res",
            iconPath);
        string characterSelectLockedIconPath = RuntimeTextureAssets.PrepareResource(
            "ui/character_select/hibiki_amane_select_locked_v04_132x195.png",
            "user://maiden_succubus_character_select_locked_icon_v04.res",
            characterSelectIconPath);

        // The two world scenes reference these fingerprinted binary resources. Preparing them
        // here keeps loose debug assets and packed releases on the same path
        // without relying on Godot's editor import database.
        RuntimeTextureAssets.PrepareResource(
            "character/hibiki_amane_merchant.png",
            "user://maiden_succubus_merchant_texture.res",
            iconPath);
        RuntimeTextureAssets.PrepareResource(
            "character/hibiki_amane_rest_site.png",
            "user://maiden_succubus_rest_site_texture.res",
            iconPath);

        return new CharacterAssetProfile(
            Scenes: new CharacterSceneAssetSet(
                VisualsPath: CharacterVisualsPath,
                MerchantAnimPath: MerchantScenePath,
                RestSiteAnimPath: RestSiteScenePath),
            Ui: new CharacterUiAssetSet(
                IconTexturePath: iconPath,
                IconOutlineTexturePath: outlinePath,
                IconPath: iconPath,
                CharacterSelectBgPath: characterSelectBgPath,
                CharacterSelectIconPath: characterSelectIconPath,
                CharacterSelectLockedIconPath: characterSelectLockedIconPath));
    }

    // 骨架阶段不实现时间线小故事
    public override bool RequiresEpochAndTimeline => false;

    // DOC-MVP-001：唯一普通开局，4打击、4防御、1张圣洁基础牌“变身”。
    [Obsolete("RitsuLib legacy override; migrate with the starter-registration milestone.")]
    protected override IEnumerable<StartingDeckEntry> StartingDeckEntries =>
    [
        new(typeof(MaidenStrike), 4),
        new(typeof(MaidenDefend), 4),
        new(typeof(Transform), 1),
        new(typeof(DarkElement), 1),
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
        if (!ReferenceEquals(player.Character, this))
        {
            return;
        }

        await Desire.ResolvePendingFirstTurnStun(choiceContext, player);
        // A route change or silent restore can lower the cap without a resource-change callback.
        await DesireResourceRules.RecheckMaximum(player, this);
        // Resumed/special entries may restore desire silently. Reconcile only
        // the derived status after a pending maximum penalty has lowered it.
        await DesireResourceRules.SyncWetPower(choiceContext, player, "player-turn-start");
        // Also repairs a combat loaded from a save created before hook wiring
        // was fixed. Initialize is idempotent and preserves saved use counts.
        await Temptation.Initialize(choiceContext, player);
        foreach (var enemy in player.Creature.CombatState?.Enemies ?? [])
        {
            if (enemy.Monster != null
                && EroticAttackCatalog.Get(enemy.Monster) != null)
            {
                await IntentAdapterRegistry.Initialize(enemy.Monster);
                IntentMoveFactory.TryApplyNaturalErotic(enemy.Monster, player);
            }
        }
    }

    public override async Task BeforeCombatStart()
    {
        var combatState = CombatManager.Instance.DebugOnlyGetState();
        if (combatState == null
            || !combatState.Players.Any(player =>
                ReferenceEquals(player.Character, this)))
        {
            return;
        }

        foreach (var enemy in combatState.Enemies)
        {
            if (enemy.Monster != null
                && EroticAttackCatalog.Get(enemy.Monster) != null)
            {
                await IntentAdapterRegistry.Initialize(enemy.Monster);
            }
        }
        Player? player = combatState.Players.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Character, this));
        if (player != null)
        {
            // Lifecycle listeners restore this earlier in normal setup. Repeat
            // the operation at the character hook (it is idempotent) because
            // this is the first point where every combat player state is
            // guaranteed to exist, including resumed and unusual encounters.
            DesirePersistenceCoordinator.RestoreForCombat(
                player,
                combatState);
            // RitsuLib restores with emit:false: no resource-change hook runs.
            // Await status restoration here instead of starting a fire-and-forget
            // lifecycle task that could race the first enemy intent check.
            await DesireResourceRules.SyncWetPower(
                new ThrowingPlayerChoiceContext(), player, "combat-start");
            await Temptation.Initialize(
                new ThrowingPlayerChoiceContext(), player);
            if (!player.Creature.HasPower<TakemikazuchiTrackerPower>())
            {
                await PowerCmd.Apply<TakemikazuchiTrackerPower>(
                    new ThrowingPlayerChoiceContext(),
                    player.Creature,
                    1,
                    player.Creature,
                    null,
                    silent: true);
            }
        }
    }

    public override async Task AfterCreatureAddedToCombat(
        MegaCrit.Sts2.Core.Entities.Creatures.Creature creature)
    {
        if (creature.Monster != null
            && EroticAttackCatalog.Get(creature.Monster) != null)
        {
            await IntentAdapterRegistry.Initialize(creature.Monster);
        }
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
        if (corruption == CorruptionData.Max)
        {
            await Desire.Modify(player, 1);
        }
    }

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        MaidenSuccubusCreatureVisuals.TryCreate()
        ?? RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            CharacterVisualsPath);
}
