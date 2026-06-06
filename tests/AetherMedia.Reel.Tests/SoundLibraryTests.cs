// SPDX-License-Identifier: MIT

using AetherMedia.Reel.Tests.Helpers;

namespace AetherMedia.Reel.Tests;

public sealed class SoundLibraryTests : IDisposable
{
    private readonly string               _tempDir;
    private readonly NoOpContentService   _content;
    private readonly IsolatedSoundLibrary _library;

    public SoundLibraryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _content = new NoOpContentService();
        _library = new IsolatedSoundLibrary(_content, "UHID-TEST", _tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── PublishAudioFileAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task PublishAudioFileAsync_PublishesAndAnnouncesToContentLayer()
    {
        var file = CreateTempAudio();
        try
        {
            await _library.PublishAudioFileAsync(file, "My Sound");

            Assert.Single(_content.Published);
            Assert.Single(_content.Announced);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task PublishAudioFileAsync_SetsTitle()
    {
        var file = CreateTempAudio();
        try
        {
            var sound = await _library.PublishAudioFileAsync(file, "Cool Track", "Artist A");
            Assert.Equal("Cool Track", sound.Title);
            Assert.Equal("Artist A", sound.ArtistName);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task PublishAudioFileAsync_IsIdempotent_SameFilePublishedOnce()
    {
        var file = CreateTempAudio();
        try
        {
            await _library.PublishAudioFileAsync(file, "Track 1");
            await _library.PublishAudioFileAsync(file, "Track 1");   // second call

            // Content service should only have been called once
            Assert.Single(_content.Published);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task PublishAudioFileAsync_Throws_WhenFileNotFound()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _library.PublishAudioFileAsync("/nonexistent/track.mp3", "Missing"));
    }

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ReturnsSound_AfterPublish()
    {
        var file = CreateTempAudio();
        try
        {
            var published = await _library.PublishAudioFileAsync(file, "Found Sound");
            var retrieved = await _library.GetAsync(published.SoundHash);

            Assert.NotNull(retrieved);
            Assert.Equal(published.SoundHash, retrieved.SoundHash);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNotInIndex()
    {
        var result = await _library.GetAsync("nonexistent-hash");
        Assert.Null(result);
    }

    // ── SearchAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_FindsByTitle()
    {
        var f1 = CreateTempAudio();
        var f2 = CreateTempAudio();
        try
        {
            await _library.PublishAudioFileAsync(f1, "Summer Vibes");
            await _library.PublishAudioFileAsync(f2, "Winter Chill");

            var results = await _library.SearchAsync("summer");
            Assert.Single(results);
            Assert.Equal("Summer Vibes", results[0].Title);
        }
        finally { File.Delete(f1); File.Delete(f2); }
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        var file = CreateTempAudio();
        try
        {
            await _library.PublishAudioFileAsync(file, "LOUD BASS");
            var results = await _library.SearchAsync("loud bass");
            Assert.Single(results);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task SearchAsync_FindsByArtistName()
    {
        var file = CreateTempAudio();
        try
        {
            await _library.PublishAudioFileAsync(file, "Track", "DJ Mesh");
            var results = await _library.SearchAsync("DJ Mesh");
            Assert.Single(results);
        }
        finally { File.Delete(file); }
    }

    // ── GetTrendingAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTrendingAsync_ReturnsPublishedSounds()
    {
        var f1 = CreateTempAudio();
        var f2 = CreateTempAudio();
        try
        {
            await _library.PublishAudioFileAsync(f1, "Track A");
            await _library.PublishAudioFileAsync(f2, "Track B");

            var trending = await _library.GetTrendingAsync();
            Assert.Equal(2, trending.Count);
        }
        finally { File.Delete(f1); File.Delete(f2); }
    }

    [Fact]
    public async Task GetTrendingAsync_RespectsCountParameter()
    {
        for (var i = 0; i < 5; i++)
        {
            var f = CreateTempAudio();
            await _library.PublishAudioFileAsync(f, $"Track {i}");
            File.Delete(f);
        }

        var trending = await _library.GetTrendingAsync(count: 3);
        Assert.Equal(3, trending.Count);
    }

    // ── ExtractAndPublishAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ExtractAndPublishAsync_PublishesSound()
    {
        var file = CreateTempAudio(".mp4");
        try
        {
            var sound = await _library.ExtractAndPublishAsync(file, "Extracted Sound");
            Assert.NotNull(sound);
            Assert.Equal("Extracted Sound", sound.Title);
        }
        finally { File.Delete(file); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string CreateTempAudio(string extension = ".mp3")
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
        // Write distinct random bytes so each file produces a unique hash
        var bytes = new byte[512];
        Random.Shared.NextBytes(bytes);
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
