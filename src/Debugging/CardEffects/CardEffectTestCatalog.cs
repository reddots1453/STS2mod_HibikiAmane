#if DEBUG
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MaidenSuccubus.Cards;
using MaidenSuccubus.Cards.Curses;
using MaidenSuccubus.Cards.Scriptures;
using MaidenSuccubus.Commands;
using MaidenSuccubus.Core.Corruption;
using MaidenSuccubus.Core.Control;
using MaidenSuccubus.Core.Desire;
using MaidenSuccubus.Core.Intents;
using MaidenSuccubus.Data;
using MaidenSuccubus.Powers;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>
/// Executable 2026-08-24 card oracle.  Numeric literals come from DesignDoc,
/// not from the card's DynamicVars, so implementation and expectation cannot
/// silently change together.
/// </summary>
internal static class CardEffectTestCatalog
{
    private static readonly List<CardEffectSpec> _specs = [];

    public static IReadOnlyList<CardEffectSpec> All { get; } = Build();

    private static IReadOnlyList<CardEffectSpec> Build()
    {
        RegisterNeutral();
        RegisterCorrupt();
        RegisterHoly();
        RegisterInvasionCurses();
        RegisterGenerated();
        return _specs.OrderBy(spec => spec.CardId, StringComparer.Ordinal).ToArray();
    }

    private static void RegisterNeutral()
    {
        Draw<AcceleratedMotion>(2, 3);
        BalanceBladeProbe();
        BalanceShieldProbe();
        BasicTrainingProbe();
        BathProbe();
        BeyondReasonForgeProbe();
        BindingInsightProbe();
        BorrowedForceStrikeProbe();
        HandDiscountProbe<BurningBracelet>(14, 20);
        DamageGenerate<CounterBarrier, CounterBarrierII>(8, 8, PileType.Discard);
        DamageTargetPower<CycloneRupture>(5, 8, "ShatterPower", 1, 1);
        Block<DoubleDefense>(12, 16);
        Pending<DreamMist>("DesignDoc: 效果正文暂未确定（用户确认暂时跳过）");
        DamageTargetPower<ExplosiveImpact>(6, 10, "ShatterPower", 2, 2);
        DamageTargetPower<FlameBloom>(7, 10, "BurningPower", 2, 2);
        DamageGenerate<FlashStab, FlashStab>(6, 8, PileType.Draw);
        HandDiscountProbe<FrozenBracelet>(12, 16);
        GenerateChosenCard<MaidenSuccubus.Cards.Fusion>();
        GoddessOfIceProbe();
        HealingArtProbe();
        DamageGenerate<IceBreakingSlash, IceShard>(7, 9, PileType.Hand,
            generatedUpgradedWithSource: true);
        Generate<IceShield, IceShard>(PileType.Hand, 3, 3,
            generatedUpgradedWithSource: true);
        JudgmentBladeProbe();
        DamageBlock<LightArrow>(5, 5, 5, 5);
        LightningRecoilProbe();
        LullabyProbe();
        MagiciansSecretProbe();
        MagicIndexProbe();
        MagicResonanceProbe();
        MagicSwordProbe();
        MentalUnityProbe();
        MindsEyeProbe();
        ObstructingShotProbe();
        ProcrastinateProbe();
        BlockSelfPower<RepairAlyssa>(4, 4, "MagicArmorPower", 1, 2);
        BlockSelfPower<SteadyGuard>(17, 21, "MagicArmorPower", 1, 1);
        StudyPlanProbe();
        SurfProbe();
        SwordVerdictProbe();
        UltimateFlareProbe();
    }

    private static void RegisterCorrupt()
    {
        AbnormalAdaptationProbe();
        AllCurseBiteProbe();
        AllHopeLostProbe();
        BerserkerMaskProbe();
        BiteInvaderProbe();
        BlackVortexProbe();
        DesireAndSelfPowers<BlasphemousDesire>(5, 7,
            ("StrengthPower", -3, -3), ("RestoreStrengthAtTurnEndPower", 3, 3));
        Damage<BlasphemousTwilight>(25, 36);
        DamageAndExhaustSelection<BurningBladeRitual>(10, 13);
        BurningDesireProbe();
        ChainDestructionProbe();
        ChangePantiesProbe();
        CoronationProbe();
        CurseInfectionProbe();
        CurseWedgeProbe();
        DarkElementProbe();
        DarkFlameBarrierProbe();
        DarkPunishmentProbe();
        DamageAllTargetPower<DarkStorm>(9, 11, "VulnerablePower", 1, 2);
        DamageDraw<DarkThrust>(8, 11, 2, 2);
        GenerateChosenCard<DemonStaff>();
        DesireRecycleProbe();
        Damage<DesireWhip>(7, 7);
        DrawAndExhaustSelection<DestructionReaction>(3, 4);
        DesireDraw<EcstasyDew>(2, 3, 1, 1);
        BlockGenerate<Exhibitionist, NakedDesireStatus>(18, 24, PileType.Hand);
        ExposePlayProbe();
        AllTargetPower<FearAura>("FearAuraStrengthLossPower", 3, 4);
        DamageAndSelectedPileMove<FinalSlash>(11, 13, 1, 2, PileType.Draw, PileType.Discard);
        DrawEnergy<FleetingYears>(2, 3, 2, 2);
        FullOfOpeningsProbe();
        IgniteProbe();
        InsatiableGreedProbe();
        LastStandProbe();
        LegendaryMinerProbe();
        DamageTargetPower<LightningKick>(10, 12, "ShatterPower", 3, 4);
        LordOfBlazeProbe();
        LoversDaggerProbe();
        TargetPower<LureDeep>("ControlPower", 3, 2);
        MagicExcessProbe();
        MasochisticGirlProbe();
        MasochisticTranceProbe();
        MentalStabilizerProbe();
        Energy<MiasmaAbsorption>(2, 3);
        DrawSelected<MiasmaAffinity>(1, 2, PileType.Draw, PileType.Hand,
            extra: AssertSelectedCardsEthereal);
        ExhaustTypesForPower<MiasmaConversion>();
        DamageAllTargetPower<MiasmaFlame>(8, 10, "BurningPower", 2, 2);
        ExhaustSelectionGenerateCopies<MimicProliferation>(2, 3);
        Draw<PlayingWithFire>(2, 3, extra: AssertSelfBurning);
        BlockDrawGenerate<PleasureDrowning, ArousalStatus>(7, 7, 2, 2, 2,
            PileType.Draw);
        PleasureGardenProbe();
        SelfPowers<PriceOfStrength>(("StrengthPower", 4, 4), ("ShatterPower", 4, 4));
        RecollectionRoomProbe();
        ReflectiveBarrierProbe();
        DamageAndTopDeckExhaust<SacrificialFrenzy>(12, 16, 3, 4, 6);
        SemenAppetiteProbe();
        SemenConversionProbe();
        SharpForgeProbe();
        TargetPowers<SmallFry>(("VulnerablePower", 3, 4), ("StrengthPower", 1, 1));
        SuperRegenerationProbe();
        TentacleArmorProbe();
        ThousandCurseScytheProbe();
        BlockGenerate<WinterHolly, WinterHolly>(10, 13, PileType.Hand,
            extra: AssertGeneratedCopyEthereal);
    }

    private static void RegisterHoly()
    {
        DiscardAutoPlayBlockProbe();
        DiscardAutoPlayDamageProbe();
        BattleTechniqueReplayProbe();
        BlizzardProbe();
        DrawTargetPower<CalmingMist>(1, 2, "WeakPower", 2, 2);
        GenerateChosenCard<Chant>();
        ChastityDefenseProbe();
        ConsecrationProbe();
        DesireWardProbe();
        BlockSelfPower<DevoutBulwark>(12, 15, "WeakPower", 2, 2);
        DivineEchoProbe();
        DragonflyTouchProbe();
        EternalDamnationProbe();
        FinalJudgmentProbe();
        FocusedSlashProbe();
        SelectedEnchant<ForgeCharge>("Charge", 2, 3, fromPile: PileType.Discard);
        ForgeNimbleProbe();
        TransformHand<Gospel>();
        HolyCurseProbe();
        DamageAndTransformDraw<HolyPunishment>(9, 12);
        HolyRadianceProbe();
        HolyResonanceProbe();
        InwardDisciplineProbe();
        DamageTargetPower<Judgment>(7, 7, "CondemnationPower", 2, 3);
        LightPowerReleaseProbe();
        MagicBurstProbe();
        MemoryImprintProbe();
        BlockGenerate<MomentaryGrace, IceMist>(6, 8, PileType.Hand,
            generatedUpgradedWithSource: true);
        MultipleReproductionProbe();
        NoLewdnessProbe();
        OriginalSinBrandProbe();
        DamageSelfPower<PenanceSlash>(14, 17, "FrailPower", 2, 2);
        PhotonVoltProbe();
        RegenerativeMagicFiberProbe();
        ResistanceGlovesProbe();
        RestProbe();
        RestraintEvasionProbe();
        Block<RetainedGuard>(8, 11);
        EnergySelfPower<SneakSnack>(2, 3, "CondemnationPower", 2, 2);
        SoulImpactProbe();
        SoulPurificationProbe();
        StigmaProbe();
        SunDanceProbe();
        TacticalAnalyzerProbe();
        TacticalCoreProbe();
        TerminalSanctuaryProbe();
        DesireDraw<Tranquilizer>(-2, -3, 1, 1, initialDesire: 5);
        TransformProbe();
        DamageAndSelectedPileMove<WindRumor>(9, 11, 1, 2, PileType.Discard, PileType.Draw);
        WorshipProbe();
        YarusMemoryProbe();
    }

    private static void RegisterInvasionCurses()
    {
        CurseDesire<AphrodisiacCurse>(2);
        CurseSelfPowers<CorrosiveSlimeCurse>(("VulnerablePower", 1), ("FrailPower", 1));
        CurseArmorLoss<DeepSeaSlimeCurse>(1);
        CurseNextTurnEnergyLoss<EctoplasmResidueCurse>(1);
        CurseRandomPower<ExperimentalLiquidCurse>();
        CurseSelfPower<FoulSlimeCurse>("VulnerablePower", 1);
        CurseCardGeneration<InkFluidCurse>(AssertRandomOtherCardEthereal);
        CurseGenerate<InsectEggCurse, ArousalStatus>(PileType.Draw, 1);
        CurseGenerate<MagicResidueCurse, Slimed>(PileType.Draw, 1);
        CurseSelfPower<ParalyticSlimeCurse>("FrailPower", 1);
        CurseHp<ParasiticEggCurse>(-3);
        CurseHp<RoyalEssenceCurse>(5, startMissingHp: 10);
        CurseSelfPower<ScorchingFluidCurse>("BurningPower", 2);
        CurseRemoved<SemenCurse>();
        CurseRemoved<SludgeSemenCurse>();
        CurseSelfPower<SporeMucusCurse>("WeakPower", 1);
        CurseGenerate<VineSeedCurse, SporeMind>(PileType.Draw, 1,
            generatedTypeMayBeUnavailable: true);
    }

    private static void RegisterGenerated()
    {
        DrawTriggerDesire<ArousalStatus>(1);
        Scripture<BlissScripture>("BlissScripturePower", 2, 3);
        ConditionalDrawEnergy<CalmMind>(2, 3, 2, 3, minimumHand: 6);
        Pending<ClimaxBanCurse>("DesignDoc: 效果待后续设计");
        DamageGenerate<CounterBarrierII, CounterBarrierIII>(16, 16, PileType.Discard,
            notUpgradable: true);
        DamageGenerate<CounterBarrierIII, CounterBarrierIV>(32, 32, PileType.Discard,
            notUpgradable: true);
        Damage<CounterBarrierIV>(200, 200, notUpgradable: true);
        EmptyDescription<DrowsyStatus>();
        ConfigureEnchantmentChoice();
        ConfigureQuestChoice();
        HandCostRestriction<GagCurse>(CardType.Skill, 1);
        Scripture<GuardianScripture>("GuardianScripturePower", 2, 3);
        SelectedCardDouble<HumilityLesson>();
        Pending<HypnosisCurse>("DesignDoc: 效果待后续设计");
        IceMistProbe();
        Block<IceShard>(3, 4);
        HandPlayRestriction<InfatuationCurse>(CardType.Attack);
        EndTurnGenerate<LewdMarkCompleteCurse, ArousalStatus>(2);
        EndTurnGenerate<LewdMarkMinorCurse, ArousalStatus>(1);
        EndTurnGenerate<LewdMarkSpreadCurse, ArousalStatus>(2);
        Block<MaidenDefend>(5, 8);
        Damage<MaidenStrike>(6, 9);
        EndTurnArmorLoss<NakedDesireStatus>(1);
        Scripture<NimbleScripture>("NimbleScripturePower", 2, 3);
        ConfigureOverdraftChoice<OverdraftAcceptChoice>(accept: true);
        ConfigureOverdraftChoice<OverdraftDeclineChoice>(accept: false);
        Scripture<PunishmentScripture>("PunishmentScripturePower", 2, 3);
        ConfigureStigmaChoice<StigmaCondemnationChoice>("CondemnationPower");
        ConfigureStigmaChoice<StigmaTargetChoice>("target");
        ConfigureStigmaChoice<StigmaWeakChoice>("WeakPower");
        HandEtherealProjection<TransparentOutfitCurse>();
        Scripture<VitalityScripture>("VitalityScripturePower", 2, 3);
        Scripture<WisdomScripture>("WisdomScripturePower", 2, 3);
    }

    // Generic executable probes -------------------------------------------------

