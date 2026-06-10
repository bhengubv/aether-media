// SPDX-License-Identifier: MIT

using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using AetherMedia.LocalLibrary.Audio.Bookmarks;
using AetherMedia.LocalLibrary.Audio.Library;
using AetherMedia.LocalLibrary.Audio.Lyrics;
using AetherMedia.LocalLibrary.Audio.Output;
using AetherNet.Content.Diagnostics;
using AetherNet.Forge;
using AetherNet.Security;
using AetherMedia.LocalLibrary.Audio.Podcast;
using AetherMedia.LocalLibrary.Audio.Radio;
using AetherMedia.LocalLibrary.Audio.Scrobble;
using AetherNet.Models;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Mesh;

/// <summary>
/// Tests that exercise every wave-16 mesh integration against the
/// <see cref="MeshInvariants"/> predicates derived from the formal Petri
/// net models.
/// </summary>
public class MeshIntegrationTests
{
    private static byte[] NewKey() => RandomNumberGenerator.GetBytes(AesGcmEnvelope.KeySize);

    // ── Item #1 — Scrobble → DTN ──────────────────────────────────────────

    [Fact]
    public async Task Scrobble_FallsBackToDtn_OnHttpFailure_AndCustodyTerminates()
    {
        var failingInner = new ThrowingScrobbler();
        var dtn = new InMemoryDtnService();
        var sut = new DtnAwareScrobbler(failingInner, dtn, "user-self", NewKey());

        await sut.ScrobbleAsync(new ScrobbleEvent("Artist", "Title", "Album",
            DateTimeOffset.UtcNow, TimeSpan.FromMinutes(3)));

        var active = await dtn.GetActiveBundlesAsync();
        Assert.Single(active);
        Assert.Equal("user-self", active[0].RecipientUhid);

        // Petri invariant: dtn-custody.
        var converged = await MeshInvariants.DtnCustodyEventuallyTerminates(
            dtn, driveDelivery: () => dtn.DeliverAllAsync());
        Assert.True(converged);
    }

    [Fact]
    public void Scrobble_BundleRoundTrips_Through_AesGcmEnvelope()
    {
        var inner = new ThrowingScrobbler();
        var dtn = new InMemoryDtnService();
        var key = NewKey();
        var sut = new DtnAwareScrobbler(inner, dtn, "user-self", key);

        var ev = new ScrobbleEvent("Artist", "Title", "Album",
            DateTimeOffset.FromUnixTimeMilliseconds(1700000000000),
            TimeSpan.FromSeconds(240));

        // Encrypt → decrypt via the same key produces the original event.
        var encrypted = AesGcmEnvelope.Encrypt(key, ScrobblePayload.FromEvent(ev).ToBytes());
        var decoded = sut.DecodeIncomingBundle(encrypted);
        Assert.Equal(ev.Artist, decoded.Artist);
        Assert.Equal(ev.Title, decoded.Title);
        Assert.Equal(ev.Album, decoded.Album);
    }

    // ── Item #2a — Bookmarks → DTN multi-device sync ──────────────────────

    [Fact]
    public async Task Bookmark_AddProducesBundle_AndOtherDeviceConverges()
    {
        var key = NewKey();
        var dtn = new InMemoryDtnService();
        var deviceA = new DtnAwareBookmarkStore(new InMemoryBookmarkStore(), dtn, "user-self", key);
        var innerB = new InMemoryBookmarkStore();
        var deviceB = new DtnAwareBookmarkStore(innerB, dtn, "user-self", key);

        var bookmark = new Bookmark("a.mp3", 60_000, Label: "intro", CreatedAtUtc: DateTimeOffset.UtcNow);
        await deviceA.AddAsync(bookmark);

        // Pull the bundle the bookmark produced, deliver it to device B.
        var bundles = await dtn.GetActiveBundlesAsync();
        Assert.Single(bundles);
        await deviceB.ApplyIncomingBundleAsync(bundles[0].EncryptedPayload);

        var listA = (await deviceA.ListAsync()).Select(b => (b.FilePath, b.PositionMs));
        var listB = (await innerB.ListAsync()).Select(b => (b.FilePath, b.PositionMs));
        Assert.True(MeshInvariants.MultiDeviceSyncConverges(listA, listB));
    }

