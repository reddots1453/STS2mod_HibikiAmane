using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MaidenSuccubus.Localization;

/// <summary>Keep the initial hit separate from the calculated damage base.</summary>
internal sealed class DesireSpendHitsVar : CalculatedVar
{
    internal DesireSpendHitsVar() : base("Hits") { }

    protected override DynamicVar GetBaseVar() =>
        ((CardModel)_owner).DynamicVars["InitialHits"];
}
