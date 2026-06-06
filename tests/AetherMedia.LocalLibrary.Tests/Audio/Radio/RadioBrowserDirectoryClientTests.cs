// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http;
using System.Text;
using AetherMedia.LocalLibrary.Audio.Radio;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Radio;

public class RadioBrowserDirectoryClientTests
{
    private static readonly Uri Base = new("https://test.example/");

    [Fact]
    public async Task SearchAsync_ParsesStationJson_AndProjectsTypedFields()
    {
        const string body = """
            [
              {
                "stationuuid": "abc-123",
                "name": "Demo Radio",
                "url": "http://stream.demo/raw",
                "url_resolved": "http://stream.demo/resolved",
                "homepage": "http://demo.example",
                "favicon": "http://demo.example/icon.png",
                "country": "South Africa",
                "countrycode": "ZA",
                "language": "english",
                "tags": "pop, rock",
                "codec": "MP3",
                "bitrate": 128,
                "votes": 42,
                "clickcount": 100
              }
            ]
            """;
        var handler = new StubHandler((req, _) => Reply(body));
        using var http = new HttpClient(handler);
        using var sut = new RadioBrowserDirectoryClient(http, Base);

        var result = await sut.SearchAsync(new RadioStationQuery(NameContains: "Demo"));

        Assert.Single(result);
        var s = result[0];
        Assert.Equal("abc-123",                       s.Id);
        Assert.Equal("Demo Radio",                    s.Name);
        Assert.Equal(new Uri("http://stream.demo/resolved"), s.StreamUrl);
        Assert.Equal(new Uri("http://demo.example"),  s.Homepage);
        Assert.Equal("ZA",                            s.CountryCode);
        Assert.Equal(2,                               s.Tags.Count);
        Assert.Contains("pop",  s.Tags);
        Assert.Contains("rock", s.Tags);
        Assert.Equal(128,                             s.BitrateKbps);
        Assert.Equal(42,                              s.Votes);
    }

    [Fact]
    public async Task TopClickedAsync_RoutesToTopClickEndpoint_AndDropsMissingUrls()
    {
        const string body = """
            [
              { "stationuuid": "1", "name": "Has URL", "url_resolved": "http://a/", "bitrate": 64,  "votes": 0, "clickcount": 0 },
              { "stationuuid": "2", "name": "No URL",  "url_resolved": "",          "bitrate": 0,   "votes": 0, "clickcount": 0 }
            ]
            """;
        Uri? observed = null;
        var handler = new StubHandler((req, _) => { observed = req.RequestUri; return Reply(body); });
        using var http = new HttpClient(handler);
        using var sut = new RadioBrowserDirectoryClient(http, Base);

        var result = await sut.TopClickedAsync(limit: 25);

        Assert.NotNull(observed);
        Assert.Contains("topclick/25", observed!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("1", result[0].Id);
    }

    [Fact]
    public async Task SearchAsync_SendsAllQueryParameters()
    {
        Uri? observed = null;
        var handler = new StubHandler((req, _) => { observed = req.RequestUri; return Reply("[]"); });
        using var http = new HttpClient(handler);
        using var sut = new RadioBrowserDirectoryClient(http, Base);

        await sut.SearchAsync(new RadioStationQuery(
            NameContains: "jazz",
            CountryCode: "ZA",
            Codec: "mp3",
            MinBitrateKbps: 96,
            Limit: 10,
            Order: RadioStationOrder.Bitrate,
            Reverse: false));

        var q = observed!.Query;
        Assert.Contains("name=jazz",      q);
        Assert.Contains("countrycode=ZA", q);
        Assert.Contains("codec=mp3",      q);
        Assert.Contains("bitrateMin=96",  q);
        Assert.Contains("limit=10",       q);
        Assert.Contains("order=bitrate",  q);
        Assert.Contains("reverse=false",  q);
        Assert.Contains("hidebroken=true", q);
    }

    private static Task<HttpResponseMessage> Reply(string body) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _f;
        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> f) => _f = f;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _f(request, cancellationToken);
    }
}
