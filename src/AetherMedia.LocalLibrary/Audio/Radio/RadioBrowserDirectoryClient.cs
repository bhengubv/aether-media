// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>
/// <see cref="IRadioDirectoryClient"/> backed by the community-run
/// radio-browser.info API — the open replacement for the retired SHOUTcast
/// Inc. directory. JSON over HTTP, no API key, ~50k curated stations.
///
/// <para>
/// The radio-browser network is a pool of mirrored servers (de1, de2, nl1,
/// at1, …). A real client SHOULD round-robin across the active mirrors
/// announced in DNS; here we accept a single base URI from the caller so
/// tests can point at a stub and prod can pick whichever mirror responds.
/// </para>
/// </summary>
public sealed class RadioBrowserDirectoryClient : IRadioDirectoryClient, IDisposable
{
    private static readonly Uri DefaultBaseUri = new("https://de1.api.radio-browser.info/");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly bool _ownsHttp;

    /// <summary>Construct with a default <see cref="HttpClient"/> pointing at the de1 mirror.</summary>
    public RadioBrowserDirectoryClient()
        : this(new HttpClient(), DefaultBaseUri, ownsHttp: true) { }

    /// <summary>Construct against an external <see cref="HttpClient"/> + base URI.</summary>
    public RadioBrowserDirectoryClient(HttpClient http, Uri baseUri)
        : this(http, baseUri, ownsHttp: false) { }

    private RadioBrowserDirectoryClient(HttpClient http, Uri baseUri, bool ownsHttp)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        _ownsHttp = ownsHttp;

        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RadioStation>> SearchAsync(RadioStationQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var qs = new List<string>();
        if (!string.IsNullOrEmpty(query.NameContains)) qs.Add($"name={Uri.EscapeDataString(query.NameContains)}");
        if (!string.IsNullOrEmpty(query.CountryCode))  qs.Add($"countrycode={Uri.EscapeDataString(query.CountryCode)}");
        if (!string.IsNullOrEmpty(query.Language))     qs.Add($"language={Uri.EscapeDataString(query.Language)}");
        if (!string.IsNullOrEmpty(query.Tag))          qs.Add($"tag={Uri.EscapeDataString(query.Tag)}");
        if (!string.IsNullOrEmpty(query.Codec))        qs.Add($"codec={Uri.EscapeDataString(query.Codec)}");
        if (query.MinBitrateKbps is { } br)            qs.Add($"bitrateMin={br.ToString(CultureInfo.InvariantCulture)}");
        qs.Add($"limit={query.Limit.ToString(CultureInfo.InvariantCulture)}");
        qs.Add($"offset={query.Offset.ToString(CultureInfo.InvariantCulture)}");
        qs.Add($"order={query.Order.ToString().ToLowerInvariant()}");
        qs.Add($"reverse={(query.Reverse ? "true" : "false")}");
        qs.Add("hidebroken=true");

        var url = new Uri(_baseUri, "json/stations/search?" + string.Join('&', qs));
        return await FetchListAsync(url, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RadioStation>> TopClickedAsync(int limit = 50, CancellationToken ct = default)
    {
        var url = new Uri(_baseUri, $"json/stations/topclick/{limit.ToString(CultureInfo.InvariantCulture)}");
        return await FetchListAsync(url, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<RadioStation?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        var url = new Uri(_baseUri, $"json/stations/byuuid?uuids={Uri.EscapeDataString(id)}");
        var list = await FetchListAsync(url, ct).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : null;
    }

    /// <inheritdoc/>
    public async Task RegisterClickAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        var url = new Uri(_baseUri, $"json/url/{Uri.EscapeDataString(id)}");
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
        // Click registration is best-effort — don't blow up on directory hiccups.
        _ = resp.IsSuccessStatusCode;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private async Task<IReadOnlyList<RadioStation>> FetchListAsync(Uri url, CancellationToken ct)
    {
        var raw = await _http.GetFromJsonAsync<List<RadioBrowserStationDto>>(url, JsonOptions, ct).ConfigureAwait(false);
        if (raw is null) return Array.Empty<RadioStation>();
        var list = new List<RadioStation>(raw.Count);
        foreach (var s in raw)
        {
            if (string.IsNullOrEmpty(s.Stationuuid)) continue;
            if (!TryParseAbsolute(s.UrlResolved ?? s.Url, out var stream)) continue;
            TryParseAbsolute(s.Homepage, out var homepage);
            TryParseAbsolute(s.Favicon, out var favicon);

            var tags = string.IsNullOrEmpty(s.Tags)
                ? (IReadOnlyList<string>)Array.Empty<string>()
                : s.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            list.Add(new RadioStation(
                Id: s.Stationuuid,
                Name: s.Name ?? "(unknown)",
                StreamUrl: stream!,
                Homepage: homepage,
                FaviconUrl: favicon,
                Country: s.Country,
                CountryCode: s.Countrycode,
                Language: s.Language,
                Tags: tags,
                Codec: s.Codec,
                BitrateKbps: s.Bitrate > 0 ? s.Bitrate : null,
                Votes: s.Votes,
                ClickCount: s.Clickcount));
        }
        return list;
    }

    private static bool TryParseAbsolute(string? value, out Uri? uri)
    {
        if (string.IsNullOrWhiteSpace(value)) { uri = null; return false; }
        if (Uri.TryCreate(value, UriKind.Absolute, out uri)) return true;
        uri = null;
        return false;
    }

    /// <summary>JSON shape returned by the radio-browser <c>/json/stations/*</c> endpoints.</summary>
    private sealed class RadioBrowserStationDto
    {
        public string? Stationuuid { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        [JsonPropertyName("url_resolved")] public string? UrlResolved { get; set; }
        public string? Homepage { get; set; }
        public string? Favicon { get; set; }
        public string? Country { get; set; }
        public string? Countrycode { get; set; }
        public string? Language { get; set; }
        public string? Tags { get; set; }
        public string? Codec { get; set; }
        public int Bitrate { get; set; }
        public int Votes { get; set; }
        public int Clickcount { get; set; }
    }
}
