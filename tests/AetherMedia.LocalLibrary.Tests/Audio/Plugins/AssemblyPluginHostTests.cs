// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Plugins;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Plugins;

public class AssemblyPluginHostTests
{
    [Fact]
    public async Task LoadAsync_OnMissingDirectory_IsNoop()
    {
        var dir = Path.Combine(Path.GetTempPath(), "am-plugins-missing-" + Guid.NewGuid().ToString("N"));
        var host = new AssemblyPluginHost(dir);
        await host.LoadAsync();
        Assert.Empty(host.InputPlugins);
        Assert.Empty(host.OutputPlugins);
    }

    [Fact]
    public async Task LoadAsync_OnEmptyDirectory_FindsNothing()
    {
        var dir = Path.Combine(Path.GetTempPath(), "am-plugins-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var host = new AssemblyPluginHost(dir);
            await host.LoadAsync();
            Assert.Empty(host.InputPlugins);
        }
        finally
        {
            try { Directory.Delete(dir); } catch { }
        }
    }
}
