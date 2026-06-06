// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Sync;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Sync;

public class FilesystemSyncTests : IDisposable
{
    private readonly string _sourceDir;
    private readonly string _deviceDir;

    public FilesystemSyncTests()
    {
        _sourceDir = Path.Combine(Path.GetTempPath(), "am-sync-src-" + Guid.NewGuid().ToString("N"));
        _deviceDir = Path.Combine(Path.GetTempPath(), "am-sync-dev-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_deviceDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_sourceDir, recursive: true); } catch { }
        try { Directory.Delete(_deviceDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task Plan_ComputesCopyAndDeleteSets()
    {
        var src1 = Path.Combine(_sourceDir, "a.mp3");
        var src2 = Path.Combine(_sourceDir, "b.mp3");
        await File.WriteAllBytesAsync(src1, new byte[100]);
        await File.WriteAllBytesAsync(src2, new byte[200]);

        // Pre-populate device with a file that should be removed.
        var musicDir = Path.Combine(_deviceDir, "Music");
        Directory.CreateDirectory(musicDir);
        var stale = Path.Combine(musicDir, "old.mp3");
        await File.WriteAllBytesAsync(stale, new byte[10]);

        var device = new PortableDevice("dev", "USB", _deviceDir, 1_000_000, 2_000_000);
        var sut = new FilesystemSync();
        var plan = await sut.PlanAsync(device, [src1, src2]);

        Assert.Equal(2, plan.ToCopy.Count);
        Assert.Single(plan.ToDelete);
        Assert.Contains(plan.ToDelete, p => p.EndsWith("old.mp3", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(300, plan.TotalBytesToCopy);
    }

    [Fact]
    public async Task Execute_CopiesFiles_ThenSecondPassIsNoop()
    {
        var src1 = Path.Combine(_sourceDir, "a.mp3");
        await File.WriteAllBytesAsync(src1, new byte[] { 1, 2, 3, 4 });

        var device = new PortableDevice("dev", "USB", _deviceDir, 1_000_000, 2_000_000);
        var sut = new FilesystemSync();

        await sut.ExecuteAsync(device, [src1]);
        var dest = Path.Combine(_deviceDir, "Music", "a.mp3");
        Assert.True(File.Exists(dest));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, await File.ReadAllBytesAsync(dest));

        var planAfter = await sut.PlanAsync(device, [src1]);
        Assert.Empty(planAfter.ToCopy);
        Assert.Empty(planAfter.ToDelete);
    }

    [Fact]
    public async Task ConfiguredDevices_Override_DiscoverDevices()
    {
        var device = new PortableDevice("custom", "Custom", _deviceDir, 0, 0);
        var sut = new FilesystemSync { ConfiguredDevices = [device] };
        var found = await sut.DiscoverDevicesAsync();
        Assert.Single(found);
        Assert.Equal("custom", found[0].Id);
    }
}
