using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.Presentation;

public enum ControlVisualKind { Humanoid, Tentacle, Device }
public enum InvasionVisualKind
{
    WeakMonster,
    Tentacle,
    Alien,
    HumanoidGroup,
    Parasite,
    Slime,
    MagicDevice,
}

public readonly record struct MonsterPerformanceProfile(
    ControlVisualKind Control,
    InvasionVisualKind Invasion,
    bool TentacleAudio);

/// <summary>
/// Presentation-only monster mapping. Keeping it outside the UI and intent
/// state machine lets later reviews adjust individual monsters without
/// changing combat rules.
/// </summary>
public static class MonsterPerformanceProfiles
{
    private static readonly HashSet<string> TentacleLike = new(StringComparer.OrdinalIgnoreCase)
    {
        "FOGMOG", "FUZZY_WURM_CRAWLER", "PHROG_PARASITE", "WRIGGLER",
        "LEAF_SLIME_M", "LEAF_SLIME_S", "TWIG_SLIME_M", "TWIG_SLIME_S",
        "SNAPPING_JAXFRUIT", "SLITHERING_STRANGLER", "VINE_SHAMBLER",
        "VANTOM", "BOWLBUG_NECTAR", "BOWLBUG_SILK", "ENTOMANCER", "MYTE",
        "THE_OBSCURA", "TUNNELER", "SLIMED_BERSERKER", "THE_LOST",
        "THE_FORGOTTEN", "CORPSE_SLUG", "LIVING_FOG", "LAGAVULIN_MATRIARCH",
        "PHANTASMAL_GARDENER", "SEWER_CLAM", "SKULKING_COLONY",
        "SLUDGE_SPINNER", "SOUL_FYSH", "TERROR_EEL", "TOADPOLE",
        "TWO_TAILED_RAT", "WATERFALL_GIANT",
    };

    private static readonly HashSet<string> Devices = new(StringComparer.OrdinalIgnoreCase)
    {
        "CHOMPER", "ROCKET", "INFESTED_PRISM", "KNOWLEDGE_DEMON",
        "AEONGLASS", "FLAIL_KNIGHT", "MAGI_KNIGHT", "OWL_MAGISTRATE",
        "TURRET_OPERATOR", "HAUNTED_SHIP",
    };

    private static readonly HashSet<string> HumanoidGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        "ASSASSIN_RUBY_RAIDER", "AXE_RUBY_RAIDER", "BRUTE_RUBY_RAIDER",
        "CROSSBOW_RUBY_RAIDER", "TRACKER_RUBY_RAIDER", "KIN_FOLLOWER",
        "KIN_PRIEST", "DEVOTED_SCULPTOR", "FLAIL_KNIGHT", "MAGI_KNIGHT",
        "SPECTRAL_KNIGHT", "OWL_MAGISTRATE", "QUEEN", "TURRET_OPERATOR",
        "CALCIFIED_CULTIST", "DAMP_CULTIST", "GREMLIN_MERC",
        "SNEAKY_GREMLIN", "FAT_GREMLIN", "SEAPUNK",
    };

    private static readonly HashSet<string> Parasites = new(StringComparer.OrdinalIgnoreCase)
    {
        "PHROG_PARASITE", "WRIGGLER", "LOUSE_PROGENITOR", "MYTE",
        "OVICOPTER", "EXOSKELETON", "TEST_SUBJECT",
    };

    private static readonly HashSet<string> Slimes = new(StringComparer.OrdinalIgnoreCase)
    {
        "LEAF_SLIME_M", "LEAF_SLIME_S", "TWIG_SLIME_M", "TWIG_SLIME_S",
        "SLIMED_BERSERKER", "CORPSE_SLUG", "SLUDGE_SPINNER", "SEWER_CLAM",
    };

    private static readonly HashSet<string> MagicDevices = new(StringComparer.OrdinalIgnoreCase)
    {
        "INFESTED_PRISM", "KNOWLEDGE_DEMON", "AEONGLASS", "SOUL_NEXUS",
        "TORCH_HEAD_AMALGAM", "HAUNTED_SHIP", "LIVING_FOG",
    };

    private static readonly HashSet<string> Aliens = new(StringComparer.OrdinalIgnoreCase)
    {
        "CEREMONIAL_BEAST", "MAWLER", "NIBBIT", "SNAPPING_JAXFRUIT",
        "CRUSHER", "DECIMILLIPEDE_SEGMENT_FRONT", "HUNTER_KILLER",
        "THE_INSATIABLE", "FROG_KNIGHT", "LAGAVULIN_MATRIARCH",
        "SOUL_FYSH", "WATERFALL_GIANT",
    };

    public static MonsterPerformanceProfile Get(MonsterModel monster) => Get(monster.Id.Entry);

    public static MonsterPerformanceProfile Get(string monsterId)
    {
        bool tentacle = TentacleLike.Contains(monsterId);
        ControlVisualKind control = Devices.Contains(monsterId)
            ? ControlVisualKind.Device
            : tentacle ? ControlVisualKind.Tentacle : ControlVisualKind.Humanoid;
        InvasionVisualKind invasion = Slimes.Contains(monsterId)
            ? InvasionVisualKind.Slime
            : Parasites.Contains(monsterId) ? InvasionVisualKind.Parasite
            : HumanoidGroups.Contains(monsterId) ? InvasionVisualKind.HumanoidGroup
            : MagicDevices.Contains(monsterId) ? InvasionVisualKind.MagicDevice
            : tentacle ? InvasionVisualKind.Tentacle
            : Aliens.Contains(monsterId) ? InvasionVisualKind.Alien
            : InvasionVisualKind.WeakMonster;
        return new MonsterPerformanceProfile(control, invasion, tentacle);
    }
}
