#if DEBUG
namespace MaidenSuccubus.Debugging.CardEffects;

/// <summary>
/// Cards added or behaviorally changed by the 2026-09-14 DesignDoc baseline.
/// This explicit list makes the focused runtime suite and its numeric assertion
/// gate independent from source-file layout or Git history.
/// </summary>
internal static class Iteration2CardEffectContract
{
    public const int ExpectedCardCount = 59;

    public static IReadOnlySet<Type> CardTypes { get; } = new HashSet<Type>
    {
        typeof(Cards.Bath),
        typeof(Cards.BiteInvader),
        typeof(Cards.BorrowedForceStrike),
        typeof(Cards.ChangePanties),
        typeof(Cards.Consecration),
        typeof(Cards.CounterBarrier),
        typeof(Cards.CounterBarrierII),
        typeof(Cards.CounterBarrierIII),
        typeof(Cards.CounterBarrierIV),
        typeof(Cards.CycloneRupture),
        typeof(Cards.DarkElement),
        typeof(Cards.DarkFlameBarrier),
        typeof(Cards.DesireWhip),
        typeof(Cards.DoubleDefense),
        typeof(Cards.DreamMist),
        typeof(Cards.Exhibitionist),
        typeof(Cards.ExposePlay),
        typeof(Cards.FearAura),
        typeof(Cards.FlameBloom),
        typeof(Cards.FrozenBracelet),
        typeof(Cards.HolyPunishment),
        typeof(Cards.Judgment),
        typeof(Cards.LightArrow),
        typeof(Cards.LightningKick),
        typeof(Cards.LightningRecoil),
        typeof(Cards.LightPowerRelease),
        typeof(Cards.LureDeep),
        typeof(Cards.MagicBurst),
        typeof(Cards.MagiciansSecret),
        typeof(Cards.MemoryImprint),
        typeof(Cards.MentalUnity),
        typeof(Cards.MiasmaConversion),
        typeof(Cards.MindsEye),
        typeof(Cards.PleasureDrowning),
        typeof(Cards.PleasureGarden),
        typeof(Cards.ReflectiveBarrier),
        typeof(Cards.RegenerativeMagicFiber),
        typeof(Cards.RepairAlyssa),
        typeof(Cards.ResistanceGloves),
        typeof(Cards.RestraintEvasion),
        typeof(Cards.TacticalAnalyzer),
        typeof(Cards.Transform),
        typeof(Cards.UltimateFlare),
        typeof(Cards.WinterHolly),
        typeof(Cards.Curses.CorrosiveSlimeCurse),
        typeof(Cards.Curses.TransparentOutfitCurse),
        typeof(Cards.BarbedHookStatus),
        typeof(Cards.ClothingBurnStatus),
        typeof(Cards.BitingPaperStatus),
        typeof(Cards.DissolvingFluidStatus),
        typeof(Cards.HolyFlame),
        typeof(Cards.BurningRack),
        typeof(Cards.ExorcismPerfume),
        typeof(Cards.PurificationOrb),
        typeof(Cards.ForgeStrike),
        typeof(Cards.SummonThunder),
        typeof(Cards.MagicStarBomb),
        typeof(Cards.Takemikazuchi),
        typeof(Cards.WindGodCloak),
    };

    public static bool Contains(Type cardType) => CardTypes.Contains(cardType);
}
#endif
