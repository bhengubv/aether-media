// SPDX-License-Identifier: MIT

// Each test below guards with OperatingSystem.IsWindows() or asserts the
// non-Windows error path. The analyzer can't see either pattern.
#pragma warning disable CA1416

using AetherMedia.LocalLibrary.Audio.Hotkeys;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Hotkeys;

public class WindowsGlobalHotkeyServiceTests
{
    [Fact]
    public void OnWindows_StartsInactive_UntilRegisterCalled()
    {
        if (!OperatingSystem.IsWindows()) return; // honest platform guard
        using var sut = new WindowsGlobalHotkeyService();
        Assert.False(sut.IsActive);
    }

    [Fact]
    public void OnNonWindows_RegisterThrowsPlatformNotSupported()
    {
        if (OperatingSystem.IsWindows()) return;
        // Constructor is allowed (SupportedOSPlatform is advisory) — Register throws.
        var sut = new WindowsGlobalHotkeyService();
        Assert.Throws<PlatformNotSupportedException>(() =>
            sut.Register([new HotkeyBinding(HotkeyCommand.PlayPause, KeyCode: 0xB3)]));
        sut.Dispose();
    }

    [Fact]
    public void OnWindows_RegistersBindingsWithoutThrowing()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var sut = new WindowsGlobalHotkeyService();
        // Use VK_F22 + Win+Alt — extremely unlikely to collide.
        sut.Register([new HotkeyBinding(
            HotkeyCommand.PlayPause,
            KeyCode: 0x85,
            Modifiers: HotkeyModifiers.Win | HotkeyModifiers.Alt)]);
        // We can't assert IsActive without OS cooperation, but Register
        // must complete and Dispose must clean up the message-only window.
        sut.UnregisterAll();
        Assert.False(sut.IsActive);
    }
}
