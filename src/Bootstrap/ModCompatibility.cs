using System.Reflection;
using HarmonyLib;
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

    public static FieldInfo? FindField(
        Type declaringType,
        string name,
        Type expectedType)
    {
        FieldInfo? field = AccessTools.Field(declaringType, name);
        if (field == null || field.FieldType != expectedType)
        {
            LogMemberMismatch(
                declaringType,
                name,
                $"field:{expectedType.FullName}",
                field == null ? "missing" : $"field:{field.FieldType.FullName}");
            return null;
        }
        return field;
    }

    public static PropertyInfo? FindWritableProperty(
        Type declaringType,
        string name,
        Type expectedType)
    {
        PropertyInfo? property = AccessTools.Property(declaringType, name);
        if (property == null
            || property.PropertyType != expectedType
            || property.SetMethod == null)
        {
            string actual = property == null
                ? "missing"
                : $"property:{property.PropertyType.FullName},writable={property.SetMethod != null}";
            LogMemberMismatch(
                declaringType,
                name,
                $"property:{expectedType.FullName},writable=true",
                actual);
            return null;
        }
        return property;
    }

    public static MethodInfo? FindInstanceMethod(
        Type declaringType,
        string name,
        Type returnType,
        params Type[] parameters)
    {
        MethodInfo? method = AccessTools.Method(declaringType, name, parameters);
        if (method == null || method.IsStatic || method.ReturnType != returnType)
        {
            string signature = string.Join(",", parameters.Select(type => type.FullName));
            string actual = method == null
                ? "missing"
                : $"static={method.IsStatic},return={method.ReturnType.FullName}";
            LogMemberMismatch(
                declaringType,
                name,
                $"instance ({signature}) -> {returnType.FullName}",
                actual);
            return null;
        }
        return method;
    }

    public static EventInfo? FindEvent(
        Type declaringType,
        string name,
        Type expectedHandlerType)
    {
        EventInfo? eventInfo = AccessTools.Event(declaringType, name);
        if (eventInfo?.EventHandlerType != expectedHandlerType)
        {
            LogMemberMismatch(
                declaringType,
                name,
                $"event:{expectedHandlerType.FullName}",
                eventInfo == null
                    ? "missing"
                    : $"event:{eventInfo.EventHandlerType?.FullName}");
            return null;
        }
        return eventInfo;
    }

    private static void LogMemberMismatch(
        Type declaringType,
        string name,
        string expected,
        string actual)
    {
        MaidenSuccubusMod.Logger.Warn(
            $"Compatibility gate disabled for {declaringType.FullName}.{name}: "
            + $"expected {expected}, found {actual}. "
            + $"Game assembly={typeof(RunState).Assembly.GetName().Version}.");
    }
}
