// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>One MusicBrainz CD lookup hit.</summary>
public sealed record CdLookupResult(
    string ReleaseMbid,
    string Title,
    string Artist,
    int? Year,
    IReadOnlyList<string> TrackTitles);

/// <summary>
/// Calculates the MusicBrainz disc ID from a <see cref="CdToc"/> and looks
/// the disc up via the MusicBrainz web service. Replacement for the now-
/// commercial Gracenote / CDDB service Winamp used.
///
/// <para>
/// Disc ID = base64(sha1(uppercase-hex of first track, last track, sector
/// offsets[0..99])) per the documented MusicBrainz algorithm.
/// </para>
/// </summary>
public sealed class MusicBrainzCdLookup : IDisposable
{
    private static readonly Uri DefaultBase = new("https://musicbrainz.org/");

    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly bool _ownsHttp;

    public MusicBrainzCdLookup() : this(new HttpClient(), DefaultBase, ownsHttp: true) { }

    public MusicBrainzCdLookup(HttpClient http, Uri? baseUri = null)
        : this(http, baseUri ?? DefaultBase, ownsHttp: false) { }

    private MusicBrainzCdLookup(HttpClient http, Uri baseUri, bool ownsHttp)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _baseUri = baseUri;
        _ownsHttp = ownsHttp;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <summary>Look up a disc by its TOC.</summary>
    public async Task<CdLookupResult?> LookupAsync(CdToc toc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(toc);
        var discId = ComputeDiscId(toc);
        var url = new Uri(_baseUri, $"ws/2/discid/{discId}?fmt=json&inc=artist-credits+recordings");
        try
        {
            var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<DiscResponse>(cancellationToken: ct).ConfigureAwait(false);
            var rel = body?.Releases?.FirstOrDefault();
            if (rel is null) return null;

            return new CdLookupResult(
                ReleaseMbid: rel.Id ?? "",
                Title: rel.Title ?? "",
                Artist: rel.ArtistCredit?.FirstOrDefault()?.Name ?? "",
                Year: ParseYear(rel.Date),
                TrackTitles: ExtractTrackTitles(rel));
        }
        catch (HttpRequestException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }
    }

    /// <summary>
    /// Compute the MusicBrainz disc ID from the table of contents. Public
    /// for offline computation and testing without network.
    /// </summary>
    public static string ComputeDiscId(CdToc toc)
    {
        ArgumentNullException.ThrowIfNull(toc);
        if (toc.Tracks.Count == 0)
            throw new InvalidOperationException("TOC has no tracks.");

        var tracks = toc.Tracks;
        var firstTrack = tracks[0].Number;
        var lastTrack  = tracks[^1].Number;

        // Slot 0 = lead-out offset (last track start + sector count + 150).
        // Slots firstTrack..lastTrack = each track start offset + 150.
        var offsets = new int[100];
        offsets[0] = tracks[^1].StartLba + tracks[^1].SectorCount + 150;
        for (var i = 0; i < tracks.Count; i++)
        {
            var idx = tracks[i].Number;
            if (idx < 1 || idx > 99) continue;
            offsets[idx] = tracks[i].StartLba + 150;
        }

        var sb = new StringBuilder(2 + 2 + 100 * 8);
        sb.Append(firstTrack.ToString("X2", CultureInfo.InvariantCulture));
        sb.Append(lastTrack.ToString("X2",  CultureInfo.InvariantCulture));
        for (var i = 0; i < 100; i++)
            sb.Append(offsets[i].ToString("X8", CultureInfo.InvariantCulture));

        var hash = SHA1.HashData(Encoding.ASCII.GetBytes(sb.ToString()));
        return Convert.ToBase64String(hash).Replace('+', '.').Replace('/', '_').Replace('=', '-');
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private static int? ParseYear(string? date)
    {
        if (string.IsNullOrEmpty(date)) return null;
        if (int.TryParse(date.Split('-')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
            return y;
        return null;
    }

    private static IReadOnlyList<string> ExtractTrackTitles(DiscRelease rel)
    {
        var list = new List<string>();
        foreach (var medium in rel.Media ?? Enumerable.Empty<DiscMedium>())
        foreach (var track in medium.Tracks ?? Enumerable.Empty<DiscTrack>())
            list.Add(track.Title ?? "");
        return list;
    }

    private sealed class DiscResponse
    {
        public List<DiscRelease>? Releases { get; set; }
    }

    private sealed class DiscRelease
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Date { get; set; }
        [JsonPropertyName("artist-credit")] public List<DiscArtistCredit>? ArtistCredit { get; set; }
        public List<DiscMedium>? Media { get; set; }
    }

    private sealed class DiscArtistCredit { public string? Name { get; set; } }
    private sealed class DiscMedium { public List<DiscTrack>? Tracks { get; set; } }
    private sealed class DiscTrack { public string? Title { get; set; } }
}
