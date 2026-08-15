using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Acts;

[RegisterAct]
public sealed class MaidenSuccubusFourthAct : ModActTemplate
{
    public override int Index => 3;
    public override bool IsDefault => false;
    public override bool IsUnlocked(UnlockState unlockState) => true;

    public override ActAssetProfile AssetProfile =>
        ContentAssetProfiles.FromVanillaActId("glory");

    public override Color MapTraveledColor => new("392B45");
    public override Color MapUntraveledColor => new("A68FB2");
    public override Color MapBgColor => new("1E1726");
    public override string[] BgMusicOptions => ["event:/music/act3_a1_v1"];
    public override string[] MusicBankPaths => ["res://banks/desktop/act3_a1.bank"];
    public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";
    protected override int NumberOfWeakEncounters => 2;
    protected override int BaseNumberOfRooms => 13;
    public override string ChestSpineSkinNameNormal => "act3";
    public override string ChestSpineSkinNameStroke => "act3_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";

    // Route selection happens immediately before this act is entered. An empty
    // discovery order prevents vanilla tutorial discovery from overriding it.
    public override IEnumerable<EncounterModel> BossDiscoveryOrder =>
        Array.Empty<EncounterModel>();

    public override IEnumerable<AncientEventModel> AllAncients =>
        [ModelDb.AncientEvent<Vakuu>()];

    public override IEnumerable<EventModel> AllEvents =>
        [ModelDb.Event<Trial>()];

    public override IEnumerable<EncounterModel> GenerateAllEncounters() =>
    [
        ModelDb.Encounter<DevotedSculptorWeak>(),
        ModelDb.Encounter<TurretOperatorWeak>(),
        ModelDb.Encounter<AxebotsNormal>(),
        ModelDb.Encounter<FabricatorNormal>(),
        ModelDb.Encounter<GlobeHeadNormal>(),
        ModelDb.Encounter<KnightsElite>(),
        ModelDb.Encounter<MechaKnightElite>(),
        ModelDb.Encounter<HolyAct4PlaceholderBoss>(),
        ModelDb.Encounter<NeutralAct4PlaceholderBoss>(),
        ModelDb.Encounter<CorruptAct4PlaceholderBoss>(),
    ];

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(
        UnlockState state) => AllAncients;

    protected override void ApplyActDiscoveryOrderModifications(
        UnlockState unlockState)
    {
    }

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) =>
        new(MapPointTypeCounts.StandardRandomUnknownCount(mapRng), 6);
}
