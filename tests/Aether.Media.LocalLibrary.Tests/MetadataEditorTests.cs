// SPDX-License-Identifier: MIT

using Aether.Media.LocalLibrary;
using Aether.Media.LocalLibrary.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aether.Media.LocalLibrary.Tests;

public sealed class MetadataEditorTests
{
    private readonly MetadataEditor _editor =
        new(NullLogger<MetadataEditor>.Instance);

    // ── CanEdit ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("song.mp3",  true)]
    [InlineData("song.flac", true)]
    [InlineData("song.m4a",  true)]
    [InlineData("song.ogg",  true)]
    [InlineData("song.opus", true)]
    [InlineData("movie.mkv", false)]
    [InlineData("movie.mp4", true)]   // MP4 is also valid for AAC audio
    [InlineData("doc.pdf",   false)]
    public void CanEdit_ReturnsCorrectResult(string fileName, bool expected)
    {
        Assert.Equal(expected, _editor.CanEdit(fileName));
    }

    // ── ReadAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        var result = await _editor.ReadAsync("/nonexistent/file.mp3");
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadAsync_ReturnsNull_ForUnsupportedExtension()
    {
        // Create a temp file with an unsupported extension
        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.xyz");
        await File.WriteAllBytesAsync(path, [0x00, 0x01, 0x02]);

        try
        {
            var result = await _editor.ReadAsync(path);
            Assert.Null(result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_RoundTrip_WriteThenRead()
    {
        // Create a minimal valid MP3 using TagLibSharp (ID3 header only)
        var path = CreateTempMp3();

        try
        {
            var original = new TrackMetadata
            {
                FilePath = path,
                Title    = "Test Title",
                Artist   = "Test Artist",
                Album    = "Test Album",
                Track    = 3,
                Year     = 2024,
                Comment  = "Unit test",
                Genres   = ["Electronic", "Ambient"]
            };

            await _editor.WriteAsync(original);

            var read = await _editor.ReadAsync(path);

            Assert.NotNull(read);
            Assert.Equal("Test Title",    read!.Title);
            Assert.Equal("Test Artist",   read.Artist);
            Assert.Equal("Test Album",    read.Album);
            Assert.Equal(3u,              read.Track);
            Assert.Equal(2024u,           read.Year);
            Assert.Equal("Unit test",     read.Comment);
            Assert.Contains("Electronic", read.Genres);
            Assert.Contains("Ambient",    read.Genres);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_UpdatesPartialFields()
    {
        var path = CreateTempMp3();

        try
        {
            // Write first pass
            await _editor.WriteAsync(new TrackMetadata
            {
                FilePath = path,
                Title    = "Original",
                Artist   = "Artist A",
                Year     = 2020
            });

            // Overwrite only Title
            await _editor.WriteAsync(new TrackMetadata
            {
                FilePath = path,
                Title    = "Updated",
                Artist   = "Artist A",   // keep same
                Year     = 2020
            });

            var result = await _editor.ReadAsync(path);
            Assert.NotNull(result);
            Assert.Equal("Updated",  result!.Title);
            Assert.Equal("Artist A", result.Artist);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── Helper ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a minimal MPEG1 Layer3 file (4 KB, one valid sync word at offset 0)
    /// that TagLibSharp can open for tag read/write without marking as corrupt.
    /// </summary>
    private static string CreateTempMp3()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.mp3");

        // Minimal MPEG1 Layer3 frame header: 0xFF 0xFB = sync + MPEG1 + L3
        // 0x90 = 128 kbps, 44.1 kHz | 0x00 = joint stereo, no padding
        // Followed by 4 KB of zeros so TagLib sees a "complete" buffer.
        var data = new byte[4096];
        data[0] = 0xFF;
        data[1] = 0xFB;
        data[2] = 0x90;
        data[3] = 0x00;
        File.WriteAllBytes(path, data);

        return path;
    }
}
