using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace MaidenSuccubus.Core.Replay;

/// <summary>Marks the temporary copied instance; native textured surfaces carry the tint.</summary>
[RegisterModelCapability(StableEntryStem = "battle_replay_origin")]
public sealed class BattleReplayOriginCapability : CardCapability { }
