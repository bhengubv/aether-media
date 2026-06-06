// SPDX-License-Identifier: MIT

using System.Net;
using AetherMedia.LocalLibrary;
using AetherMedia.LocalLibrary.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.LocalLibrary.Tests;

public sealed class SubtitleServiceTests
{
    // ── SearchAsync — no API key ───────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNoApiKey()
    {
        var svc = MakeService(apiKey: null);
        var results = await svc.SearchAsync("/video/movie.mkv");
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNoApiKey_EmptyString()
    {
        var svc = MakeService(apiKey: string.Empty);
        var results = await svc.SearchAsync("/video/movie.mkv");
        Assert.Empty(results);
    }

    // ── SearchAsync — network errors ───────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNetworkFails()
    {
        var svc = MakeService(apiKey: "test-key",
            handler: new AlwaysFailHandler());

        var results = await svc.SearchAsync("/video/movie.mkv");
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenApiReturns500()
    {
        var svc = MakeService(apiKey: "test-key",
            handler: new FixedStatusHandler(HttpStatusCode.InternalServerError));

        var results = await svc.SearchAsync("/video/movie.mkv");
        Assert.Empty(results);
    }

    // ── SearchAsync — JSON parsing ─────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ParsesResults_FromValidResponse()
    {
        const string json = """
            {
              "total_count": 1,
              "data": [{
                "id": "1",
                "attributes": {
                  "language": "en",
                  "download_count": 5000,
                  "moviehash_match": true,
                  "ratings": 8.5,
                  "release": "Matrix.1999.BluRay",
                  "files": [{ "file_id": 42, "file_name": "Matrix.srt" }],
                  "feature_details": { "movie_name": "The Matrix", "year": 1999 }
                }
              }]
            }
            """;

        var svc = MakeService(apiKey: "test-key",
            handler: new JsonResponseHandler(json));

        var results = await svc.SearchAsync("/video/matrix.mkv");

        Assert.Single(results);
        Assert.Equal("42",            results[0].FileId);
        Assert.Equal("The Matrix",    results[0].MovieTitle);
        Assert.Equal("en",            results[0].Language);
        Assert.Equal(5000,            results[0].DownloadCount);
        Assert.Equal(8.5f,            results[0].Rating, precision: 1);
        Assert.True(results[0].HashMatch);
    }

    // ── DownloadAsync — no API key ─────────────────────────────────────────

    [Fact]
    public async Task DownloadAsync_Throws_WhenNoApiKey()
    {
        var svc = MakeService(apiKey: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DownloadAsync("/video/movie.mkv", "123"));
    }

    // ── DownloadAsync — saves file ─────────────────────────────────────────

    [Fact]
    public async Task DownloadAsync_SavesSubtitleAlongsideVideo()
    {
        var dir       = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var videoPath = Path.Combine(dir, "The Matrix.mkv");
        var srtPath   = Path.Combine(dir, "The Matrix.srt");

        try
        {
            const string srtContent = "1\n00:00:01,000 --> 00:00:04,000\nHello, world.";
            const string downloadJson = """
                {
                  "link": "http://localhost/sub.srt",
                  "file_name": "The Matrix.srt",
                  "remaining": 190
                }
                """;

            // The handler serves the download response on POST and the SRT content on GET
            var handler = new TwoStepHandler(
                postResponse: downloadJson,
                getResponse:  srtContent);

            var svc    = MakeService(apiKey: "test-key", handler: handler);
            var result = await svc.DownloadAsync(videoPath, "42");

            Assert.Equal(srtPath, result);
            Assert.True(File.Exists(srtPath));
            Assert.Equal(srtContent, await File.ReadAllTextAsync(srtPath));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static SubtitleService MakeService(
        string? apiKey,
        HttpMessageHandler? handler = null)
    {
        var http   = new HttpClient(handler ?? new AlwaysFailHandler());
        var hasher = new StubHasher("0000000000020000"); // valid 16-char hash
        return new SubtitleService(http, hasher,
            NullLogger<SubtitleService>.Instance, apiKey);
    }

    /// <summary>Stub hasher that always returns a fixed hash.</summary>
    private sealed class StubHasher : IMovieHasher
    {
        private readonly string? _hash;
        public StubHasher(string? hash) => _hash = hash;

        public Task<string?> ComputeHashAsync(string filePath, CancellationToken ct = default)
            => Task.FromResult(_hash);
    }

    private sealed class AlwaysFailHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(
                new HttpRequestException("No network (test)"));
    }

    private sealed class FixedStatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        public FixedStatusHandler(HttpStatusCode code) => _code = code;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_code));
    }

    private sealed class JsonResponseHandler : HttpMessageHandler
    {
        private readonly string _json;
        public JsonResponseHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json,
                    System.Text.Encoding.UTF8, "application/json")
            });
    }

    /// <summary>
    /// Returns <paramref name="postResponse"/> for POST requests (download link),
    /// and <paramref name="getResponse"/> for GET requests (the subtitle bytes).
    /// </summary>
    private sealed class TwoStepHandler : HttpMessageHandler
    {
        private readonly string _postResponse;
        private readonly string _getResponse;

        public TwoStepHandler(string postResponse, string getResponse)
        {
            _postResponse = postResponse;
            _getResponse  = getResponse;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Method == HttpMethod.Post ? _postResponse : _getResponse;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body,
                    System.Text.Encoding.UTF8,
                    request.Method == HttpMethod.Post ? "application/json" : "text/plain")
            });
        }
    }
}
