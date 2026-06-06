// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace AetherMedia.LocalLibrary.Audio.Scrobble;

/// <summary>
/// <see cref="IScrobbler"/> backed by the Last.fm 2.0 API. Buffers locally
/// when the network is unreachable; <see cref="FlushAsync"/> drains the
/// buffer in FIFO order.
///
/// <para>
/// The host shell is responsible for completing the user-auth flow (token →
/// session-key) — wire the resulting <c>SessionKey</c> into the constructor.
/// API contract: <c>https://www.last.fm/api/show/track.scrobble</c>.
/// </para>
/// </summary>
public sealed class LastFmScrobbler : IScrobbler, IDisposable
{
    private static readonly Uri DefaultBase = new("https://ws.audioscrobbler.com/2.0/");

    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _sessionKey;
    private readonly bool _ownsHttp;
    private readonly object _bufGate = new();
    private readonly Queue<ScrobbleEvent> _buffer = new();

    public LastFmScrobbler(string apiKey, string apiSecret, string sessionKey, HttpClient? http = null, Uri? endpoint = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey);
        ArgumentException.ThrowIfNullOrEmpty(apiSecret);
        ArgumentException.ThrowIfNullOrEmpty(sessionKey);
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        _sessionKey = sessionKey;
        _http = http ?? new HttpClient();
        _ownsHttp = http is null;
        _endpoint = endpoint ?? DefaultBase;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <inheritdoc/>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_sessionKey);

    /// <inheritdoc/>
    public async Task UpdateNowPlayingAsync(ScrobbleEvent ev, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ev);
        var fields = NowPlayingFields(ev);
        try { await PostSignedAsync(fields, ct).ConfigureAwait(false); }
        catch (HttpRequestException) { /* now-playing isn't worth buffering — drops are fine */ }
    }

    /// <inheritdoc/>
    public async Task ScrobbleAsync(ScrobbleEvent ev, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ev);
        var fields = ScrobbleFields(ev);
        try { await PostSignedAsync(fields, ct).ConfigureAwait(false); }
        catch (HttpRequestException)
        {
            lock (_bufGate) _buffer.Enqueue(ev);
        }
    }

    /// <inheritdoc/>
    public async Task FlushAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ScrobbleEvent? next;
            lock (_bufGate) next = _buffer.Count > 0 ? _buffer.Dequeue() : null;
            if (next is null) return;
            try { await PostSignedAsync(ScrobbleFields(next), ct).ConfigureAwait(false); }
            catch (HttpRequestException)
            {
                lock (_bufGate) _buffer.Enqueue(next);
                return;
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    /// <summary>Pending scrobbles not yet successfully transmitted — exposed for tests + UI.</summary>
    public int PendingCount
    {
        get { lock (_bufGate) return _buffer.Count; }
    }

    private Dictionary<string, string> ScrobbleFields(ScrobbleEvent ev) => new()
    {
        ["method"]    = "track.scrobble",
        ["artist"]    = ev.Artist,
        ["track"]     = ev.Title,
        ["album"]     = ev.Album ?? "",
        ["timestamp"] = ev.StartedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
        ["duration"]  = ((long)ev.Duration.TotalSeconds).ToString(CultureInfo.InvariantCulture),
        ["api_key"]   = _apiKey,
        ["sk"]        = _sessionKey,
    };

    private Dictionary<string, string> NowPlayingFields(ScrobbleEvent ev) => new()
    {
        ["method"]   = "track.updateNowPlaying",
        ["artist"]   = ev.Artist,
        ["track"]    = ev.Title,
        ["album"]    = ev.Album ?? "",
        ["duration"] = ((long)ev.Duration.TotalSeconds).ToString(CultureInfo.InvariantCulture),
        ["api_key"]  = _apiKey,
        ["sk"]       = _sessionKey,
    };

    private async Task PostSignedAsync(Dictionary<string, string> fields, CancellationToken ct)
    {
        fields["api_sig"] = Sign(fields);
        fields["format"] = "json";
        using var content = new FormUrlEncodedContent(fields);
        using var resp = await _http.PostAsync(_endpoint, content, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }

    private string Sign(Dictionary<string, string> fields)
    {
        var sb = new StringBuilder();
        foreach (var k in fields.Keys.Where(k => k is not ("format" or "callback")).OrderBy(k => k, StringComparer.Ordinal))
        {
            sb.Append(k);
            sb.Append(fields[k]);
        }
        sb.Append(_apiSecret);
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        var hex = new StringBuilder(32);
        foreach (var b in hash) hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return hex.ToString();
    }
}
