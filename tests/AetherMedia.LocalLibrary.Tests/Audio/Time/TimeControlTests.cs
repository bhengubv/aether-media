// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Time;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Time;

public class TimeControlTests
{
    [Fact]
    public async Task SleepTimer_FiresElapsed_AfterDelay()
    {
        using var t = new SleepTimer();
        var fired = new TaskCompletionSource();
        t.Elapsed += (_, _) => fired.TrySetResult();
        t.Arm(TimeSpan.FromMilliseconds(100));

        var completed = await Task.WhenAny(fired.Task, Task.Delay(2000));
        Assert.Same(fired.Task, completed);
        Assert.False(t.IsArmed);
    }

    [Fact]
    public void SleepTimer_CancelStopsFiring()
    {
        using var t = new SleepTimer();
        var fired = false;
        t.Elapsed += (_, _) => fired = true;
        t.Arm(TimeSpan.FromMilliseconds(200));
        t.Cancel();
        Thread.Sleep(300);
        Assert.False(fired);
    }

    [Fact]
    public void Alarm_ThrowsForPastTime()
    {
        using var a = new PlaybackAlarm();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => a.ArmAt(DateTimeOffset.UtcNow.AddSeconds(-1)));
    }

    [Fact]
    public async Task Alarm_FiresAtTrigger()
    {
        using var a = new PlaybackAlarm();
        var fired = new TaskCompletionSource();
        a.Triggered += (_, _) => fired.TrySetResult();
        a.ArmAt(DateTimeOffset.UtcNow.AddMilliseconds(100));

        var completed = await Task.WhenAny(fired.Task, Task.Delay(2000));
        Assert.Same(fired.Task, completed);
        Assert.Null(a.NextTrigger);
    }
}
