using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;

namespace MaidenSuccubus.Telemetry;

/// <summary>
/// RitsuLib owns the consent UI and durable queue. Until an endpoint is configured,
/// its disabled adapter leaves authorized run histories on disk without sending them.
/// </summary>
internal static class MaidenTelemetry
{
    internal static void Register()
    {
        RitsuLibFramework.RegisterTelemetryApplicant(new TelemetryApplicant
        {
            ApplicantId = MaidenSuccubusMod.ModId,
            OwnerModId = MaidenSuccubusMod.ModId,
            DisplayName = MaidenSuccubusMod.ModDisplayName,
            DisplayNameText = ModSettingsText.Literal(MaidenSuccubusMod.ModDisplayName),
            Adapter = new DisabledTelemetryAdapter("仅保存到本地；尚未配置接收端，不会上传"),
            Requests =
            [
                TelemetryRequest.RunHistory(
                    ModSettingsText.Literal("授权后保存响木天音参与的完整跑局记录到本地队列，包括多人跑局；目前不会上传。可在 RitsuLib 设置中撤销授权并清除队列。"),
                    captureFilter: evt => evt.Run.Players.Any(player =>
                        player.CharacterId?.Category == "MAIDEN_SUCCUBUS_CHARACTER")),
            ],
        });
    }
}