    // ── Item #2b — Play history → DTN multi-device sync ───────────────────

    [Fact]
    public async Task PlayHistory_RecordProducesBundle_AndOtherDeviceConverges()
    {
        var key = NewKey();
        var dtn = new InMemoryDtnService();
        var deviceA = new DtnAwarePlayHistoryStore(new InMemoryPlayHistoryStore(), dtn, "user-self", key);
        var innerB = new InMemoryPlayHistoryStore();
        var deviceB = new DtnAwarePlayHistoryStore(innerB, dtn, "user-self", key);

        await deviceA.RecordAsync(new PlayEvent("a.mp3", DateTimeOffset.UtcNow, 240_000));
        var bundles = await dtn.GetActiveBundlesAsync();
        Assert.Single(bundles);
        await deviceB.ApplyIncomingBundleAsync(bundles[0].EncryptedPayload);

        var statsB = await innerB.GetAsync("a.mp3");
        Assert.Equal(1, statsB.PlayCount);
    }

    // ── Item #3 — Podcast → IContentService mesh-first ────────────────────

    [Fact]
    public async Task Podcast_HitsMeshCache_WithoutHttp()
    {
        var content = new InMemoryContentService();
        var episode = new PodcastEpisode(
            Guid: "ep-1",
            Title: "Episode 1",
            Description: null,
            PublishedAtUtc: DateTimeOffset.UtcNow,
            AudioUrl: new Uri("https://demo.example/ep1.mp3"),
            LengthBytes: 5,
            MimeType: "audio/mpeg",
            Duration: TimeSpan.FromMinutes(5));

        // Seed the content service as if another peer had published the episode.
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        content.SeedRemote(MeshFirstPodcastDownloader.ContentKeyFor(episode), bytes, "audio/mpeg");

        var dir = Path.Combine(Path.GetTempPath(), "wave16-podcast-" + Guid.NewGuid().ToString("N"));
        try
        {
            var httpThatWouldFailIfCalled = new HttpClient(new AlwaysFailHandler());
            using var inner = new PodcastDownloader(httpThatWouldFailIfCalled);
            using var sut = new MeshFirstPodcastDownloader(inner, content);

            var path = await sut.DownloadAsync(episode, dir);
            Assert.True(File.Exists(path));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    // ── Item #4 — Cover art / lyrics mesh-first ───────────────────────────

    [Fact]
    public async Task CoverArt_HitsMeshCache_WithoutHttp()
    {
        var content = new InMemoryContentService();
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG SOI
        content.SeedRemote(MeshFirstCoverArtFetcher.ContentKeyFor("Artist", "Album", null), bytes);

        using var sut = new MeshFirstCoverArtFetcher(new AlwaysFailCoverArt(), content);
        var result = await sut.FetchAsync("Artist", "Album");
        Assert.Equal(bytes, result);
    }

    [Fact]
    public async Task Lyrics_HitsMeshCache_WithoutHttp()
    {
        var content = new InMemoryContentService();
        var lrc = "[00:00.50]Hello\n[00:02.00]World";
        content.SeedRemote(MeshFirstLyricFetcher.ContentKeyFor("Artist", "Track", null),
            Encoding.UTF8.GetBytes(lrc), "text/plain; charset=utf-8");

        using var sut = new MeshFirstLyricFetcher(new AlwaysFailLyrics(), content);
        var result = await sut.FetchAsync("Artist", "Track");
        Assert.NotNull(result);
        Assert.Equal(2, result!.Lines.Count);
    }

    // ── Item #5 — Mesh package distributor ────────────────────────────────

    [Fact]
    public async Task PackageDistributor_PublishThenFetch_RoundTrips_AndIntegrityHolds()
    {
        var content = new InMemoryContentService();
        var forge = new InMemoryForgeService();
        var inc = new RecordingIncentiveProvider();
        var sut = new MeshPackageDistributor(forge, content, inc, "node-self");

        var payload = "milkdrop preset body"u8.ToArray();
        var packageId = MeshPackageDistributor.PresetPackageId("milkdrop", "demo");
        var entry = await sut.PublishAsync(packageId, payload, "text/plain");
        Assert.Equal(packageId, entry.PackageId);
        Assert.True(MeshInvariants.ForgeIntegrity(payload, MeshPackageDistributor.IntegrityHash(payload)));

        var fetched = await sut.TryFetchAsync(packageId);
        Assert.NotNull(fetched);
        Assert.Equal(payload, fetched);
    }

    // ── Item #6 — Mesh audio output ───────────────────────────────────────

    [Fact]
    public void MeshAudioOutput_PublishesMonotonicSegments()
    {
        var streaming = new InMemoryStreamingService();
        using var sut = new MeshAudioOutput(streaming, "test-device", segmentMs: 5);
        sut.Open(new AudioFormat(SampleRateHz: 8000, Channels: 1), sampleProvider: buf =>
        {
            buf.Span.Fill(0.5f);
            return buf.Length;
        });
        sut.Play();
        var sessionId = sut.Session!.Id; // capture before Stop() nulls it

        // Poll until at least one segment has been published OR a 2 s ceiling
        // is hit. Replaces a fragile Thread.Sleep(60) that under net9's slower
        // scheduler intermittently captured zero segments before Stop().
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline &&
               streaming.GetPublishedSegments(sessionId).Count == 0)
        {
            Thread.Sleep(20);
        }
        sut.Stop();

        var segments = streaming.GetPublishedSegments(sessionId);
        Assert.NotEmpty(segments);
        Assert.True(MeshInvariants.StreamSequenceMonotonic(segments.Select(s => s.Sequence)));
        Assert.True(segments[0].IsKeyframe);
    }

    // ── Item #7 — Smoke tests for each invariant ──────────────────────────

    [Fact]
    public async Task Invariant_DtnCustody_FailsWhenBundleStuck()
    {
        var dtn = new InMemoryDtnService();
        await dtn.CreateBundleAsync("user-self", new byte[] { 1, 2 });
        // No driveDelivery does anything — bundle stays pending.
        var converged = await MeshInvariants.DtnCustodyEventuallyTerminates(
            dtn, driveDelivery: () => Task.CompletedTask, maxScans: 3);
        Assert.False(converged);
    }

    [Fact]
    public void Invariant_StreamSequence_DetectsRegression()
    {
        Assert.True(MeshInvariants.StreamSequenceMonotonic(new uint[] { 0, 1, 2, 3 }));
        Assert.False(MeshInvariants.StreamSequenceMonotonic(new uint[] { 0, 2, 1 }));
        Assert.False(MeshInvariants.StreamSequenceMonotonic(new uint[] { 5, 5 }));
    }

    [Fact]
    public void Invariant_MultiDeviceSync_SetEquality_NotListEquality()
    {
        Assert.True(MeshInvariants.MultiDeviceSyncConverges(new[] { 1, 2, 3 }, new[] { 3, 2, 1 }));
        Assert.False(MeshInvariants.MultiDeviceSyncConverges(new[] { 1, 2 }, new[] { 1, 2, 3 }));
    }

    [Fact]
    public void Invariant_ForgeIntegrity_DetectsTamperedBytes()
    {
        var payload = "original"u8.ToArray();
        var hash = MeshPackageDistributor.IntegrityHash(payload);
        Assert.True(MeshInvariants.ForgeIntegrity(payload, hash));
        Assert.False(MeshInvariants.ForgeIntegrity("tampered"u8.ToArray(), hash));
    }

    // ── Stub helpers ──────────────────────────────────────────────────────

    private sealed class ThrowingScrobbler : IScrobbler
    {
        public bool IsAuthenticated => true;
        public Task UpdateNowPlayingAsync(ScrobbleEvent ev, CancellationToken ct = default)
            => throw new HttpRequestException("offline");
        public Task ScrobbleAsync(ScrobbleEvent ev, CancellationToken ct = default)
            => throw new HttpRequestException("offline");
        public Task FlushAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class AlwaysFailHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("network down");
    }

    private sealed class AlwaysFailCoverArt : ICoverArtFetcher
    {
        public Task<byte[]?> FetchAsync(string artist, string album, string? track = null, CancellationToken ct = default)
            => throw new InvalidOperationException("HTTP should not have been called.");
    }

    private sealed class AlwaysFailLyrics : ILyricFetcher
    {
        public Task<LrcFile?> FetchAsync(string artist, string trackTitle, string? album = null, CancellationToken ct = default)
            => throw new InvalidOperationException("HTTP should not have been called.");
    }
}
