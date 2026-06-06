// SPDX-License-Identifier: MIT

using System.Net;
using System.Security.Cryptography;
using AetherNet.Content;
using AetherNet.Content.Models;
using AetherMedia.Distribution;
using AetherMedia.Distribution.Models;
using AetherNet.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.Distribution.Tests;

public sealed class MeshAppDistributorTests : IAsyncDisposable
{
    // ── Minimal IContentService stub ──────────────────────────────────────────

    private sealed class NoOpContentService : IContentService
    {
        public event EventHandler<ContentDescriptor>? ContentAnnounced;

        public List<ContentDescriptor> Published { get; } = [];
        public List<ContentDescriptor> Announced { get; } = [];

#pragma warning disable CS0067  // Events not raised in this test stub — intentional
        public event EventHandler<ChunkArrivedEventArgs>? ChunkReceived;
        public event EventHandler<ContentDescriptor>?     ContentComplete;
#pragma warning restore CS0067

        public Task<ContentDescriptor> PublishAsync(
            string name, byte[] data, string contentType = "application/octet-stream",
            int chunkSizeBytes = 0, CancellationToken cancellationToken = default)
        {
            var descriptor = ContentDescriptor.FromBytes(name, data, contentType);
            Published.Add(descriptor);
            return Task.FromResult(descriptor);
        }

        public Task AnnounceAsync(ContentDescriptor descriptor,
            CancellationToken cancellationToken = default)
        {
            Announced.Add(descriptor);
            return Task.CompletedTask;
        }

