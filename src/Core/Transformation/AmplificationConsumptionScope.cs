using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Util;

namespace MaidenSuccubus.Core.Transformation;

/// <summary>Only the exact Vortex child play is exempt, across its awaited replays.</summary>
internal static class AmplificationConsumptionScope
{
    private static readonly WeakInstanceScope<CardModel> Active = new();

    internal static bool IsExempt(CardModel card) => Active.Contains(card);
    internal static IDisposable Enter(CardModel card) => Active.Enter(card);
}
