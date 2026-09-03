using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace MaidenSuccubus.Powers;

internal static class CommonPowerAssets
{
    internal static readonly PowerAssetProfile Corruption = new(
        IconPath: "res://images/powers/vulnerable_power.png",
        BigIconPath: "res://images/powers/vulnerable_power.png");

    internal static readonly PowerAssetProfile Generic = new(
        IconPath: "res://images/powers/strength_power.png",
        BigIconPath: "res://images/powers/strength_power.png");
}

/// <summary>
/// Every custom power gets a valid built-in asset by default. Raw debug-mod
/// PNG/SVG files are not registered in Godot's imported-resource database, so
/// asking RitsuLib to load them produces a warning and a failed lookup every
/// time the power UI refreshes.
/// </summary>
public abstract class MaidenSuccubusPowerTemplate : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => CommonPowerAssets.Generic;
}