    private static void Damage<T>(int baseDamage, int upgradedDamage,
        Action<CardEffectTestContext, CardModel>? extra = null,
        bool notUpgradable = false) where T : CardModel =>
        Variants<T>(async (ctx, card, expected) =>
        {
            int before = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, before, expected);
            extra?.Invoke(ctx, card);
        }, baseDamage, upgradedDamage, extra == null ? 1 : 2, notUpgradable);

    private static void DamageAll<T>(int baseDamage, int upgradedDamage,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        Variants<T>(async (ctx, card, expected) =>
        {
            Dictionary<Creature, int> before = ctx.Enemies.ToDictionary(enemy => enemy, enemy => enemy.CurrentHp);
            await ctx.Play(card);
            foreach ((Creature enemy, int hp) in before)
                ctx.AssertDamage($"damage {enemy.Name}", enemy, hp, expected);
            extra?.Invoke(ctx, card);
        }, baseDamage, upgradedDamage, 1 + (extra == null ? 0 : 1));

    private static void Block<T>(int baseBlock, int upgradedBlock,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        Variants<T>(async (ctx, card, expected) =>
        {
            int before = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", before, expected);
            extra?.Invoke(ctx, card);
        }, baseBlock, upgradedBlock, 1 + (extra == null ? 0 : 1));

    private static void Heal<T>(int baseHeal, int upgradedHeal) where T : CardModel =>
        Variants<T>(async (ctx, card, expected) =>
        {
            await MegaCrit.Sts2.Core.Commands.CreatureCmd.SetCurrentHp(ctx.Self, ctx.Self.MaxHp - 20);
            int before = ctx.Self.CurrentHp;
            await ctx.Play(card);
            ctx.AssertEqual("healing", expected, ctx.Self.CurrentHp - before);
        }, baseHeal, upgradedHeal);

    private static void Draw<T>(int baseCards, int upgradedCards,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        Variants<T>(async (ctx, card, expected) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 10);
            int before = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, before, expected);
            extra?.Invoke(ctx, card);
        }, baseCards, upgradedCards, 1 + (extra == null ? 0 : 1));

    private static void DrawEnergy<T>(int baseDraw, int upgradedDraw,
        int baseEnergy, int upgradedEnergy,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        VariantPairs<T>(async (ctx, card, draw, energy) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 10);
            int handBefore = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            int energyBefore = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, handBefore, draw);
            ctx.AssertEqual("energy gained", energy, ctx.Player.PlayerCombatState.Energy - energyBefore);
            extra?.Invoke(ctx, card);
        }, (baseDraw, baseEnergy), (upgradedDraw, upgradedEnergy), 2 + (extra == null ? 0 : 1));

    private static void DamageEnergy<T>(int baseDamage, int upgradedDamage,
        int baseEnergy, int upgradedEnergy,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        VariantPairs<T>(async (ctx, card, damage, energy) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int energyBefore = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, damage);
            ctx.AssertEqual("energy gained", energy, ctx.Player.PlayerCombatState.Energy - energyBefore);
            extra?.Invoke(ctx, card);
        }, (baseDamage, baseEnergy), (upgradedDamage, upgradedEnergy), 2 + (extra == null ? 0 : 1));

    private static void Energy<T>(int baseEnergy, int upgradedEnergy) where T : CardModel =>
        Variants<T>(async (ctx, card, expected) =>
        {
            int before = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card);
            ctx.AssertEqual("energy gained", expected, ctx.Player.PlayerCombatState.Energy - before);
        }, baseEnergy, upgradedEnergy);

    private static void DamageDraw<T>(int baseDamage, int upgradedDamage,
        int baseDraw, int upgradedDraw,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        VariantPairs<T>(async (ctx, card, damage, draw) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 12);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, damage);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand, draw);
            extra?.Invoke(ctx, card);
        }, (baseDamage, baseDraw), (upgradedDamage, upgradedDraw), 2 + (extra == null ? 0 : 1));

    private static void BlockDraw<T>(int baseBlock, int upgradedBlock,
        int baseDraw, int upgradedDraw,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        VariantPairs<T>(async (ctx, card, block, draw) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 12);
            int blockBefore = ctx.Self.Block;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertBlock("block", blockBefore, block);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand, draw);
            extra?.Invoke(ctx, card);
        }, (baseBlock, baseDraw), (upgradedBlock, upgradedDraw), 2 + (extra == null ? 0 : 1));

    private static void DamageBlock<T>(int baseDamage, int upgradedDamage,
        int baseBlock, int upgradedBlock,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        VariantPairs<T>(async (ctx, card, damage, block) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int blockBefore = ctx.Self.Block;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, damage);
            ctx.AssertBlock("block", blockBefore, block);
            extra?.Invoke(ctx, card);
        }, (baseDamage, baseBlock), (upgradedDamage, upgradedBlock), 2 + (extra == null ? 0 : 1));

    private static void SelfPower<T>(string power, int baseAmount, int upgradedAmount,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        Power<T>(self: true, power, baseAmount, upgradedAmount, extra);

    private static void TargetPower<T>(string power, int baseAmount, int upgradedAmount,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        Power<T>(self: false, power, baseAmount, upgradedAmount, extra);

    private static void Power<T>(bool self, string power, int baseAmount, int upgradedAmount,
        Action<CardEffectTestContext, CardModel>? extra) where T : CardModel =>
        Variants<T>(async (ctx, card, expected) =>
        {
            Creature target = self ? ctx.Self : ctx.PrimaryEnemy;
            await ctx.Play(card, self ? null : target);
            ctx.AssertPower(power, target, power, expected);
            extra?.Invoke(ctx, card);
        }, baseAmount, upgradedAmount, 1 + (extra == null ? 0 : 1));

    private static void SelfPowers<T>(params (string Power, int Base, int Upgraded)[] powers)
        where T : CardModel => TargetedPowers<T>(self: true, powers, null);

    private static void TargetPowers<T>(
        (string Power, int Base, int Upgraded) first,
        (string Power, int Base, int Upgraded) second,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        TargetedPowers<T>(self: false, [first, second], extra);

    private static void TargetedPowers<T>(bool self,
        (string Power, int Base, int Upgraded)[] powers,
        Action<CardEffectTestContext, CardModel>? extra) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            Creature target = self ? ctx.Self : ctx.PrimaryEnemy;
            await ctx.Play(card, self ? null : target);
            foreach (var power in powers)
                ctx.AssertPower(power.Power, target, power.Power, upgraded ? power.Upgraded : power.Base);
            extra?.Invoke(ctx, card);
        }, powers.Length + (extra == null ? 0 : 1));

    private static void DamageTargetPower<T>(int baseDamage, int upgradedDamage,
        string power, int basePower, int upgradedPower,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? upgradedDamage : baseDamage);
            ctx.AssertPower(power, ctx.PrimaryEnemy, power, upgraded ? upgradedPower : basePower);
            extra?.Invoke(ctx, card);
        }, 2 + (extra == null ? 0 : 1));

    private static void DamageSelfPower<T>(int baseDamage, int upgradedDamage,
        string power, int basePower, int upgradedPower) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? upgradedDamage : baseDamage);
            ctx.AssertPower(power, ctx.Self, power, upgraded ? upgradedPower : basePower);
        }, 2);

    private static void BlockSelfPower<T>(int baseBlock, int upgradedBlock,
        string power, int basePower, int upgradedPower,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int before = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", before, upgraded ? upgradedBlock : baseBlock);
            ctx.AssertPower(power, ctx.Self, power, upgraded ? upgradedPower : basePower);
            extra?.Invoke(ctx, card);
        }, 2 + (extra == null ? 0 : 1));

    private static void DrawSelfPower<T>(int baseDraw, int upgradedDraw,
        string power, int basePower, int upgradedPower) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 10);
            int before = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, before,
                upgraded ? upgradedDraw : baseDraw);
            ctx.AssertPower(power, ctx.Self, power, upgraded ? upgradedPower : basePower);
        }, 2);

    private static void DrawTargetPower<T>(int baseDraw, int upgradedDraw,
        string power, int basePower, int upgradedPower) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 10);
            int before = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, before,
                upgraded ? upgradedDraw : baseDraw);
            ctx.AssertPower(power, ctx.PrimaryEnemy, power,
                upgraded ? upgradedPower : basePower);
        }, 2);

    private static void EnergySelfPower<T>(int baseEnergy, int upgradedEnergy,
        string power, int basePower, int upgradedPower) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int before = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card);
            ctx.AssertEqual("energy gained", upgraded ? upgradedEnergy : baseEnergy,
                ctx.Player.PlayerCombatState.Energy - before);
            ctx.AssertPower(power, ctx.Self, power, upgraded ? upgradedPower : basePower);
        }, 2);

    private static void DamageAllTargetPower<T>(int baseDamage, int upgradedDamage,
        string power, int basePower, int upgradedPower,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            Dictionary<Creature, int> before = ctx.Enemies.ToDictionary(e => e, e => e.CurrentHp);
            await ctx.Play(card);
            foreach ((Creature enemy, int hp) in before)
            {
                ctx.AssertDamage($"damage {enemy.Name}", enemy, hp, upgraded ? upgradedDamage : baseDamage);
                ctx.AssertPower(power, enemy, power, upgraded ? upgradedPower : basePower);
            }
            extra?.Invoke(ctx, card);
        }, 2 + (extra == null ? 0 : 1));

    private static void AllTargetPower<T>(string power, int basePower, int upgradedPower)
        where T : CardModel => CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            foreach (Creature enemy in ctx.Enemies)
                ctx.AssertPower(power, enemy, power, upgraded ? upgradedPower : basePower);
        }, 1);

    private static void DamageGenerate<TCard, TGenerated>(int baseDamage, int upgradedDamage,
        PileType pile, bool generatedUpgradedWithSource = false, bool notUpgradable = false)
        where TCard : CardModel where TGenerated : CardModel =>
        CustomVariants<TCard>(async (ctx, card, upgraded) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int before = ctx.CountCards<TGenerated>(pile);
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? upgradedDamage : baseDamage);
            ctx.AssertPileDelta<TGenerated>("generated card", pile, before, 1);
            if (generatedUpgradedWithSource)
            {
                TGenerated? generated = pile.GetPile(ctx.Player).Cards.OfType<TGenerated>().FirstOrDefault();
                ctx.AssertEqual("generated upgrade state", upgraded, generated?.IsUpgraded == true);
            }
        }, 2 + (generatedUpgradedWithSource ? 1 : 0), notUpgradable);

    private static void Generate<TCard, TGenerated>(PileType pile, int baseCount, int upgradedCount,
        bool generatedUpgradedWithSource = false,
        Action<CardEffectTestContext, CardModel>? extra = null)
        where TCard : CardModel where TGenerated : CardModel =>
        CustomVariants<TCard>(async (ctx, card, upgraded) =>
        {
            int before = ctx.CountCards<TGenerated>(pile);
            await ctx.Play(card);
            ctx.AssertPileDelta<TGenerated>("generated cards", pile, before,
                upgraded ? upgradedCount : baseCount);
            if (generatedUpgradedWithSource)
            {
                bool allMatch = pile.GetPile(ctx.Player).Cards.OfType<TGenerated>()
                    .All(generated => generated.IsUpgraded == upgraded);
                ctx.AssertTrue("generated upgrade state", allMatch);
            }
            extra?.Invoke(ctx, card);
        }, 1 + (generatedUpgradedWithSource ? 1 : 0) + (extra == null ? 0 : 1));

    private static void BlockGenerate<TCard, TGenerated>(int baseBlock, int upgradedBlock,
        PileType pile, bool generatedUpgradedWithSource = false,
        Action<CardEffectTestContext, CardModel>? extra = null)
        where TCard : CardModel where TGenerated : CardModel =>
        CustomVariants<TCard>(async (ctx, card, upgraded) =>
        {
            int block = ctx.Self.Block;
            int count = ctx.CountCards<TGenerated>(pile);
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? upgradedBlock : baseBlock);
            ctx.AssertPileDelta<TGenerated>("generated card", pile, count, 1);
            if (generatedUpgradedWithSource)
            {
                TGenerated? generated = pile.GetPile(ctx.Player).Cards.OfType<TGenerated>().FirstOrDefault();
                ctx.AssertEqual("generated upgrade state", upgraded, generated?.IsUpgraded == true);
            }
            extra?.Invoke(ctx, card);
        }, 2 + (generatedUpgradedWithSource ? 1 : 0) + (extra == null ? 0 : 1));

    private static void BlockDrawGenerate<TCard, TGenerated>(
        int baseBlock, int upgradedBlock, int baseDraw, int upgradedDraw,
        int generatedCount, PileType generatedPile)
        where TCard : CardModel where TGenerated : CardModel =>
        CustomVariants<TCard>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 10);
            int block = ctx.Self.Block;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            int generated = ctx.CountCards<TGenerated>(generatedPile);
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? upgradedBlock : baseBlock);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand,
                upgraded ? upgradedDraw : baseDraw);
            ctx.AssertPileDelta<TGenerated>("generated cards", generatedPile, generated,
                generatedCount);
        }, 3);

    private static void DamageAllGenerate<TCard, TGenerated>(int baseDamage, int upgradedDamage,
        PileType pile, Action<CardEffectTestContext, CardModel>? extra = null)
        where TCard : CardModel where TGenerated : CardModel =>
        CustomVariants<TCard>(async (ctx, card, upgraded) =>
        {
            Dictionary<Creature, int> before = ctx.Enemies.ToDictionary(e => e, e => e.CurrentHp);
            int generated = ctx.CountCards<TGenerated>(pile);
            await ctx.Play(card);
            foreach ((Creature enemy, int hp) in before)
                ctx.AssertDamage($"damage {enemy.Name}", enemy, hp, upgraded ? upgradedDamage : baseDamage);
            ctx.AssertPileDelta<TGenerated>("generated card", pile, generated, 1);
            extra?.Invoke(ctx, card);
        }, 2 + (extra == null ? 0 : 1));

    private static void DesireDraw<T>(int baseDesire, int upgradedDesire,
        int baseDraw, int upgradedDraw, int initialDesire = 0) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            await Desire.Set(ctx.Player, initialDesire);
            await ctx.AddFillerCards(PileType.Draw, 10);
            int desire = Desire.Get(ctx.Player);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertEqual("desire delta", upgraded ? upgradedDesire : baseDesire,
                Desire.Get(ctx.Player) - desire);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand,
                upgraded ? upgradedDraw : baseDraw);
        }, 2);

    private static void DesireAndSelfPowers<T>(int baseDesire, int upgradedDesire,
        params (string Power, int Base, int Upgraded)[] powers) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int before = Desire.Get(ctx.Player);
            await ctx.Play(card);
            ctx.AssertEqual("desire delta", upgraded ? upgradedDesire : baseDesire,
                Desire.Get(ctx.Player) - before);
            foreach (var power in powers)
                ctx.AssertPower(power.Power, ctx.Self, power.Power, upgraded ? power.Upgraded : power.Base);
        }, 1 + powers.Length);

    private static void ConditionalDrawEnergy<T>(int baseDraw, int upgradedDraw,
        int baseEnergy, int upgradedEnergy, int minimumHand) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            T belowThreshold = (T)ctx.Create(typeof(T), upgraded);
            int lowEnergy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(belowThreshold);
            ctx.AssertEqual("no energy below hand threshold", 0,
                ctx.Player.PlayerCombatState.Energy - lowEnergy);

            await ctx.AddFillerCards(PileType.Hand, minimumHand - 1);
            await ctx.AddFillerCards(PileType.Draw, 10);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            int energy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card);
            ctx.AssertPileDelta<StrikeIronclad>("conditional cards drawn", PileType.Hand, hand,
                upgraded ? upgradedDraw : baseDraw);
            ctx.AssertEqual("conditional energy gained", upgraded ? upgradedEnergy : baseEnergy,
                ctx.Player.PlayerCombatState.Energy - energy);
        }, 3);

    private static void Scripture<T>(string powerName, int baseDuration, int upgradedDuration)
        where T : CardModel => CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int duration = upgraded ? upgradedDuration : baseDuration;
            if (card is WisdomScripture or BlissScripture)
                await ctx.AddFillerCards(PileType.Draw, duration + 1);
            if (card is BlissScripture)
                await MaidenSuccubus.Data.Desire.Set(ctx.Player, 1);

            int block = ctx.Self.Block;
            int energy = ctx.Player.PlayerCombatState!.Energy;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            PowerModel power = ctx.Self.Powers.Single(candidate =>
                candidate.GetType().Name == powerName);
            ctx.AssertEqual("scripture duration", duration, power.Amount);

            if (card is NimbleScripture)
            {
                ctx.AssertPower("temporary dexterity applied", ctx.Self,
                    "DexterityPower", 2);
                for (int turn = 0; turn < duration; turn++)
                    await power.AfterSideTurnEnd(new BlockingPlayerChoiceContext(),
                        ctx.Self.Side, [ctx.Self]);
                ctx.AssertPower("temporary dexterity removed after duration", ctx.Self,
                    "DexterityPower", 0);
                return;
            }

            for (int turn = 0; turn < duration; turn++)
            {
                if (card is GuardianScripture or PunishmentScripture)
                    await power.AfterSideTurnEnd(new BlockingPlayerChoiceContext(),
                        ctx.Self.Side, [ctx.Self]);
                else
                    await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            }

            if (card is GuardianScripture)
                ctx.AssertBlock("three block per duration", block, 3 * duration);
            else if (card is PunishmentScripture)
                ctx.AssertEqual("one condemnation per duration", duration,
                    ctx.Enemies.Sum(enemy => ctx.PowerAmount(enemy, "CondemnationPower")));
            else if (card is WisdomScripture)
                ctx.AssertPileDelta<StrikeIronclad>("one draw per duration",
                    PileType.Hand, hand, duration);
            else if (card is VitalityScripture)
                ctx.AssertEqual("one energy per duration", duration,
                    ctx.Player.PlayerCombatState.Energy - energy);
            else if (card is BlissScripture)
            {
                ctx.AssertEqual("desire reduced to zero", 0,
                    MaidenSuccubus.Data.Desire.Get(ctx.Player));
                ctx.AssertEqual("zero-desire energy per duration", duration,
                    ctx.Player.PlayerCombatState.Energy - energy);
                ctx.AssertPileDelta<StrikeIronclad>("zero-desire draw per duration",
                    PileType.Hand, hand, duration);
            }
        }, typeof(T) == typeof(NimbleScripture) || typeof(T) == typeof(BlissScripture) ? 3 : 2);

    // Specialized probes.  Each one performs at least one observable assertion;
    // they are intentionally named after the unabridged description clause.
    private static void BasicTrainingProbe() =>
        CustomVariants<BasicTraining>(async (ctx, card, upgraded) =>
        {
            int bonus = upgraded ? 5 : 3;
            await ctx.Play(card);
            ctx.AssertPower("training amount", ctx.Self, "BasicTrainingPower", bonus);

            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(ctx.Create<MaidenStrike>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("strike-tag damage bonus", ctx.PrimaryEnemy, hp, 6 + bonus);

            int block = ctx.Self.Block;
            await ctx.Play(ctx.Create<MaidenDefend>());
            ctx.AssertBlock("defend-tag block bonus", block, 5 + bonus);
        }, 3);

    private static void BindingInsightProbe() =>
        CustomVariants<BindingInsight>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            int energy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(ctx.Create<ResistanceGloves>());
            ctx.AssertPower("binding-insight amount", ctx.Self, "BindingInsightPower", 1);
            ctx.AssertEqual("energy after escape card", 1,
                ctx.Player.PlayerCombatState.Energy - energy);
        }, 2);

    private static void GoddessOfIceProbe() =>
        CustomVariants<GoddessOfIce>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            StrikeIronclad enchanted = await ctx.Add<StrikeIronclad>(PileType.Hand);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(enchanted, 1);
            await ctx.Play(enchanted, ctx.PrimaryEnemy);
            IceShard shard = PileType.Hand.GetPile(ctx.Player).Cards.OfType<IceShard>().Single();
            ctx.AssertPower("goddess amount", ctx.Self, "GoddessOfIcePower",
                upgraded ? 2 : 1);
            ctx.AssertEqual("one ice shard generated", 1,
                ctx.CountCards<IceShard>(PileType.Hand));
            ctx.AssertEqual("generated shard upgrade state", upgraded, shard.IsUpgraded);
        }, 3);

    private static void LullabyProbe() =>
        CustomVariants<Lullaby>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            await ctx.AddFillerCards(PileType.Hand, 2);
            int block = ctx.Self.Block;
            LullabyPower power = ctx.Self.Powers.OfType<LullabyPower>().Single();
            await power.BeforeFlush(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertEqual("one drowsy generated", 1,
                ctx.CountCards<DrowsyStatus>(PileType.Hand));
            ctx.AssertBlock("two block per resulting hand card", block, 6);
        }, 2);

    private static void MagicIndexProbe() =>
        CustomVariants<MagicIndex>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            await ctx.AddFillerCards(PileType.Draw, 1);
            StrikeIronclad enchanted = ctx.Create<StrikeIronclad>();
            await CardPileCmd.Add(enchanted, PileType.Draw, CardPilePosition.Top,
                skipVisuals: true);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(enchanted, 1);
            await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, ctx.Player);
            ctx.AssertPower("index amount", ctx.Self, "MagicIndexPower", 1);
            ctx.AssertEqual("enchanted draw plus one extra card", 2,
                ctx.CountCards<StrikeIronclad>(PileType.Hand));
        }, 2);

    private static void MagicResonanceProbe() =>
        CustomVariants<MagicResonance>(async (ctx, card, upgraded) =>
        {
            int threshold = upgraded ? 2 : 3;
            await ctx.Play(card);
            for (int i = 0; i < threshold; i++)
            {
                StrikeIronclad fixture = await ctx.Add<StrikeIronclad>(PileType.Hand);
                CombatEnchantmentCmd.ApplyVanilla<Sharp>(fixture, 1);
            }
            MagicResonancePower resonance =
                ctx.Self.Powers.OfType<MagicResonancePower>().Single();
            await ctx.Play(ctx.Create<MaidenDefend>());
            ctx.AssertEqual("threshold progress resets", 0, resonance.Progress);
            ctx.AssertEqual("pending amplification resolves", 0,
                resonance.PendingAmplification);
            ctx.AssertPower("one amplification per threshold", ctx.Self,
                "MagicAmplificationPower", 1);
        }, 3);

    private static void StudyPlanProbe() =>
        CustomVariants<StudyPlan>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 3);
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? 10 : 7);
            PowerModel next = ctx.Self.Powers.Single(power =>
                power.GetType().Name == "DrawCardsNextTurnPower");
            ctx.AssertEqual("next-turn draw amount", 2, next.Amount);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await next.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPileDelta<StrikeIronclad>("actual next-turn draw",
                PileType.Hand, hand, 2);
        }, 3);

    private static void AbnormalAdaptationProbe() =>
        CustomVariants<AbnormalAdaptation>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            AbnormalAdaptationPower power =
                ctx.Self.Powers.OfType<AbnormalAdaptationPower>().Single();
            ctx.AssertEqual("two triggers per turn", 2, power.RemainingTriggers);
            ctx.AssertEqual("upgrade grants innate", upgraded,
                card.Keywords.Contains(CardKeyword.Innate));

            await ctx.AddFillerCards(PileType.Draw, 2);
            DrowsyStatus status = ctx.Create<DrowsyStatus>();
            await CardPileCmd.Add(status, PileType.Draw, CardPilePosition.Top,
                skipVisuals: true);
            await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, ctx.Player);
            ctx.AssertEqual("drawn status moved to discard", PileType.Discard,
                status.Pile?.Type);
            ctx.AssertEqual("two replacement cards drawn", 2,
                ctx.CountCards<StrikeIronclad>(PileType.Hand));
            ctx.AssertEqual("one trigger consumed", 1, power.RemainingTriggers);
        }, 5);

    private static void BerserkerMaskProbe() =>
        CustomVariants<BerserkerMask>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            MaidenStrike strike = await ctx.Add<MaidenStrike>(PileType.Hand);
            ctx.AssertEqual("attack energy cost becomes zero", 0,
                strike.EnergyCost.GetWithModifiers(CostModifiers.All));
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(strike, ctx.PrimaryEnemy);
            ctx.AssertDamage("attack still deals damage", ctx.PrimaryEnemy, hp, 6);
            ctx.AssertPower("strength lost after attack", ctx.Self, "StrengthPower", -1);
        }, 3);

    private static void ChainDestructionProbe() =>
        CustomVariants<ChainDestruction>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            ChainDestructionPower power =
                ctx.Self.Powers.OfType<ChainDestructionPower>().Single();
            for (int i = 0; i < 4; i++)
                await CardCmd.Exhaust(new BlockingPlayerChoiceContext(),
                    await ctx.Add<MaidenDefend>(PileType.Hand));
            ctx.AssertEqual("four exhausts arm one replay", 1, power.ArmedReplays);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(ctx.Create<MaidenStrike>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("next card plays twice", ctx.PrimaryEnemy, hp, 12);
            ctx.AssertEqual("armed replay consumed", 0, power.ArmedReplays);
        }, 3);

    private static void CurseWedgeProbe() =>
        CustomVariants<CurseWedge>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            await ctx.Play(ctx.Create<MaidenStrike>(), ctx.PrimaryEnemy);
            ctx.AssertPower("shatter after unblocked damage", ctx.PrimaryEnemy,
                "ShatterPower", upgraded ? 2 : 1);
            CurseWedgePower power = ctx.Self.Powers.OfType<CurseWedgePower>().Single();
            await power.AfterSideTurnEnd(new BlockingPlayerChoiceContext(),
                ctx.Self.Side, [ctx.Self]);
            ctx.AssertPower("removed at turn end", ctx.Self, "CurseWedgePower", 0);
        }, 2);

    private static void DarkFlameBarrierProbe() =>
        CustomVariants<DarkFlameBarrier>(async (ctx, card, upgraded) =>
        {
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? 8 : 5);
            await ctx.ApplyPower<BurningPower>(ctx.PrimaryEnemy, 1);
            await CreatureCmd.LoseBlock(new BlockingPlayerChoiceContext(), ctx.Self,
                ctx.Self.Block, null);
            int hp = ctx.Self.CurrentHp;
            await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), ctx.Self,
                10, ValueProp.Move, ctx.PrimaryEnemy);
            ctx.AssertDamage("burning-enemy damage halved", ctx.Self, hp, 5);
        }, 2);

    private static void DesireRecycleProbe() =>
        CustomVariants<DesireRecycle>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            await ctx.AddFillerCards(PileType.Draw, 2);
            AllHopeLost costCard = await ctx.Add<AllHopeLost>(PileType.Hand);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), costCard);
            ctx.AssertEqual("desire gained from exhausted desire-cost card", 1,
                Desire.Get(ctx.Player));
            ctx.AssertPileDelta<StrikeIronclad>("card drawn from qualifying exhaust",
                PileType.Hand, hand, 1);
        }, 2);

    private static void InsatiableGreedProbe() =>
        CustomVariants<InsatiableGreed>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            await Desire.Modify(ctx.Player, 12);
            ctx.AssertEqual("desire cap removed", 12, Desire.Get(ctx.Player));
            await ctx.Play(ctx.Create<DesireWhip>(), ctx.PrimaryEnemy);
            ctx.AssertEqual("corrupt card grants one desire", 13, Desire.Get(ctx.Player));
        }, 2);

    private static void LegendaryMinerProbe() =>
        CustomVariants<LegendaryMiner>(async (ctx, card, upgraded) =>
        {
            int perPoint = upgraded ? 4 : 3;
            await ctx.Play(card);
            int block = ctx.Self.Block;
            await Desire.Modify(ctx.Player, 2);
            ctx.AssertBlock("block from two desire gained", block, perPoint * 2);
            LegendaryMinerPower power =
                ctx.Self.Powers.OfType<LegendaryMinerPower>().Single();
            block = ctx.Self.Block;
            await power.AfterSecondaryResourceSpent(new SecondaryResourceSpendContext(
                ctx.Combat, ctx.Player, DesireResource.Definition, card, 2, card));
            ctx.AssertBlock("block from two desire spent", block, perPoint * 2);
        }, 2);

    private static void LordOfBlazeProbe() =>
        CustomVariants<LordOfBlaze>(async (ctx, card, upgraded) =>
        {
            int amount = upgraded ? 3 : 2;
            await ctx.Play(card);
            LordOfBlazePower power = ctx.Self.Powers.OfType<LordOfBlazePower>().Single();
            await power.AfterSideTurnEnd(new BlockingPlayerChoiceContext(),
                ctx.Self.Side, ctx.Combat.Creatures);
            foreach (Creature creature in ctx.Combat.Creatures.Where(c => c.IsAlive))
                ctx.AssertPower($"burning on {creature.Name}", creature,
                    "BurningPower", amount);
        }, 2);

    private static void MasochisticGirlProbe() =>
        CustomVariants<MasochisticGirl>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            int block = ctx.Self.Block;
            await ctx.ApplyPower<WeakPower>(ctx.Self, 2);
            ctx.AssertBlock("block per self-debuff layer", block,
                (upgraded ? 4 : 3) * 2);
        }, 1);

    private static void RecollectionRoomProbe() =>
        CustomVariants<RecollectionRoom>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            IReadOnlyList<CardModel> exhausted = await ctx.AddFillerCards(
                PileType.Exhaust, 7);
            RecollectionRoomPower power =
                ctx.Self.Powers.OfType<RecollectionRoomPower>().Single();
            ctx.AssertEqual("normal hand draw replaced", 0m,
                power.ModifyHandDraw(ctx.Player, 5));
            await power.AfterModifyingHandDraw();
            ctx.AssertEqual("normal draw plus two recovered from exhaust", 7,
                exhausted.Count(candidate => candidate.Pile?.Type == PileType.Hand));
        }, 2);

    private static void SemenAppetiteProbe() =>
        CustomVariants<SemenAppetite>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            SemenAppetitePower power = ctx.Self.Powers.OfType<SemenAppetitePower>().Single();
            int maxHp = ctx.Self.MaxHp;
            bool allowed = power.ShouldAddToDeck(ctx.Create<SemenCurse>());
            for (int i = 0; i < 20 && ctx.Self.MaxHp == maxHp; i++)
                await Task.Yield();
            ctx.AssertEqual("invasion curse rejected from deck", false, allowed);
            ctx.AssertEqual("max hp gained instead", upgraded ? 3 : 2,
                ctx.Self.MaxHp - maxHp);
        }, 2);

    private static void BattleTechniqueReplayProbe() =>
        CustomVariants<BattleTechniqueReplay>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            BattleTechniqueReplay replay = await ctx.Add<BattleTechniqueReplay>(
                PileType.Hand, upgraded);
            MaidenStrike strike = ctx.Create<MaidenStrike>();
            await ctx.Play(strike, ctx.PrimaryEnemy);

            MaidenStrike projected = PileType.Hand.GetPile(ctx.Player).Cards
                .OfType<MaidenStrike>().Single();
            ctx.AssertEqual("replay card transformed to last-played type",
                typeof(MaidenStrike), projected.GetType());
            ctx.AssertEqual("upgraded replay projection retains", upgraded,
                projected.Keywords.Contains(CardKeyword.Retain));
            ctx.AssertTrue("original replay instance was transformed",
                replay.HasBeenRemovedFromState);

            await ctx.Play(projected, ctx.PrimaryEnemy);
            ctx.AssertEqual("projection restores replay card after use", 1,
                ctx.CountCards<BattleTechniqueReplay>(PileType.Discard));
        }, 4);

    private static void BlizzardProbe() =>
        CustomVariants<Blizzard>(async (ctx, card, upgraded) =>
        {
            int damage = upgraded ? 8 : 6;
            Dictionary<Creature, int> initial = ctx.Enemies.ToDictionary(e => e, e => e.CurrentHp);
            await ctx.Play(card);
            foreach ((Creature enemy, int hp) in initial)
                ctx.AssertDamage($"initial damage {enemy.Name}", enemy, hp, damage);
            ctx.AssertEqual("initial ice mist", 1, ctx.CountCards<IceMist>(PileType.Hand));

            BlizzardEchoPower echo = ctx.Self.Powers.OfType<BlizzardEchoPower>().Single();
            ctx.AssertEqual("stored delayed damage", (decimal)damage, echo.Damage);
            for (int turn = 1; turn <= 2; turn++)
            {
                Dictionary<Creature, int> before = ctx.Enemies.ToDictionary(e => e, e => e.CurrentHp);
                int mist = ctx.CountCards<IceMist>(PileType.Hand);
                await echo.AfterSideTurnStart(ctx.Self.Side, [ctx.Self], ctx.Combat);
                foreach ((Creature enemy, int hp) in before)
                    ctx.AssertDamage($"echo {turn} damage {enemy.Name}", enemy, hp, damage);
                ctx.AssertPileDelta<IceMist>($"echo {turn} ice mist",
                    PileType.Hand, mist, 1);
            }
            ctx.AssertPower("echo expires after two turns", ctx.Self,
                "BlizzardEchoPower", 0);
        }, 7);

    private static void ChastityDefenseProbe() =>
        CustomVariants<ChastityDefense>(async (ctx, card, upgraded) =>
        {
            int turns = upgraded ? 2 : 1;
            await ctx.Play(card);
            ctx.AssertPower("invasion protection duration", ctx.Self,
                "ChastityDefensePower", turns);
            ctx.AssertPower("hand retention duration", ctx.Self,
                "RetainHandPower", turns);

            int hp = ctx.Self.CurrentHp;
            int deck = ctx.Player.Deck.Cards.Count;
            bool resolved = await InvasionCmd.Resolve(new BlockingPlayerChoiceContext(),
                ctx.PrimaryEnemy.Monster!, ctx.Player, new InvasionIntentSpec(10));
            ctx.AssertEqual("invasion is blocked", false, resolved);
            ctx.AssertEqual("blocked invasion deals no damage", hp, ctx.Self.CurrentHp);
            ctx.AssertEqual("blocked invasion adds no curse", deck,
                ctx.Player.Deck.Cards.Count);
        }, 5);

    private static void ConsecrationProbe() =>
        CustomVariants<Consecration>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            MaidenStrike selected = await ctx.Add<MaidenStrike>(PileType.Hand);
            TestCardSelector selector = new();
            selector.PrepareToSelect([selected]);
            using (CardSelectCmd.UseSelector(selector))
            {
                ConsecrationPower power =
                    ctx.Self.Powers.OfType<ConsecrationPower>().Single();
                await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            }
            ctx.AssertTrue("selected hand card transformed",
                selected.HasBeenRemovedFromState);
            ctx.AssertEqual("exactly one scripture replaces selected card", 1,
                PileType.Hand.GetPile(ctx.Player).Cards.Count(c => c is ScriptureCardTemplate));
        }, 2);

    private static void DesireWardProbe() =>
        CustomVariants<DesireWard>(async (ctx, card, upgraded) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 11 : 8);
            await Desire.Modify(ctx.Player, 2);
            ctx.AssertEqual("next desire gain prevented", 0, Desire.Get(ctx.Player));
            ctx.AssertPower("prevention consumed", ctx.Self,
                "PreventNextDesireGainPower", 0);
        }, 3);

    private static void EternalDamnationProbe() =>
        CustomVariants<EternalDamnation>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            await ctx.ApplyPower<CondemnationPower>(ctx.PrimaryEnemy, 6);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(ctx.Create<Judgment>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("judgment card plus eight-layer judgment",
                ctx.PrimaryEnemy, hp, 63);
            ctx.AssertPower("condemnation retained after judgment",
                ctx.PrimaryEnemy, "CondemnationPower", 8);
        }, 2);

    private static void HolyRadianceProbe() =>
        CustomVariants<HolyRadiance>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            int hp = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
            await ctx.ApplyPower<StrengthPower>(ctx.Self, 1);
            ctx.AssertEqual("one random enemy takes exact radiance damage",
                upgraded ? 6 : 4, hp - ctx.Enemies.Sum(enemy => enemy.CurrentHp));
        }, 1);

    private static void HolyResonanceProbe() =>
        CustomVariants<HolyResonance>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            int block = ctx.Self.Block;
            await ctx.Play(ctx.Create<GuardianScripture>());
            ctx.AssertBlock("block after scripture trigger", block,
                upgraded ? 3 : 2);
        }, 1);

    private static void InwardDisciplineProbe() =>
        CustomVariants<InwardDiscipline>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            int amount = upgraded ? 50 : 25;
            InwardDisciplinePower power =
                ctx.Self.Powers.OfType<InwardDisciplinePower>().Single();
            await ctx.ApplyPower<WeakPower>(ctx.Self, 1);
            await ctx.ApplyPower<FrailPower>(ctx.Self, 1);
            ctx.AssertEqual("block multiplier while weak",
                1m + amount / 100m,
                power.ModifyBlockMultiplicative(ctx.Self, 10, ValueProp.Move,
                    ctx.Create<MaidenDefend>(), null));
            ctx.AssertEqual("damage multiplier while frail",
                1m + amount / 100m,
                power.ModifyDamageMultiplicative(ctx.PrimaryEnemy, 10,
                    ValueProp.Move, ctx.Self, ctx.Create<MaidenStrike>(), null));
        }, 2);

    private static void MemoryImprintProbe() =>
        CustomVariants<MemoryImprint>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            MaidenStrike selected = await ctx.Add<MaidenStrike>(PileType.Discard);
            TestCardSelector selector = new();
            selector.PrepareToSelect([selected]);
            using (CardSelectCmd.UseSelector(selector))
            {
                MemoryImprintPower power =
                    ctx.Self.Powers.OfType<MemoryImprintPower>().Single();
                await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            }
            ctx.AssertEqual("selected discard card moved to draw top", selected,
                PileType.Draw.GetPile(ctx.Player).Cards.First());
            ctx.AssertEqual("upgrade grants innate", upgraded,
                card.Keywords.Contains(CardKeyword.Innate));
        }, 2);

    private static void PhotonVoltProbe() =>
        CustomVariants<PhotonVolt>(async (ctx, card, upgraded) =>
        {
            int damage = upgraded ? 13 : 10;
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("low-desire damage", ctx.PrimaryEnemy, hp, damage);
            ctx.AssertPower("low-desire amplification", ctx.Self,
                "MagicAmplificationPower", 1);

            await PowerCmd.Remove(ctx.Self.Powers.Single(power =>
                power.GetType().Name == "MagicAmplificationPower"));
            await Desire.Set(ctx.Player, 3);
            PhotonVolt high = ctx.Create<PhotonVolt>(upgraded);
            hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(high, ctx.PrimaryEnemy);
            ctx.AssertDamage("high-desire damage", ctx.PrimaryEnemy, hp, damage);
            ctx.AssertPower("no amplification above two desire", ctx.Self,
                "MagicAmplificationPower", 0);
        }, 4);

    private static void RegenerativeMagicFiberProbe() =>
        CustomVariants<RegenerativeMagicFiber>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            RegenerativeMagicFiberPower power =
                ctx.Self.Powers.OfType<RegenerativeMagicFiberPower>().Single();
            await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("one armor each turn", ctx.Self, "MagicArmorPower", 1);
        }, 1);

    private static void SoulPurificationProbe() =>
        CustomVariants<SoulPurification>(async (ctx, card, upgraded) =>
        {
            int draw = upgraded ? 2 : 1;
            await ctx.Play(card);
            await ctx.AddFillerCards(PileType.Draw, draw);
            GuardianScripture scripture = ctx.Create<GuardianScripture>();
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(scripture);
            ctx.AssertEqual("played scripture exhausts", PileType.Exhaust,
                scripture.Pile?.Type);
            ctx.AssertPileDelta<StrikeIronclad>("draw after scripture",
                PileType.Hand, hand, draw);
        }, 2);

    private static void TacticalCoreProbe() =>
        CustomVariants<TacticalCore>(async (ctx, card, upgraded) =>
        {
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? 9 : 7);
            TacticalCorePower power = ctx.Self.Powers.OfType<TacticalCorePower>().Single();
            ctx.AssertEqual("duration", upgraded ? 4 : 3, power.Amount);

            await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
            MaidenStrike strike = await ctx.Add<MaidenStrike>(PileType.Hand);
            CombatEnchantmentCmd.ApplyVanilla<Sharp>(strike, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(strike, ctx.PrimaryEnemy);
            ctx.AssertDamage("enchanted card amplification becomes 100 percent",
                ctx.PrimaryEnemy, hp, 14);
        }, 3);

    private static void MagiciansSecretProbe() =>
        CustomVariants<MagiciansSecret>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 1);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand, 1);
            ctx.AssertPower("amplification layers", ctx.Self,
                "MagicAmplificationPower", upgraded ? 2 : 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(ctx.Create<MaidenStrike>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("next card amplified by 50 percent", ctx.PrimaryEnemy, hp, 9);
            ctx.AssertPower("one amplification layer consumed", ctx.Self,
                "MagicAmplificationPower", upgraded ? 1 : 0);
        }, 4);

    private static void IgniteProbe() =>
        CustomVariants<Ignite>(async (ctx, card, upgraded) =>
        {
            MaidenStrike selected = await ctx.Add<MaidenStrike>(PileType.Hand, upgraded);
            await ctx.Play(card, selectedCards: [selected]);
            IgnitePower power = ctx.Self.Powers.OfType<IgnitePower>().Single();
            ctx.AssertEqual("selected card id stored", selected.Id.Entry, power.CardId);
            ctx.AssertEqual("selected upgrade state stored", upgraded, power.WasUpgraded);
            ctx.AssertEqual("selected card exhausted", PileType.Exhaust, selected.Pile?.Type);
            int hp = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
            await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertEqual("exhausted card autoplay damage", upgraded ? 9 : 6,
                hp - ctx.Enemies.Sum(enemy => enemy.CurrentHp));
            ctx.AssertPower("ignite expires after replay", ctx.Self, "IgnitePower", 0);
        }, 5);

    private static void TentacleArmorProbe() =>
        CustomVariants<TentacleArmor>(async (ctx, card, upgraded) =>
        {
            int amount = upgraded ? 6 : 4;
            await ctx.Play(card);
            PlatingPower plating = ctx.Self.Powers.OfType<PlatingPower>().Single();
            ctx.AssertEqual("plating amount", amount, plating.Amount);
            ctx.AssertEqual("self copy in draw", 1,
                ctx.CountCards<TentacleArmor>(PileType.Draw));
        }, 2);

    private static void LightPowerReleaseProbe() =>
        CustomVariants<LightPowerRelease>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            ctx.AssertPower("enters immaculate robe", ctx.Self,
                "ImmaculateRobePower", 1);
            ctx.AssertPower("transformation armor", ctx.Self, "MagicArmorPower", 3);
            ctx.AssertPower("eternal robe amplification amount", ctx.Self,
                "EternalRobePower", 9);
            ImmaculateRobePower robe = ctx.Self.Powers.OfType<ImmaculateRobePower>().Single();
            await robe.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("nine amplification each turn", ctx.Self,
                "MagicAmplificationPower", 9);
        }, 4);

    private static void TransformProbe() =>
        CustomVariants<Transform>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            ctx.AssertPower("enters immaculate robe", ctx.Self,
                "ImmaculateRobePower", 1);
            ctx.AssertPower("transformation armor", ctx.Self, "MagicArmorPower", 3);
            ctx.AssertEqual("upgrade grants innate", upgraded,
                card.Keywords.Contains(CardKeyword.Innate));
            ImmaculateRobePower robe = ctx.Self.Powers.OfType<ImmaculateRobePower>().Single();
            await robe.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("normal robe gives one amplification each turn", ctx.Self,
                "MagicAmplificationPower", 1);
        }, 4);

    private static void TerminalSanctuaryProbe() =>
        CustomVariants<TerminalSanctuary>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<WeakPower>(ctx.Self, 2);
            await ctx.Play(card);
            ctx.AssertPower("purification", ctx.Self, "PurificationPower", 2);
            ctx.AssertPower("blur", ctx.Self, "BlurPower", 2);
            ctx.AssertPower("sanctuary duration", ctx.Self, "SanctuaryPower", 2);
            ctx.AssertEqual("upgrade removes ethereal", !upgraded,
                card.Keywords.Contains(CardKeyword.Ethereal));

            PurificationPower purification =
                ctx.Self.Powers.OfType<PurificationPower>().Single();
            await purification.AfterPlayerTurnStart(
                new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("two weak cleansed", ctx.Self, "WeakPower", 0);

            int hp = ctx.Self.CurrentHp;
            await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), ctx.Self,
                10, ValueProp.Move, ctx.PrimaryEnemy);
            ctx.AssertDamage("incoming damage halved", ctx.Self, hp, 5);
        }, 6);

    private static void WorshipProbe() =>
        CustomVariants<Worship>(async (ctx, card, upgraded) =>
        {
            int amount = upgraded ? 2 : 1;
            await ctx.ApplyPower<WeakPower>(ctx.Self, 2);
            await ctx.Play(card);
            ctx.AssertPower("dexterity", ctx.Self, "DexterityPower", amount);
            PurificationPower purification =
                ctx.Self.Powers.OfType<PurificationPower>().Single();
            await purification.AfterPlayerTurnStart(
                new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("purification removes exact weak layers", ctx.Self,
                "WeakPower", 2 - amount);
        }, 2);

    private static void IceMistProbe() =>
        CustomVariants<IceMist>(async (ctx, card, upgraded) =>
        {
            int amount = upgraded ? 3 : 2;
            await ctx.Play(card);
            ctx.AssertPower("temporary dexterity", ctx.Self, "DexterityPower", amount);
            RestoreDexterityAtTurnEndPower restore = ctx.Self.Powers
                .OfType<RestoreDexterityAtTurnEndPower>().Single();
            await restore.AfterSideTurnEnd(new BlockingPlayerChoiceContext(),
                ctx.Self.Side, [ctx.Self]);
            ctx.AssertPower("dexterity restored at turn end", ctx.Self,
                "DexterityPower", 0);
            ctx.AssertPower("restore marker removed", ctx.Self,
                "RestoreDexterityAtTurnEndPower", 0);
        }, 3);

    private static void SemenConversionProbe() =>
        CustomVariants<SemenConversion>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 2);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand, 2);
            ctx.AssertPower("hp-payment replacement armed", ctx.Self,
                "DesirePaidWithHpPower", 1);
            ctx.AssertEqual("upgrade removes exhaust", !upgraded,
                card.Keywords.Contains(CardKeyword.Exhaust));

            int hp = ctx.Self.CurrentHp;
            await ctx.Play(ctx.Create<DarkThrust>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("one desire cost paid as one hp", ctx.Self, hp, 1);
            ctx.AssertEqual("desire is not spent", 0, Desire.Get(ctx.Player));
            ctx.AssertPower("replacement consumed", ctx.Self,
                "DesirePaidWithHpPower", 0);
        }, 6);

    private static void ResistanceGlovesProbe() =>
        CustomVariants<ResistanceGloves>(async (ctx, card, upgraded) =>
        {
            ControlPower control = (ControlPower)ModelDb.Power<ControlPower>().ToMutable();
            control.ControlType = ControlType.Attack;
            await PowerCmd.Apply(new BlockingPlayerChoiceContext(), control,
                ctx.Self, 3, ctx.PrimaryEnemy, null);
            await ctx.Play(card);
            ctx.AssertEqual("escape reduces control by two", 1, control.Amount);
            ctx.AssertEqual("upgrade reduces energy cost", upgraded ? 0 : 1,
                card.EnergyCost.Canonical);
        }, 2);

    private static void RestraintEvasionProbe() =>
        CustomVariants<RestraintEvasion>(async (ctx, card, _) =>
        {
            IntentMoveFactory.SetTransient(ctx.PrimaryEnemy.Monster!,
                IntentMoveFactory.CreateControl(ctx.PrimaryEnemy.Monster!,
                    new ControlIntentSpec(4, ControlType.Attack, 3)));
            int block = ctx.Self.Block;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertBlock("base six plus control block requirement", block, 10);
        }, 1);

    private static void GenerateChosenCard<T>() where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card, selectedIndices: [0]);
            CardModel[] generated = PileType.Hand.GetPile(ctx.Player).Cards.ToArray();
            ctx.AssertEqual("exactly one chosen card generated", 1, generated.Length);
            ctx.AssertEqual("generated card upgrade state", upgraded,
                generated.Single().IsUpgraded);
            ctx.AssertEqual("generated card is free this turn", 0,
                generated.Single().EnergyCost.GetWithModifiers(CostModifiers.All));
        }, 3);

    private static void BalanceBladeProbe() =>
        CustomVariants<BalanceBlade>(async (ctx, card, upgraded) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage at zero corruption", ctx.PrimaryEnemy, hp,
                upgraded ? 21 : 17);

            CorruptionCmd.Set((MegaCrit.Sts2.Core.Runs.RunState)ctx.Player.RunState, 3);
            BalanceBlade shifted = ctx.Create<BalanceBlade>(upgraded);
            hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(shifted, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage at three corruption", ctx.PrimaryEnemy, hp,
                upgraded ? 12 : 8);
        }, 2);

    private static void BalanceShieldProbe() =>
        CustomVariants<BalanceShield>(async (ctx, card, upgraded) =>
        {
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block at zero corruption", block, upgraded ? 18 : 14);

            CorruptionCmd.Set((MegaCrit.Sts2.Core.Runs.RunState)ctx.Player.RunState, 3);
            BalanceShield shifted = ctx.Create<BalanceShield>(upgraded);
            block = ctx.Self.Block;
            await ctx.Play(shifted);
            ctx.AssertBlock("block at three corruption", block, upgraded ? 9 : 5);
        }, 2);

    private static void BathProbe() =>
        CustomVariants<Bath>(async (ctx, card, upgraded) =>
        {
            SemenCurse deckCurse = ctx.Player.RunState.CreateCard<SemenCurse>(ctx.Player);
            await CardPileCmd.Add(deckCurse, PileType.Deck, skipVisuals: true);
            try
            {
                await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
                int energy = ctx.Player.PlayerCombatState!.Energy;
                await ctx.Play(card);
                ctx.AssertEqual("energy gained", upgraded ? 3 : 2,
                    ctx.Player.PlayerCombatState.Energy - energy);
                ctx.AssertTrue("one invasion curse permanently removed",
                    deckCurse.HasBeenRemovedFromState);
                ctx.AssertPower("amplified next-turn energy", ctx.Self,
                    "EnergyNextTurnPower", 2);
            }
            finally
            {
                if (!deckCurse.HasBeenRemovedFromState && deckCurse.Pile?.Type == PileType.Deck)
                    await CardPileCmd.RemoveFromDeck(deckCurse, showPreview: false);
            }
        }, 3);

    private static void BorrowedForceStrikeProbe() =>
        CustomVariants<BorrowedForceStrike>(async (ctx, card, upgraded) =>
        {
            bool attacking = ctx.PrimaryEnemy.Monster?.IntendsToAttack == true;
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int energy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 12 : 9);
            ctx.AssertEqual("energy follows attack intent", attacking ? 1 : 0,
                ctx.Player.PlayerCombatState.Energy - energy);
        }, 2);

    private static void HandDiscountProbe<T>(int baseValue, int upgradedValue)
        where T : CardModel => CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await ctx.AddFillerCards(PileType.Hand, 3);
            ctx.AssertEqual("cost with three other cards", 3,
                card.EnergyCost.GetWithModifiers(CostModifiers.All));
            if (card is BurningBracelet)
            {
                int hp = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(card, ctx.PrimaryEnemy);
                ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp,
                    upgraded ? upgradedValue : baseValue);
            }
            else
            {
                int block = ctx.Self.Block;
                await ctx.Play(card);
                ctx.AssertBlock("block", block, upgraded ? upgradedValue : baseValue);
            }
        }, 2);

    private static void HealingArtProbe() =>
        CustomVariants<HealingArt>(async (ctx, card, upgraded) =>
        {
            await CreatureCmd.SetCurrentHp(ctx.Self, ctx.Self.MaxHp - 30);
            await ctx.ApplyPower<StrengthPower>(ctx.Self, 2);
            int hp = ctx.Self.CurrentHp;
            await ctx.Play(card);
            ctx.AssertEqual("base healing plus two per buff layer",
                (upgraded ? 8 : 4) + 4, ctx.Self.CurrentHp - hp);
        }, 1);

    private static void MagicSwordProbe() =>
        CustomVariants<MagicSword>(async (ctx, card, upgraded) =>
        {
            MagicSword deckCard = ctx.Player.RunState.CreateCard<MagicSword>(ctx.Player);
            if (upgraded)
                CardCmd.Upgrade(deckCard);
            await CardPileCmd.Add(deckCard, PileType.Deck, skipVisuals: true);
            try
            {
                ctx.AssertEqual("pickup charge enchantment type", "ChargeEnchantment",
                    deckCard.Enchantment?.GetType().Name ?? "none");
                ctx.AssertEqual("pickup charge amount", 2,
                    deckCard.Enchantment?.Amount ?? 0);
            }
            finally
            {
                if (!deckCard.HasBeenRemovedFromState && deckCard.Pile?.Type == PileType.Deck)
                    await CardPileCmd.RemoveFromDeck(deckCard, showPreview: false);
            }

            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 18 : 12);
        }, 3);

    private static void JudgmentBladeProbe() =>
        CustomVariants<JudgmentBlade>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<WeakPower>(ctx.PrimaryEnemy, 1);
            await ctx.ApplyPower<VulnerablePower>(ctx.PrimaryEnemy, 2);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage with three debuff layers", ctx.PrimaryEnemy, hp,
                upgraded ? 22 : 16);
        }, 1);

    private static void LightningRecoilProbe() =>
        CustomVariants<LightningRecoil>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MagicArmorPower>(ctx.Self, 1);
            await ctx.AddFillerCards(PileType.Draw, 2);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, ctx.PrimaryEnemy, selectedIndices: [0]);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 7 : 4);
            ctx.AssertPileDelta<StrikeIronclad>("overdraft draw", PileType.Hand, hand, 1);
            ctx.AssertPower("overdraft armor payment", ctx.Self, "MagicArmorPower", 0);
        }, 3);

    private static void MindsEyeProbe() =>
        CustomVariants<MindsEye>(async (ctx, card, upgraded) =>
        {
            bool attacking = ctx.PrimaryEnemy.Monster?.IntendsToAttack == true;
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, 4);
            ctx.AssertPower("weak branch", ctx.PrimaryEnemy, "WeakPower",
                attacking ? (upgraded ? 2 : 1) : 0);
            ctx.AssertPower("vulnerable branch", ctx.PrimaryEnemy, "VulnerablePower",
                attacking ? 0 : (upgraded ? 2 : 1));
        }, 3);

    private static void ObstructingShotProbe() =>
        CustomVariants<ObstructingShot>(async (ctx, card, upgraded) =>
        {
            bool shouldStun = ctx.PrimaryEnemy.Monster?.IntendsToAttack == false;
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 6 : 3);
            ctx.AssertEqual("stun follows non-attack intent", shouldStun,
                ctx.PrimaryEnemy.IsStunned);
        }, 2);

    private static void ProcrastinateProbe() =>
        CustomVariants<Procrastinate>(async (ctx, card, upgraded) =>
        {
            StrikeIronclad selected = await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.AddFillerCards(PileType.Draw, 6);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, selectedCards: [selected]);
            ctx.AssertEqual("selected card moved to draw bottom", selected,
                PileType.Draw.GetPile(ctx.Player).Cards.Last());
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand,
                (upgraded ? 3 : 2) - 1);
        }, 2);

    private static void SurfProbe() => DamageDraw<Surf>(12, 16, 3, 4);

    private static void SwordVerdictProbe() =>
        CustomVariants<SwordVerdict>(async (ctx, card, upgraded) =>
        {
            await CreatureCmd.SetCurrentHp(ctx.PrimaryEnemy, ctx.PrimaryEnemy.MaxHp / 2 - 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("double damage below half hp", ctx.PrimaryEnemy, hp, 48);
            ctx.AssertTrue("low-hp target stunned", ctx.PrimaryEnemy.IsStunned);
            ctx.AssertEqual("upgrade grants retain", upgraded,
                card.Keywords.Contains(CardKeyword.Retain));
        }, 3);

    private static void UltimateFlareProbe() =>
        CustomVariants<UltimateFlare>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MagicArmorPower>(ctx.Self, 1);
            int energy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card, selectedIndices: [0]);
            UltimateFlarePower power = ctx.Self.Powers.OfType<UltimateFlarePower>().Single();
            ctx.AssertEqual("stored delayed damage", upgraded ? 32m : 24m, power.Damage);
            ctx.AssertEqual("overdraft energy", 1,
                ctx.Player.PlayerCombatState.Energy - energy);
            ctx.AssertPower("overdraft armor payment", ctx.Self, "MagicArmorPower", 0);

            Dictionary<Creature, int> hp = ctx.Enemies.ToDictionary(enemy => enemy,
                enemy => enemy.CurrentHp);
            await power.AfterSideTurnStart(ctx.Self.Side, [ctx.Self], ctx.Combat);
            foreach ((Creature enemy, int before) in hp)
                ctx.AssertDamage($"next-turn delayed damage {enemy.Name}", enemy, before,
                    upgraded ? 32 : 24);
        }, 4);

    private static void AllCurseBiteProbe() =>
        CustomVariants<AllCurseBite>(async (ctx, card, _) =>
        {
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            MaidenStrike fuel = await ctx.Add<MaidenStrike>(PileType.Hand);
            await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), fuel);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("one plus exhausted attack damage", ctx.PrimaryEnemy, hp, 7);
            ctx.AssertEqual("stored exhausted attack damage", 6, card.ExhaustedAttackDamage);
        }, 2);

    private static void AllHopeLostProbe() =>
        CustomVariants<AllHopeLost>(async (ctx, card, upgraded) =>
        {
            await PlayerCmd.SetEnergy(3, ctx.Player);
            await card.AfterSecondaryResourceSpent(new SecondaryResourceSpendContext(
                ctx.Combat, ctx.Player, DesireResource.Definition, card, 2, card));
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("two desire times six damage for each X hit",
                ctx.PrimaryEnemy, hp, upgraded ? 48 : 36);
        }, 1);

    private static void BiteInvaderProbe() =>
        CustomVariants<BiteInvader>(async (ctx, card, _) =>
        {
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertPower("weak", ctx.PrimaryEnemy, "WeakPower", 7);
            ctx.AssertTrue("target stunned", ctx.PrimaryEnemy.IsStunned);
        }, 2);

    private static void BlackVortexProbe() =>
        CustomVariants<BlackVortex>(async (ctx, card, _) =>
        {
            await ctx.ApplyPower<MagicArmorPower>(ctx.Self, 1);
            await ctx.AddFillerCards(PileType.Draw, 2);
            int totalHp = ctx.Enemies.Sum(enemy => enemy.CurrentHp);
            await ctx.Play(card, selectedIndices: [0]);
            ctx.AssertEqual("two top attacks total damage", 12,
                totalHp - ctx.Enemies.Sum(enemy => enemy.CurrentHp));
            ctx.AssertEqual("overdraft exhausts both played cards", 2,
                ctx.CountCards<StrikeIronclad>(PileType.Exhaust));
            ctx.AssertPower("overdraft armor payment", ctx.Self, "MagicArmorPower", 0);
        }, 3);

    private static void BurningDesireProbe() =>
        CustomVariants<BurningDesire>(async (ctx, card, upgraded) =>
        {
            await MaidenSuccubus.Data.Desire.Set(ctx.Player, 2);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("three hits at two current desire", ctx.PrimaryEnemy, hp,
                upgraded ? 12 : 9);
        }, 1);

    private static void ChangePantiesProbe() =>
        CustomVariants<ChangePanties>(async (ctx, card, _) =>
        {
            await MaidenSuccubus.Data.Desire.Set(ctx.Player, 4);
            ctx.AssertTrue("blocked below five desire",
                !MegaCrit.Sts2.Core.Hooks.Hook.ShouldPlay(ctx.Combat, card, out AbstractModel? _,
                    AutoPlayType.Default));
            await MaidenSuccubus.Data.Desire.Set(ctx.Player, 5);
            ctx.AssertTrue("playable at five desire",
                MegaCrit.Sts2.Core.Hooks.Hook.ShouldPlay(ctx.Combat, card, out AbstractModel? _,
                    AutoPlayType.Default));
            await ctx.Play(card);
            ctx.AssertPower("slippery", ctx.Self, "SlipperyPower", 2);
            ctx.AssertPower("vulnerable", ctx.Self, "VulnerablePower", 2);
        }, 4);

    private static void CoronationProbe() =>
        CustomVariants<Coronation>(async (ctx, card, upgraded) =>
        {
            CorruptionCmd.Set((MegaCrit.Sts2.Core.Runs.RunState)ctx.Player.RunState, 3);
            await ctx.Play(card);
            ctx.AssertPower("strength equals corruption", ctx.Self, "StrengthPower", 3);
            ctx.AssertEqual("upgrade grants innate", upgraded,
                card.Keywords.Contains(CardKeyword.Innate));
        }, 2);

    private static void DarkElementProbe() =>
        CustomVariants<DarkElement>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MagicArmorPower>(ctx.Self, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int block = ctx.Self.Block;
            await ctx.Play(card, ctx.PrimaryEnemy, selectedIndices: [0]);
            int expected = upgraded ? 6 : 4;
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, expected);
            ctx.AssertBlock("overdraft block", block, expected);
            ctx.AssertPower("overdraft armor payment", ctx.Self, "MagicArmorPower", 0);
        }, 3);

    private static void DarkPunishmentProbe() =>
        CustomVariants<DarkPunishment>(async (ctx, card, upgraded) =>
        {
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            for (int i = 0; i < 3; i++)
                await CardCmd.Exhaust(new BlockingPlayerChoiceContext(),
                    await ctx.Add<StrikeIronclad>(PileType.Hand));
            ctx.AssertEqual("cost after three exhausts", 5,
                card.EnergyCost.GetWithModifiers(CostModifiers.All));
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 44 : 33);
        }, 2);

    private static void FullOfOpeningsProbe() =>
        CustomVariants<FullOfOpenings>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<ControlPower>(ctx.Self, 1);
            await ctx.Play(card, ctx.PrimaryEnemy);
            int expected = upgraded ? 3 : 2;
            ctx.AssertPower("repeated weak while controlled", ctx.PrimaryEnemy,
                "WeakPower", expected);
            ctx.AssertPower("repeated vulnerable while controlled", ctx.PrimaryEnemy,
                "VulnerablePower", expected);
        }, 2);

    private static void ExposePlayProbe() =>
        CustomVariants<ExposePlay>(async (ctx, card, upgraded) =>
        {
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? 5 : 3);
            ctx.AssertTrue("an enemy intent becomes erotic at zero armor",
                ctx.Enemies.Any(enemy => enemy.Monster?.NextMove.StateId.StartsWith(
                    "MAIDENSUCCUBUS_", StringComparison.Ordinal) == true));
        }, 2);

    private static void LastStandProbe() =>
        CustomVariants<LastStand>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<WeakPower>(ctx.Self, 1);
            await ctx.ApplyPower<VulnerablePower>(ctx.Self, 2);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage with three self-debuff layers", ctx.PrimaryEnemy, hp,
                upgraded ? 16 : 12);
        }, 1);

    private static void LoversDaggerProbe() =>
        CustomVariants<LoversDagger>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<ControlPower>(ctx.Self, 1);
            await CreatureCmd.Stun(ctx.PrimaryEnemy);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("controlled and stunned damage is quadrupled",
                ctx.PrimaryEnemy, hp, upgraded ? 96 : 72);
        }, 1);

    private static void MagicExcessProbe() =>
        CustomVariants<MagicExcess>(async (ctx, card, upgraded) =>
        {
            CardModel[] selected = (await ctx.AddFillerCards(PileType.Hand, 4)).ToArray();
            await ctx.AddFillerCards(PileType.Draw, 12);
            await ctx.Play(card, selectedCards: selected);
            ctx.AssertEqual("exactly four selected cards exhausted", 4,
                selected.Count(candidate => candidate.Pile?.Type == PileType.Exhaust));
            ctx.AssertEqual("draws to ten-card hand after four exhausts", 10,
                PileType.Hand.GetPile(ctx.Player).Cards.Count);
            ctx.AssertEqual("upgrade grants retain", upgraded,
                card.Keywords.Contains(CardKeyword.Retain));
        }, 3);

    private static void MasochisticTranceProbe() =>
        CustomVariants<MasochisticTrance>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<WeakPower>(ctx.Self, 1);
            await ctx.ApplyPower<VulnerablePower>(ctx.Self, 2);
            await ctx.AddFillerCards(PileType.Draw, 6);
            int block = ctx.Self.Block;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertBlock("block", block, 6);
            ctx.AssertPileDelta<StrikeIronclad>("one draw per debuff layer",
                PileType.Hand, hand, 3);
            ctx.AssertEqual("upgrade removes exhaust", upgraded,
                !card.Keywords.Contains(CardKeyword.Exhaust));
        }, 3);

    private static void MentalStabilizerProbe() =>
        CustomVariants<MentalStabilizer>(async (ctx, card, upgraded) =>
        {
            DrowsyStatus status = await ctx.Add<DrowsyStatus>(PileType.Hand);
            SemenCurse curse = await ctx.Add<SemenCurse>(PileType.Hand);
            await ctx.AddFillerCards(PileType.Draw, 5);
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? 14 : 11);
            ctx.AssertEqual("status discarded", PileType.Discard, status.Pile?.Type);
            ctx.AssertEqual("curse discarded", PileType.Discard, curse.Pile?.Type);
            ctx.AssertEqual("draws same count as discarded status and curse", 2,
                ctx.CountCards<StrikeIronclad>(PileType.Hand));
        }, 4);

    private static void ReflectiveBarrierProbe() =>
        CustomVariants<ReflectiveBarrier>(async (ctx, card, upgraded) =>
        {
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block when played", block, upgraded ? 18 : 9);

            ReflectiveBarrier exhausted = ctx.Create<ReflectiveBarrier>(upgraded);
            await CardPileCmd.Add(exhausted, PileType.Hand, skipVisuals: true);
            block = ctx.Self.Block;
            await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), exhausted);
            ctx.AssertBlock("block when exhausted", block, 9);
        }, 2);

    private static void ThousandCurseScytheProbe() =>
        CustomVariants<ThousandCurseScythe>(async (ctx, card, upgraded) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, 8);
            ctx.AssertEqual("permanent damage growth after exhaust",
                upgraded ? 13 : 12, card.CurrentDamage);
        }, 2);

    private static void SharpForgeProbe() =>
        CustomVariants<SharpForge>(async (ctx, card, upgraded) =>
        {
            IReadOnlyList<CardModel> attacks = await ctx.AddFillerCards(PileType.Draw, 2);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 10 : 8);
            ctx.AssertEqual("exactly two draw attacks enchanted", 2,
                attacks.Count(candidate => candidate.Enchantment?.GetType().Name == "Sharp"));
            foreach (CardModel attack in attacks)
                ctx.AssertEqual("sharp amount", upgraded ? 3 : 2,
                    attack.Enchantment?.Amount ?? 0);
        }, 4);

    private static void DiscardAutoPlayBlockProbe() =>
        CustomVariants<AutoReactionArmor>(async (ctx, card, upgraded) =>
        {
            await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
            int block = ctx.Self.Block;
            await card.BeforeFlush(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertBlock("discard end-turn autoplay block", block, upgraded ? 7 : 5);
        }, 1);

    private static void DiscardAutoPlayDamageProbe() =>
        CustomVariants<ExternalPowerSkeleton>(async (ctx, card, upgraded) =>
        {
            await CardPileCmd.Add(card, PileType.Discard, skipVisuals: true);
            Dictionary<Creature, int> hp = ctx.Enemies.ToDictionary(enemy => enemy,
                enemy => enemy.CurrentHp);
            await card.BeforeFlush(new BlockingPlayerChoiceContext(), ctx.Player);
            foreach ((Creature enemy, int before) in hp)
                ctx.AssertDamage($"discard end-turn autoplay damage {enemy.Name}", enemy,
                    before, upgraded ? 8 : 6);
        }, 1);

    private static void DragonflyTouchProbe() =>
        CustomVariants<DragonflyTouch>(async (ctx, card, upgraded) =>
        {
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block repeated once per enemy", block,
                (upgraded ? 10 : 7) * ctx.Enemies.Count);
        }, 1);

    private static void FocusedSlashProbe() =>
        CustomVariants<FocusedSlash>(async (ctx, card, upgraded) =>
        {
            await MaidenSuccubus.Data.Desire.Set(ctx.Player, 2);
            ctx.AssertEqual("cost equals current desire", 2,
                card.EnergyCost.GetWithModifiers(CostModifiers.All));
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 40 : 30);
        }, 2);

    private static void ForgeNimbleProbe() =>
        CustomVariants<ForgeNimble>(async (ctx, card, upgraded) =>
        {
            DefendIronclad selected = await ctx.Add<DefendIronclad>(PileType.Hand);
            int block = ctx.Self.Block;
            await ctx.Play(card, selectedCards: [selected]);
            ctx.AssertBlock("block", block, upgraded ? 8 : 5);
            ctx.AssertEqual("selected card nimble amount", 3,
                selected.Enchantment?.Amount ?? 0);
            ctx.AssertEqual("selected card nimble type", "Nimble",
                selected.Enchantment?.GetType().Name ?? "none");
        }, 3);

    private static void HolyCurseProbe() =>
        CustomVariants<HolyCurse>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<WeakPower>(ctx.PrimaryEnemy, 1);
            await ctx.ApplyPower<VulnerablePower>(ctx.PrimaryEnemy, 2);
            await ctx.AddFillerCards(PileType.Draw, 5);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, 7);
            ctx.AssertPileDelta<StrikeIronclad>("one draw per target debuff layer",
                PileType.Hand, hand, 3);
            ctx.AssertEqual("upgrade removes exhaust", upgraded,
                !card.Keywords.Contains(CardKeyword.Exhaust));
        }, 3);

    private static void MagicBurstProbe() =>
        CustomVariants<MagicBurst>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<StrengthPower>(ctx.Self, 2);
            await ctx.ApplyPower<MagicArmorPower>(ctx.Self, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy, selectedIndices: [0]);
            ctx.AssertDamage("overdraft damage scales with buff layers",
                ctx.PrimaryEnemy, hp, upgraded ? 13 : 11);
            ctx.AssertPower("overdraft armor payment", ctx.Self, "MagicArmorPower", 0);
        }, 2);

    private static void MultipleReproductionProbe() =>
        CustomVariants<MultipleReproduction>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            MultipleReproductionPower delayed = ctx.Self.Powers
                .OfType<MultipleReproductionPower>().Single();
            ctx.AssertTrue("without overdraft extra turn waits one turn", delayed.DelayOneTurn);

            await PowerCmd.Remove(delayed);
            await ctx.ApplyPower<MagicArmorPower>(ctx.Self, 1);
            MultipleReproduction immediateCard = ctx.Create<MultipleReproduction>();
            await ctx.Play(immediateCard, selectedIndices: [0]);
            MultipleReproductionPower immediate = ctx.Self.Powers
                .OfType<MultipleReproductionPower>().Single();
            ctx.AssertTrue("overdraft makes extra turn immediate", !immediate.DelayOneTurn);
            ctx.AssertPower("overdraft armor payment", ctx.Self, "MagicArmorPower", 0);
        }, 3);

    private static void NoLewdnessProbe() =>
        CustomVariants<NoLewdness>(async (ctx, card, upgraded) =>
        {
            await MaidenSuccubus.Data.Desire.Set(ctx.Player, 2);
            await ctx.AddFillerCards(PileType.Draw, 6);
            ctx.AssertEqual("cost equals current desire", 2,
                card.EnergyCost.GetWithModifiers(CostModifiers.All));
            int energy = ctx.Player.PlayerCombatState!.Energy;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            int expected = upgraded ? 4 : 3;
            ctx.AssertEqual("energy gained", expected,
                ctx.Player.PlayerCombatState.Energy - energy);
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand, expected);
        }, 3);

    private static void OriginalSinBrandProbe() =>
        CustomVariants<OriginalSinBrand>(async (ctx, card, upgraded) =>
        {
            int count = upgraded ? 2 : 1;
            IReadOnlyList<CardModel> fixtures = await ctx.AddFillerCards(PileType.Discard, count);
            await ctx.Play(card, selectedCards: fixtures);
            foreach (Creature enemy in ctx.Enemies)
                ctx.AssertPower($"condemnation {enemy.Name}", enemy,
                    "CondemnationPower", 2);
            ctx.AssertEqual("selected discard cards returned to hand", count,
                fixtures.Count(candidate => candidate.Pile?.Type == PileType.Hand));
        }, 2);

    private static void RestProbe() =>
        CustomVariants<Rest>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<ImmaculateRobePower>(ctx.Self, 1);
            await ctx.AddFillerCards(PileType.Draw, 4);
            using (CardEffectTestEndTurnInterception.Begin())
                await ctx.Play(card);

            ctx.AssertEqual("requests end turn", 1,
                CardEffectTestEndTurnInterception.SuppressedCalls);
            ctx.AssertPower("exits transformation", ctx.Self,
                "ImmaculateRobePower", 0);
            RestNextTurnPower next = ctx.Self.Powers.OfType<RestNextTurnPower>().Single();
            ctx.AssertEqual("next-turn effect marker", 1, next.Amount);
            ctx.AssertEqual("upgrade grants retain", upgraded,
                card.Keywords.Contains(CardKeyword.Retain));

            await PlayerCmd.SetEnergy(5, ctx.Player);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await next.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertEqual("next-turn energy", 7, ctx.Player.PlayerCombatState!.Energy);
            ctx.AssertPileDelta<StrikeIronclad>("next-turn draw", PileType.Hand, hand, 2);
        }, 6);

    private static void SoulImpactProbe() =>
        CustomVariants<SoulImpact>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<WeakPower>(ctx.PrimaryEnemy, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int energy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 14 : 12);
            ctx.AssertEqual("energy against debuffed target", upgraded ? 3 : 2,
                ctx.Player.PlayerCombatState.Energy - energy);
        }, 2);

    private static void StigmaProbe() =>
        CustomVariants<Stigma>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card, selectedIndices: [0]);
            ctx.AssertPower("selected condemnation applied to selected self target",
                ctx.Self, "CondemnationPower", upgraded ? 3 : 2);
        }, 1);

    private static void TacticalAnalyzerProbe() =>
        CustomVariants<TacticalAnalyzer>(async (ctx, card, upgraded) =>
        {
            int count = upgraded ? 2 : 1;
            IReadOnlyList<CardModel> fixtures = await ctx.AddFillerCards(PileType.Draw, count);
            CardModel selected = fixtures[0];
            await ctx.Play(card, selectedCards: [selected]);
            ctx.AssertEqual("cards drawn", count,
                ctx.CountCards<StrikeIronclad>(PileType.Hand));
            ctx.AssertTrue("selected drawn card upgraded", selected.IsUpgraded);
            ctx.AssertTrue("selected drawn card retained",
                selected.Keywords.Contains(CardKeyword.Retain));
        }, 3);

    private static void BeyondReasonForgeProbe() =>
        CustomVariants<BeyondReasonForge>(async (ctx, card, _) =>
        {
            StrikeIronclad fixture = await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, selectedIndices: [0]);
            ctx.AssertTrue("selected hand card enchanted", fixture.Enchantment != null);
            ctx.AssertTrue("source exhausted", card.Pile?.Type == PileType.Exhaust);
        }, 2);

    private static void MentalUnityProbe() =>
        CustomVariants<MentalUnity>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MaidenSuccubus.Powers.MagicArmorPower>(ctx.Self, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy, selectedIndices: [0]);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 10 : 8);
            ctx.AssertPower("delayed block per target attack", ctx.PrimaryEnemy,
                "MentalUnityPower", upgraded ? 3 : 2);
            ctx.AssertPower("overdraft armor payment", ctx.Self, "MagicArmorPower", 0);
        }, 3);

    private static void CurseInfectionProbe() =>
        CustomVariants<CurseInfection>(async (ctx, card, _) =>
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(
                card, PileType.Hand, skipVisuals: true);
            StrikeIronclad recipient = await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.AddFillerCards(PileType.Draw, 4);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await MegaCrit.Sts2.Core.Commands.CardCmd.Exhaust(
                new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(), card);
            ctx.AssertPileDelta<StrikeIronclad>("draw two on exhaust", PileType.Hand, hand, 2);
            ctx.AssertTrue("infection transferred to hand card",
                CurseInfectionStatus.Has(recipient)
                || PileType.Hand.GetPile(ctx.Player).Cards.Any(CurseInfectionStatus.Has));
        }, 2);

    private static void PleasureGardenProbe() =>
        CustomVariants<PleasureGarden>(async (ctx, card, _) =>
        {
            await ctx.Play(card);
            foreach (Creature enemy in ctx.Enemies)
            {
                ctx.AssertTrue($"{enemy.Name} intent replaced by erotic intent",
                    enemy.Monster?.NextMove.StateId.StartsWith(
                        "MAIDENSUCCUBUS_", StringComparison.Ordinal) == true);
            }
        }, 1);

    private static void DivineEchoProbe() =>
        CustomVariants<DivineEcho>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(ctx.Self, 2);
            await ctx.Play(card);
            ctx.AssertPower("stackable buff extra layers", ctx.Self,
                "StrengthPower", upgraded ? 4 : 3);
        }, 1);

    private static void FinalJudgmentProbe() =>
        CustomVariants<FinalJudgment>(async (ctx, card, upgraded) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("forced judgment damage for one layer", ctx.PrimaryEnemy, hp, 7);
            ctx.AssertPower("condemnation cleared after judgment", ctx.PrimaryEnemy,
                "CondemnationPower", 0);
            ctx.AssertEqual("upgrade grants retain", upgraded,
                card.Keywords.Contains(CardKeyword.Retain));
        }, 3);

    private static void SunDanceProbe() =>
        CustomVariants<SunDance>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(ctx.Self, 2);
            await ctx.Play(card);
            ctx.AssertPower("dexterity equals buff layers", ctx.Self, "DexterityPower", 2);
            ctx.AssertPower("temporary dexterity restoration", ctx.Self,
                "RestoreDexterityAtTurnEndPower", 2);
            ctx.AssertEqual("upgrade grants retain", upgraded,
                card.Keywords.Contains(CardKeyword.Retain));
        }, 3);

    private static void SuperRegenerationProbe() =>
        CustomVariants<SuperRegeneration>(async (ctx, card, upgraded) =>
        {
            MaidenStrike replay = await ctx.Add<MaidenStrike>(PileType.Exhaust, upgraded);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, selectedCards: [replay]);
            ctx.AssertDamage("selected exhausted card replayed", ctx.PrimaryEnemy, hp,
                upgraded ? 9 : 6);
            ctx.AssertTrue("replayed card left exhaust pile", replay.Pile?.Type != PileType.Exhaust);
        }, 2);

    private static void SelectedEnchant<T>(string enchantment, int baseAmount, int upgradedAmount,
        PileType fromPile) where T : CardModel => CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            CardModel fixture = await ctx.Add<StrikeIronclad>(fromPile);
            await ctx.Play(card, selectedCards: [fixture]);
            ctx.AssertTrue($"selected card receives {enchantment}", fixture.Enchantment != null);
            ctx.AssertEqual("selected card moved to draw top", fixture, PileType.Draw.GetPile(ctx.Player).Cards.First());
            ctx.AssertEqual("enchantment amount", upgraded ? upgradedAmount : baseAmount,
                fixture.Enchantment?.Amount ?? 0);
        }, 3);

    private static void TransformHand<T>() where T : CardModel => CustomVariants<T>(async (ctx, card, _) =>
    {
        await ctx.Add<StrikeIronclad>(PileType.Hand);
        await ctx.Add<DefendIronclad>(PileType.Hand);
        await ctx.Play(card);
        int scriptures = PileType.Hand.GetPile(ctx.Player).Cards.Count(c =>
            c is GuardianScripture or PunishmentScripture);
        ctx.AssertEqual("attack and skill transformed to scriptures", 2, scriptures);
    }, 1);

    private static void YarusMemoryProbe() =>
        CustomVariants<YarusMemory>(async (ctx, _, upgraded) =>
        {
            YarusMemory pickup = ctx.Player.RunState.CreateCard<YarusMemory>(ctx.Player);
            StrikeIronclad first = ctx.Player.RunState.CreateCard<StrikeIronclad>(ctx.Player);
            DefendIronclad second = ctx.Player.RunState.CreateCard<DefendIronclad>(ctx.Player);
            if (upgraded)
                CardCmd.Upgrade(pickup);
            await CardPileCmd.Add(first, PileType.Deck, skipVisuals: true);
            await CardPileCmd.Add(second, PileType.Deck, skipVisuals: true);
            try
            {
                TestCardSelector selector = new();
                selector.PrepareToSelect([first, second]);
                using IDisposable scope = CardSelectCmd.UseSelector(selector);
                await CardPileCmd.Add(pickup, PileType.Deck, skipVisuals: true);
                foreach (CardModel linked in new CardModel[] { pickup, first, second })
                    ctx.AssertEqual($"soul-link enchantment {linked.GetType().Name}",
                        "SoulLinkEnchantment", linked.Enchantment?.GetType().Name ?? "none");
            }
            finally
            {
                CardModel[] cleanup = [pickup, first, second];
                CardModel[] inDeck = cleanup.Where(candidate =>
                    !candidate.HasBeenRemovedFromState && candidate.Pile?.Type == PileType.Deck).ToArray();
                if (inDeck.Length > 0)
                    await CardPileCmd.RemoveFromDeck(inDeck, showPreview: false);
            }
        }, 3);

    private static void DamageAndExhaustSelection<T>(int baseDamage, int upgradedDamage)
        where T : CardModel => CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            CardModel fixture = await ctx.Add<StrikeIronclad>(PileType.Hand);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy, [fixture]);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? upgradedDamage : baseDamage);
            ctx.AssertEqual("selected card exhausted", PileType.Exhaust, fixture.Pile?.Type);
        }, 2);

    private static void DrawAndExhaustSelection<T>(int baseDraw, int upgradedDraw)
        where T : CardModel => CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 10);
            int draw = upgraded ? upgradedDraw : baseDraw;
            await ctx.Play(card, selectedIndices: [0]);
            ctx.AssertEqual("one selected drawn card exhausted", 1,
                PileType.Exhaust.GetPile(ctx.Player).Cards.Count);
            ctx.AssertEqual("remaining drawn fixtures", draw - 1,
                PileType.Hand.GetPile(ctx.Player).Cards.OfType<StrikeIronclad>().Count());
        }, 2);

    private static void DamageAndSelectedPileMove<T>(int baseDamage, int upgradedDamage,
        int baseCount, int upgradedCount, PileType from, PileType to) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int count = upgraded ? upgradedCount : baseCount;
            IReadOnlyList<CardModel> fixtures = await ctx.AddFillerCards(from, count + 2);
            CardModel[] selected = fixtures.Take(count).ToArray();
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy, selected);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? upgradedDamage : baseDamage);
            ctx.AssertEqual("selected cards moved", count, selected.Count(c => c.Pile?.Type == to));
        }, 2);

    private static void DrawSelected<T>(int baseCount, int upgradedCount, PileType from, PileType to,
        Action<CardEffectTestContext, CardModel>? extra = null) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int count = upgraded ? upgradedCount : baseCount;
            IReadOnlyList<CardModel> fixtures = await ctx.AddFillerCards(from, count + 1);
            CardModel[] selected = fixtures.Take(count).ToArray();
            await ctx.Play(card, selectedCards: selected);
            ctx.AssertEqual("selected cards moved", count, selected.Count(c => c.Pile?.Type == to));
            extra?.Invoke(ctx, card);
        }, 1 + (extra == null ? 0 : 1));

    private static void ExhaustSelectionGenerateCopies<T>(int baseCopies, int upgradedCopies)
        where T : CardModel => CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            CardModel fixture = await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, selectedCards: [fixture]);
            ctx.AssertEqual("selected card exhausted", PileType.Exhaust, fixture.Pile?.Type);
            ctx.AssertEqual("copies generated", upgraded ? upgradedCopies : baseCopies,
                PileType.Hand.GetPile(ctx.Player).Cards.OfType<StrikeIronclad>().Count());
        }, 2);

    private static void DamageAndTopDeckExhaust<T>(int baseDamage, int upgradedDamage,
        int baseCount, int upgradedCount, int bonusPerAttack) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            int count = upgraded ? upgradedCount : baseCount;
            await ctx.AddFillerCards(PileType.Draw, count);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            int expected = (upgraded ? upgradedDamage : baseDamage) + count * bonusPerAttack;
            ctx.AssertDamage("base plus exhausted attack damage", ctx.PrimaryEnemy, hp, expected);
            ctx.AssertEqual("top cards exhausted", count,
                PileType.Exhaust.GetPile(ctx.Player).Cards.OfType<StrikeIronclad>().Count());
        }, 2);

    private static void ExhaustTypesForPower<T>() where T : CardModel =>
        CustomVariants<T>(async (ctx, card, _) =>
        {
            await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.Add<SemenCurse>(PileType.Hand);
            await ctx.Add<DefendIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertEqual("attack and curse exhausted", 2,
                PileType.Exhaust.GetPile(ctx.Player).Cards.Count);
            ctx.AssertPower("strength per exhausted card", ctx.Self, "StrengthPower", 2);
        }, 2);

    private static void DamageAndTransformDraw<T>(int baseDamage, int upgradedDamage)
        where T : CardModel => CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            CardModel fixture = await ctx.Add<StrikeIronclad>(PileType.Draw);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy, [fixture]);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? upgradedDamage : baseDamage);
            ctx.AssertTrue("selected draw card transformed", fixture.HasBeenRemovedFromState);
        }, 2);

    private static void SelectedCardDouble<T>() where T : CardModel =>
        CustomVariants<T>(async (ctx, card, _) =>
        {
            MaidenStrike fixture = await ctx.Add<MaidenStrike>(PileType.Hand);
            decimal before = fixture.DynamicVars.Damage.BaseValue;
            await ctx.Play(card, selectedCards: [fixture]);
            ctx.AssertEqual("selected damage doubled", before * 2, fixture.DynamicVars.Damage.BaseValue);
        }, 1);

    private static void EmptyDescription<T>() where T : CardModel =>
        BaseOnly<T>((ctx, card) =>
        {
            ctx.AssertTrue("empty-description status has no active effect",
                card.Keywords.Contains(CardKeyword.Unplayable));
            return Task.CompletedTask;
        });

    private static void ConfigureEnchantmentChoice() =>
        BaseOnly<EnchantmentChoiceCard>((ctx, card) =>
        {
            card.Configure("fixture", "fixture");
            ctx.AssertEqual("configured enchantment id", "fixture", card.ChoiceId);
            return Task.CompletedTask;
        });

    private static void ConfigureQuestChoice() =>
        BaseOnly<FourthRouteQuestChoice>((ctx, card) =>
        {
            card.Configure("fixture", "name", "trial", "reward");
            ctx.AssertEqual("configured quest id", "fixture", card.QuestId);
            return Task.CompletedTask;
        });

    private static void ConfigureOverdraftChoice<T>(bool accept) where T : CardModel =>
        BaseOnly<T>((ctx, card) =>
        {
            if (card is OverdraftAcceptChoice accepted)
            {
                accepted.Configure(2);
                ctx.AssertEqual("configured armor cost", 2, accepted.DynamicVars["ArmorCost"].IntValue);
            }
            else
            {
                ctx.AssertEqual("decline helper has no armor-cost variable", false,
                    card.DynamicVars.ContainsKey("ArmorCost"));
            }
            return Task.CompletedTask;
        });

    private static void ConfigureStigmaChoice<T>(string expectedRole) where T : CardModel =>
        BaseOnly<T>((ctx, card) =>
        {
            if (card is StigmaTargetChoice target)
            {
                target.Configure(2, "fixture");
                ctx.AssertEqual("configured stigma target index", 2, target.TargetIndex);
            }
            else
            {
                ctx.AssertEqual($"stigma {expectedRole} helper cannot upgrade", 0,
                    card.MaxUpgradeLevel);
            }
            return Task.CompletedTask;
        });

    private static void HandCostRestriction<T>(CardType affectedType, int surcharge) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            DefendIronclad fixture = await ctx.Add<DefendIronclad>(PileType.Hand);
            ctx.AssertEqual("hand cost surcharge", 1 + surcharge,
                fixture.EnergyCost.GetWithModifiers(CostModifiers.All));
        });

    private static void HandPlayRestriction<T>(CardType affectedType) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            StrikeIronclad fixture = await ctx.Add<StrikeIronclad>(PileType.Hand);
            ctx.AssertTrue("hand play restriction active",
                !MegaCrit.Sts2.Core.Hooks.Hook.ShouldPlay(ctx.Combat, fixture, out _, MegaCrit.Sts2.Core.Entities.Cards.AutoPlayType.Default));
        });

    private static void HandEtherealProjection<T>() where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            StrikeIronclad fixture = await ctx.Add<StrikeIronclad>(PileType.Hand);
            ctx.AssertTrue("other hand cards become ethereal",
                fixture.Keywords.Contains(CardKeyword.Ethereal));
        });

    private static void DrawTriggerDesire<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Draw, skipVisuals: true);
            int before = Desire.Get(ctx.Player);
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Draw(
                new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(), 1, ctx.Player);
            ctx.AssertEqual("draw-triggered desire", amount, Desire.Get(ctx.Player) - before);
        });

    private static void EndTurnArmorLoss<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await ctx.ApplyPower<MaidenSuccubus.Powers.MagicArmorPower>(ctx.Self, 3);
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await ctx.InvokeTurnEndInHand(card);
            ctx.AssertPower("armor loss", ctx.Self, "MagicArmorPower", 3 - amount);
        });

    private static void EndTurnGenerate<TCard, TGenerated>(int count)
        where TCard : CardModel where TGenerated : CardModel => BaseOnly<TCard>(async (ctx, card) =>
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            int before = ctx.CountCards<TGenerated>(PileType.Draw);
            await ctx.InvokeTurnEndInHand(card);
            ctx.AssertPileDelta<TGenerated>("end-turn generated cards", PileType.Draw, before, count);
        });

    private static void CurseRemoved<T>() where T : CardModel => BaseOnly<T>(async (ctx, card) =>
    {
        await ctx.Play(card);
        ctx.AssertTrue("removed from combat after play", card.HasBeenRemovedFromState);
    });

    private static void CurseDesire<T>(int amount) where T : CardModel => BaseOnly<T>(async (ctx, card) =>
    {
        int before = Desire.Get(ctx.Player);
        await ctx.Play(card);
        ctx.AssertEqual("desire gained", amount, Desire.Get(ctx.Player) - before);
        ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
    }, 2);

    private static void CurseSelfPower<T>(string power, int amount)
        where T : CardModel => BaseOnly<T>(async (ctx, card) =>
        {
            await ctx.Play(card);
            ctx.AssertPower(power, ctx.Self, power, amount);
            ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
        }, 2);

    private static void CurseSelfPowers<T>(params (string Power, int Amount)[] powers)
        where T : CardModel => BaseOnly<T>(async (ctx, card) =>
        {
            await ctx.Play(card);
            foreach (var power in powers)
                ctx.AssertPower(power.Power, ctx.Self, power.Power, power.Amount);
            ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
        }, powers.Length + 1);

    private static void CurseHp<T>(int delta, int startMissingHp = 0) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await MegaCrit.Sts2.Core.Commands.CreatureCmd.SetCurrentHp(ctx.Self, ctx.Self.MaxHp - startMissingHp);
            int before = ctx.Self.CurrentHp;
            await ctx.Play(card);
            ctx.AssertEqual("hp delta", delta, ctx.Self.CurrentHp - before);
            ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
        }, 2);

    private static void CurseGenerate<TCard, TGenerated>(PileType pile, int count,
        bool generatedTypeMayBeUnavailable = false) where TCard : CardModel where TGenerated : CardModel =>
        BaseOnly<TCard>(async (ctx, card) =>
        {
            int before = ctx.CountCards<TGenerated>(pile);
            await ctx.Play(card);
            ctx.AssertPileDelta<TGenerated>("generated card", pile, before, count);
            ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
        }, 2);

    private static void CurseCardGeneration<T>(Action<CardEffectTestContext, CardModel> assertion)
        where T : CardModel => BaseOnly<T>(async (ctx, card) =>
        {
            await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            assertion(ctx, card);
            ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
        }, 2);

    private static void CurseArmorLoss<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await ctx.ApplyPower<MagicArmorPower>(ctx.Self, 2);
            await ctx.Play(card);
            ctx.AssertPower("armor after loss", ctx.Self, "MagicArmorPower", 2 - amount);
            ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
        }, 2);

    private static void CurseNextTurnEnergyLoss<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await ctx.Play(card);
            await PlayerCmd.SetEnergy(5, ctx.Player);
            int energy = ctx.Player.PlayerCombatState!.Energy;
            foreach (PowerModel power in ctx.Self.Powers.ToArray())
                await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertEqual("next-turn energy delta", -amount,
                ctx.Player.PlayerCombatState.Energy - energy);
            ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
        }, 2);

    private static void CurseRandomPower<T>() where T : CardModel => BaseOnly<T>(async (ctx, card) =>
    {
        await ctx.Play(card);
        int amount = new[] { "WeakPower", "FrailPower", "VulnerablePower", "StrengthPower", "DexterityPower" }
            .Sum(power => Math.Abs(ctx.PowerAmount(ctx.Self, power)));
        ctx.AssertEqual("exactly one random power layer", 1, amount);
        ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
    }, 2);

    private static void AssertExhaustKeyword(CardEffectTestContext context, CardModel card) =>
        context.AssertTrue("exhaust keyword", card.Keywords.Contains(CardKeyword.Exhaust));

    private static void AssertSelectedCardsEthereal(CardEffectTestContext context, CardModel _) =>
        context.AssertTrue("all selected cards become ethereal",
            PileType.Hand.GetPile(context.Player).Cards.All(card =>
                card.Keywords.Contains(CardKeyword.Ethereal)));

    private static void AssertSelfBurning(CardEffectTestContext context, CardModel _) =>
        context.AssertPower("self burning", context.Self, "BurningPower", 1);

    private static void AssertGeneratedCopyEthereal(CardEffectTestContext context, CardModel source) =>
        context.AssertTrue("generated copy is ethereal",
            PileType.Hand.GetPile(context.Player).Cards.OfType<WinterHolly>()
                .Any(card => card != source && card.Keywords.Contains(CardKeyword.Ethereal)));

    private static void AssertRandomOtherCardEthereal(CardEffectTestContext context, CardModel source) =>
        context.AssertEqual("exactly one other hand card becomes ethereal", 1,
            PileType.Hand.GetPile(context.Player).Cards.Count(card =>
                card != source && card.Keywords.Contains(CardKeyword.Ethereal)));

    // Registration plumbing -----------------------------------------------------
    private static void Pending<T>(string reason) where T : CardModel =>
        _specs.Add(new CardEffectSpec(typeof(T), CardUpgradePolicy.NotUpgradable, [], reason));

    private static void BaseOnly<T>(Func<CardEffectTestContext, T, Task> execute,
        int minimumAssertions = 1) where T : CardModel =>
        _specs.Add(new CardEffectSpec(typeof(T), CardUpgradePolicy.NotUpgradable,
        [new CardEffectScenario("base", false, minimumAssertions,
            (ctx, card) => execute(ctx, (T)card))]));

    private static void Variants<T>(Func<CardEffectTestContext, T, int, Task> execute,
        int baseValue, int upgradedValue, int minimumAssertions = 1,
        bool notUpgradable = false) where T : CardModel =>
        _specs.Add(new CardEffectSpec(typeof(T),
            notUpgradable ? CardUpgradePolicy.NotUpgradable : CardUpgradePolicy.BaseAndUpgraded,
            notUpgradable
                ? [new CardEffectScenario("base", false, minimumAssertions,
                    (ctx, card) => execute(ctx, (T)card, baseValue))]
                : [
                    new CardEffectScenario("base", false, minimumAssertions,
                        (ctx, card) => execute(ctx, (T)card, baseValue)),
                    new CardEffectScenario("upgraded", true, minimumAssertions,
                        (ctx, card) => execute(ctx, (T)card, upgradedValue)),
                ]));

    private static void VariantPairs<T>(Func<CardEffectTestContext, T, int, int, Task> execute,
        (int First, int Second) baseValues, (int First, int Second) upgradedValues,
        int minimumAssertions = 2) where T : CardModel =>
        _specs.Add(new CardEffectSpec(typeof(T), CardUpgradePolicy.BaseAndUpgraded,
        [
            new CardEffectScenario("base", false, minimumAssertions,
                (ctx, card) => execute(ctx, (T)card, baseValues.First, baseValues.Second)),
            new CardEffectScenario("upgraded", true, minimumAssertions,
                (ctx, card) => execute(ctx, (T)card, upgradedValues.First, upgradedValues.Second)),
        ]));

    private static void CustomVariants<T>(Func<CardEffectTestContext, T, bool, Task> execute,
        int minimumAssertions, bool notUpgradable = false) where T : CardModel =>
        _specs.Add(new CardEffectSpec(typeof(T),
            notUpgradable ? CardUpgradePolicy.NotUpgradable : CardUpgradePolicy.BaseAndUpgraded,
            notUpgradable
                ? [new CardEffectScenario("base", false, minimumAssertions,
                    (ctx, card) => execute(ctx, (T)card, false))]
                : [
                    new CardEffectScenario("base", false, minimumAssertions,
                        (ctx, card) => execute(ctx, (T)card, false)),
                    new CardEffectScenario("upgraded", true, minimumAssertions,
                        (ctx, card) => execute(ctx, (T)card, true)),
                ]));

}
#endif
