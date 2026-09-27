using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace MaidenSuccubus.UI;

/// <summary>Connects the reviewed formal power art to RitsuLib's texture hooks.</summary>
public static class PowerIconAssets
{
    private static readonly IReadOnlyDictionary<string, string> Basenames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ScorchingMagicPower"] = "scorching_magic",
            ["RestoreStrengthAtTurnEndPower"] = "restore_strength_at_turn_end",
            ["DelayedStrengthPricePower"] = "delayed_strength_price",
            ["CondemnationPower"] = "condemnation",
            ["CondemnationRetentionPower"] = "condemnation_retention",
            ["DesireIntentThresholdPower"] = "desire_attack",
            ["ControlIntentThresholdPower"] = "restraint",
            ["InvasionIntentThresholdPower"] = "violation",
            ["ControlPower"] = "restraint",
            ["DesirePaidWithHpPower"] = "desire_paid_with_hp",
            ["DesireStunPower"] = "desire_stun",
            ["SelfImportantPower"] = "self_important",
            ["RestoreDexterityAtTurnEndPower"] = "restore_dexterity_at_turn_end",
            ["PreventNextDesireGainPower"] = "prevent_next_desire_gain",
            ["InwardDisciplinePower"] = "inward_discipline",
            ["HolyRadiancePower"] = "holy_radiance",
            ["HolyResonancePower"] = "holy_resonance",
            ["LordOfBlazePower"] = "lord_of_blaze",
            ["DarkFlameBarrierPower"] = "dark_flame_barrier",
            ["RecollectionRoomPower"] = "recollection_room",
            ["YarusLibraryPower"] = "recollection_room",
            ["SemenAppetitePower"] = "semen_appetite",
            ["ChastityDefensePower"] = "chastity_defense",
            ["RegenerativeMagicFiberPower"] = "regenerative_magic_fiber",
            ["RestNextTurnPower"] = "rest_next_turn",
            ["EctoplasmResidueEnergyLossPower"] = "ectoplasm_residue_energy_loss",
            ["EternalRobePower"] = "eternal_robe",
            ["CorruptRobePower"] = "corrupt_robe",
            ["HolyFlamePower"] = "holy_flame",
            ["OpeningPrayerPower"] = "opening_prayer",
            ["WetPower"] = "wet",
            ["TacticalCorePower"] = "tactical_core",
            ["MagicResonancePower"] = "magic_resonance",
            ["GoddessOfIcePower"] = "goddess_of_ice",
            ["BindingInsightPower"] = "binding_insight",
            ["ShatterPower"] = "shatter",
            ["BurningPower"] = "burning",
            ["FearAuraStrengthLossPower"] = "fear_aura_strength_loss",
            ["LegendaryMinerPower"] = "legendary_miner",
            ["DesireRecyclePower"] = "desire_recycle",
            ["IgnitePower"] = "ignite",
            ["ChainDestructionPower"] = "chain_destruction",
            ["ChainDestructionReplayPower"] = "chain_destruction",
            ["CurseCorridorPower"] = "curse_corridor",
            ["ConsecrationPower"] = "consecration",
            ["SoulPurificationPower"] = "soul_purification",
            ["MemoryImprintPower"] = "memory_imprint",
            ["MentalUnityPower"] = "mental_unity",
            ["CounterDefensePower"] = "counter_defense",
            ["UltimateFlarePower"] = "ultimate_flare",
            ["MagicIndexPower"] = "magic_index",
            ["ResonanceArmorPower"] = "resonance_armor",
            ["LullabyPower"] = "lullaby",
            ["TenaciousResistancePower"] = "tenacious_resistance",
            ["AbnormalAdaptationPower"] = "abnormal_adaptation",
            ["MasochisticGirlPower"] = "masochistic_girl",
            ["SanctuaryPower"] = "sanctuary",
            ["BlizzardEchoPower"] = "blizzard_echo",
            ["BerserkerMaskPower"] = "berserker_mask",
            ["CurseWedgePower"] = "curse_wedge",
            ["MultipleReproductionPower"] = "multiple_reproduction",
            ["BasicTrainingPower"] = "basic_training",
            ["PurificationPower"] = "purification",
            ["SteadfastPower"] = "steadfast",
            ["ImmaculateRobePower"] = "immaculate_robe",
            ["MagicArmorPower"] = "magic_armor",
            ["MagicAmplificationPower"] = "magic_amplification",
            ["UnboundedDesirePower"] = "unbounded_desire",
            ["GuardianScripturePower"] = "guardian_scripture",
            ["NimbleScripturePower"] = "nimble_scripture",
            ["PunishmentScripturePower"] = "punishment_scripture",
            ["WisdomScripturePower"] = "wisdom_scripture",
            ["VitalityScripturePower"] = "vitality_scripture",
            ["BlissScripturePower"] = "bliss_scripture",
        };

    public static void Register()
    {
        ExternalAssetOverrideRegistry.RegisterPowerIconTextureProvider(
            "maiden_formal_power_icons",
            power => Load(power, big: false));
        ExternalAssetOverrideRegistry.RegisterPowerBigIconTextureProvider(
            "maiden_formal_power_big_icons",
            power => Load(power, big: true));
    }

    private static Texture2D? Load(PowerModel power, bool big)
    {
        if (!Basenames.TryGetValue(power.GetType().Name, out string? basename))
        {
            return null;
        }

        string folder = big ? "256x256" : "64x64";
        string suffix = big ? "_power_big.png" : "_power.png";
        return RuntimeTextureAssets.Load($"powers/{folder}/{basename}{suffix}");
    }
}
