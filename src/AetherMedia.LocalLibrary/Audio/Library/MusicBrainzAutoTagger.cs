// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AetherMedia.LocalLibrary.Audio.Tags;

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// <see cref="IAutoTagger"/> backed by the MusicBrainz recording-search API.
/// Searches by whatever the current tags provide (artist + title + optional
/// album) and returns the top hit projected back into an <see cref="AudioTags"/>
/// patch. Match-confidence threshold is configurable via
/// <see cref="MinScore"/> (MusicBrainz reports 0..100).
///
/// <para>
/// True acoustic fingerprinting (AcoustID / Chromaprint) is not in scope
/// here because Chromaprint is a native library; this tagger covers the
/// majority of well-tagged-but-typo'd files without that dependency.
/// </para>
/// </summary>
public sealed class MusicBrainzAutoTagger : IAutoTagger, IDisposable
{
    private static readonly Uri DefaultBase = new("https://musicbrainz.org/");

    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly bool _ownsHttp;

    public MusicBrainzAutoTagger()
        : this(new HttpClient(), DefaultBase, ownsHttp: true) { }

    public MusicBrainzAutoTagger(HttpClient http, Uri? baseUri = null)
        : this(http, baseUri ?? DefaultBase, ownsHttp: false) { }

    private MusicBrainzAutoTagger(HttpClient http, Uri baseUri, bool ownsHttp)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _baseUri = baseUri;
        _ownsHttp = ownsHttp;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <summary>Minimum MusicBrainz hit score (0..100) accepted as a suggestion. Default 90.</summary>
    public int MinScore { get; init; } = 90;

    /// <inheritdoc/>
    public async Task<AudioTags?> SuggestAsync(string filePath, AudioTags currentTags, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(currentTags);
        if (string.IsNullOrWhiteSpace(currentTags.Title) && string.IsNullOrWhiteSpace(currentTags.Artist))
            return null;

        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(currentTags.Title))  q.Add($"recording:\"{Escape(currentTags.Title!)}\"");
        if (!string.IsNullOrWhiteSpace(currentTags.Artist)) q.Add($"artist:\"{Escape(currentTags.Artist!)}\"");
        if (!string.IsNullOrWhiteSpace(currentTags.Album))  q.Add($"release:\"{Escape(currentTags.Album!)}\"");

        var url = new Uri(_baseUri, $"ws/2/recording/?query={Uri.EscapeDataString(string.Join(" AND ", q))}&fmt=json&limit=1");
        SearchResponse? search;
        try
        {
            search = await _http.GetFromJsonAsync<SearchResponse>(url, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }

        var top = search?.Recordings?.FirstOrDefault();
        if (top is null || top.Score < MinScore) return null;

        var firstRelease = top.Releases?.FirstOrDefault();
        var year = TryParseYear(firstRelease?.Date);

        return new AudioTags(
            Title: top.Title ?? currentTags.Title,
            Artist: top.ArtistCredit?.FirstOrDefault()?.Name ?? currentTags.Artist,
            Album: firstRelease?.Title ?? currentTags.Album,
            Year: year ?? currentTags.Year,
            TrackNumber: currentTags.TrackNumber,
            Genre: currentTags.Genre,
            ReplayGainTrackDb: currentTags.ReplayGainTrackDb,
            ReplayGainTrackPeakDbfs: currentTags.ReplayGainTrackPeakDbfs);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private static int? TryParseYear(string? date)
    {
        if (string.IsNullOrEmpty(date)) return null;
        if (int.TryParse(date.Split('-')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
            return y;
        return null;
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private sealed class SearchResponse
    {
        public List<MbRecording>? Recordings { get; set; }
    }

    private sealed class MbRecording
    {
        public string? Title { get; set; }
        public int Score { get; set; }
        [JsonPropertyName("artist-credit")] public List<MbArtistCredit>? ArtistCredit { get; set; }
        public List<MbRelease>? Releases { get; set; }
    }

    private sealed class MbArtistCredit
    {
        public string? Name { get; set; }
    }

    private sealed class MbRelease
    {
        public string? Title { get; set; }
        public string? Date { get; set; }
    }
}
