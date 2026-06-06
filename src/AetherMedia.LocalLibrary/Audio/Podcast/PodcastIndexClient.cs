// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>
/// <see cref="IPodcastDirectoryClient"/> backed by podcastindex.org — open
/// directory of ~4 million podcasts, free API tier.
///
/// <para>
/// Auth: each request carries <c>X-Auth-Key</c>, <c>X-Auth-Date</c>, and an
/// <c>Authorization</c> header containing SHA-1(key + secret + date) in
/// lowercase hex. The constructor takes the developer credentials; the host
/// shell sources them from config so they aren't baked into the binary.
/// </para>
/// </summary>
public sealed class PodcastIndexClient : IPodcastDirectoryClient, IDisposable
{
    private static readonly Uri DefaultBase = new("https://api.podcastindex.org/");

    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly bool _ownsHttp;

    public PodcastIndexClient(string apiKey, string apiSecret, HttpClient? http = null, Uri? baseUri = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey);
        ArgumentException.ThrowIfNullOrEmpty(apiSecret);
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        _http = http ?? new HttpClient();
        _ownsHttp = http is null;
        _baseUri = baseUri ?? DefaultBase;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PodcastDirectoryResult>> SearchAsync(string query, int limit = 25, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(query);
        var url = new Uri(_baseUri, $"api/1.0/search/byterm?q={Uri.EscapeDataString(query)}&max={limit.ToString(CultureInfo.InvariantCulture)}");
        return await FetchAsync(url, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PodcastDirectoryResult>> TrendingAsync(int limit = 25, CancellationToken ct = default)
    {
        var url = new Uri(_baseUri, $"api/1.0/podcasts/trending?max={limit.ToString(CultureInfo.InvariantCulture)}");
        return await FetchAsync(url, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private async Task<IReadOnlyList<PodcastDirectoryResult>> FetchAsync(Uri url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var date = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        req.Headers.TryAddWithoutValidation("X-Auth-Key", _apiKey);
        req.Headers.TryAddWithoutValidation("X-Auth-Date", date);
        req.Headers.TryAddWithoutValidation("Authorization", SignAuth(date));

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<PodcastIndexResponse>(ct).ConfigureAwait(false);
        if (body?.Feeds is null) return Array.Empty<PodcastDirectoryResult>();

        var list = new List<PodcastDirectoryResult>(body.Feeds.Count);
        foreach (var f in body.Feeds)
        {
            if (string.IsNullOrEmpty(f.Url) || !Uri.TryCreate(f.Url, UriKind.Absolute, out var feed)) continue;
            Uri.TryCreate(f.Link, UriKind.Absolute, out var homepage);
            Uri.TryCreate(f.Image, UriKind.Absolute, out var image);
            list.Add(new PodcastDirectoryResult(
                Id: f.Id,
                Title: f.Title ?? "(untitled)",
                Author: f.Author,
                FeedUrl: feed,
                Homepage: homepage,
                ImageUrl: image,
                Categories: f.Categories is null ? null : string.Join(", ", f.Categories.Values),
                Description: f.Description));
        }
        return list;
    }

    private string SignAuth(string date)
    {
        var input = Encoding.UTF8.GetBytes(_apiKey + _apiSecret + date);
        var hash = SHA1.HashData(input);
        var sb = new StringBuilder(40);
        foreach (var b in hash) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private sealed class PodcastIndexResponse
    {
        public List<PodcastIndexFeed>? Feeds { get; set; }
    }

    private sealed class PodcastIndexFeed
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Url { get; set; }
        public string? Link { get; set; }
        public string? Image { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        public Dictionary<string, string>? Categories { get; set; }
    }
}
