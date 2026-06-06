// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http;
using AetherMedia.LocalLibrary.Audio.Scrobble;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Scrobble;

public class LastFmScrobblerTests
{
    [Fact]
    public async Task Scrobble_BuffersOnNetworkFailure_AndFlushes()
    {
        var attempts = 0;
        var handler = new StubHandler((req, _) =>
        {
            attempts++;
            if (attempts == 1) throw new HttpRequestException("network down");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        });
        using var http = new HttpClient(handler);
        using var sut = new LastFmScrobbler("k", "s", "sk", http);

        var ev = new ScrobbleEvent("Artist", "Title", "Album", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(3));
        await sut.ScrobbleAsync(ev);
        Assert.Equal(1, sut.PendingCount);

        await sut.FlushAsync();
        Assert.Equal(0, sut.PendingCount);
    }

    [Fact]
    public async Task Scrobble_SignsRequest_AndPostsToEndpoint()
    {
        Uri? observedUri = null;
        string? formBody = null;
        var handler = new StubHandler(async (req, _) =>
        {
            observedUri = req.RequestUri;
            formBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        using var http = new HttpClient(handler);
        using var sut = new LastFmScrobbler("key", "secret", "session", http);

        await sut.ScrobbleAsync(new ScrobbleEvent("A", "T", null, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(3)));

        Assert.NotNull(observedUri);
        Assert.NotNull(formBody);
        Assert.Contains("method=track.scrobble", formBody!);
        Assert.Contains("api_sig=", formBody);
        Assert.Contains("api_key=key", formBody);
        Assert.Contains("sk=session", formBody);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _f;
        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> f) => _f = f;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _f(request, cancellationToken);
    }
}
