using Godot;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib;

namespace MaidenSuccubus.Presentation;

public static class PerformanceAudioService
{
    private static readonly Dictionary<PerformanceLoopCue, AudioStreamPlayer> Loops = [];
    private static readonly HashSet<AudioStreamPlayer> OneShots = [];
    private static readonly HashSet<AudioStreamPlayer> Fading = [];
    private static readonly Dictionary<string, AudioStream> Cache = new(StringComparer.Ordinal);
    private static readonly HashSet<string> MissingAssets = new(StringComparer.Ordinal);
    private static readonly List<IDisposable> Subscriptions = [];
    private static Node? _host;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        Subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(
            _ => PerformanceDirector.OnCombatEnded(), replayCurrentState: false));
    }

    public static void PlayOneShot(PerformanceAudioCue cue)
    {
        try
        {
            PlayOneShotCore(cue);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Performance one-shot '{cue}' failed safely: {ex.Message}");
        }
    }

    private static void PlayOneShotCore(PerformanceAudioCue cue)
    {
        bool adult = PerformanceAssets.IsAdult(cue);
        if (adult && !PerformanceSettings.Current.AdultAudioEnabled) return;

        AudioStream? stream = Load(PerformanceAssets.AudioPath(cue), adult);
        Node? host = EnsureHost();
        if (stream == null || host == null) return;

        var player = new AudioStreamPlayer
        {
            Name = $"MaidenPerformance-{cue}",
            Stream = stream,
            Bus = new StringName("SFX"),
            VolumeLinear = Volume(adult),
        };
        host.AddChild(player);
        OneShots.Add(player);
        player.Finished += () => ReleaseOneShot(player);
        player.Play();
    }

    /// <summary>Desire maximum reuses the same single cue as the rest-site action.</summary>
    public static void PlayDesireMaximum() => PlayOneShot(PerformanceAudioCue.Climax);

    public static void StartLoop(PerformanceLoopCue cue, float fadeSeconds = 0.2f)
    {
        try
        {
            StartLoopCore(cue, fadeSeconds);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Performance loop '{cue}' failed safely: {ex.Message}");
        }
    }

    private static void StartLoopCore(PerformanceLoopCue cue, float fadeSeconds)
    {
        bool adult = PerformanceAssets.IsAdult(cue);
        if (adult && !PerformanceSettings.Current.AdultAudioEnabled
            || Loops.TryGetValue(cue, out AudioStreamPlayer? existing)
                && GodotObject.IsInstanceValid(existing)
                && existing.IsPlaying())
        {
            return;
        }

        if (Loops.Remove(cue, out AudioStreamPlayer? stale)) Release(stale);

        AudioStream? stream = Load(PerformanceAssets.AudioPath(cue), adult);
        Node? host = EnsureHost();
        if (stream == null || host == null) return;
        if (stream is AudioStreamOggVorbis ogg) ogg.Loop = true;

        float targetVolume = Volume(adult);
        var player = new AudioStreamPlayer
        {
            Name = $"MaidenPerformanceLoop-{cue}",
            Stream = stream,
            Bus = new StringName("SFX"),
            VolumeLinear = fadeSeconds > 0f ? 0f : targetVolume,
        };
        host.AddChild(player);
        Loops[cue] = player;
        player.Play();
        if (fadeSeconds > 0f)
        {
            player.CreateTween().TweenProperty(
                player,
                "volume_linear",
                targetVolume,
                fadeSeconds);
        }
    }

    public static void StopLoop(PerformanceLoopCue cue, float fadeSeconds = 0.2f)
    {
        try
        {
            StopLoopCore(cue, fadeSeconds);
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Stopping performance loop '{cue}' failed safely: {ex.Message}");
        }
    }

    private static void StopLoopCore(PerformanceLoopCue cue, float fadeSeconds)
    {
        if (!Loops.Remove(cue, out AudioStreamPlayer? player)
            || !GodotObject.IsInstanceValid(player))
        {
            return;
        }

        if (fadeSeconds <= 0f || !player.IsInsideTree())
        {
            Release(player);
            return;
        }

        Tween tween = player.CreateTween();
        Fading.Add(player);
        tween.TweenProperty(player, "volume_linear", 0f, fadeSeconds);
        tween.TweenCallback(Callable.From(() =>
        {
            Fading.Remove(player);
            Release(player);
        }));
    }

    public static void StopAll()
    {
        try
        {
            StopAllCore();
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn(
                $"Performance audio cleanup failed safely: {ex.Message}");
        }
    }

    private static void StopAllCore()
    {
        foreach (AudioStreamPlayer player in Loops.Values.ToArray()) Release(player);
        Loops.Clear();
        foreach (AudioStreamPlayer player in OneShots.ToArray()) Release(player);
        OneShots.Clear();
        foreach (AudioStreamPlayer player in Fading.ToArray()) Release(player);
        Fading.Clear();
        if (_host != null && GodotObject.IsInstanceValid(_host)) _host.QueueFree();
        _host = null;
    }

    internal static bool IsLoopPlaying(PerformanceLoopCue cue) =>
        Loops.TryGetValue(cue, out AudioStreamPlayer? player)
        && GodotObject.IsInstanceValid(player)
        && player.IsPlaying();

    private static Node? EnsureHost()
    {
        if (_host != null && GodotObject.IsInstanceValid(_host) && _host.IsInsideTree())
        {
            return _host;
        }
        if (NGame.Instance == null) return null;
        _host = new Node { Name = "MaidenPerformanceAudio" };
        NGame.Instance.AddChild(_host);
        return _host;
    }

    private static AudioStream? Load(string path, bool adult)
    {
        if (adult && !PerformanceSettings.Current.AdultAudioEnabled) return null;
        if (Cache.TryGetValue(path, out AudioStream? cached)
            && GodotObject.IsInstanceValid(cached))
        {
            return cached;
        }
        if (!Godot.FileAccess.FileExists(path))
        {
            if (MissingAssets.Add(path))
            {
                MaidenSuccubusMod.Logger.Warn($"Performance audio is missing: {path}");
            }
            return null;
        }
        byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
        AudioStreamOggVorbis? stream = bytes.Length == 0
            ? null
            : AudioStreamOggVorbis.LoadFromBuffer(bytes);
        if (stream == null)
        {
            if (MissingAssets.Add(path))
            {
                MaidenSuccubusMod.Logger.Warn($"Performance audio could not be decoded: {path}");
            }
            return null;
        }
        Cache[path] = stream;
        return stream;
    }

    private static float Volume(bool adult) => Math.Clamp(
        adult ? PerformanceSettings.Current.AdultAudioVolume
            : PerformanceSettings.Current.NormalAudioVolume,
        0f,
        1f);

    private static void ReleaseOneShot(AudioStreamPlayer player)
    {
        OneShots.Remove(player);
        Release(player);
    }

    private static void Release(AudioStreamPlayer player)
    {
        if (!GodotObject.IsInstanceValid(player)) return;
        if (player.IsPlaying()) player.Stop();
        player.QueueFree();
    }
}
