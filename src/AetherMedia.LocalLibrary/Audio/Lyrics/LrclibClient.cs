// SPDX-License-Identifier: MIT

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AetherMedia.LocalLibrary.Audio.Lyrics;

/// <summary>
/// <see cref="ILyricFetcher"/> backed by lrclib.net — an open lyrics
/// directory that returns both plain and synchronised (LRC) lyrics for free.
/// </summary>
public sealed class LrclibClient : ILyricFetcher, IDisposable
{
    private static readonly Uri DefaultBase = new("https://lrclib.net/");

    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly bool _ownsHttp;
    private readonly LrcParser _parser = new();

    public LrclibClient() : this(new HttpClient(), DefaultBase, ownsHttp: true) { }

    public LrclibClient(HttpClient http, Uri? baseUri = null)
        : this(http, baseUri ?? DefaultBase, ownsHttp: false) { }

    private LrclibClient(HttpClient http, Uri baseUri, bool ownsHttp)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _baseUri = baseUri;
        _ownsHttp = ownsHttp;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <inheritdoc/>
    public async Task<LrcFile?> FetchAsync(string artist, string trackTitle, string? album = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(trackTitle))
            return null;

        var qs = new List<string>
        {
            $"artist_name={Uri.EscapeDataString(artist)}",
            $"track_name={Uri.EscapeDataString(trackTitle)}",
        };
        if (!string.IsNullOrEmpty(album)) qs.Add($"album_name={Uri.EscapeDataString(album)}");

        var url = new Uri(_baseUri, $"api/search?{string.Join('&', qs)}");
        List<LrclibHit>? hits;
        try
        {
            hits = await _http.GetFromJsonAsync<List<LrclibHit>>(url, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }

        var best = hits?.FirstOrDefault();
        if (best is null) return null;

        if (!string.IsNullOrWhiteSpace(best.SyncedLyrics))
            return _parser.Parse(best.SyncedLyrics);

        // Fall back to plain lyrics with no offsets — surface as a single
        // line at 00:00 so the UI can still display the static lyric.
        if (!string.IsNullOrWhiteSpace(best.PlainLyrics))
            return new LrcFile(best.TrackName, best.ArtistName, best.AlbumName,
                new[] { new LyricLine(TimeSpan.Zero, best.PlainLyrics) });

        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private sealed class LrclibHit
    {
        public string? TrackName { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        [JsonPropertyName("syncedLyrics")] public string? SyncedLyrics { get; set; }
        [JsonPropertyName("plainLyrics")] public string? PlainLyrics { get; set; }
    }
}
