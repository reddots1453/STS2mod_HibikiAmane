using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MaidenSuccubus.UI;

namespace MaidenSuccubus.Presentation;

public static class CutscenePlaybackService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static CancellationTokenSource _lifecycle = new();
    private static CancellationTokenSource? _active;
    private static CanvasLayer? _layer;

    public static async Task PlayAsync(
        Player player,
        IReadOnlyList<string> paths,
        Action<int>? frameStarted = null)
    {
        if (!PerformanceSettings.Current.AdultCgEnabled
            || !PerformanceAudience.IsLocalMaiden(player)
            || paths.Count == 0)
        {
            return;
        }

        CancellationToken lifecycleToken = _lifecycle.Token;
        using var local = CancellationTokenSource.CreateLinkedTokenSource(lifecycleToken);
        CanvasLayer? localLayer = null;
        bool gateAcquired = false;
        try
        {
            const string imageRoot = "res://MaidenSuccubus/images/";
            foreach (string path in paths)
            {
                if (!path.StartsWith(imageRoot, StringComparison.Ordinal))
                {
                    MaidenSuccubusMod.Logger.Warn(
                        $"Cutscene path is outside the runtime image root: {path}");
                    return;
                }
            }

            await Gate.WaitAsync(local.Token);
            gateAcquired = true;
            _active = local;
            NGame? game = NGame.Instance;
            if (game == null) return;

            localLayer = new CanvasLayer { Name = "MaidenCutscene", Layer = 120 };
            _layer = localLayer;
            var blocker = new CutsceneInputBlocker();
            blocker.SkipRequested += local.Cancel;
            localLayer.AddChild(blocker);
            game.AddChild(localLayer);
            blocker.TakeFocus();

            for (int index = 0; index < paths.Count && !local.IsCancellationRequested; index++)
            {
                // Yield between frames and acquire the playback gate before any decoding.
                // A queued or skipped sequence must not retain a whole image set.
                await blocker.ToSignal(blocker.GetTree(), SceneTree.SignalName.ProcessFrame);
                local.Token.ThrowIfCancellationRequested();
                Texture2D? texture = RuntimeTextureAssets.Load(paths[index][imageRoot.Length..]);
                if (texture == null) return;
                frameStarted?.Invoke(index);
                blocker.Image.Texture = texture;
                await Fade(blocker.Image, 0f, 1f, 0.25, local.Token);
                await Wait(blocker, 0.85, local.Token);
                await Fade(blocker.Image, 1f, 0f, 0.25, local.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // User skip and lifecycle cancellation intentionally end only the presentation.
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Warn($"Cutscene playback failed safely: {ex.Message}");
        }
        finally
        {
            if (localLayer != null && GodotObject.IsInstanceValid(localLayer))
            {
                localLayer.QueueFree();
            }
            if (ReferenceEquals(_layer, localLayer)) _layer = null;
            if (ReferenceEquals(_active, local)) _active = null;
            if (gateAcquired)
            {
                RuntimeTextureAssets.ReleasePrefix("cutscenes/", "cutscene-ended");
                Gate.Release();
            }
        }
    }

    public static void CancelActive()
    {
        CancellationTokenSource previous = _lifecycle;
        _lifecycle = new CancellationTokenSource();
        previous.Cancel();
        previous.Dispose();
        _active?.Cancel();
        if (_layer != null && GodotObject.IsInstanceValid(_layer)) _layer.QueueFree();
        _layer = null;
    }

    private static async Task Fade(
        CanvasItem item,
        float from,
        float to,
        double seconds,
        CancellationToken token)
    {
        ulong started = Time.GetTicksMsec();
        double totalMs = seconds * 1000d;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            double progress = Math.Clamp((Time.GetTicksMsec() - started) / totalMs, 0d, 1d);
            Color color = item.Modulate;
            color.A = Mathf.Lerp(from, to, (float)progress);
            item.Modulate = color;
            if (progress >= 1d) return;
            await item.ToSignal(item.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task Wait(Node node, double seconds, CancellationToken token)
    {
        ulong deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000d);
        while (Time.GetTicksMsec() < deadline)
        {
            token.ThrowIfCancellationRequested();
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
}

internal sealed partial class CutsceneInputBlocker : Control
{
    public event Action? SkipRequested;
    public TextureRect Image { get; }

    public CutsceneInputBlocker()
    {
        Name = "InputBlocker";
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ProcessMode = ProcessModeEnum.Always;
        SetProcessInput(true);

        var backdrop = new ColorRect
        {
            Color = Colors.Black,
            MouseFilter = MouseFilterEnum.Stop,
        };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        Image = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 1f, 1f, 0f),
        };
        Image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(Image);
    }

    public void TakeFocus() => GetViewport()?.GuiReleaseFocus();

    public override void _Input(InputEvent @event)
    {
        GetViewport()?.SetInputAsHandled();
        bool mouse = @event is InputEventMouseButton { Pressed: true };
        if (mouse
            || @event.IsActionPressed("ui_accept")
            || @event.IsActionPressed("ui_cancel"))
        {
            SkipRequested?.Invoke();
        }
    }
}
