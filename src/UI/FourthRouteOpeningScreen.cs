using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Acts;

namespace MaidenSuccubus.UI;

/// <summary>Opaque, non-cancellable choice → narrative flow. Scene exit cancels the UI, not the saved choice.</summary>
public sealed partial class FourthRouteOpeningScreen : Control, IScreenContext
{
    private readonly Player _player;
    private readonly RunState _run;
    private readonly Func<bool> _isCurrent;
    private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TrialEventPage _page = null!;
    private bool _busy;
    private bool _closed;
    public Control? DefaultFocusedControl => _page?.DefaultFocusedControl;

    internal static string TextFor(string key) => new LocString("events", "MAIDEN_SUCCUBUS_ROUTE_OPENING." + key).GetFormattedText();

    private FourthRouteOpeningScreen(Player player, RunState run, Func<bool> isCurrent)
    {
        _player = player; _run = run; _isCurrent = isCurrent;
        Name = "FourthRouteOpening";
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    internal static async Task<bool> Show(Player player, RunState run, Func<bool> isCurrent)
    {
        if (!isCurrent()) return false;
        NModalContainer container = NModalContainer.Instance ?? throw new InvalidOperationException("Opening requires modal container.");
        if (container.OpenModal != null) throw new InvalidOperationException("Opening cannot replace another modal.");
        var screen = new FourthRouteOpeningScreen(player, run, isCurrent);
        container.Add(screen);
        return await screen._completion.Task;
    }

    public override void _Ready()
    {
        try { BuildPage(); }
        catch (Exception ex)
        {
            _completion.TrySetException(ex);
            Close(false);
        }
    }

    private void BuildPage()
    {
        NHotkeyManager.Instance?.AddBlockingScreen(this);
        _page = TrialEventPage.Create();
        AddChild(_page);
        var opening = FourthRouteOpeningService.Prepare(_run);
        if (opening.Chosen is { } chosen)
            TaskHelper.RunSafely(Confirm(chosen)); // Resume an interrupted, already locked narrative.
        else ShowChoices(opening);
    }

    private void ClearPage() => _page.ClearOptions();

    private void ShowChoices(FourthRouteOpeningState opening)
    {
        ClearPage();
        _page.SetStory(TextFor("title"), TextFor("common"));
        _page.AddQuest(opening.Dark!.Value, () => TaskHelper.RunSafely(Confirm(opening.Dark.Value)));
        _page.AddQuest(opening.Light!.Value, () => TaskHelper.RunSafely(Confirm(opening.Light.Value)));
        _page.LinkFocus();
        FocusFirst();
    }

    private async Task Confirm(FourthRouteQuest quest)
    {
        if (_busy || _closed || !_isCurrent()) return;
        _busy = true;
        _page.DisableOptions();
        try
        {
            if (!await FourthRouteOpeningService.Confirm(_player, quest))
                throw new InvalidOperationException("Opening choice no longer matches the saved offer.");
            if (_closed || !_isCurrent()) { Close(false); return; }
            ClearPage();
            _page.SetStory(FourthRouteProgressService.QuestName(quest), TextFor(quest + ".story"));
            _page.AddOption("EnterSpire", $"[gold][b]{TextFor("continue")}[/b][/gold]", () =>
            {
                if (!_busy && !_closed && _isCurrent() && FourthRouteOpeningService.Finish(_run)) Close(true);
            });
            _page.LinkFocus();
            FocusFirst();
        }
        catch (Exception ex)
        {
            MaidenSuccubusMod.Logger.Error("[FourthRouteOpening] " + ex);
            _completion.TrySetException(ex);
            Close(false);
        }
        finally { _busy = false; }
    }

    private void FocusFirst() => Callable.From(() =>
    {
        if (!_closed && IsInsideTree()) DefaultFocusedControl?.GrabFocus();
    }).CallDeferred();

    private void Close(bool result)
    {
        if (_closed) return;
        _closed = true;
        _completion.TrySetResult(result);
        var container = NModalContainer.Instance;
        if (container != null && ReferenceEquals(container.OpenModal, this)) container.Clear();
        else QueueFree();
    }

    public override void _Process(double delta)
    {
        if (!_isCurrent()) Close(false);
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input.IsActionPressed("ui_cancel")) GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        _closed = true;
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
        _completion.TrySetResult(false);
    }
}
