// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Ui;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Ui;

public class UiAbstractionTests
{
    [Fact]
    public void PlayerWindowController_FiresModeChanged_OnTransition()
    {
        var ctl = new PlayerWindowController();
        PlayerWindowMode? observed = null;
        ctl.ModeChanged += (_, m) => observed = m;

        ctl.Mode = PlayerWindowMode.Mini;
        Assert.Equal(PlayerWindowMode.Mini, observed);

        // Setting to the same value must not re-fire.
        observed = null;
        ctl.Mode = PlayerWindowMode.Mini;
        Assert.Null(observed);
    }

    [Fact]
    public async Task Osd_FiresDismissed_AfterDuration()
    {
        using var osd = new InMemoryOnScreenDisplay();
        var dismissed = new TaskCompletionSource();
        osd.MessageDismissed += (_, _) => dismissed.TrySetResult();

        osd.Show(new OsdMessage("Now Playing", "Artist - Title", TimeSpan.FromMilliseconds(80)));
        Assert.NotNull(osd.CurrentMessage);

        var completed = await Task.WhenAny(dismissed.Task, Task.Delay(2000));
        Assert.Same(dismissed.Task, completed);
        Assert.Null(osd.CurrentMessage);
    }

    [Fact]
    public async Task InAppToastNotifier_RaisesToastRequested_Event()
    {
        var notifier = new InAppToastNotifier();
        ToastNotification? observed = null;
        notifier.ToastRequested += (_, t) => observed = t;

        await notifier.ShowAsync(new ToastNotification("Title", "Body"));
        Assert.NotNull(observed);
        Assert.Equal("Title", observed!.Title);
    }

    [Fact]
    public void SystemTrayService_HandleMenuClick_RaisesEvent()
    {
        var svc = new InAppSystemTrayService();
        svc.SetMenu(new[] { new TrayMenuItem("play-pause", "Play / Pause") });

        TrayMenuItem? clicked = null;
        svc.MenuItemClicked += (_, item) => clicked = item;
        svc.HandleMenuClick("play-pause");

        Assert.NotNull(clicked);
        Assert.Equal("Play / Pause", clicked!.Label);
    }

    [Fact]
    public void DetachablePanelManager_FiresStateChanged_OnTransitionsOnly()
    {
        var mgr = new DetachablePanelManager();
        var events = new List<bool>();
        mgr.StateChanged += (_, e) => events.Add(e.IsDetached);

        mgr.Detach(DetachablePanel.Playlist);
        mgr.Detach(DetachablePanel.Playlist); // dup → no event
        mgr.Attach(DetachablePanel.Playlist);
        mgr.Attach(DetachablePanel.Playlist); // dup → no event

        Assert.Equal(new[] { true, false }, events);
    }
}
