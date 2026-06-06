// SPDX-License-Identifier: MIT

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// <see cref="ICoverArtFetcher"/> backed by the MusicBrainz + Cover Art
/// Archive APIs. Both are open and free; no API key required, just a
/// well-formed User-Agent (per MusicBrainz etiquette).
///
/// <para>
/// Flow: search MusicBrainz for the (artist, album) release → take the top
/// hit's MBID → request <c>coverartarchive.org/release/{mbid}/front-500</c>
/// → return JPEG/PNG bytes. Falls back to null if any step fails.
/// </para>
/// </summary>
public sealed class MusicBrainzCoverArtFetcher : ICoverArtFetcher, IDisposable
{
    private static readonly Uri MusicBrainzBase   = new("https://musicbrainz.org/");
    private static readonly Uri CoverArtArchiveBase = new("https://coverartarchive.org/");

    private readonly HttpClient _http;
    private readonly Uri _mbBase;
    private readonly Uri _caaBase;
    private readonly bool _ownsHttp;

    public MusicBrainzCoverArtFetcher()
        : this(new HttpClient(), MusicBrainzBase, CoverArtArchiveBase, ownsHttp: true) { }

    public MusicBrainzCoverArtFetcher(HttpClient http, Uri? musicBrainzBase = null, Uri? coverArtBase = null)
        : this(http, musicBrainzBase ?? MusicBrainzBase, coverArtBase ?? CoverArtArchiveBase, ownsHttp: false) { }

    private MusicBrainzCoverArtFetcher(HttpClient http, Uri mbBase, Uri caaBase, bool ownsHttp)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _mbBase = mbBase;
        _caaBase = caaBase;
        _ownsHttp = ownsHttp;

        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <inheritdoc/>
    public async Task<byte[]?> FetchAsync(string artist, string album, string? track = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(album))
            return null;

        // 1. Search MusicBrainz for the release.
        var q = $"artist:\"{Escape(artist)}\" AND release:\"{Escape(album)}\"";
        var url = new Uri(_mbBase, $"ws/2/release/?query={Uri.EscapeDataString(q)}&fmt=json&limit=1");
        SearchResponse? search;
        try
        {
            search = await _http.GetFromJsonAsync<SearchResponse>(url, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }

        var mbid = search?.Releases?.FirstOrDefault()?.Id;
        if (string.IsNullOrEmpty(mbid)) return null;

        // 2. Pull the front cover art for that release.
        var artUrl = new Uri(_caaBase, $"release/{mbid}/front-500");
        try
        {
            using var resp = await _http.GetAsync(artUrl, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
        catch (HttpRequestException) { return null; }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private static string Escape(string s)
    {
        // MusicBrainz Lucene-style escape — bare minimum for paren / quote.
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private sealed class SearchResponse
    {
        public List<MbRelease>? Releases { get; set; }
    }

    private sealed class MbRelease
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
    }
}
