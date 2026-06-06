// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Radio;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Radio;

public class StreamRecorderTests : IDisposable
{
    private readonly string _dir;
    public StreamRecorderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "am-rec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public async Task WritesBytesToFile_AndRotatesOnTrackChange()
    {
        using var rec = new StreamRecorder(_dir) { SplitOnTrackChange = true };
        rec.Start(initialTitle: "First Track");
        await rec.WriteAsync(new byte[] { 1, 2, 3 });
        rec.OnTrackChanged("Second Track");
        await rec.WriteAsync(new byte[] { 4, 5 });
        rec.Stop();

        var files = Directory.GetFiles(_dir);
        Assert.Equal(2, files.Length);
        Assert.Equal(5, rec.BytesWritten);
    }
}
