// SPDX-License-Identifier: MIT

// CA1416 flags every direct reference to a [SupportedOSPlatform("windows")]
// type. Each test below either guards with OperatingSystem.IsWindows() or
// is itself testing the non-Windows error path — the analyzer can't see
// either pattern, so suppress for the file.
#pragma warning disable CA1416

using AetherMedia.LocalLibrary.Audio.Cd;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Cd;

public class WindowsCdRipperTests
{
    [Fact]
    public void EnumerateDrives_OnNonWindows_ReturnsEmpty()
    {
        if (OperatingSystem.IsWindows()) return;
        var found = new WindowsCdRipper().EnumerateDrives();
        Assert.Empty(found);
    }

    [Fact]
    public void EnumerateDrives_OnWindows_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows()) return;
        // Result may be empty if no CD drive is present — the call must
        // simply not throw and must return a non-null list.
        var found = new WindowsCdRipper().EnumerateDrives();
        Assert.NotNull(found);
    }

    [Fact]
    public async Task ReadToc_OnNonWindows_ThrowsPlatformNotSupported()
    {
        if (OperatingSystem.IsWindows()) return;
        await Assert.ThrowsAsync<PlatformNotSupportedException>(
            () => new WindowsCdRipper().ReadTocAsync("D:"));
    }
}
