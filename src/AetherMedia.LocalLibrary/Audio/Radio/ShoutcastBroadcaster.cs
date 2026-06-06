// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net;
using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>
/// Self-hosted SHOUTcast-compatible broadcaster. Listens on
/// <see cref="HttpListener"/>, serves the Icy/MP3 protocol that every modern
/// player understands. The host pushes audio bytes via
/// <see cref="WriteAsync"/>; the broadcaster fans them out to all connected
/// listeners, with the configured Icy metadata interpolated.
///
/// <para>
/// Winamp's <c>SHOUTcast DSP</c> plugin sent audio <em>to</em> a SHOUTcast
/// relay server which then served listeners. This is the opposite shape:
/// AetherMedia is the server. Self-hosted, mesh-friendly, no third-party
/// relay needed. Use HTTP-tunnelling middleware (Cloudflare Tunnel /
/// ngrok / Tailscale) when you need public reach.
/// </para>
/// </summary>
public sealed class ShoutcastBroadcaster : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly object _gate = new();
    private readonly List<ListenerSession> _sessions = new();
    private Task? _acceptLoop;
    private CancellationTokenSource? _cts;
    private int _metaInterval;
    private long _bytesSinceMeta;
    private string _currentMetaTitle = "";

    /// <summary>Construct a broadcaster bound to <paramref name="prefix"/> (e.g. <c>"http://+:8000/"</c>).</summary>
    public ShoutcastBroadcaster(string prefix, ShoutcastBroadcasterOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        Options = options ?? new ShoutcastBroadcasterOptions();
        _listener.Prefixes.Add(prefix);
        _metaInterval = Math.Max(1024, Options.MetadataIntervalBytes);
    }

    /// <summary>Configuration used at construction.</summary>
    public ShoutcastBroadcasterOptions Options { get; }

    /// <summary>Current count of connected listeners.</summary>
    public int ListenerCount
    {
        get { lock (_gate) return _sessions.Count; }
    }

    /// <summary>Update the announced <c>StreamTitle</c>. Takes effect on the next metadata block.</summary>
    public void SetMetadataTitle(string title)
    {
        _currentMetaTitle = title ?? string.Empty;
    }

    /// <summary>Begin accepting listeners.</summary>
    public void Start()
    {
        if (_acceptLoop is not null) return;
        _listener.Start();
        _cts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    /// <summary>Stop accepting + drop all listeners.</summary>
    public void Stop()
    {
        _cts?.Cancel();
        try { _listener.Stop(); } catch (ObjectDisposedException) { }
        lock (_gate)
        {
            foreach (var s in _sessions) s.Cancel();
            _sessions.Clear();
        }
        _acceptLoop = null;
    }

    /// <summary>Fan the bytes out to every connected listener.</summary>
    public async Task WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken ct = default)
    {
        if (bytes.IsEmpty) return;

        ListenerSession[] snapshot;
        lock (_gate) snapshot = _sessions.ToArray();
        if (snapshot.Length == 0) return;

        // Inject metadata blocks so the wire format matches what a SHOUTcast
        // listener expects. Simplest impl: each listener gets the same
        // (audio + meta) interleaved stream.
        var i = 0;
        while (i < bytes.Length)
        {
            var spaceLeft = _metaInterval - (int)_bytesSinceMeta;
            var chunk = Math.Min(spaceLeft, bytes.Length - i);
            var slice = bytes.Slice(i, chunk);
            foreach (var s in snapshot) await s.WriteAsync(slice, ct).ConfigureAwait(false);
            _bytesSinceMeta += chunk;
            i += chunk;

            if (_bytesSinceMeta >= _metaInterval)
            {
                var metaBlock = BuildMetadataBlock(_currentMetaTitle);
                foreach (var s in snapshot) await s.WriteAsync(metaBlock, ct).ConfigureAwait(false);
                _bytesSinceMeta = 0;
            }
        }

        // Drop dead sessions.
        lock (_gate) _sessions.RemoveAll(s => s.IsClosed);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        ((IDisposable)_listener).Dispose();
        _cts?.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext context;
            try { context = await _listener.GetContextAsync().ConfigureAwait(false); }
            catch (HttpListenerException) { return; }
            catch (ObjectDisposedException) { return; }

            var resp = context.Response;
            resp.ContentType = Options.ContentType;
            resp.Headers["icy-name"]    = Options.StationName;
            resp.Headers["icy-genre"]   = Options.Genre ?? "";
            resp.Headers["icy-br"]      = Options.BitrateKbps.ToString(CultureInfo.InvariantCulture);
            resp.Headers["icy-metaint"] = _metaInterval.ToString(CultureInfo.InvariantCulture);
            resp.Headers["icy-pub"]     = Options.Public ? "1" : "0";
            // Force chunked off so listeners get a continuous byte stream.
            resp.SendChunked = false;
            resp.KeepAlive = true;

            var session = new ListenerSession(resp);
            lock (_gate) _sessions.Add(session);
        }
    }

    /// <summary>Build a SHOUTcast metadata block (length-prefixed, padded to 16 bytes).</summary>
    private static byte[] BuildMetadataBlock(string streamTitle)
    {
        if (string.IsNullOrEmpty(streamTitle)) return new byte[] { 0 };
        var inner = Encoding.UTF8.GetBytes($"StreamTitle='{streamTitle}';");
        var blockLen = ((inner.Length + 15) / 16) * 16;
        if (blockLen > 16 * 255) blockLen = 16 * 255;
        var result = new byte[1 + blockLen];
        result[0] = (byte)(blockLen / 16);
        Array.Copy(inner, 0, result, 1, Math.Min(inner.Length, blockLen));
        return result;
    }

    private sealed class ListenerSession
    {
        private readonly HttpListenerResponse _response;
        private readonly Stream _output;
        public bool IsClosed { get; private set; }

        public ListenerSession(HttpListenerResponse response)
        {
            _response = response;
            _output = response.OutputStream;
        }

        public async Task WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken ct)
        {
            if (IsClosed) return;
            try { await _output.WriteAsync(bytes, ct).ConfigureAwait(false); }
            catch (HttpListenerException) { Cancel(); }
            catch (IOException) { Cancel(); }
            catch (ObjectDisposedException) { Cancel(); }
        }

        public void Cancel()
        {
            IsClosed = true;
            try { _response.Close(); } catch { }
        }
    }
}

/// <summary>Construction-time configuration for <see cref="ShoutcastBroadcaster"/>.</summary>
public sealed class ShoutcastBroadcasterOptions
{
    /// <summary>Value of the icy-name response header.</summary>
    public string StationName { get; init; } = "AetherMedia Broadcast";

    /// <summary>Value of the icy-genre response header.</summary>
    public string? Genre { get; init; }

    /// <summary>Value of the icy-br response header (kbps).</summary>
    public int BitrateKbps { get; init; } = 128;

    /// <summary>Content-Type. Audio MP3 by default; switch to audio/aac for AAC.</summary>
    public string ContentType { get; init; } = "audio/mpeg";

    /// <summary>icy-pub flag: 1 = listed publicly, 0 = private.</summary>
    public bool Public { get; init; } = false;

    /// <summary>Bytes between inline icy metadata blocks. Default 16 KiB.</summary>
    public int MetadataIntervalBytes { get; init; } = 16 * 1024;
}
