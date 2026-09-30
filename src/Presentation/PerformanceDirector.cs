using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MaidenSuccubus.Core.Intents;

namespace MaidenSuccubus.Presentation;

public static class PerformanceDirector
{
    private static readonly HashSet<string> ActionAudio = new(StringComparer.Ordinal);
    private static int _sceneGeneration;

    public static async Task PlayControlAsync(MonsterModel source, Creature target)
    {
        if (!PerformanceAudience.IsLocalMaiden(target.Player)) return;
        MonsterPerformanceProfile profile = MonsterPerformanceProfiles.Get(source);
        if (Mark(source, EroticIntentKind.Control))
        {
            PerformanceAudioService.PlayOneShot(profile.Control == ControlVisualKind.Device
                ? PerformanceAudioCue.RestraintChain
                : PerformanceAudioCue.RestraintHold);
        }
        PerformanceAudioService.StartLoop(PerformanceLoopCue.Breath);
        try
        {
            await CutscenePlaybackService.PlayAsync(
                target.Player!,
                [PerformanceAssets.Control(profile.Control)]);
        }
        finally
        {
            PerformanceAudioService.StopLoop(PerformanceLoopCue.Breath);
        }
    }

    public static async Task PlayInvasionAsync(MonsterModel source, Player target)
    {
        if (!PerformanceAudience.IsLocalMaiden(target)) return;
        int sceneGeneration = _sceneGeneration;
        bool playAudio = Mark(source, EroticIntentKind.Invasion);
        if (playAudio)
        {
            PerformanceAudioService.PlayOneShot(PerformanceAudioCue.InvasionStart);
        }
        PerformanceAudioService.StartLoop(PerformanceLoopCue.Breath);
        try
        {
            MonsterPerformanceProfile profile = MonsterPerformanceProfiles.Get(source);
            await CutscenePlaybackService.PlayAsync(
                target,
                [PerformanceAssets.Invasion(profile.Invasion)]);
        }
        finally
        {
            PerformanceAudioService.StopLoop(PerformanceLoopCue.Breath);
            if (playAudio && sceneGeneration == _sceneGeneration)
            {
                PerformanceAudioService.PlayOneShot(PerformanceAudioCue.InvasionFinish);
            }
        }
    }

    public static IDisposable BeginDesireAction(MonsterModel source, Creature target)
    {
        if (!PerformanceAudience.IsLocalMaiden(target.Player)) return EmptyScope.Instance;
        if (Mark(source, EroticIntentKind.Desire))
        {
            MonsterPerformanceProfile profile = MonsterPerformanceProfiles.Get(source);
            PerformanceAudioService.PlayOneShot(profile.TentacleAudio
                ? PerformanceAudioCue.EroticAttackTentacle
                : PerformanceAudioCue.EroticAttackTouch);
        }
        PerformanceAudioService.StartLoop(PerformanceLoopCue.Breath);
        return new BreathScope();
    }

    public static async Task PlayMasturbationAsync(Player player)
    {
        if (!PerformanceAudience.IsLocalMaiden(player)) return;

        if (!PerformanceSettings.Current.AdultCgEnabled)
        {
            PerformanceAudioService.PlayOneShot(PerformanceAudioCue.Climax);
            return;
        }

        await CutscenePlaybackService.PlayAsync(
            player,
            PerformanceAssets.MasturbationSequence,
            index =>
            {
                if (index == 1)
                    PerformanceAudioService.PlayOneShot(PerformanceAudioCue.Climax);
            });
    }

    public static void OnSceneTransition()
    {
        _sceneGeneration++;
        CutscenePlaybackService.CancelActive();
        PerformanceAudioService.StopAll();
        ActionAudio.Clear();
    }

    public static void OnCombatEnded()
    {
        _sceneGeneration++;
        CutscenePlaybackService.CancelActive();
        PerformanceAudioService.StopAll();
        ActionAudio.Clear();
    }

    private static bool Mark(MonsterModel source, EroticIntentKind kind)
    {
        int round = source.CombatState?.RoundNumber ?? -1;
        string key = $"{RuntimeHelpers.GetHashCode(source)}:{round}:{kind}";
        return ActionAudio.Add(key);
    }

    private sealed class BreathScope : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            PerformanceAudioService.StopLoop(PerformanceLoopCue.Breath);
        }
    }

    private sealed class EmptyScope : IDisposable
    {
        public static readonly EmptyScope Instance = new();
        public void Dispose() { }
    }
}
