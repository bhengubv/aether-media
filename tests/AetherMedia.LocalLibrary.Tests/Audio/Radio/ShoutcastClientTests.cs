// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using AetherMedia.LocalLibrary.Audio.Radio;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Radio;

public class ShoutcastClientTests
{
    [Fact]
    public async Task ConnectAsync_ReadsIcyHeadersIntoMetadata()
    {
        var handler = new StubHandler((req, ct) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Array.Empty<byte>()),
            };
            resp.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
            resp.Headers.TryAddWithoutValidation("icy-name",    "Demo Station");
            resp.Headers.TryAddWithoutValidation("icy-genre",   "Electronic");
            resp.Headers.TryAddWithoutValidation("icy-br",      "128");
            resp.Headers.TryAddWithoutValidation("icy-metaint", "16000");
            return Task.FromResult(resp);
        });

        using var http = new HttpClient(handler);
        using var sut = new ShoutcastClient(http);
        var meta = await sut.ConnectAsync(new Uri("http://stream.example.com/radio"));

        Assert.Equal("Demo Station", meta.StationName);
        Assert.Equal("Electronic",  meta.Genre);
        Assert.Equal(128,           meta.BitrateKbps);
        Assert.Equal(16000,         meta.MetadataIntervalBytes);
        Assert.Equal("audio/mpeg",  meta.ContentType);
    }

    [Fact]
    public async Task OpenStreamAsync_StripsInlineIcyMetadata_AndFiresUpdateEvent()
    {
        // 8 bytes of audio, then a metadata block (length = 2, so 32 bytes of ASCII).
        var audio = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var metaTitle = "StreamTitle='Artist - Song';"u8.ToArray();
        var metaBlock = new byte[33]; // 1 length byte + 32 payload bytes
        metaBlock[0] = 2; // → 32 byte payload
        Array.Copy(metaTitle, 0, metaBlock, 1, metaTitle.Length);
        // Remaining bytes already 0 (padding)
        var tail = new byte[] { 9, 10, 11 };

        var body = new List<byte>();
        body.AddRange(audio);
        body.AddRange(metaBlock);
        body.AddRange(tail);

        var handler = new StubHandler((req, ct) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(body.ToArray()),
            };
            resp.Headers.TryAddWithoutValidation("icy-metaint", "8");
            return Task.FromResult(resp);
        });

        using var http = new HttpClient(handler);
        using var sut = new ShoutcastClient(http);
        string? gotTitle = null;
        sut.MetadataUpdated += (_, t) => gotTitle = t;

        var stream = await sut.OpenStreamAsync(new Uri("http://stream.example.com/radio"));
        var outBuf = new byte[64];
        var read1 = await stream.ReadAsync(outBuf.AsMemory(0, 64));
        // First read returns audio before the meta block.
        Assert.Equal(8, read1);
        Assert.Equal(audio, outBuf.AsSpan(0, 8).ToArray());

        var read2 = await stream.ReadAsync(outBuf.AsMemory(0, 64));
        // Second read consumes the meta block then returns the tail.
        Assert.Equal(3, read2);
        Assert.Equal(tail, outBuf.AsSpan(0, 3).ToArray());
        Assert.Equal("Artist - Song", gotTitle);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _f;
        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> f) => _f = f;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _f(request, cancellationToken);
    }
}
