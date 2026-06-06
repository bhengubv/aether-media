// SPDX-License-Identifier: MIT

// Each test below guards with OperatingSystem.IsWindows() or asserts the
// non-Windows error path. The analyzer can't see either pattern.
#pragma warning disable CA1416

using AetherMedia.LocalLibrary.Audio.Cd;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Cd;

public class WindowsCdBurnerTests
{
    [Fact]
    public void EnumerateRecorders_OnNonWindows_ReturnsEmpty()
    {
        if (OperatingSystem.IsWindows()) return;
        var found = new WindowsCdBurner().EnumerateRecorders();
        Assert.Empty(found);
    }

    [Fact]
    public void EnumerateRecorders_OnWindows_DoesNotThrow_WhenNoBurnerPresent()
    {
        if (!OperatingSystem.IsWindows()) return;
        // Result may be empty if no recorder is present; the call must not throw.
        var found = new WindowsCdBurner().EnumerateRecorders();
        Assert.NotNull(found);
    }

    [Fact]
    public async Task BurnAsync_OnNonWindows_ThrowsPlatformNotSupported()
    {
        if (OperatingSystem.IsWindows()) return;
        var sut = new WindowsCdBurner();
        var request = new CdBurnRequest("does-not-matter", new Func<Stream>[] { () => new MemoryStream(new byte[8]) });
        await Assert.ThrowsAsync<PlatformNotSupportedException>(() => sut.BurnAsync(request));
    }

    [Fact]
    public async Task BurnAsync_RejectsEmptyTrackList()
    {
        var sut = new WindowsCdBurner();
        var request = new CdBurnRequest("recorder", Array.Empty<Func<Stream>>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.BurnAsync(request));
    }

    [Fact]
    public async Task BurnAsync_RejectsEmptyRecorderId()
    {
        var sut = new WindowsCdBurner();
        var request = new CdBurnRequest("", new Func<Stream>[] { () => new MemoryStream(new byte[8]) });
        await Assert.ThrowsAsync<ArgumentException>(() => sut.BurnAsync(request));
    }
}
