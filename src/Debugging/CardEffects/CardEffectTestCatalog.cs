#if DEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Monsters;
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
using MaidenSuccubus.Core.Replay;
using MaidenSuccubus.Core.Routes;
using MaidenSuccubus.Core.Seals;
using MaidenSuccubus.Core.Temptation;
using MaidenSuccubus.Core.Transformation;
using MaidenSuccubus.Data;
using MaidenSuccubus.Enchantments;
using MaidenSuccubus.Powers;
using MaidenSuccubus.Powers.Scriptures;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>
/// Executable 2026-09-16 iteration-two card oracle. Numeric literals come from DesignDoc,
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
        DesignSyncTransformationContract.Extend(_specs);
        return _specs.OrderBy(spec => spec.CardId, StringComparer.Ordinal).ToArray();
    }

    private static void RegisterNeutral()
    {
        CustomVariants<AcceleratedMotion>(DesignSyncAcceleratedMotionContract.Run, 41);
        BalanceBladeProbe();
        BalanceShieldProbe();
        BasicTrainingProbe();
        BathProbe();
        BeyondReasonForgeProbe();
        BindingInsightProbe();
        BorrowedForceStrikeProbe();
        HandDiscountProbe<BurningBracelet>(14, 20);
        CounterBarrierProbe();
        CustomVariants<CycloneRupture>((ctx, card, upgraded) => DesignSyncShatterRandomContract.Targeted(ctx, card, upgraded), 26);
        Block<DoubleDefense>(8, 12);
        DreamMistProbe();
        DreamPigmentProbe();
        CustomVariants<ExplosiveImpact>((ctx, card, upgraded) => DesignSyncShatterRandomContract.Random(ctx, card, upgraded), 14);
        FlameBloomProbe();
        FlameSwordProbe();
        CustomVariants<FlashStab>(DesignSyncChainCopyContract.Flash, 17);
        ForgeStrikeProbe();
        HandDiscountProbe<FrozenBracelet>(12, 16);
        GenerateChosenCard<MaidenSuccubus.Cards.Fusion>();
        GaleSwordProbe();
        GoddessOfIceProbe();
        HealingArtProbe();
        IceBreakingSlashProbe();
        Generate<IceShield, IceShard>(PileType.Hand, 3, 3,
            generatedUpgradedWithSource: true);
        JudgmentBladeProbe();
        DamageBlock<LightArrow>(5, 7, 5, 7);
        LightningRecoilProbe();
        LullabyProbe();
        MagiciansSecretProbe();
        MagicIndexProbe();
        MagicResonanceProbe();
        MagicStarBombProbe();
        MagicSwordProbe();
        MentalUnityProbe();
        MindsEyeProbe();
        ObstructingShotProbe();
        ProcrastinateProbe();
        BlockSelfPower<RepairAlyssa>(4, 7, "MagicArmorPower", 1, 1);
        ShiningSwordProbe();
        StudyPlanProbe();
        SummonThunderProbe();
        SurfProbe();
        SwordVerdictProbe();
        UltimateFlareProbe();
        WindGodCloakProbe();
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
        CustomVariants<BlasphemousTwilight>((ctx, card, upgraded) => DesignSyncShatterRandomContract.Random(ctx, card, upgraded), 14);
        BurningBladeRitualProbe();
        BurningDesireProbe();
        ChainDestructionProbe();
        ChangePantiesProbe();
        CoronationProbe();
        CurseInfectionProbe();
        CurseWedgeProbe();
        DarkElementProbe();
        DarkOriginProbe();
        DarkFlameBarrierProbe();
        DarkPunishmentProbe();
        DarkStormProbe();
        DamageDraw<DarkThrust>(9, 12, 2, 2);
        DemonStaffProbe();
        DesireRecycleProbe();
        DesireWhipProbe();
        DrawAndExhaustSelection<DestructionReaction>(3, 4);
        DesireDraw<EcstasyDew>(2, 3, 1, 1);
        ExhibitionistProbe();
        ExposePlayProbe();
        FearAuraProbe();
        DamageAndSelectedPileMove<FinalSlash>(9, 11, 1, 2, PileType.Draw, PileType.Discard);
        DrawEnergy<FleetingYears>(2, 3, 2, 2);
        FullOfOpeningsProbe();
        IgniteProbe();
        InsatiableGreedProbe();
        LastStandProbe();
        LegendaryMinerProbe();
        CustomVariants<LightningKick>((ctx, card, upgraded) => DesignSyncShatterRandomContract.Targeted(ctx, card, upgraded), 26);
        LordOfBlazeProbe();
        LoversDaggerProbe();
        LureDeepProbe();
        MasochisticGirlProbe();
        MasochisticTranceProbe();
        MentalStabilizerProbe();
        Energy<MiasmaAbsorption>(2, 3);
        DrawSelected<MiasmaAffinity>(1, 2, PileType.Draw, PileType.Hand,
            extra: AssertSelectedCardsEthereal);
        ExhaustTypesForPower<MiasmaConversion>();
        DamageAllTargetPower<MiasmaFlame>(7, 10, "BurningPower", 3, 3);
        MimicProliferationProbe();
        Draw<PlayingWithFire>(2, 3, extra: AssertSelfBurning);
        BlockDrawGenerate<PleasureDrowning, ArousalStatus>(7, 10, 2, 2, 2,
            PileType.Draw);
        PleasureGardenProbe();
        SelfPowers<PriceOfStrength>(("StrengthPower", 4, 4), ("ShatterPower", 4, 4));
        RecollectionRoomProbe();
        ReflectiveBarrierProbe();
        DamageAndTopDeckExhaust<SacrificialFrenzy>(
            12, 16, 3, 4, 6, CardRarity.Common);
        SemenAppetiteProbe();
        SemenConversionProbe();
        SharpForgeProbe();
        TargetPowers<SmallFry>(("VulnerablePower", 3, 4), ("StrengthPower", 1, 1));
        SuperRegenerationProbe();
        TentacleArmorProbe();
        ThousandCurseScytheProbe();
        WinterHollyProbe();
    }

    private static void RegisterHoly()
    {
        DiscardAutoPlayBlockProbe();
        DiscardAutoPlayDamageProbe();
        BattleTechniqueReplayProbe();
        BlizzardProbe();
        BurningRackProbe();
        CalmingMistProbe();
        ChantProbe();
        ChastityDefenseProbe();
        ConsecrationProbe();
        DesireWardProbe();
        BlockSelfPower<DevoutBulwark>(12, 15, "WeakPower", 2, 2);
        DivineEchoProbe();
        DragonflyTouchProbe();
        EternalDamnationProbe();
        ExorcismPerfumeProbe();
        FamiliarContractProbe();
        FinalJudgmentProbe();
        FocusedSlashProbe();
        SelectedEnchant<ForgeCharge>("Charge", 2, 3, fromPile: PileType.Discard);
        ForgeNimbleProbe();
        CustomVariants<Gospel>(DesignSyncScriptureGenerationContract.Gospel, 10);
        HolyCurseProbe();
        HolyFlameProbe();
        CustomVariants<HolyPunishment>(DesignSyncScriptureGenerationContract.Punishment, 30);
        HolyRadianceProbe();
        HolyResonanceProbe();
        InwardDisciplineProbe();
        DamageTargetPower<Judgment>(7, 7, "CondemnationPower", 2, 3);
        LightPowerReleaseProbe();
        LightWingsProbe();
        MagicBurstProbe();
        MemoryImprintProbe();
        BlockGenerate<MomentaryGrace, IceMist>(6, 8, PileType.Hand,
            generatedUpgradedWithSource: true);
        MultipleReproductionProbe();
        NoLewdnessProbe();
        OpeningPrayerProbe();
        OriginalSinBrandProbe();
        DamageSelfPower<PenanceSlash>(14, 17, "FrailPower", 2, 2);
        PhotonVoltProbe();
        PurificationOrbProbe();
        RegenerativeMagicFiberProbe();
        ResistanceGlovesProbe();
        RestProbe();
        RestraintEvasionProbe();
        Block<RetainedGuard>(8, 11);
        EnergySelfPower<SneakSnack>(2, 3, "CondemnationPower", 2, 2);
        SoulFuenikaProbe();
        SoulImpactProbe();
        SoulPurificationProbe();
        StigmaProbe();
        SunDanceProbe();
        TacticalAnalyzerProbe();
        TacticalCoreProbe();
        TakemikazuchiProbe();
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
        CurseArmorLoss<CorrosiveSlimeCurse>(1);
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
        PlayedArmorLoss<BarbedHookStatus>(1);
        EndTurnArmorLoss<BitingPaperStatus>(1, CardKeyword.Exhaust);
        Scripture<BlissScripture>();
        EndTurnArmorLoss<ClothingBurnStatus>(1, CardKeyword.Ethereal, CardKeyword.Exhaust);
        ConditionalDrawEnergy<CalmMind>(2, 3, 2, 3, minimumHand: 6);
        Pending<ClimaxBanCurse>("DesignDoc: 效果待后续设计");
        CounterBarrierTokenProbe<CounterBarrierII, CounterBarrierIII>(3);
        CounterBarrierTokenProbe<CounterBarrierIII, CounterBarrierIV>(5);
        CounterBarrierFinalProbe();
        RetainedEndTurnArmorLoss<DissolvingFluidStatus>(1);
        EmptyDescription<DrowsyStatus>();
        ConfigureEnchantmentChoice();
        ConfigureQuestChoice();
        LibraryPileChoiceProbe();
        HandCostRestriction<GagCurse>(CardType.Skill, 1, expectedOwnCost: 1);
        Scripture<GuardianScripture>();
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
        Scripture<NimbleScripture>();
        ConfigureOverdraftChoice<OverdraftAcceptChoice>(accept: true);
        ConfigureOverdraftChoice<OverdraftDeclineChoice>(accept: false);
        Scripture<PunishmentScripture>();
        ConfigureStigmaChoice<StigmaCondemnationChoice>("CondemnationPower");
        ConfigureStigmaChoice<StigmaTargetChoice>("target");
        ConfigureStigmaChoice<StigmaWeakChoice>("WeakPower");
        HandTemptation<TransparentOutfitCurse>(30);
        Scripture<VitalityScripture>();
        Scripture<WisdomScripture>();
    }

    // Generic executable probes -------------------------------------------------

    private static void CounterBarrierProbe() =>
        CustomVariants<CounterBarrier>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Rare, upgraded ? 0 : 1);
            ctx.AssertTrue("first barrier retains sinking", card.Keywords.Contains(MaidenSuccubus.Keywords.SinkingKeyword.Value), effect: false);
            await ctx.Play(card);
            ctx.AssertPower<ThornsPower>("first stage thorns", ctx.Self, 2);
            ctx.AssertPower<PlatingPower>("first stage plating", ctx.Self, 2);
            CounterBarrierII second = PileType.Discard.GetPile(ctx.Player).Cards
                .OfType<CounterBarrierII>().Single();
            ctx.AssertTrue("next stage is base as described", !second.IsUpgraded, effect: false);
            await ctx.Play(second);
            ctx.AssertPower<ThornsPower>("second stage cumulative thorns", ctx.Self, 5);
            ctx.AssertPower<PlatingPower>("second stage cumulative plating", ctx.Self, 5);
            CounterBarrierIII third = PileType.Discard.GetPile(ctx.Player).Cards
                .OfType<CounterBarrierIII>().Single();
            await ctx.Play(third);
            ctx.AssertPower<ThornsPower>("third stage cumulative thorns", ctx.Self, 10);
            ctx.AssertPower<PlatingPower>("third stage cumulative plating", ctx.Self, 10);
            CounterBarrierIV fourth = PileType.Discard.GetPile(ctx.Player).Cards
                .OfType<CounterBarrierIV>().Single();
            await ctx.Play(fourth);
            ctx.AssertPower<ThornsPower>("fourth stage cumulative thorns", ctx.Self, 40);
            ctx.AssertPower<PlatingPower>("fourth stage cumulative plating", ctx.Self, 40);
        }, 8);

    private static void CounterBarrierTokenProbe<TCard, TNext>(int thorns)
        where TCard : CardModel
        where TNext : CardModel =>
        CustomVariants<TCard>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Rare, upgraded ? 0 : 1);
            int before = ctx.CountCards<TNext>(PileType.Discard);
            await ctx.Play(card);
            ctx.AssertPower<ThornsPower>("thorns", ctx.Self, thorns);
            ctx.AssertPower<PlatingPower>("plating equals thorns", ctx.Self, thorns);
            ctx.AssertPileDelta<TNext>(
                "next counter-barrier stage generated", PileType.Discard, before, 1);
        }, 3);

    private static void CounterBarrierFinalProbe() =>
        CustomVariants<CounterBarrierIV>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Rare, upgraded ? 1 : 2);
            await ctx.Play(card);
            ctx.AssertPower<ThornsPower>("final-stage thorns", ctx.Self, 30);
            ctx.AssertPower<PlatingPower>("final-stage plating", ctx.Self, 30);
        }, 2);

    private static void DreamMistProbe() =>
        CustomVariants<DreamMist>(async (ctx, card, upgraded) =>
        {
            int amount = upgraded ? 3 : 2;
            AssertNeutralMetadata(ctx, card, CardRarity.Common, 0);
            await ctx.Play(card);
            foreach (Creature creature in ctx.Combat.Creatures)
            {
                ctx.AssertPower<WeakPower>("all-creature weak", creature, amount);
                ctx.AssertPower<VulnerablePower>("does not apply vulnerable", creature, 0);
            }
        }, 2);

    private static void ForgeStrikeProbe() =>
        CustomVariants<ForgeStrike>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Uncommon, 1);
            MaidenStrike selected = await ctx.Add<MaidenStrike>(PileType.Hand);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy, selectedCards: [selected]);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 9 : 6);
            ctx.AssertEqual("selected strike receives Instinct", "Instinct",
                selected.Enchantment?.GetType().Name ?? "none");
            ctx.AssertEqual("Instinct reduces selected strike cost", 0,
                selected.EnergyCost.GetWithModifiers(CostModifiers.All));

            await ctx.Reset();
            MaidenStrike zeroCost = await ctx.Add<MaidenStrike>(PileType.Hand);
            zeroCost.EnergyCost.UpgradeBy(-1);
            ctx.AssertTrue("zero-cost strike is not enchantable by Instinct",
                !ModelDb.Enchantment<Instinct>().CanEnchant(zeroCost), effect: false);
            await ctx.Play(ctx.Create<ForgeStrike>(upgraded), ctx.PrimaryEnemy);
            ctx.AssertTrue("no legal strike finishes without applying enchantment", zeroCost.Enchantment == null);
        }, 4);

    private static void AssertNeutralMetadata(CardEffectTestContext ctx, CardModel card, CardRarity rarity, int cost)
    {
        ctx.AssertEqual("DesignDoc rarity", rarity, card.Rarity, effect: false);
        ctx.AssertEqual("DesignDoc energy cost", cost,
            card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
    }

    private static void IceBreakingSlashProbe() =>
        CustomVariants<IceBreakingSlash>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Common, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("fixed seven damage", ctx.PrimaryEnemy, hp, 7);
            IceShard[] shards = PileType.Hand.GetPile(ctx.Player).Cards.OfType<IceShard>().ToArray();
            ctx.AssertEqual("one or two shards", upgraded ? 2 : 1, shards.Length);
            ctx.AssertTrue("generated shards are not upgraded", shards.All(shard => !shard.IsUpgraded));
        }, 3);

    private static void FlameBloomProbe() =>
        CustomVariants<FlameBloom>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Common, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage without release", ctx.PrimaryEnemy, hp, upgraded ? 11 : 8);
            ctx.AssertPower<BurningPower>("one base burning", ctx.PrimaryEnemy, 1);
            await ctx.Reset();
            await ctx.SetUpArmour(1);
            await ctx.Play(ctx.Create<FlameBloom>(upgraded), ctx.PrimaryEnemy, selectedIndices: [0]);
            ctx.AssertPower<BurningPower>("release adds one more burning", ctx.PrimaryEnemy, 2);
        }, 3);

    private static void MagicStarBombProbe() =>
        CustomVariants<MagicStarBomb>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
            int energy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card);
            UltimateFlarePower power =
                ctx.Self.Powers.OfType<UltimateFlarePower>().Single();
            ctx.AssertEqual("amplified delayed damage",
                upgraded ? 42m : 30m, power.Damage);
            ctx.AssertEqual("magic release gains one energy", 1,
                ctx.Player.PlayerCombatState.Energy - energy);
        }, 2);

    private static void SummonThunderProbe() =>
        CustomVariants<SummonThunder>(DesignSyncChainCopyContract.Thunder, 33);

    private static void TakemikazuchiProbe() =>
        CustomVariants<Takemikazuchi>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<TakemikazuchiTrackerPower>(ctx.Self, 1);
            TakemikazuchiTrackerPower tracker = ctx.Self
                .Powers.OfType<TakemikazuchiTrackerPower>().Single();
            tracker.PlayedEnchantedCards = 3;
            DesignSyncCombatTextContract.TrackedHits(ctx, card, upgraded);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("two base hits plus played enchanted cards",
                ctx.PrimaryEnemy, hp, upgraded ? 40 : 30);
        }, 1);

    private static void GaleSwordProbe() =>
        CustomVariants<GaleSword>((ctx, card, upgraded) => DesignSyncEnchantmentInputContract.Sword(ctx, card, upgraded), 17);

    private static void ShiningSwordProbe() =>
        CustomVariants<ShiningSword>((ctx, card, upgraded) => DesignSyncEnchantmentInputContract.Sword(ctx, card, upgraded), 17);

    private static void FlameSwordProbe() =>
        CustomVariants<FlameSword>(DesignSyncFlameSwordContract.Run, 35);

    private static void WindGodCloakProbe() =>
        CustomVariants<WindGodCloak>(DesignSyncWindGodCloakContract.Run, 25);

    private static void HolyFlameProbe() =>
        CustomVariants<HolyFlame>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertPower<BurningPower>("initial burning", ctx.PrimaryEnemy,
                upgraded ? 5 : 4);
            ctx.AssertPower<HolyFlamePower>("future burning bonus", ctx.Self,
                upgraded ? 3 : 2);
            await ctx.ApplyPower<BurningPower>(ctx.PrimaryEnemy, 1);
            ctx.AssertPower<BurningPower>("subsequent burning receives bonus",
                ctx.PrimaryEnemy, upgraded ? 9 : 7);
        }, 3);

    private static void BurningRackProbe() =>
        CustomVariants<BurningRack>(async (ctx, card, upgraded) =>
        {
            int block = ctx.Self.Block;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertPower<BurningPower>("rack burning", ctx.PrimaryEnemy, upgraded ? 3 : 2);
            ctx.AssertPower<WeakPower>("rack no longer applies weak", ctx.PrimaryEnemy, 0);
            ctx.AssertBlock("rack block", block, upgraded ? 3 : 2);
            ctx.AssertEqual("rack is reusable", PileType.Discard, card.Pile?.Type);
        }, 4);

    private static void CalmingMistProbe() =>
        CustomVariants<CalmingMist>(async (ctx, card, upgraded) =>
        {
            foreach (bool friendly in new[] { false, true })
            {
                await ctx.Reset();
                CalmingMist current = ctx.Create<CalmingMist>(upgraded);
                Creature target = friendly ? ctx.Self : ctx.PrimaryEnemy;
                ctx.AssertTrue("mist permits selected living character", current.IsValidTarget(target), effect: false);
                await ctx.AddFillerCards(PileType.Draw, 3);
                await ctx.Play(current, target);
                ctx.AssertEqual("mist draws before weak", upgraded ? 2 : 1, ctx.CountCards<StrikeIronclad>(PileType.Hand));
                ctx.AssertPower<WeakPower>("selected character receives weak", target, 2);
                ctx.AssertPower<WeakPower>("unselected side remains unchanged", friendly ? ctx.PrimaryEnemy : ctx.Self, 0);
            }
        }, 6);

    private static void ExorcismPerfumeProbe() =>
        CustomVariants<ExorcismPerfume>(async (ctx, card, upgraded) =>
        {
            await Temptation.Initialize(new BlockingPlayerChoiceContext(), ctx.Player);
            await Temptation.Modify(
                new BlockingPlayerChoiceContext(), ctx.Player, 20);
            await ctx.AddFillerCards(PileType.Draw, 1);
            int beforeTemptation = Temptation.Get(ctx.Player);
            int beforeHand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertEqual("temptation lost", upgraded ? 15 : 10,
                beforeTemptation - Temptation.Get(ctx.Player));
            ctx.AssertPileDelta<StrikeIronclad>("draw one", PileType.Hand,
                beforeHand, 1);
        }, 2);

    private static void PurificationOrbProbe() =>
        CustomVariants<PurificationOrb>(async (ctx, card, upgraded) =>
        {
            await TransformationCmd.EnterImmaculateRobe(
                new BlockingPlayerChoiceContext(), ctx.Self, null);
            await Desire.Set(ctx.Player, 2);
            await Temptation.Modify(
                new BlockingPlayerChoiceContext(), ctx.Player, 20);
            int temptation = Temptation.Get(ctx.Player);
            await ctx.Play(card);
            ctx.AssertEqual("desire reduced to zero", 0, Desire.Get(ctx.Player));
            ctx.AssertPower<MagicArmorPower>("armor gained", ctx.Self, 4);
            ctx.AssertEqual("temptation modifier lost", 10,
                temptation - Temptation.Get(ctx.Player));
            ctx.AssertEqual("upgrade makes cost zero", upgraded ? 0 : 1,
                card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
        }, 3);

    private static void SoulFuenikaProbe() =>
        CustomVariants<SoulFuenika>(async (ctx, card, upgraded) =>
        {
            HashSet<CardModel> deckBefore = ctx.Player.Deck.Cards.ToHashSet();
            HashSet<CardModel> handBefore = PileType.Hand.GetPile(ctx.Player).Cards
                .ToHashSet();
            await ctx.Play(card, selectedIndices: [0]);
            CardModel selected = PileType.Hand.GetPile(ctx.Player).Cards
                .Single(candidate => !handBefore.Contains(candidate)
                    && candidate is MSHolyCard);
            ctx.AssertTrue("chosen holy card added to hand", selected is MSHolyCard);
            ctx.AssertEqual("post-combat copy scheduled", 1,
                card.PendingPostCombatCards.Count);
            ctx.AssertEqual("scheduled card is the chosen card", selected.Id,
                card.PendingPostCombatCards.Single().Id!);

            int deckCountBefore = ctx.Player.Deck.Cards.Count;
            await card.AfterCombatEnd(null!);
            CardModel[] added = ctx.Player.Deck.Cards
                .Where(candidate => !deckBefore.Contains(candidate))
                .ToArray();
            try
            {
                ctx.AssertEqual("unavoidable post-combat deck copy", deckCountBefore + 1,
                    ctx.Player.Deck.Cards.Count);
                ctx.AssertEqual("copy is the chosen card", selected.Id,
                    added.Single().Id);
                ctx.AssertEqual("copy preserves chosen-card upgrade",
                    selected.CurrentUpgradeLevel,
                    added.Single().CurrentUpgradeLevel);
                ctx.AssertEqual("copy schedule consumed", 0,
                    card.PendingPostCombatCards.Count);
                await card.AfterCombatEnd(null!);
                ctx.AssertEqual("repeated combat-end notification adds no duplicate",
                    deckCountBefore + 1, ctx.Player.Deck.Cards.Count);
            }
            finally
            {
                foreach (CardModel copy in added)
                {
                    if (!copy.HasBeenRemovedFromState
                        && copy.Pile?.Type == PileType.Deck)
                    {
                        await CardPileCmd.RemoveFromDeck(copy, showPreview: false);
                    }
                }
            }

            SoulFuenika skipped = ctx.Create<SoulFuenika>(upgraded);
            HashSet<CardModel> handBeforeSkip = PileType.Hand.GetPile(ctx.Player)
                .Cards.ToHashSet();
            int deckBeforeSkip = ctx.Player.Deck.Cards.Count;
            await ctx.Play(skipped, selectedIndices: []);
            ctx.AssertEqual("skip schedules no card", 0, skipped.PendingPostCombatCards.Count);
            ctx.AssertTrue("skip generates no hand card",
                handBeforeSkip.SetEquals(PileType.Hand.GetPile(ctx.Player).Cards));
            await skipped.AfterCombatEnd(null!);
            ctx.AssertEqual("skip produces no post-combat reward", deckBeforeSkip,
                ctx.Player.Deck.Cards.Count);
        }, 9);

    private static void FamiliarContractProbe() =>
        CustomVariants<FamiliarContract>(async (ctx, card, upgraded) =>
        {
            await PlayerCmd.SetEnergy(2, ctx.Player);
            int hand = PileType.Hand.GetPile(ctx.Player).Cards.Count;
            await ctx.Play(card);
            CardModel[] familiars = PileType.Hand.GetPile(ctx.Player).Cards
                .Where(candidate => candidate.Enchantment is FamiliarEnchantment)
                .ToArray();
            ctx.AssertEqual("X=2 creates two cards", hand + 2,
                PileType.Hand.GetPile(ctx.Player).Cards.Count);
            ctx.AssertEqual("all generated cards receive Familiar", 2,
                familiars.Length);
            ctx.AssertTrue("generated cards match contract upgrade",
                familiars.All(generated => generated.IsUpgraded == upgraded));

            MaidenStrike trigger = await ctx.Add<MaidenStrike>(PileType.Hand);
            FamiliarEnchantment familiar =
                CombatEnchantmentCmd.Apply<FamiliarEnchantment>(trigger, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await familiar.BeforeSideTurnEnd(
                new BlockingPlayerChoiceContext(),
                CombatSide.Player,
                [ctx.Self]);
            ctx.AssertDamage("Familiar auto-plays at turn end",
                ctx.PrimaryEnemy, hp, 6);
            ctx.AssertTrue("auto-play removes Familiar card from hand",
                trigger.Pile?.Type != PileType.Hand);
        }, 5);

    private static void DarkStormProbe() =>
        CustomVariants<DarkStorm>(DesignSyncDarkStormContract.Run, 26);

    private static void LightWingsProbe() =>
        CustomVariants<LightWings>(DesignSyncLightWingsContract.Run, 35);

    private static void OpeningPrayerProbe() =>
        CustomVariants<OpeningPrayer>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            OpeningPrayerPower power = ctx.Self.Powers
                .OfType<OpeningPrayerPower>().Single();
            ctx.AssertEqual("scheduled turn count", upgraded ? 3 : 2, power.Amount);
            await power.AfterPlayerTurnStart(
                new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("one amplification next turn", ctx.Self,
                "MagicAmplificationPower", 1);
            ctx.AssertEqual("one scheduled turn consumed",
                upgraded ? 2 : 1,
                ctx.Self.Powers.OfType<OpeningPrayerPower>().Single().Amount);
            for (int i = 1; i < (upgraded ? 3 : 2); i++)
                await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower<MagicAmplificationPower>("all scheduled prayer turns granted", ctx.Self, upgraded ? 3 : 2);
            ctx.AssertTrue("prayer removed after final scheduled turn", !ctx.Self.HasPower<OpeningPrayerPower>());
        }, 5);

    // Reusable probe helpers -----------------------------------------------------

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
            int startingPower = 0;
            if (power == nameof(MagicArmorPower))
            {
                await TransformationCmd.EnterImmaculateRobe(
                    new BlockingPlayerChoiceContext(), ctx.Self, null);
                MagicArmorPower armor = ctx.Self.Powers
                    .OfType<MagicArmorPower>().Single();
                await PowerCmd.ModifyAmount(
                    new BlockingPlayerChoiceContext(),
                    armor,
                    -2,
                    ctx.Self,
                    null);
                startingPower = 1;
            }
            int before = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", before, upgraded ? upgradedBlock : baseBlock);
            ctx.AssertPower(power, ctx.Self, power,
                startingPower + (upgraded ? upgradedPower : basePower));
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

            await ctx.AddFillerCards(PileType.Hand, minimumHand);
            await ctx.AddFillerCards(PileType.Draw, 10);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            int energy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card);
            ctx.AssertPileDelta<StrikeIronclad>("conditional cards drawn", PileType.Hand, hand,
                upgraded ? upgradedDraw : baseDraw);
            ctx.AssertEqual("conditional energy gained", upgraded ? upgradedEnergy : baseEnergy,
                ctx.Player.PlayerCombatState.Energy - energy);
        }, 3);

    private static void Scripture<T>()
        where T : CardModel => CustomVariants<T>(DesignSyncScriptureContract.Run, 20);

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
        CustomVariants<GoddessOfIce>(DesignSyncIceGoddessContract.Run, 20);

    private static void LullabyProbe() =>
        CustomVariants<Lullaby>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Rare, upgraded ? 1 : 2);
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
        CustomVariants<MagicIndex>(DesignSyncEnchantmentInputContract.Index, 25);

    private static void DreamPigmentProbe() =>
        CustomVariants<DreamPigment>(async (ctx, _, upgraded) =>
        {
            foreach (string mode in new[] { "normal", "missing-holy", "near-full", "no-draw", "empty" })
            {
                await ctx.Reset();
                CardModel corrupt = await ctx.Add<DarkThrust>(PileType.Draw);
                CardModel holy = await ctx.Add<PhotonVolt>(PileType.Draw);
                CardModel neutral = await ctx.Add<MaidenDefend>(PileType.Draw);
                CardModel secondCorrupt = await ctx.Add<MiasmaAbsorption>(PileType.Draw);
                CardModel vanilla = await ctx.Add<StrikeIronclad>(PileType.Draw);
                CardModel generated = await ctx.Add<IceShard>(PileType.Draw);
                CardModel discarded = await ctx.Add<PhotonVolt>(PileType.Discard);
                int notifications = 0;
                foreach (CardModel drawn in new[] { corrupt, holy, neutral })
                    drawn.Drawn += () => notifications++;
                if (mode == "missing-holy")
                    await CardPileCmd.Add(holy, PileType.Discard, skipVisuals: true);
                if (mode == "near-full")
                    await ctx.AddFillerCards(PileType.Hand, 9);
                if (mode == "no-draw")
                    await ctx.ApplyPower<YarusLibraryPower>(ctx.Self, 1);
                if (mode == "empty")
                    foreach (CardModel candidate in PileType.Draw.GetPile(ctx.Player).Cards.ToArray())
                        await CardPileCmd.Add(candidate, PileType.Discard, skipVisuals: true);
                await ctx.Play(ctx.Create<DreamPigment>(upgraded));
                bool blocked = mode is "no-draw" or "empty";
                ctx.AssertEqual(mode + ": corrupt first", !blocked, corrupt.Pile?.Type == PileType.Hand);
                ctx.AssertEqual(mode + ": holy", mode == "normal", holy.Pile?.Type == PileType.Hand);
                ctx.AssertEqual(mode + ": neutral", mode is "normal" or "missing-holy", neutral.Pile?.Type == PileType.Hand);
                ctx.AssertEqual(mode + ": draw notifications", blocked ? 0 : mode == "near-full" ? 1 : mode == "missing-holy" ? 2 : 3,
                    notifications);
                ctx.AssertTrue(mode + ": one card per route", secondCorrupt.Pile?.Type != PileType.Hand);
                ctx.AssertTrue(mode + ": no vanilla or generated-pool substitution",
                    vanilla.Pile?.Type != PileType.Hand && generated.Pile?.Type != PileType.Hand);
                ctx.AssertEqual(mode + ": no discard reshuffle", PileType.Discard, discarded.Pile?.Type);
                ctx.AssertTrue(mode + ": hand limit", PileType.Hand.GetPile(ctx.Player).Cards.Count <= 10);
            }
            foreach (int corruption in new[] { -3, 3 })
            {
                await ctx.Reset();
                CorruptionCmd.Set((MegaCrit.Sts2.Core.Runs.RunState)ctx.Player.RunState, corruption);
                CardModel first = await ctx.Add<DarkElement>(PileType.Draw);
                CardModel second = await ctx.Add<Transform>(PileType.Draw);
                CardModel neutral = await ctx.Add<MaidenDefend>(PileType.Draw);
                await ctx.Play(ctx.Create<DreamPigment>(upgraded));
                // Both starter cards have the same live route at these extremes.
                // Draw just the first; their registered pools are not the rule.
                ctx.AssertEqual("variation uses current route " + corruption, PileType.Hand, first.Pile?.Type);
                ctx.AssertEqual("variation does not use original pool " + corruption, PileType.Draw, second.Pile?.Type);
                ctx.AssertEqual("variation still draws neutral " + corruption, PileType.Hand, neutral.Pile?.Type);
            }
        }, 46);

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

            MagicResonance projected = await ctx.Add<MagicResonance>(
                PileType.Hand, upgraded);
            projected.AddKeyword(CardKeyword.Retain);
            Glam originalEnchantment =
                (Glam)ModelDb.Enchantment<Glam>().ToMutable();
            projected.EnchantInternal(originalEnchantment, 1);
            Bound originalAffliction =
                (Bound)ModelDb.Affliction<Bound>().ToMutable();
            projected.AfflictInternal(originalAffliction, 1);
            originalAffliction.AfterApplied();
            ControlPower control = (ControlPower)ModelDb.Power<ControlPower>().ToMutable();
            control.ControlType = ControlType.Power;
            await PowerCmd.Apply(new BlockingPlayerChoiceContext(), control,
                ctx.Self, 3, ctx.PrimaryEnemy, null);
            ctx.AssertTrue("power card is projected while controlled",
                ControlQuery.GetProjection(projected) != null);
            string projectedTitle = string.Empty;
            string projectedDescription = string.Empty;
            IReadOnlySet<CardKeyword> projectedKeywords = new HashSet<CardKeyword>();
            TargetType projectedTarget = TargetType.AnyEnemy;
            for (int i = 0; i < 32; i++)
            {
                projectedTitle = projected.Title;
                projectedDescription = projected.GetDescriptionForPile(PileType.Hand);
                projectedKeywords = projected.Keywords;
                projectedTarget = projected.TargetType;
            }
            ctx.AssertEqual("projected title remains stable under repeated reads",
                new LocString("cards", "MAIDENSUCCUBUS_ESCAPE.title")
                    .GetFormattedText(), projectedTitle);
            ctx.AssertTrue("projected description exposes escape amount",
                projectedDescription.Contains("3", StringComparison.Ordinal));
            ctx.AssertEqual("projected keywords remain hidden", 0,
                projectedKeywords.Count);
            ctx.AssertTrue("projected enchantment remains hidden",
                projected.Enchantment == null);
            ctx.AssertTrue("projected affliction remains hidden",
                projected.Affliction == null);
            ctx.AssertEqual("projected target remains none", TargetType.None,
                projectedTarget);
            ctx.AssertTrue("serialization preserves original enchantment",
                projected.ToSerializable().Enchantment != null);
            int resonanceAmount = resonance.Amount;
            await ctx.Play(projected);
            ctx.AssertEqual("projected card original effect is suppressed",
                resonanceAmount, resonance.Amount);
            ctx.AssertEqual("projected card pays one escape point", 2, control.Amount);
            ctx.AssertEqual("projected card resolves to discard", PileType.Discard,
                projected.Pile?.Type ?? PileType.None);
            await PowerCmd.Remove(control);
            ctx.AssertTrue("original keyword restores after control",
                projected.Keywords.Contains(CardKeyword.Retain));
            ctx.AssertTrue("original enchantment restores after control",
                projected.Enchantment is Glam);
            ctx.AssertTrue("original affliction restores after control",
                projected.Affliction is Bound);
        }, 7);

    private static void StudyPlanProbe() =>
        CustomVariants<StudyPlan>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 10);
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? 10 : 7);
            PowerModel next = ctx.Self.Powers.Single(power =>
                power.GetType().Name == "DrawCardsNextTurnPower");
            ctx.AssertEqual("next-turn draw amount", 2, next.Amount);
            next.AmountOnTurnStart = next.Amount;
            decimal draw = MegaCrit.Sts2.Core.Hooks.Hook.ModifyHandDraw(
                ctx.Combat, ctx.Player, 5, out IEnumerable<AbstractModel> modifiers);
            await MegaCrit.Sts2.Core.Hooks.Hook.AfterModifyingHandDraw(
                ctx.Combat, modifiers);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await CardPileCmd.Draw(
                new BlockingPlayerChoiceContext(), draw, ctx.Player, fromHandDraw: true);
            ctx.AssertPileDelta<StrikeIronclad>("actual next-turn hand draw",
                PileType.Hand, hand, 7);
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
            ctx.AssertEqual("counter starts four exhausts from trigger", 4,
                power.DisplayAmount);
            for (int i = 0; i < 4; i++)
            {
                await CardCmd.Exhaust(new BlockingPlayerChoiceContext(),
                    await ctx.Add<MaidenDefend>(PileType.Hand));
                ctx.AssertEqual("counter displays remaining exhausts",
                    i == 3 ? 4 : 3 - i,
                    power.DisplayAmount);
            }
            ctx.AssertPower("four exhausts arm one visible replay", ctx.Self,
                "ChainDestructionReplayPower", 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(ctx.Create<MaidenStrike>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("next card plays twice", ctx.PrimaryEnemy, hp, 12);
            ctx.AssertPower("visible replay consumed", ctx.Self,
                "ChainDestructionReplayPower", 0);
        }, 8);

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

    private static void ExhibitionistProbe() =>
        CustomVariants<Exhibitionist>(async (ctx, card, _) =>
        {
            await Temptation.Modify(
                new BlockingPlayerChoiceContext(), ctx.Player, 7);
            int currentTemptation = Temptation.Get(ctx.Player);
            int block = ctx.Self.Block;
            int statuses = ctx.CountCards<NakedDesireStatus>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertBlock("block equals current temptation", block, currentTemptation);
            ctx.AssertPileDelta<NakedDesireStatus>(
                "generates naked desire", PileType.Hand, statuses, 1);
        }, 2);

    private static void FearAuraProbe() =>
        CustomVariants<FearAura>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
            await ctx.Play(card);
            int amount = upgraded ? 4 : 3;
            foreach (Creature enemy in ctx.Enemies)
            {
                ctx.AssertPower($"strength loss {enemy.Name}", enemy,
                    "FearAuraStrengthLossPower", amount);
            }
            ctx.AssertPower("temporary magic-release strength", ctx.Self,
                "StrengthPower", amount);
            ctx.AssertPower("temporary strength restoration marker", ctx.Self,
                "RestoreStrengthAtTurnEndPower", -amount);
        }, 3);

    private static void WinterHollyProbe() =>
        CustomVariants<WinterHolly>(async (ctx, card, upgraded) =>
        {
            await Desire.Set(ctx.Player, 2);
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await card.SpendResources();
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("block", block, upgraded ? 13 : 10);
            ctx.AssertEqual("desire cost", 0, Desire.Get(ctx.Player));
            WinterHolly copy = PileType.Hand.GetPile(ctx.Player).Cards
                .OfType<WinterHolly>().Single();
            ctx.AssertTrue("copy retains ethereal",
                copy.Keywords.Contains(CardKeyword.Ethereal));
            ctx.AssertTrue("copy gains exhaust",
                copy.Keywords.Contains(CardKeyword.Exhaust));
            ctx.AssertEqual("copy preserves upgrade", upgraded, copy.IsUpgraded);
        }, 5);

    private static void DarkFlameBarrierProbe() =>
        CustomVariants<DarkFlameBarrier>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
            int block = ctx.Self.Block;
            await ctx.Play(card);
            ctx.AssertBlock("amplified magic-release block", block, upgraded ? 13 : 9);
            ctx.AssertPower("barrier duration", ctx.Self,
                "DarkFlameBarrierPower", 2);
            await ctx.ApplyPower<BurningPower>(ctx.PrimaryEnemy, 1);
            await CreatureCmd.LoseBlock(new BlockingPlayerChoiceContext(), ctx.Self,
                ctx.Self.Block, null);
            int hp = ctx.Self.CurrentHp;
            await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), ctx.Self,
                10, ValueProp.Move, ctx.PrimaryEnemy);
            ctx.AssertDamage("burning-enemy damage halved", ctx.Self, hp, 5);
        }, 3);

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
        CustomVariants<InsatiableGreed>(DesignSyncLibraryContract.Run, 200);

    private static void LibraryPileChoiceProbe() =>
        CustomVariants<LibraryPileChoice>((ctx, card, _) =>
        {
            foreach (PileType pile in new[] { PileType.Draw, PileType.Discard, PileType.Exhaust })
            {
                card.Configure(pile);
                ctx.AssertEqual("choice keeps selected pile", pile, card.SelectedPile, effect: false);
                string expectedTitle = pile switch
                {
                    PileType.Draw => "抽牌堆",
                    PileType.Discard => "弃牌堆",
                    _ => "消耗牌堆",
                };
                ctx.AssertEqual("choice title binds its own pile", expectedTitle, card.Title, effect: false);
            }
            return Task.CompletedTask;
        }, 0);

    private static void DarkOriginProbe() =>
        CustomVariants<DarkOrigin>(async (ctx, card, upgraded) =>
        {
            ctx.AssertEqual("origin is ancient", CardRarity.Ancient, card.Rarity, effect: false);
            ctx.AssertTrue("origin registered as tooth transcendence",
                MegaCrit.Sts2.Core.Models.Relics.ArchaicTooth.TranscendenceCards
                    .Any(candidate => candidate.Id == card.Id), effect: false);
            var run = (MegaCrit.Sts2.Core.Runs.RunState)ctx.Player.RunState;
            foreach (int corruption in new[] { -3, -2, 0 })
            {
                await ctx.Reset();
                CorruptionCmd.Set(run, corruption);
                DarkOrigin current = ctx.Create<DarkOrigin>(upgraded);
                ctx.AssertEqual("origin variation route", corruption <= -3 ? RouteCardKind.Holy : RouteCardKind.Corrupt,
                    current.RouteKind);
                await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
                int hp = ctx.PrimaryEnemy.CurrentHp;
                int block = ctx.Self.Block;
                await ctx.Play(current, ctx.PrimaryEnemy);
                int amplified = upgraded ? 18 : 12;
                ctx.AssertDamage("origin released damage", ctx.PrimaryEnemy, hp,
                    amplified * (corruption <= -3 ? 1 : 2));
                ctx.AssertBlock("origin released block", block, corruption <= -3 ? amplified : 0);
            }
        }, 9);

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
        CustomVariants<RecollectionRoom>(async (ctx, card, upgraded) =>
        {
            ctx.AssertEqual("upgraded Recollection Room is innate", upgraded,
                card.Keywords.Contains(CardKeyword.Innate), effect: false);
            ctx.AssertEqual("upgrade does not give retain", false,
                card.Keywords.Contains(CardKeyword.Retain), effect: false);
            foreach (int available in new[] { 0, 1, 2, 6, 7 })
            {
                await ctx.Reset();
                await ctx.Play(ctx.Create<RecollectionRoom>(upgraded));
                IReadOnlyList<CardModel> exhausted = await ctx.AddFillerCards(
                    PileType.Exhaust, available);
                int drawNotifications = 0;
                foreach (CardModel recoveredCard in exhausted)
                    recoveredCard.Drawn += () => drawNotifications++;
                IReadOnlyList<CardModel> normal = await ctx.AddFillerCards(PileType.Draw, 8);
                decimal remainder = MegaCrit.Sts2.Core.Hooks.Hook.ModifyHandDraw(
                    ctx.Combat, ctx.Player, 5, out IEnumerable<AbstractModel> modifiers);
                await MegaCrit.Sts2.Core.Hooks.Hook.AfterModifyingHandDraw(ctx.Combat, modifiers);
                await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), remainder,
                    ctx.Player, fromHandDraw: true);
                int recovered = Math.Min(6, available);
                ctx.AssertEqual($"exhaust={available}: remainder", 6m - recovered, remainder);
                ctx.AssertEqual($"exhaust={available}: recover first", recovered,
                    exhausted.Count(candidate => candidate.Pile?.Type == PileType.Hand));
                ctx.AssertEqual($"exhaust={available}: draw notifications", recovered, drawNotifications);
                ctx.AssertEqual($"exhaust={available}: normal draw fills only remainder", 6 - recovered,
                    normal.Count(candidate => candidate.Pile?.Type == PileType.Hand));
                int exhaustLeft = PileType.Exhaust.GetPile(ctx.Player).Cards.Count;
                await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, ctx.Player);
                ctx.AssertEqual($"exhaust={available}: manual draw leaves exhaust alone", exhaustLeft,
                    PileType.Exhaust.GetPile(ctx.Player).Cards.Count);
            }
            await ctx.Reset();
            await ctx.Play(ctx.Create<RecollectionRoom>(upgraded));
            await ctx.AddFillerCards(PileType.Hand, 9);
            await ctx.AddFillerCards(PileType.Exhaust, 6);
            decimal overflow = MegaCrit.Sts2.Core.Hooks.Hook.ModifyHandDraw(
                ctx.Combat, ctx.Player, 5, out IEnumerable<AbstractModel> fullHandModifiers);
            await MegaCrit.Sts2.Core.Hooks.Hook.AfterModifyingHandDraw(ctx.Combat, fullHandModifiers);
            await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), overflow,
                ctx.Player, fromHandDraw: true);
            ctx.AssertEqual("recovery respects ten-card hand limit", 10,
                PileType.Hand.GetPile(ctx.Player).Cards.Count);
            ctx.AssertEqual("hand overflow remains exhausted", 5,
                PileType.Exhaust.GetPile(ctx.Player).Cards.Count);
        }, 27);

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
            BattleTechniqueReplay replay = await ctx.Add<BattleTechniqueReplay>(
                PileType.Hand, upgraded, skipVisuals: false);
            ctx.AssertEqual("entering combat installs replay power", 1,
                ctx.PowerAmount<BattleTechniqueReplayPower>(ctx.Self));
            ctx.AssertEqual("replay listener power stays hidden", false,
                ctx.Self.GetPower<BattleTechniqueReplayPower>()!.IsVisible);
            MaidenStrike strike = ctx.Create<MaidenStrike>();
            await ctx.Play(strike, ctx.PrimaryEnemy);

            MaidenStrike projected = PileType.Hand.GetPile(ctx.Player).Cards
                .OfType<MaidenStrike>().Single();
            ctx.AssertEqual("replay card transformed to last-played type",
                typeof(MaidenStrike), projected.GetType());
            ctx.AssertEqual("upgraded replay projection retains", upgraded,
                projected.Keywords.Contains(CardKeyword.Retain));
            ctx.AssertTrue("replay projection carries source shadow",
                projected.TryGetCapability<BattleReplayOriginCapability>(out _));
            ctx.AssertTrue("original replay instance was transformed",
                replay.HasBeenRemovedFromState);
            DesignSyncCombatTextContract.AssertText(ctx, projected, PileType.Hand,
                (upgraded ? "保留。\n" : "") + "造成6点伤害。", "replay projection renders copied strike not source text");

            await ctx.Play(projected, ctx.PrimaryEnemy);
            BattleTechniqueReplay restored = PileType.Discard.GetPile(ctx.Player).Cards
                .OfType<BattleTechniqueReplay>().Single();
            ctx.AssertEqual("projection restores replay card after use",
                typeof(BattleTechniqueReplay), restored.GetType());
            ctx.AssertEqual("restored replay preserves upgrade", upgraded,
                restored.IsUpgraded);
            ctx.AssertEqual("restored replay drops source shadow", false,
                restored.TryGetCapability<BattleReplayOriginCapability>(out _));
            ctx.AssertEqual("played projection leaves play pile", 0,
                PileType.Play.GetPile(ctx.Player).Cards.Count);
            DesignSyncCombatTextContract.AssertText(ctx, restored, PileType.Discard,
                DesignSyncCombatTextContract.ExpectedOutside(restored, upgraded), "replay restores full original description");
        }, 7);

    private static void DesireWhipProbe() =>
        CustomVariants<DesireWhip>(async (ctx, card, _) =>
        {
            IntentMoveFactory.SetTransient(ctx.PrimaryEnemy.Monster!,
                IntentMoveFactory.CreateControl(ctx.PrimaryEnemy.Monster!,
                    new ControlIntentSpec(4, ControlType.Attack, 3)));
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, 7);
            ctx.AssertTrue("control-intent target stunned", ctx.PrimaryEnemy.IsStunned);
        }, 2);

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
            ctx.AssertDamage(
                "already-selected invasion still deals damage", ctx.Self, hp, 10);
            ctx.AssertEqual("blocked invasion adds no curse", deck,
                ctx.Player.Deck.Cards.Count);
        }, 5);

    private static void ConsecrationProbe() =>
        CustomVariants<Consecration>(DesignSyncScriptureGenerationContract.Consecration, 10);

    private static void ChantProbe() =>
        CustomVariants<Chant>(DesignSyncScriptureGenerationContract.Chant, 20);

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
            MaidenSuccubus.Powers.Scriptures.GuardianScripturePower scripture =
                ctx.Self.Powers
                    .OfType<MaidenSuccubus.Powers.Scriptures.GuardianScripturePower>()
                    .Single();
            await scripture.AfterSideTurnEnd(
                new BlockingPlayerChoiceContext(), ctx.Self.Side, [ctx.Self]);
            ctx.AssertBlock("guardian and resonance block after scripture trigger",
                block, upgraded ? 6 : 5);
        }, 1);

    private static void InwardDisciplineProbe() =>
        CustomVariants<InwardDiscipline>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            int amount = upgraded ? 75 : 50;
            InwardDisciplinePower power =
                ctx.Self.Powers.OfType<InwardDisciplinePower>().Single();
            await ctx.ApplyPower<WeakPower>(ctx.Self, 1);
            int blockBefore = ctx.Self.Block;
            await ctx.Play(ctx.Create<DefendIronclad>());
            ctx.AssertBlock("real block while weak", blockBefore, upgraded ? 8 : 7);
            await PowerCmd.Remove(ctx.Self.GetPower<WeakPower>()!);
            await ctx.ApplyPower<FrailPower>(ctx.Self, 1);
            int hpBefore = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(ctx.Create<StrikeIronclad>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("real damage while frail", ctx.PrimaryEnemy, hpBefore, upgraded ? 10 : 9);
            await ctx.ApplyPower<WeakPower>(ctx.Self, 1);
            ctx.AssertEqual("block multiplier while weak",
                1m + amount / 100m,
                power.ModifyBlockMultiplicative(ctx.Self, 10, ValueProp.Move,
                    ctx.Create<MaidenDefend>(), null));
            ctx.AssertEqual("damage multiplier while frail",
                1m + amount / 100m,
                power.ModifyDamageMultiplicative(ctx.PrimaryEnemy, 10,
                    ValueProp.Move, ctx.Self, ctx.Create<MaidenStrike>(), null));
        }, 4);

    private static void MemoryImprintProbe() =>
        CustomVariants<MemoryImprint>(async (ctx, card, upgraded) =>
        {
            await ctx.Play(card);
            MaidenStrike selected = await ctx.Add<MaidenStrike>(PileType.Discard);
            int drawBefore = ctx.CountCards<MaidenStrike>(PileType.Draw);
            int discardBefore = ctx.CountCards<MaidenStrike>(PileType.Discard);
            TestCardSelector selector = new();
            selector.PrepareToSelect([selected]);
            using (CardSelectCmd.UseSelector(selector))
            {
                MemoryImprintPower power =
                    ctx.Self.Powers.OfType<MemoryImprintPower>().Single();
                await power.BeforeHandDraw(
                    ctx.Player, new BlockingPlayerChoiceContext(), ctx.Combat);
            }
            ctx.AssertPileDelta<MaidenStrike>(
                "exactly one selected card added to draw pile",
                PileType.Draw, drawBefore, 1);
            ctx.AssertPileDelta<MaidenStrike>(
                "exactly one selected card removed from discard pile",
                PileType.Discard, discardBefore, -1);
            ctx.AssertEqual("selected discard card moved to draw top", selected,
                PileType.Draw.GetPile(ctx.Player).Cards.First());
            ctx.AssertEqual("upgrade grants innate", upgraded,
                card.Keywords.Contains(CardKeyword.Innate), effect: false);
        }, 3);

    private static void PhotonVoltProbe() =>
        CustomVariants<PhotonVolt>(async (ctx, card, upgraded) =>
        {
            int damage = upgraded ? 12 : 10;
            await Desire.Set(ctx.Player, 2);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("low-desire damage", ctx.PrimaryEnemy, hp, damage);
            ctx.AssertPower("low-desire amplification", ctx.Self,
                "MagicAmplificationPower", upgraded ? 2 : 1);

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
            await TransformationCmd.EnterImmaculateRobe(
                new BlockingPlayerChoiceContext(), ctx.Self, null);
            MagicArmorPower armor = ctx.Self.Powers
                .OfType<MagicArmorPower>().Single();
            await PowerCmd.ModifyAmount(
                new BlockingPlayerChoiceContext(), armor, -2, ctx.Self, null);
            await ctx.Play(card);
            RegenerativeMagicFiberPower power =
                ctx.Self.Powers.OfType<RegenerativeMagicFiberPower>().Single();
            await power.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("one armor each turn", ctx.Self, "MagicArmorPower", 2);
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
            await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
            await ctx.Play(card);
            ctx.AssertPower("base and magic-release amplification", ctx.Self,
                "MagicAmplificationPower", upgraded ? 4 : 2);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(ctx.Create<MaidenStrike>(), ctx.PrimaryEnemy);
            ctx.AssertDamage("next card amplified by 50 percent", ctx.PrimaryEnemy, hp, 9);
            ctx.AssertPower("one amplification layer consumed", ctx.Self,
                "MagicAmplificationPower", upgraded ? 3 : 1);
        }, 3);

    private static void IgniteProbe() =>
        CustomVariants<Ignite>(DesignSyncIgniteContract.Run, 35);

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
            ctx.AssertPower("no underlying immaculate robe", ctx.Self,
                "ImmaculateRobePower", 0);
            ctx.AssertPower("transformation armor", ctx.Self, "MagicArmorPower", 3);
            ctx.AssertPower("eternal robe amplification amount", ctx.Self,
                "EternalRobePower", 9);
            EternalRobePower robe = ctx.Self.Powers.OfType<EternalRobePower>().Single();
            await robe.AfterPlayerTurnStart(new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("nine amplification each turn", ctx.Self,
                "MagicAmplificationPower", 9);
        }, 4);

    private static void TransformProbe() =>
        CustomVariants<Transform>(async (ctx, card, upgraded) =>
        {
            var runState = (MegaCrit.Sts2.Core.Runs.RunState)ctx.Player.RunState;
            CorruptionCmd.Set(runState, -3);
            ctx.AssertEqual("holy variation route identity",
                RouteCardKind.Holy, RouteCardQuery.Get(card));
            ctx.AssertTrue("holy variation remains unsealed",
                !CombatSealQuery.IsSealed(runState, card));
            CorruptionCmd.Set(runState, 3);
            ctx.AssertEqual("corrupt variation route identity",
                RouteCardKind.Corrupt, RouteCardQuery.Get(card));
            ctx.AssertTrue("corrupt variation remains unsealed",
                !CombatSealQuery.IsSealed(runState, card));
            CorruptionCmd.Set(runState, 0);

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
            ctx.AssertPower("purification persists after triggering", ctx.Self,
                "PurificationPower", 2);

            int hp = ctx.Self.CurrentHp;
            await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), ctx.Self,
                10, ValueProp.Move, ctx.PrimaryEnemy);
            ctx.AssertDamage("incoming damage halved", ctx.Self, hp, 5);
        }, 7);

    private static void WorshipProbe() =>
        CustomVariants<Worship>(async (ctx, card, upgraded) =>
        {
            int amount = upgraded ? 2 : 1;
            await ctx.ApplyPower<PoisonPower>(ctx.Self, 2);
            await ctx.ApplyPower<NoDrawPower>(ctx.Self, 1);
            await ctx.Play(card);
            ctx.AssertPower("dexterity", ctx.Self, "DexterityPower", amount);
            PurificationPower purification =
                ctx.Self.Powers.OfType<PurificationPower>().Single();
            IReadOnlyList<PowerModel> candidates = purification.GetCandidates();
            ctx.AssertEqual("all stackable debuff types are discovered", true,
                candidates.Any(power => power is PoisonPower));
            ctx.AssertEqual("single debuffs are not purification candidates", false,
                candidates.Any(power => power is NoDrawPower));
            await purification.AfterPlayerTurnStart(
                new BlockingPlayerChoiceContext(), ctx.Player);
            ctx.AssertPower("non-hardcoded stackable debuff loses exact layers", ctx.Self,
                "PoisonPower", 2 - amount);
            ctx.AssertPower("single debuff remains", ctx.Self, "NoDrawPower", 1);
            ctx.AssertPower("buff remains", ctx.Self, "DexterityPower", amount);
            ctx.AssertPower("purification amount is permanent", ctx.Self,
                "PurificationPower", amount);
        }, 7);

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
            DarkThrust paymentCard = await ctx.Add<DarkThrust>(PileType.Hand);
            await paymentCard.SpendResources();
            await ctx.Play(paymentCard, ctx.PrimaryEnemy);
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
            ctx.AssertEqual("escape amount", upgraded ? 0 : 1, control.Amount);
            ctx.AssertEqual("zero energy cost", 0,
                card.EnergyCost.GetWithModifiers(CostModifiers.Local));
            ctx.AssertEqual("displayed escape amount", upgraded ? 3 : 2,
                card.DynamicVars["Escape"].IntValue);
        }, 3);

    private static void RestraintEvasionProbe() =>
        CustomVariants<RestraintEvasion>(async (ctx, card, upgraded) =>
        {
            ControlPower control = (ControlPower)ModelDb.Power<ControlPower>().ToMutable();
            control.ControlType = ControlType.Attack;
            await PowerCmd.Apply(new BlockingPlayerChoiceContext(), control,
                ctx.Self, 2, ctx.PrimaryEnemy, null);
            IntentMoveFactory.SetTransient(ctx.PrimaryEnemy.Monster!,
                IntentMoveFactory.CreateControl(ctx.PrimaryEnemy.Monster!,
                    new ControlIntentSpec(4, ControlType.Attack, 3)));
            int block = ctx.Self.Block;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertBlock("base block plus control block requirement", block,
                upgraded ? 13 : 10);
            ctx.AssertEqual("escape one layer", 1, control.Amount);
        }, 2);

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

    private static void DemonStaffProbe() =>
        CustomVariants<DemonStaff>(async (ctx, card, upgraded) =>
        {
            string[] expectedCandidateIds =
            [
                "DEMONIC_SHIELD", "DOMINATE", "FEED", "FIEND_FIRE", "IMPERVIOUS",
                "INFERNAL_BLADE", "MOLTEN_FIST", "OFFERING", "STOKE", "CINDER",
                "TRUE_GRIT", "HAVOC", "TREMBLE", "ASHEN_STRIKE", "HOWL_FROM_BEYOND",
                "FORGOTTEN_RITUAL", "BURNING_PACT", "EVIL_EYE", "DRUM_OF_BATTLE",
                "SECOND_WIND", "FEEL_NO_PAIN", "PACTS_END", "THRASH", "BRAND",
                "DARK_EMBRACE", "ADRENALINE", "ASSASSINATE", "BACKSTAB", "BLADE_DANCE",
                "CALCULATED_GAMBLE", "EXPOSE", "MALAISE", "MIRAGE", "NIGHTMARE",
                "PIERCING_WAIL", "THE_HUNT", "INTIMIDATE", "KNIFE_TRAP", "BOOT_SEQUENCE",
                "CHILL", "DOUBLE_ENERGY", "ENERGY_SURGE", "GENETIC_ALGORITHM", "HOLOGRAM",
                "IGNITION", "RAINBOW", "REBOOT", "SIGNAL_BOOST", "SUPERCRITICAL",
                "VOLTAIC", "WHITE_NOISE", "HOTFIX", "FUSION", "SCAVENGE", "FLAK_CANNON",
                "AFTERLIFE", "DREDGE", "GLIMPSE_BEYOND", "GRAVEBLAST", "LEGION_OF_BONE",
                "PUTREFY", "REANIMATE", "SHARED_FATE", "THE_SCYTHE", "TRANSFIGURE", "WISP",
                "DIRGE", "CLEANSE", "SOUL_STORM", "EIDOLON", "BIG_BANG", "BOMBARDMENT",
                "BUNDLE_OF_JOY", "DECISIONS_DECISIONS", "GUARDS", "KNOW_THY_PLACE",
                "ROYAL_GAMBLE", "TYRANNY",
            ];
            ctx.AssertEqual("exact DesignDoc candidate ID count", 78,
                DemonStaff.CandidateIds.Count);
            ctx.AssertTrue("exact DesignDoc candidate ID set",
                new HashSet<string>(expectedCandidateIds, StringComparer.Ordinal)
                    .SetEquals(DemonStaff.CandidateIds));
            ctx.AssertTrue("base exhaust card is eligible",
                DemonStaff.IsEligibleCandidate(ModelDb.Card<DemonicShield>(), false));
            ctx.AssertTrue("description exhaust card is eligible after upgrade",
                DemonStaff.IsEligibleCandidate(ModelDb.Card<TrueGrit>(), true));
            ctx.AssertTrue("renamed memory-cleanup card is eligible",
                DemonStaff.IsEligibleCandidate(ModelDb.Card<Scavenge>(), true));
            ctx.AssertTrue("removed Synchronize is excluded",
                !DemonStaff.IsEligibleCandidate(ModelDb.Card<Synchronize>(), false));
            ctx.AssertTrue("removed Time's Up is excluded",
                !DemonStaff.IsEligibleCandidate(ModelDb.Card<TimesUp>(), false));
            ctx.AssertTrue("ancient Corruption is excluded",
                !DemonStaff.IsEligibleCandidate(
                    ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Corruption>(), false));
            ctx.AssertTrue("upgrade losing Exhaust is removed",
                !DemonStaff.IsEligibleCandidate(ModelDb.Card<KnowThyPlace>(), true));

            await ctx.Play(card, selectedIndices: [0]);
            CardModel[] generated = PileType.Hand.GetPile(ctx.Player).Cards.ToArray();
            ctx.AssertEqual("exactly one chosen card generated", 1, generated.Length);
            ctx.AssertEqual("generated card upgrade state", upgraded,
                generated.Single().IsUpgraded);
            ctx.AssertEqual("generated card is free this turn", 0,
                generated.Single().EnergyCost.GetWithModifiers(CostModifiers.All));
            ctx.AssertTrue("generated card remains in current candidate pool",
                DemonStaff.CandidateIds.Contains(generated.Single().Id.Entry));
            ctx.AssertTrue("staff has real Exhaust keyword", card.Keywords.Contains(CardKeyword.Exhaust), effect: false);
            ctx.AssertEqual("staff exhausts after generation", PileType.Exhaust, card.Pile?.Type);
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
            AssertNeutralMetadata(ctx, card, CardRarity.Uncommon, 1);
            SemenCurse firstCurse = ctx.Player.RunState.CreateCard<SemenCurse>(ctx.Player);
            FoulSlimeCurse secondCurse = ctx.Player.RunState.CreateCard<FoulSlimeCurse>(ctx.Player);
            await CardPileCmd.Add(firstCurse, PileType.Deck, skipVisuals: true);
            await CardPileCmd.Add(secondCurse, PileType.Deck, skipVisuals: true);
            try
            {
                await ctx.SetUpArmour(1);
                int energy = ctx.Player.PlayerCombatState!.Energy;
                await ctx.Play(card, selectedIndices: [0]);
                ctx.AssertEqual("immediate energy", 2,
                    ctx.Player.PlayerCombatState.Energy - energy);
                ctx.AssertTrue("all invasion curses permanently removed",
                    firstCurse.HasBeenRemovedFromState && secondCurse.HasBeenRemovedFromState);
                ctx.AssertPower("magic-release next-turn energy", ctx.Self,
                    "EnergyNextTurnPower", upgraded ? 3 : 2);
                ctx.AssertPower("magic release pays one armor", ctx.Self,
                    "MagicArmorPower", 0);
            }
            finally
            {
                foreach (CardModel curse in new CardModel[] { firstCurse, secondCurse })
                {
                    if (!curse.HasBeenRemovedFromState && curse.Pile?.Type == PileType.Deck)
                        await CardPileCmd.RemoveFromDeck(curse, showPreview: false);
                }
            }
        }, 4);

    private static void BorrowedForceStrikeProbe() =>
        CustomVariants<BorrowedForceStrike>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Common, 1);
            bool attacking = ctx.PrimaryEnemy.Monster?.IntendsToAttack == true;
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int energy = ctx.Player.PlayerCombatState!.Energy;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 10 : 9);
            ctx.AssertEqual("energy follows attack intent", attacking ? (upgraded ? 2 : 1) : 0,
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
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await CreatureCmd.SetCurrentHp(ctx.Self, ctx.Self.MaxHp - 30);
            await ctx.ApplyPower<StrengthPower>(ctx.Self, 2);
            string text = DesignSyncTextBatchContract.Text(card, PileType.Hand);
            ctx.AssertTrue("healing preview after buff increase and Exhaust before total",
                text.EndsWith($"消耗。\n（恢复{(upgraded ? 12 : 8)}点生命值）", StringComparison.Ordinal));
            await PowerCmd.Remove(ctx.Self.GetPower<StrengthPower>()!);
            ctx.AssertTrue("healing preview after buff removal",
                DesignSyncTextBatchContract.Text(card, PileType.Hand)
                    .EndsWith($"（恢复{(upgraded ? 8 : 4)}点生命值）", StringComparison.Ordinal));
            await ctx.ApplyPower<StrengthPower>(ctx.Self, 2);
            int hp = ctx.Self.CurrentHp;
            await ctx.Play(card);
            ctx.AssertEqual("base healing plus two per buff layer",
                (upgraded ? 8 : 4) + 4, ctx.Self.CurrentHp - hp);
            ctx.AssertEqual("healing still exhausts once", PileType.Exhaust, card.Pile?.Type);
            await DesignSyncSignedLayerContract.Healing(ctx, upgraded);
        }, 12);

    private static void MagicSwordProbe() =>
        CustomVariants<MagicSword>((ctx, card, upgraded) => DesignSyncEnchantmentInputContract.Sword(ctx, card, upgraded), 17);

    private static void JudgmentBladeProbe() =>
        CustomVariants<JudgmentBlade>(async (ctx, card, upgraded) =>
        {
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await ctx.ApplyPower<WeakPower>(ctx.PrimaryEnemy, 1);
            await ctx.ApplyPower<FrailPower>(ctx.PrimaryEnemy, 2);
            ctx.AssertTrue("damage preview includes three debuff layers",
                DesignSyncTextBatchContract.Text(card, PileType.Hand, ctx.PrimaryEnemy)
                    .EndsWith($"（造成{(upgraded ? 22 : 16)}点伤害）", StringComparison.Ordinal));
            ctx.AssertTrue("clearing target resets damage preview",
                DesignSyncTextBatchContract.Text(card, PileType.Hand)
                    .EndsWith("（造成7点伤害）", StringComparison.Ordinal));
            ctx.AssertTrue("retargeting restores calculated preview",
                DesignSyncTextBatchContract.Text(card, PileType.Hand, ctx.PrimaryEnemy)
                    .EndsWith($"（造成{(upgraded ? 22 : 16)}点伤害）", StringComparison.Ordinal));
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage with three debuff layers", ctx.PrimaryEnemy, hp,
                upgraded ? 22 : 16);
            await DesignSyncSignedLayerContract.Judgment(ctx, upgraded);
        }, 8);

    private static void LightningRecoilProbe() =>
        CustomVariants<LightningRecoil>(async (ctx, card, upgraded) =>
        {
            await ctx.SetUpArmour(1);
            await ctx.AddFillerCards(PileType.Draw, 2);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, ctx.PrimaryEnemy, selectedIndices: [0]);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 9 : 6);
            ctx.AssertPileDelta<StrikeIronclad>("overdraft draw", PileType.Hand, hand, 1);
            ctx.AssertPower("overdraft armor payment", ctx.Self, "MagicArmorPower", 0);
        }, 3);

    private static void MindsEyeProbe() =>
        CustomVariants<MindsEye>(async (ctx, card, upgraded) =>
        {
            bool attacking = ctx.PrimaryEnemy.Monster?.IntendsToAttack == true;
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, 3);
            ctx.AssertPower("weak branch", ctx.PrimaryEnemy, "WeakPower",
                attacking ? (upgraded ? 2 : 1) : 0);
            ctx.AssertPower("vulnerable branch", ctx.PrimaryEnemy, "VulnerablePower",
                attacking ? 0 : (upgraded ? 2 : 1));
            ctx.AssertTrue("does not exhaust",
                !card.Keywords.Contains(CardKeyword.Exhaust));
        }, 4);

    private static void ObstructingShotProbe() =>
        CustomVariants<ObstructingShot>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Rare, upgraded ? 1 : 2);
            bool shouldStun = ctx.PrimaryEnemy.Monster?.IntendsToAttack == false;
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, 3);
            ctx.AssertEqual("stun follows non-attack intent", shouldStun,
                ctx.PrimaryEnemy.IsStunned);
        }, 2);

    private static void ProcrastinateProbe() =>
        CustomVariants<Procrastinate>(async (ctx, card, upgraded) =>
        {
            AssertNeutralMetadata(ctx, card, CardRarity.Common, 0);
            StrikeIronclad selected = await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.AddFillerCards(PileType.Draw, 6);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            await ctx.Play(card, selectedCards: [selected]);
            ctx.AssertEqual("selected card moved to draw bottom", selected,
                PileType.Draw.GetPile(ctx.Player).Cards.Last());
            ctx.AssertPileDelta<StrikeIronclad>("cards drawn", PileType.Hand, hand,
                (upgraded ? 3 : 2) - 1);
        }, 2);

    private static void SurfProbe() =>
        CustomVariants<Surf>(async (ctx, card, upgraded) =>
        {
            StrikeIronclad ember = await ctx.Add<StrikeIronclad>(PileType.Draw);
            CombatEnchantmentCmd.ApplyVanilla<TezcatarasEmber>(ember, 1);
            Whirlwind xCost = await ctx.Add<Whirlwind>(PileType.Draw);
            Burn unplayable = await ctx.Add<Burn>(PileType.Draw);
            Bash discounted = await ctx.Add<Bash>(PileType.Draw);
            discounted.EnergyCost.SetThisCombat(1);
            await ctx.Add<Bash>(PileType.Draw);
            await ctx.Add<StrikeIronclad>(PileType.Draw);

            ctx.AssertEqual("Tezcataras Ember current cost", 0,
                Surf.GetCurrentEnergyCostForAccumulation(ember), effect: false);
            ctx.AssertEqual("temporary reduced current cost", 1,
                Surf.GetCurrentEnergyCostForAccumulation(discounted), effect: false);
            ctx.AssertEqual("X cost accumulates as zero", 0,
                Surf.GetCurrentEnergyCostForAccumulation(xCost), effect: false);
            ctx.AssertEqual("unplayable cost accumulates as zero", 0,
                Surf.GetCurrentEnergyCostForAccumulation(unplayable), effect: false);

            int hp = ctx.PrimaryEnemy.CurrentHp;
            int hand = PileType.Hand.GetPile(ctx.Player).Cards.Count;
            await ctx.Play(card, ctx.PrimaryEnemy);
            int expectedDraws = upgraded ? 6 : 5;
            ctx.AssertEqual("draws until modified costs reach threshold", expectedDraws,
                PileType.Hand.GetPile(ctx.Player).Cards.Count - hand);
            ctx.AssertDamage("one area hit per drawn card", ctx.PrimaryEnemy, hp,
                expectedDraws * 4);
        }, 6);

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
        CustomVariants<UltimateFlare>(DesignSyncChainCopyContract.Flare, 25);

    private static void AllCurseBiteProbe() =>
        CustomVariants<AllCurseBite>(DesignSyncAllCurseBiteContract.Run, 28);

    private static void AllHopeLostProbe() =>
        CustomVariants<AllHopeLost>(async (ctx, card, upgraded) =>
        {
            await PlayerCmd.SetEnergy(3, ctx.Player);
            await Desire.Set(ctx.Player, 2);
            card.DynamicVars.Damage.UpdateCardPreview(
                card,
                CardPreviewMode.Normal,
                ctx.PrimaryEnemy,
                runGlobalHooks: true);
            card.DynamicVars["Hits"].UpdateCardPreview(
                card,
                CardPreviewMode.Normal,
                ctx.PrimaryEnemy,
                runGlobalHooks: true);
            ctx.AssertEqual("desire-scaled damage preview", 12m,
                card.DynamicVars.Damage.PreviewValue);
            ctx.AssertEqual("current-energy hit preview", upgraded ? 4m : 3m,
                card.DynamicVars["Hits"].PreviewValue);
            await card.AfterSecondaryResourceSpent(new SecondaryResourceSpendContext(
                ctx.Combat, ctx.Player, DesireResource.Definition, card, 2, card));
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("two desire times six damage for each X hit",
                ctx.PrimaryEnemy, hp, upgraded ? 48 : 36);
        }, 3);

    private static void BiteInvaderProbe() =>
        CustomVariants<BiteInvader>(async (ctx, card, _) =>
        {
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertPower("weak", ctx.PrimaryEnemy, "WeakPower", 7);
            ctx.AssertTrue("target stunned", ctx.PrimaryEnemy.IsStunned);
        }, 2);

    private static void BlackVortexProbe() =>
        CustomVariants<BlackVortex>(DesignSyncExhaustContract.Vortex, 20);

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
        CustomVariants<ChangePanties>(async (ctx, card, upgraded) =>
        {
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            int threshold = upgraded ? 5 : 6;
            await MaidenSuccubus.Data.Desire.Set(ctx.Player, threshold - 1);
            ctx.AssertTrue("blocked below threshold",
                !MegaCrit.Sts2.Core.Hooks.Hook.ShouldPlay(ctx.Combat, card, out AbstractModel? _,
                    AutoPlayType.Default));
            await MaidenSuccubus.Data.Desire.Set(ctx.Player, threshold);
            ctx.AssertTrue("playable at threshold",
                MegaCrit.Sts2.Core.Hooks.Hook.ShouldPlay(ctx.Combat, card, out AbstractModel? _,
                    AutoPlayType.Default));
            ctx.AssertEqual("card type", CardType.Power, card.Type);
            await ctx.Play(card);
            ctx.AssertPower("slippery", ctx.Self, "SlipperyPower", 2);
            ctx.AssertPower("vulnerable", ctx.Self, "VulnerablePower", 2);
        }, 5);

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
            var runState = (MegaCrit.Sts2.Core.Runs.RunState)ctx.Player.RunState;
            CorruptionCmd.Set(runState, -3);
            ctx.AssertEqual("holy variation route identity",
                RouteCardKind.Holy, RouteCardQuery.Get(card));
            ctx.AssertTrue("holy variation remains unsealed",
                !CombatSealQuery.IsSealed(runState, card));
            ctx.AssertTrue("holy variation gains block", card.GainsBlock);

            CorruptionCmd.Set(runState, -2);
            ctx.AssertEqual("crossing above minus three restores corrupt identity",
                RouteCardKind.Corrupt, RouteCardQuery.Get(card));
            CorruptionCmd.Set(runState, 0);
            ctx.AssertEqual("zero corruption remains the corrupt base form",
                RouteCardKind.Corrupt, RouteCardQuery.Get(card));
            ctx.AssertTrue("corrupt variation remains unsealed",
                !CombatSealQuery.IsSealed(runState, card));
            ctx.AssertTrue("corrupt base form does not gain block", !card.GainsBlock);

            await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            int block = ctx.Self.Block;
            await ctx.Play(card, ctx.PrimaryEnemy);
            int amplified = upgraded ? 9 : 6;
            ctx.AssertDamage("zero-corruption magic release repeats amplified damage",
                ctx.PrimaryEnemy, hp, amplified * 2);
            ctx.AssertBlock("zero-corruption base form grants no release block", block, 0);

            CorruptionCmd.Set(runState, -3);
            DarkElement holy = ctx.Create<DarkElement>(upgraded);
            await ctx.ApplyPower<MagicAmplificationPower>(ctx.Self, 1);
            hp = ctx.PrimaryEnemy.CurrentHp;
            block = ctx.Self.Block;
            await ctx.Play(holy, ctx.PrimaryEnemy);
            ctx.AssertDamage("holy variation keeps the initial amplified damage",
                ctx.PrimaryEnemy, hp, amplified);
            ctx.AssertBlock("holy variation magic release grants amplified block",
                block, amplified);
        }, 11);

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
            await Desire.Set(ctx.Player, 1);
            int temptation = Temptation.Get(ctx.Player);
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await card.SpendResources();
            await ctx.Play(card);
            ctx.AssertEqual("desire cost", 0, Desire.Get(ctx.Player));
            ctx.AssertEqual("temptation gained", 20,
                Temptation.Get(ctx.Player) - temptation);
            ctx.AssertTrue("an enemy intent becomes erotic below armor threshold",
                ctx.Enemies.Any(enemy => enemy.Monster?.NextMove.StateId.StartsWith(
                    "MAIDENSUCCUBUS_", StringComparison.Ordinal) == true));
            ctx.AssertEqual("armor threshold", upgraded ? 2 : 1,
                card.DynamicVars["ArmorThreshold"].IntValue);
        }, 4);

    private static void LastStandProbe() =>
        CustomVariants<LastStand>(async (ctx, card, upgraded) =>
        {
            LastStand outsideCombat = ctx.Player.RunState.CreateCard<LastStand>(ctx.Player);
            if (upgraded) CardCmd.Upgrade(outsideCombat);
            string outsideText = outsideCombat.GetDescriptionForPile(PileType.Deck);
            ctx.AssertTrue("deck description does not contain combat total", !outsideText.Contains("（造成"));
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            card.UpdateDynamicVarPreview(CardPreviewMode.Normal, ctx.PrimaryEnemy, card.DynamicVars);
            string initialText = System.Text.RegularExpressions.Regex.Replace(
                card.GetDescriptionForPile(PileType.Hand, ctx.PrimaryEnemy), @"\[[^\]]+\]", "");
            ctx.AssertTrue("zero debuffs preview base damage", initialText.Contains(upgraded ? "（造成4点伤害）" : "（造成3点伤害）"));
            await ctx.ApplyPower<FrailPower>(ctx.Self, 1);
            await ctx.ApplyPower<VulnerablePower>(ctx.Self, 2);
            card.UpdateDynamicVarPreview(CardPreviewMode.Normal, ctx.PrimaryEnemy, card.DynamicVars);
            string preview = System.Text.RegularExpressions.Regex.Replace(
                card.GetDescriptionForPile(PileType.Hand, ctx.PrimaryEnemy), @"\[[^\]]+\]", "");
            ctx.AssertTrue("base damage is not counted twice in description", preview.StartsWith(upgraded ? "造成4点伤害。" : "造成3点伤害。"));
            ctx.AssertTrue("combat total updates to debuff layers", preview.Contains(upgraded ? "（造成16点伤害）" : "（造成12点伤害）"));
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("damage with three self-debuff layers", ctx.PrimaryEnemy, hp,
                upgraded ? 16 : 12);
            await DesignSyncSignedLayerContract.LastStand(ctx, upgraded);
        }, 9);

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

    private static void LureDeepProbe() =>
        CustomVariants<LureDeep>(async (ctx, card, upgraded) =>
        {
            await ctx.AddFillerCards(PileType.Draw, 1);
            int hand = ctx.CountCards<StrikeIronclad>(PileType.Hand);
            int temptation = Temptation.Get(ctx.Player);
            await ctx.Play(card);
            ctx.AssertEqual("temptation gained", upgraded ? 15 : 10,
                Temptation.Get(ctx.Player) - temptation);
            ctx.AssertPileDelta<StrikeIronclad>("draw one", PileType.Hand, hand, 1);
            ctx.AssertEqual("zero energy cost", 0,
                card.EnergyCost.GetWithModifiers(CostModifiers.Local));
        }, 2);

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
            ctx.AssertEqual("upgrade changes cost from one to zero", upgraded ? 0 : 1,
                card.EnergyCost.GetWithModifiers(CostModifiers.All));
            ctx.AssertTrue("does not gain retain",
                !card.Keywords.Contains(CardKeyword.Retain), effect: false);
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
            await DesignSyncSignedLayerContract.Draw(ctx, upgraded);
        }, 6);

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
            ctx.AssertBlock("block when played", block, 8);
            ctx.AssertEqual("played barrier is discarded, not exhausted", PileType.Discard, card.Pile?.Type);
            ctx.AssertEqual("upgrade grants ethereal", upgraded, card.Keywords.Contains(CardKeyword.Ethereal));
            ctx.AssertEqual("upgrade does not grant exhaust", false, card.Keywords.Contains(CardKeyword.Exhaust));

            ReflectiveBarrier exhausted = ctx.Create<ReflectiveBarrier>(upgraded);
            await CardPileCmd.Add(exhausted, PileType.Hand, skipVisuals: true);
            block = ctx.Self.Block;
            int amplification = ctx.PowerAmount(ctx.Self, "MagicAmplificationPower");
            await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), exhausted);
            ctx.AssertBlock("block when exhausted", block, 8);
            ctx.AssertEqual("one amplification gained when exhausted", 1,
                ctx.PowerAmount(ctx.Self, "MagicAmplificationPower") - amplification);
        }, 6);

    private static void ThousandCurseScytheProbe() =>
        CustomVariants<ThousandCurseScythe>(async (ctx, card, upgraded) =>
        {
            ThousandCurseScythe deckCard =
                ctx.Player.RunState.CreateCard<ThousandCurseScythe>(ctx.Player);
            if (upgraded)
            {
                CardCmd.Upgrade(deckCard);
            }
            await CardPileCmd.Add(deckCard, PileType.Deck, skipVisuals: true);
            card.DeckVersion = deckCard;
            try
            {
                int hp = ctx.PrimaryEnemy.CurrentHp;
                await ctx.Play(card, ctx.PrimaryEnemy);
                ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, 8);
                int expected = upgraded ? 14 : 12;
                ctx.AssertEqual("combat-card damage growth after exhaust",
                    expected, card.CurrentDamage);
                ctx.AssertEqual("run-deck damage growth after exhaust",
                    expected, deckCard.CurrentDamage);
                await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
                await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card);
                int secondExpected = upgraded ? 20 : 16;
                ctx.AssertEqual("repeated exhaust compounds combat growth", secondExpected, card.CurrentDamage);
                ctx.AssertEqual("repeated exhaust compounds permanent growth", secondExpected, deckCard.CurrentDamage);
                ctx.AssertEqual("clone preserves permanent damage", secondExpected,
                    ((ThousandCurseScythe)deckCard.MutableClone()).CurrentDamage);
            }
            finally
            {
                if (!deckCard.HasBeenRemovedFromState
                    && deckCard.Pile?.Type == PileType.Deck)
                {
                    await CardPileCmd.RemoveFromDeck(deckCard, showPreview: false);
                }
            }
        }, 6);

    private static void MimicProliferationProbe() =>
        CustomVariants<MimicProliferation>(async (ctx, card, upgraded) =>
        {
            ThousandCurseScythe fixture =
                await ctx.Add<ThousandCurseScythe>(PileType.Hand);
            ctx.AssertTrue("generated permanent-growth fixture has no deck version",
                fixture.DeckVersion == null);
            await ctx.Play(card, selectedCards: [fixture]);
            ctx.AssertEqual("selected generated card exhausted",
                PileType.Exhaust,
                fixture.Pile?.Type);
            ctx.AssertEqual("combat-only permanent growth still resolves", 12,
                fixture.CurrentDamage);
            ThousandCurseScythe[] copies = PileType.Hand.GetPile(ctx.Player).Cards
                .OfType<ThousandCurseScythe>()
                .ToArray();
            ctx.AssertEqual("copies generated after safe combat-only exhaust",
                upgraded ? 3 : 2,
                copies.Length);
            ctx.AssertTrue("copies preserve combat growth",
                copies.All(copy => copy.CurrentDamage == 12));
        }, 5);

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
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
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
            ctx.AssertEqual("selected card adroit amount", 3,
                selected.Enchantment?.Amount ?? 0);
            ctx.AssertEqual("selected card uses Kifuda Adroit enchantment", "Adroit",
                selected.Enchantment?.GetType().Name ?? "none");
            int blockBeforeEnchantedCard = ctx.Self.Block;
            await ctx.Play(selected);
            ctx.AssertBlock("Adroit adds three block when enchanted card is played",
                blockBeforeEnchantedCard, 8);
        }, 4);

    private static void HolyCurseProbe() =>
        CustomVariants<HolyCurse>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<WeakPower>(ctx.PrimaryEnemy, 1);
            await ctx.ApplyPower<FrailPower>(ctx.PrimaryEnemy, 2);
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
        CustomVariants<MagicBurst>(DesignSyncMagicBurstContract.Run, 100);

    private static void MultipleReproductionProbe() =>
        CustomVariants<MultipleReproduction>(DesignSyncExtraTurnContract.Run, 40);

    private static void NoLewdnessProbe() =>
        CustomVariants<NoLewdness>(async (ctx, card, upgraded) =>
        {
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
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
            await DesignSyncSignedLayerContract.Energy(ctx, upgraded);
        }, 5);

    private static void StigmaProbe() =>
        CustomVariants<Stigma>(async (ctx, card, upgraded) =>
        {
            ctx.AssertTrue("stigma accepts a non-enemy target",
                card.IsValidTarget(ctx.Self));
            await ctx.Play(card, ctx.Self, selectedIndices: [0]);
            ctx.AssertPower("condemnation choice applies to the pointed target",
                ctx.Self, "CondemnationPower", upgraded ? 3 : 2);
        }, 1);

    private static void TacticalAnalyzerProbe() =>
        CustomVariants<TacticalAnalyzer>(DesignSyncTacticalAnalyzerContract.Run, 25);

    private static void BeyondReasonForgeProbe() =>
        CustomVariants<BeyondReasonForge>(DesignSyncForgeContract.Run, 120);

    private static void MentalUnityProbe() =>
        CustomVariants<MentalUnity>(async (ctx, card, upgraded) =>
        {
            await ctx.ApplyPower<DexterityPower>(ctx.Self, 2);
            await CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            decimal expectedBlock = upgraded ? 3m : 2m;
            card.DynamicVars.Block.UpdateCardPreview(
                card,
                CardPreviewMode.Normal,
                ctx.Self,
                runGlobalHooks: true);
            ctx.AssertEqual(
                "delayed unpowered block preview ignores dexterity",
                expectedBlock,
                card.DynamicVars.Block.PreviewValue);
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy, selectedIndices: [0]);
            ctx.AssertDamage("damage", ctx.PrimaryEnemy, hp, upgraded ? 8 : 6);
            ctx.AssertPower("delayed block per target attack", ctx.PrimaryEnemy,
                "MentalUnityPower", upgraded ? 3 : 2);
            MentalUnityPower power = ctx.PrimaryEnemy.Powers
                .OfType<MentalUnityPower>()
                .Single();
            int block = ctx.Self.Block;
            await power.BeforeDamageReceived(
                new BlockingPlayerChoiceContext(),
                ctx.Self,
                1m,
                ValueProp.Move,
                ctx.PrimaryEnemy,
                null);
            ctx.AssertBlock(
                "triggered unpowered block ignores dexterity",
                block,
                (int)expectedBlock);
        }, 4);

    private static void CurseInfectionProbe() =>
        CustomVariants<CurseInfection>(DesignSyncCurseInfectionContract.Run, 65);

    private static void PleasureGardenProbe() =>
        CustomVariants<PleasureGarden>(async (ctx, card, _) =>
        {
            int temptation = Temptation.Get(ctx.Player);
            Creature[] eligible = ctx.Enemies.Where(enemy =>
            {
                EroticMonsterSpec? spec = enemy.Monster == null
                    ? null
                    : EroticAttackCatalog.Get(enemy.Monster);
                return spec is { Steadfast: false }
                    && !enemy.HasPower<SteadfastPower>();
            }).ToArray();
            await ctx.Play(card);
            ctx.AssertEqual("temptation gained", 30,
                Temptation.Get(ctx.Player) - temptation);
            ctx.AssertTrue("fixture contains an erotic-eligible enemy", eligible.Length > 0,
                effect: false);
            foreach (Creature enemy in eligible)
            {
                ctx.AssertTrue($"{enemy.Name} intent replaced by erotic intent",
                    enemy.Monster?.NextMove.StateId.StartsWith(
                        "MAIDENSUCCUBUS_", StringComparison.Ordinal) == true);
            }
        }, 2);

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
            ctx.AssertEqual("judgment upgrade does not grant retain", false,
                card.Keywords.Contains(CardKeyword.Retain));
            await ctx.Reset();
            Creature splashTarget = await CreatureCmd.Add<Byrdonis>(ctx.Combat);
            try
            {
                await CreatureCmd.SetMaxAndCurrentHp(splashTarget, 20000);
                await ctx.ApplyPower<CondemnationPower>(ctx.PrimaryEnemy, 6);
                await ctx.SetUpArmour(1);
                int primaryHp = ctx.PrimaryEnemy.CurrentHp;
                int splashHp = splashTarget.CurrentHp;
                await ctx.Play(ctx.Create<FinalJudgment>(upgraded), ctx.PrimaryEnemy, selectedIndices: [0]);
                ctx.AssertDamage("seven-layer judgment fires only once", ctx.PrimaryEnemy, primaryHp, 49);
                ctx.AssertDamage("release copies exact judgment damage to other enemy", splashTarget, splashHp, 49);
                ctx.AssertPower<CondemnationPower>("threshold judgment clears layers", ctx.PrimaryEnemy, 0);
            }
            finally
            {
                if (!splashTarget.IsDead) await CreatureCmd.Escape(splashTarget);
            }
        }, 6);

    private static void SunDanceProbe() =>
        CustomVariants<SunDance>(async (ctx, card, upgraded) =>
        {
            ctx.AssertEqual("base and upgraded cost are zero", 0,
                card.EnergyCost.GetWithModifiers(CostModifiers.All));
            await ctx.ApplyPower<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(ctx.Self, 2);
            await ctx.Play(card);
            ctx.AssertPower("dexterity equals buff layers", ctx.Self, "DexterityPower", 2);
            ctx.AssertPower("temporary dexterity restoration", ctx.Self,
                "RestoreDexterityAtTurnEndPower", 2);
            ctx.AssertEqual("upgrade grants retain", upgraded,
                card.Keywords.Contains(CardKeyword.Retain));
        }, 4);

    private static void SuperRegenerationProbe() =>
        CustomVariants<SuperRegeneration>(DesignSyncExhaustContract.Regeneration, 19);

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

    private static void BurningBladeRitualProbe() =>
        CustomVariants<BurningBladeRitual>(async (ctx, card, upgraded) =>
        {
            int hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy);
            ctx.AssertDamage("no other hand card still deals independent damage",
                ctx.PrimaryEnemy, hp, upgraded ? 13 : 10);
            ctx.AssertEqual("empty selection does not exhaust this card", PileType.Discard, card.Pile?.Type);
            CardModel fixture = await ctx.Add<StrikeIronclad>(PileType.Hand);
            hp = ctx.PrimaryEnemy.CurrentHp;
            await ctx.Play(card, ctx.PrimaryEnemy, [fixture]);
            ctx.AssertDamage("damage after actual exhaustion", ctx.PrimaryEnemy, hp, upgraded ? 13 : 10);
            ctx.AssertEqual("selected hand card exhausted", PileType.Exhaust, fixture.Pile?.Type);
        }, 4);

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
        int baseCount, int upgradedCount, int bonusPerAttack,
        CardRarity expectedRarity) where T : CardModel =>
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            ctx.AssertEqual("rarity", expectedRarity, card.Rarity);
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
        CustomVariants<T>(async (ctx, card, upgraded) =>
        {
            ctx.AssertEqual("conversion costs 1/0", upgraded ? 0 : 1,
                card.EnergyCost.GetWithModifiers(CostModifiers.All), effect: false);
            await ctx.Add<StrikeIronclad>(PileType.Hand);
            await ctx.Add<SemenCurse>(PileType.Hand);
            await ctx.Add<DefendIronclad>(PileType.Hand);
            await ctx.Play(card);
            ctx.AssertEqual("attack and curse exhausted", 2,
                PileType.Exhaust.GetPile(ctx.Player).Cards.Count(candidate =>
                    candidate is StrikeIronclad or SemenCurse));
            ctx.AssertPower("amplification per exhausted card", ctx.Self,
                "MagicAmplificationPower", 2);
            ctx.AssertEqual("conversion does not exhaust itself", PileType.Discard, card.Pile?.Type);
        }, 3);

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

    private static void HandCostRestriction<T>(CardType affectedType, int surcharge, int expectedOwnCost) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            ctx.AssertEqual("restriction canonical cost", expectedOwnCost, card.EnergyCost.Canonical);
            ctx.AssertEqual("restriction cannot upgrade", 0, card.MaxUpgradeLevel);
            ctx.AssertEqual("restriction card type", CardType.Curse, card.Type);
            ctx.AssertEqual("restriction rarity", CardRarity.Curse, card.Rarity);
            ctx.AssertEqual("restriction target", TargetType.None, card.TargetType);
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            DefendIronclad fixture = await ctx.Add<DefendIronclad>(PileType.Hand);
            StrikeIronclad attack = await ctx.Add<StrikeIronclad>(PileType.Hand);
            ctx.AssertEqual("restriction fixture type", affectedType, fixture.Type);
            ctx.AssertEqual("hand cost surcharge", 1 + surcharge,
                fixture.EnergyCost.GetWithModifiers(CostModifiers.All));
            ctx.AssertEqual("non skill cost unchanged", 1, attack.EnergyCost.GetWithModifiers(CostModifiers.All));
            ctx.AssertEqual("restriction does not surcharge itself", expectedOwnCost,
                card.EnergyCost.GetWithModifiers(CostModifiers.All));
            DesignSyncCombatTextContract.AssertText(ctx, card, PileType.Hand,
                "如果这张牌在你的手牌中，你的技能牌额外耗能〈能量〉。", "restriction full text");
            foreach (PileType pile in new[] { PileType.Draw, PileType.Discard, PileType.Exhaust })
            {
                await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, pile, skipVisuals: true);
                ctx.AssertEqual($"{pile} restores skill cost", 1,
                    fixture.EnergyCost.GetWithModifiers(CostModifiers.All));
                ctx.AssertTrue($"{pile} restriction inactive",
                    !card.TryModifyEnergyCostInCombat(fixture, 1, out decimal unchanged));
                ctx.AssertEqual($"{pile} preserves incoming cost", 1m, unchanged);
                await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
                ctx.AssertEqual($"return from {pile} restores surcharge once", 1 + surcharge,
                    fixture.EnergyCost.GetWithModifiers(CostModifiers.All));
            }
        }, 21);

    private static void HandPlayRestriction<T>(CardType affectedType) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            StrikeIronclad fixture = await ctx.Add<StrikeIronclad>(PileType.Hand);
            ctx.AssertTrue("hand play restriction active",
                !MegaCrit.Sts2.Core.Hooks.Hook.ShouldPlay(ctx.Combat, fixture, out _, MegaCrit.Sts2.Core.Entities.Cards.AutoPlayType.Default));
        });

    private static void HandTemptation<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            int before = Temptation.Get(ctx.Player);
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            ctx.AssertEqual("temptation while in hand", amount,
                Temptation.Get(ctx.Player) - before);
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Draw, skipVisuals: true);
            ctx.AssertEqual("temptation removed after leaving hand", 0,
                Temptation.Get(ctx.Player) - before);
        }, 2);

    private static void DrawTriggerDesire<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Draw, skipVisuals: true);
            int before = Desire.Get(ctx.Player);
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Draw(
                new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(), 1, ctx.Player);
            ctx.AssertEqual("draw-triggered desire", amount, Desire.Get(ctx.Player) - before);
        });

    private static void PlayedArmorLoss<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await TransformationCmd.EnterImmaculateRobe(
                new BlockingPlayerChoiceContext(), ctx.Self, null);
            int armor = ctx.PowerAmount(ctx.Self, "MagicArmorPower");
            await ctx.Play(card);
            ctx.AssertPower("armor loss on play", ctx.Self,
                "MagicArmorPower", armor - amount);
            ctx.AssertTrue("played status exhausted",
                card.Pile?.Type == PileType.Exhaust || card.HasBeenRemovedFromState);
        }, 2);

    private static void EndTurnArmorLoss<T>(int amount, params CardKeyword[] keywords)
        where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await TransformationCmd.EnterImmaculateRobe(
                new BlockingPlayerChoiceContext(), ctx.Self, null);
            int armor = ctx.PowerAmount(ctx.Self, "MagicArmorPower");
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await ctx.InvokeTurnEndInHand(card);
            ctx.AssertPower("armor loss", ctx.Self, "MagicArmorPower", armor - amount);
            foreach (CardKeyword keyword in keywords)
                ctx.AssertTrue($"{keyword} keyword", card.Keywords.Contains(keyword));
        }, 1 + keywords.Length);

    private static void RetainedEndTurnArmorLoss<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await TransformationCmd.EnterImmaculateRobe(
                new BlockingPlayerChoiceContext(), ctx.Self, null);
            int armor = ctx.PowerAmount(ctx.Self, "MagicArmorPower");
            await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(card, PileType.Hand, skipVisuals: true);
            await card.BeforeSideTurnEnd(
                new BlockingPlayerChoiceContext(), CombatSide.Player, [ctx.Self]);
            ctx.AssertPower("first retained armor loss", ctx.Self,
                "MagicArmorPower", armor - amount);
            ctx.AssertTrue("retains in hand", card.ShouldRetainThisTurn && card.Pile?.Type == PileType.Hand);
            await card.BeforeSideTurnEnd(
                new BlockingPlayerChoiceContext(), CombatSide.Player, [ctx.Self]);
            ctx.AssertPower("repeated retained armor loss", ctx.Self,
                "MagicArmorPower", armor - amount * 2);
        }, 3);

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
            await TransformationCmd.EnterImmaculateRobe(
                new BlockingPlayerChoiceContext(), ctx.Self, null);
            int armor = ctx.PowerAmount(ctx.Self, "MagicArmorPower");
            await ctx.Play(card);
            ctx.AssertPower("armor after loss", ctx.Self,
                "MagicArmorPower", armor - amount);
            ctx.AssertTrue("removed after play", card.HasBeenRemovedFromState);
        }, 2);

    private static void CurseNextTurnEnergyLoss<T>(int amount) where T : CardModel =>
        BaseOnly<T>(async (ctx, card) =>
        {
            await ctx.Play(card);
            await PlayerCmd.SetEnergy(5, ctx.Player);
            int energy = ctx.Player.PlayerCombatState!.Energy;
            foreach (PowerModel power in ctx.Self.Powers.ToArray())
                await power.AfterEnergyReset(ctx.Player);
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
