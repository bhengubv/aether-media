// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>
/// Default <see cref="IShoutcastClient"/> built on <see cref="HttpClient"/>.
/// Sends <c>Icy-MetaData: 1</c>, parses the <c>icy-*</c> response headers,
/// and (when the server agrees) strips inline metadata blocks from the
/// audio stream as it's read.
/// </summary>
public sealed class ShoutcastClient : IShoutcastClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    /// <summary>Construct using a default <see cref="HttpClient"/>.</summary>
    public ShoutcastClient()
    {
        _http = new HttpClient();
        _ownsHttp = true;
    }

    /// <summary>
    /// Construct with an externally-owned <see cref="HttpClient"/>. The
    /// caller is responsible for disposing it.
    /// </summary>
    public ShoutcastClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _ownsHttp = false;
    }

    /// <inheritdoc/>
    public event EventHandler<string>? MetadataUpdated;

    /// <inheritdoc/>
    public async Task<ShoutcastStreamMetadata> ConnectAsync(Uri streamUrl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(streamUrl);
        using var req = new HttpRequestMessage(HttpMethod.Get, streamUrl);
        req.Headers.TryAddWithoutValidation("Icy-MetaData", "1");
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));

        var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var metadata = ProjectHeaders(resp);
        resp.Dispose();
        return metadata;
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenStreamAsync(Uri streamUrl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(streamUrl);
        using var req = new HttpRequestMessage(HttpMethod.Get, streamUrl);
        req.Headers.TryAddWithoutValidation("Icy-MetaData", "1");
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));

        var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var metaInt = ParseInt(resp, "icy-metaint") ?? 0;
        var raw = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        if (metaInt <= 0)
            return new ResponseOwningStream(raw, resp);

        return new IcyMetaStripStream(raw, resp, metaInt, title => MetadataUpdated?.Invoke(this, title));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private static ShoutcastStreamMetadata ProjectHeaders(HttpResponseMessage resp)
    {
        return new ShoutcastStreamMetadata(
            StationName: First(resp, "icy-name"),
            Genre: First(resp, "icy-genre"),
            BitrateKbps: ParseInt(resp, "icy-br"),
            ContentType: resp.Content.Headers.ContentType?.MediaType,
            MetadataIntervalBytes: ParseInt(resp, "icy-metaint") ?? 0,
            CurrentTitle: null);
    }

    private static string? First(HttpResponseMessage r, string name)
    {
        if (r.Headers.TryGetValues(name, out var hv)) return hv.FirstOrDefault();
        if (r.Content.Headers.TryGetValues(name, out var cv)) return cv.FirstOrDefault();
        return null;
    }

    private static int? ParseInt(HttpResponseMessage r, string name)
    {
        var raw = First(r, name);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    /// <summary>
    /// Wraps the audio body and the parent <see cref="HttpResponseMessage"/>
    /// so disposing the stream cleans up the connection too.
    /// </summary>
    private sealed class ResponseOwningStream(Stream inner, HttpResponseMessage resp) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => inner.ReadAsync(buffer, offset, count, ct);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => inner.ReadAsync(buffer, ct);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                resp.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Reads audio while stripping the inline icy metadata blocks that arrive
    /// every <c>metaInt</c> bytes. Each block is <c>length×16</c> bytes of
    /// ASCII; we parse the <c>StreamTitle='...';</c> form and notify the
    /// listener.
    /// </summary>
    private sealed class IcyMetaStripStream : Stream
    {
        private readonly Stream _inner;
        private readonly HttpResponseMessage _resp;
        private readonly int _metaInt;
        private readonly Action<string> _onTitle;
        private int _untilMeta;

        public IcyMetaStripStream(Stream inner, HttpResponseMessage resp, int metaInt, Action<string> onTitle)
        {
            _inner = inner;
            _resp = resp;
            _metaInt = metaInt;
            _onTitle = onTitle;
            _untilMeta = metaInt;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
            ReadAsync(buffer.AsMemory(offset, count), ct).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (buffer.IsEmpty) return 0;

            // Drain any pending metadata block first.
            if (_untilMeta == 0)
            {
                await ConsumeMetadataBlockAsync(ct).ConfigureAwait(false);
                _untilMeta = _metaInt;
            }

            // Read at most as much as remains before the next metadata block.
            var toRead = Math.Min(buffer.Length, _untilMeta);
            var n = await _inner.ReadAsync(buffer[..toRead], ct).ConfigureAwait(false);
            if (n > 0) _untilMeta -= n;
            return n;
        }

        private async Task ConsumeMetadataBlockAsync(CancellationToken ct)
        {
            var lenBuf = new byte[1];
            if (await _inner.ReadAsync(lenBuf.AsMemory(0, 1), ct).ConfigureAwait(false) != 1) return;
            var blockLen = lenBuf[0] * 16;
            if (blockLen == 0) return;

            var block = new byte[blockLen];
            var read = 0;
            while (read < blockLen)
            {
                var n = await _inner.ReadAsync(block.AsMemory(read, blockLen - read), ct).ConfigureAwait(false);
                if (n == 0) return;
                read += n;
            }
            ParseStreamTitle(block.AsSpan(0, read));
        }

        private void ParseStreamTitle(ReadOnlySpan<byte> block)
        {
            // Block is ASCII: StreamTitle='Artist - Song';StreamUrl='...';
            var text = Encoding.ASCII.GetString(block).TrimEnd('\0');
            const string Key = "StreamTitle='";
            var idx = text.IndexOf(Key, StringComparison.Ordinal);
            if (idx < 0) return;
            idx += Key.Length;
            var end = text.IndexOf('\'', idx);
            if (end < 0) return;
            var title = text[idx..end];
            if (!string.IsNullOrWhiteSpace(title))
                _onTitle(title);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _resp.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
