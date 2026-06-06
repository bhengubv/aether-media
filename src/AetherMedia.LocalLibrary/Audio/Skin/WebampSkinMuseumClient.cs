// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// <see cref="IWinampSkinCatalog"/> backed by the Webamp Skin Museum
/// (api.webamp.org) — the modern preservation effort that mirrors the entire
/// classic Winamp Skin Archive (~80,000 skins).
/// </summary>
public sealed class WebampSkinMuseumClient : IWinampSkinCatalog, IDisposable
{
    private static readonly Uri DefaultBase = new("https://api.webamp.org/");

    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly bool _ownsHttp;

    public WebampSkinMuseumClient() : this(new HttpClient(), DefaultBase, ownsHttp: true) { }

    public WebampSkinMuseumClient(HttpClient http, Uri? baseUri = null)
        : this(http, baseUri ?? DefaultBase, ownsHttp: false) { }

    private WebampSkinMuseumClient(HttpClient http, Uri baseUri, bool ownsHttp)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _baseUri = baseUri;
        _ownsHttp = ownsHttp;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WinampSkinCatalogEntry>> SearchAsync(
        string? query, int limit = 50, int offset = 0, bool includeNsfw = false, CancellationToken ct = default)
    {
        var qs = new List<string>
        {
            $"first={limit.ToString(CultureInfo.InvariantCulture)}",
            $"offset={offset.ToString(CultureInfo.InvariantCulture)}",
        };
        if (!string.IsNullOrEmpty(query)) qs.Add($"query={Uri.EscapeDataString(query)}");
        if (!includeNsfw) qs.Add("filter=approved");

        var url = new Uri(_baseUri, $"skins?{string.Join('&', qs)}");
        List<SkinMuseumEntry>? entries;
        try
        {
            entries = await _http.GetFromJsonAsync<List<SkinMuseumEntry>>(url, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException) { return Array.Empty<WinampSkinCatalogEntry>(); }
        catch (System.Text.Json.JsonException) { return Array.Empty<WinampSkinCatalogEntry>(); }

        if (entries is null) return Array.Empty<WinampSkinCatalogEntry>();
        var list = new List<WinampSkinCatalogEntry>(entries.Count);
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.Md5) || string.IsNullOrEmpty(e.DownloadUrl)) continue;
            if (!Uri.TryCreate(e.DownloadUrl, UriKind.Absolute, out var dl)) continue;
            Uri.TryCreate(e.ScreenshotUrl, UriKind.Absolute, out var ss);
            list.Add(new WinampSkinCatalogEntry(
                Id: e.Md5,
                Name: e.FileName ?? "(untitled)",
                DownloadUrl: dl,
                ScreenshotUrl: ss,
                Author: e.AuthorName,
                IsNsfw: e.IsNsfw));
        }
        return list;
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenSkinAsync(WinampSkinCatalogEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var resp = await _http.GetAsync(entry.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private sealed class SkinMuseumEntry
    {
        public string? Md5 { get; set; }
        public string? FileName { get; set; }
        [JsonPropertyName("downloadUrl")] public string? DownloadUrl { get; set; }
        [JsonPropertyName("screenshotUrl")] public string? ScreenshotUrl { get; set; }
        [JsonPropertyName("authorName")] public string? AuthorName { get; set; }
        [JsonPropertyName("nsfw")] public bool IsNsfw { get; set; }
    }
}