        public Task RequestChunksAsync(string rootHash, IReadOnlyList<int> chunkIndices,
            string? peerUhid = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<byte[]?> AssembleAsync(string rootHash, CancellationToken cancellationToken = default)
            => Task.FromResult<byte[]?>(null);

        public Task BroadcastBitmapAsync(string rootHash, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void RaiseContentAnnounced(ContentDescriptor descriptor) =>
            ContentAnnounced?.Invoke(this, descriptor);
    }

    private readonly NoOpContentService _contentService = new();
    private readonly HttpClient _httpClient = new();
    private MeshAppDistributor? _distributor;

    private MeshAppDistributor CreateDistributor() =>
        _distributor = new MeshAppDistributor(
            _contentService,
            _httpClient,
            NullLogger<MeshAppDistributor>.Instance);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StartHostingAsync_PublishesAndAnnouncesToMesh()
    {
        var distributor = CreateDistributor();
        var tempFile    = CreateTempApk(256);

        try
        {
            var pkg = new AppPackage
            {
                AppId        = "test-app",
                Name         = "Test App",
                Version      = "1.0.0",
                ContentHash  = string.Empty,
                SizeBytes    = 256,
                CloudflareUrl= string.Empty,
            };

            await distributor.StartHostingAsync(tempFile, pkg);

            Assert.Single(_contentService.Published);
            Assert.Single(_contentService.Announced);
            Assert.Equal("test-app|1.0.0", _contentService.Published[0].Name);
            Assert.Equal("application/vnd.android.package-archive",
                _contentService.Published[0].ContentType);
        }
        finally
        {
            await distributor.StopHostingAsync();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task StartHostingAsync_SetsBootstrapUri()
    {
        var distributor = CreateDistributor();
        var tempFile    = CreateTempApk(64);

        try
        {
            var pkg = MakePackage("uri-test");
            await distributor.StartHostingAsync(tempFile, pkg);

            Assert.NotNull(distributor.BootstrapUri);
            Assert.Equal("http", distributor.BootstrapUri!.Scheme);
            Assert.EndsWith("/app", distributor.BootstrapUri.AbsolutePath);
        }
        finally
        {
            await distributor.StopHostingAsync();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task BootstrapServer_ServesApkOverHttp()
    {
        var distributor = CreateDistributor();
        var apkData     = new byte[512];
        Random.Shared.NextBytes(apkData);
        var tempFile = CreateTempApk(apkData);

        try
        {
            await distributor.StartHostingAsync(tempFile, MakePackage("http-serve-test"));

            // Download the served file
            using var http = new HttpClient();
            var downloaded = await http.GetByteArrayAsync(distributor.BootstrapUri!);

            Assert.Equal(apkData, downloaded);
        }
        finally
        {
            await distributor.StopHostingAsync();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task StopHostingAsync_ClearsBootstrapUri()
    {
        var distributor = CreateDistributor();
        var tempFile    = CreateTempApk(32);

        try
        {
            await distributor.StartHostingAsync(tempFile, MakePackage("stop-test"));
            Assert.NotNull(distributor.BootstrapUri);

            await distributor.StopHostingAsync();
            Assert.Null(distributor.BootstrapUri);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DownloadAndVerifyAsync_RejectsHashMismatch()
    {
        var distributor = CreateDistributor();

        // Point at a real URL that returns *something* but give the wrong hash
        // We test with a temp file served by our own bootstrap server
        var apkData  = new byte[128];
        Random.Shared.NextBytes(apkData);
        var tempFile = CreateTempApk(apkData);

        try
        {
            await distributor.StartHostingAsync(tempFile, MakePackage("hash-test"));

            var wrongHash = new string('0', 64);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                distributor.DownloadAndVerifyAsync(
                    distributor.BootstrapUri!.ToString(),
                    wrongHash));
        }
        finally
        {
            await distributor.StopHostingAsync();
            // Retry: the HTTP server may still hold the file handle for a brief moment
            // after StopHostingAsync returns (OS I/O completion ports are async).
            for (var i = 0; i < 10; i++)
            {
                try { File.Delete(tempFile); break; }
                catch (IOException) when (i < 9) { await Task.Delay(50); }
            }
        }
    }

    [Fact]
    public async Task DownloadAndVerifyAsync_AcceptsCorrectHash()
    {
        var distributor = CreateDistributor();
        var apkData     = new byte[128];
        Random.Shared.NextBytes(apkData);
        var tempFile = CreateTempApk(apkData);

        try
        {
            await distributor.StartHostingAsync(tempFile, MakePackage("correct-hash-test"));

            var correctHash = Convert.ToHexString(SHA256.HashData(apkData)).ToLowerInvariant();
            var outPath = await distributor.DownloadAndVerifyAsync(
                distributor.BootstrapUri!.ToString(),
                correctHash);

            Assert.True(File.Exists(outPath));
            Assert.Equal(apkData, await File.ReadAllBytesAsync(outPath));
        }
        finally
        {
            await distributor.StopHostingAsync();
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void OnContentAnnounced_FiresAppDiscovered_ForApkType()
    {
        var distributor = CreateDistributor();
        AppPackage? discovered = null;
        distributor.AppDiscovered += (_, pkg) => discovered = pkg;

        var descriptor = ContentDescriptor.FromBytes(
            "slepton|1.0.0",
            [0x01, 0x02],
            "application/vnd.android.package-archive");

        _contentService.RaiseContentAnnounced(descriptor);

        Assert.NotNull(discovered);
        Assert.Equal("slepton", discovered!.AppId);
        Assert.Equal("1.0.0",   discovered.Version);
    }

    [Fact]
    public void OnContentAnnounced_Ignores_NonApkContent()
    {
        var distributor = CreateDistributor();
        AppPackage? discovered = null;
        distributor.AppDiscovered += (_, pkg) => discovered = pkg;

        var descriptor = ContentDescriptor.FromBytes(
            "some-video",
            [0xFF, 0xD8],
            "video/mp4");

        _contentService.RaiseContentAnnounced(descriptor);

        Assert.Null(discovered);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenOffline()
    {
        // HttpClient with no network — should return null, not throw
        using var offline = new HttpClient(new OfflineHandler());
        await using var dist = new MeshAppDistributor(
            _contentService, offline, NullLogger<MeshAppDistributor>.Instance);

        var result = await dist.CheckForUpdateAsync("aether-media", "1.0.0");
        Assert.Null(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_distributor is not null)
            await _distributor.DisposeAsync();
        _httpClient.Dispose();
    }

    private static string CreateTempApk(int size)
    {
        var data = new byte[size];
        Random.Shared.NextBytes(data);
        return CreateTempApk(data);
    }

    private static string CreateTempApk(byte[] data)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.apk");
        File.WriteAllBytes(path, data);
        return path;
    }

    private static AppPackage MakePackage(string appId) => new()
    {
        AppId        = appId,
        Name         = appId,
        Version      = "1.0.0",
        ContentHash  = string.Empty,
        SizeBytes    = 0,
        CloudflareUrl= string.Empty,
    };

    // Handler that simulates no network connectivity
    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(
                new HttpRequestException("No network (test)"));
    }
}
