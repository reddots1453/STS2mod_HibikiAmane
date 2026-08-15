using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;

namespace MaidenSuccubus.Bootstrap;

/// <summary>
/// Centralized dependency diagnostics. Referencing SecondaryResourceDefinition
/// here also makes API availability a compile-time requirement.
/// </summary>
public static class ModCompatibility
{
    public static void LogAndValidate()
    {
        var modAssembly = typeof(MaidenSuccubusMod).Assembly.GetName();
        var gameAssembly = typeof(RunState).Assembly.GetName();
        var ritsuAssembly = typeof(RitsuLibFramework).Assembly.GetName();
        var resourceAssembly = typeof(SecondaryResourceDefinition).Assembly.GetName();

        MaidenSuccubusMod.Logger.Info(
            $"Compatibility: gameAssembly={gameAssembly.Version}, " +
            $"mod={modAssembly.Version}, " +
            $"RitsuLib={ritsuAssembly.Version}, " +
            $"SecondaryResourceAssembly={resourceAssembly.Name} {resourceAssembly.Version}");

        // Compile-time API probe plus an explicit runtime diagnostic.
        var registryMethod = typeof(RitsuLibFramework).GetMethod(
            nameof(RitsuLibFramework.GetSecondaryResourceRegistry));
        if (registryMethod == null)
        {
            throw new MissingMethodException(
                typeof(RitsuLibFramework).FullName,
                nameof(RitsuLibFramework.GetSecondaryResourceRegistry));
        }
    }
}
