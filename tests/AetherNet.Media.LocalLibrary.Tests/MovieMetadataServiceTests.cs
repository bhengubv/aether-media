// SPDX-License-Identifier: MIT

using AetherNet.Media.LocalLibrary;
using AetherNet.Media.LocalLibrary.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherNet.Media.LocalLibrary.Tests;

public sealed class MovieMetadataServiceTests
{
    private readonly MovieMetadataService _service =
        new(NullLogger<MovieMetadataService>.Instance);

    // ── GetNfoPath ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/films/The Matrix (1999)/The Matrix (1999).mkv",
                "/films/The Matrix (1999)/The Matrix (1999).nfo")]
    [InlineData("C:\\Movies\\Inception.mp4", "C:\\Movies\\Inception.nfo")]
    public void GetNfoPath_ReturnsCorrectPath(string video, string expected)
    {
        Assert.Equal(expected, _service.GetNfoPath(video));
    }

    // ── ReadAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_ReturnsNull_WhenNoNfoFile()
    {
        var videoPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mkv");
        // Video file does NOT exist and neither does the NFO
        var result = await _service.ReadAsync(videoPath);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadAsync_ReturnsNull_ForMalformedXml()
    {
        var videoPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mkv");
        var nfoPath   = Path.ChangeExtension(videoPath, ".nfo");

        await File.WriteAllTextAsync(nfoPath, "<<<not xml>>>");

        try
        {
            var result = await _service.ReadAsync(videoPath);
            Assert.Null(result);
        }
        finally
        {
            File.Delete(nfoPath);
        }
    }

    // ── WriteAsync + ReadAsync round-trip ──────────────────────────────────

    [Fact]
    public async Task WriteAsync_ThenReadAsync_PreservesAllFields()
    {
        var videoPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mkv");
        var nfoPath   = Path.ChangeExtension(videoPath, ".nfo");

        var original = new MovieMetadata
        {
            FilePath       = videoPath,
            Title          = "The Matrix",
            Year           = 1999,
            Plot           = "A computer hacker learns the truth.",
            Tagline        = "Free your mind.",
            Rating         = 8.7f,
            RuntimeMinutes = 136,
            Genres         = ["Action", "Sci-Fi"],
            Directors      = ["Lana Wachowski", "Lilly Wachowski"],
            Cast           = ["Keanu Reeves", "Laurence Fishburne"],
            ImdbId         = "tt0133093",
            TmdbId         = "603",
            Watched        = true
        };

        try
        {
            await _service.WriteAsync(original);
            Assert.True(File.Exists(nfoPath));

            var read = await _service.ReadAsync(videoPath);

            Assert.NotNull(read);
            Assert.Equal("The Matrix",     read!.Title);
            Assert.Equal(1999,             read.Year);
            Assert.Equal(8.7f,             read.Rating, precision: 1);
            Assert.Equal(136,              read.RuntimeMinutes);
            Assert.Contains("Action",      read.Genres);
            Assert.Contains("Sci-Fi",      read.Genres);
            Assert.Contains("Lana Wachowski", read.Directors);
            Assert.Contains("Keanu Reeves",   read.Cast);
            Assert.Equal("tt0133093",      read.ImdbId);
            Assert.Equal("603",            read.TmdbId);
            Assert.True(read.Watched);
        }
        finally
        {
            if (File.Exists(nfoPath)) File.Delete(nfoPath);
        }
    }

    [Fact]
    public async Task WriteAsync_ProducesValidXml()
    {
        var videoPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mkv");
        var nfoPath   = Path.ChangeExtension(videoPath, ".nfo");

        await _service.WriteAsync(new MovieMetadata
        {
            FilePath = videoPath,
            Title    = "Interstellar",
            Year     = 2014
        });

        try
        {
            var content = await File.ReadAllTextAsync(nfoPath);
            Assert.Contains("<movie>",         content);
            Assert.Contains("<title>Interstellar</title>", content);
            Assert.Contains("<year>2014</year>",           content);
        }
        finally
        {
            if (File.Exists(nfoPath)) File.Delete(nfoPath);
        }
    }
}
