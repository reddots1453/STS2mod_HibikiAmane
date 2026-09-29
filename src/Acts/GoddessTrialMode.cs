using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Data;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace MaidenSuccubus.Acts;

public sealed class GoddessTrialSettingsData
{
    public bool Enabled { get; set; } = true;
}

/// <summary>Global menu preference is captured per run so changing settings cannot rewrite a live run.</summary>
internal static class GoddessTrialMode
{
    private const string SettingsKey = "goddess_trials";

    internal static void Register()
    {
        RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId).Register(
            key: SettingsKey, fileName: "goddess_trials.json", scope: SaveScope.Global,
            defaultFactory: () => new GoddessTrialSettingsData());
        var binding = ModSettingsBindings.Global<GoddessTrialSettingsData, bool>(
            MaidenSuccubusMod.ModId, SettingsKey,
            data => data.Enabled, (data, value) => data.Enabled = value);
        ModSettingsRegistry.Register(MaidenSuccubusMod.ModId, page => page
            .WithTitle(ModSettingsText.Literal("响木天音"))
            .WithModDisplayName(ModSettingsText.Literal("Maiden & Succubus"))
            .WithVisibleOnHostSurfaces(ModSettingsHostSurface.MainMenu)
            .AddSection("routes", section => section
                .WithTitle(ModSettingsText.Literal("路线规则"))
                .AddToggle("goddess_trials", ModSettingsText.Literal("启用女神试炼"), binding,
                    ModSettingsText.Literal("关闭后，新开局不选择女神试炼；进入第二幕和第三幕后分别选择堕落值 +2 或 -2。已有跑局沿用开局时的规则。"))));
    }

    internal static void CaptureNewRun(RunState run)
    {
        if (!run.Players.Any(player => player.Character is MaidenSuccubusCharacter)) return;
        bool enabled = RitsuLibFramework.GetDataStore(MaidenSuccubusMod.ModId)
            .Get<GoddessTrialSettingsData>(SettingsKey).Enabled;
        M5Progress.Handle.Modify(run, state => state.GoddessTrialsEnabled = enabled);
    }

    internal static bool Enabled(RunState run) => M5Progress.Handle.Get(run).GoddessTrialsEnabled ?? true;

    internal static bool NeedsActChoice(RunState run) => !Enabled(run)
        && ActAlignmentChoiceRules.Needs(run.CurrentActIndex,
            M5Progress.Handle.Get(run).ResolvedAlignmentActs);

    internal static bool ResolveActChoice(RunState run, int actIndex, int delta)
    {
        if (Enabled(run) || run.CurrentActIndex != actIndex
            || !ActAlignmentChoiceRules.ValidDelta(delta) || !NeedsActChoice(run)) return false;
        // Receipt is persisted before publishing the resource change. Re-entering the map cannot double-award.
        M5Progress.Handle.Modify(run, state => state.ResolvedAlignmentActs.Add(actIndex));
        Core.Corruption.CorruptionCmd.Modify(run, delta,
            new Core.Corruption.CorruptionChangeSource($"act_alignment.{actIndex}"));
        return true;
    }
}
