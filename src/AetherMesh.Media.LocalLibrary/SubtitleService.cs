// SPDX-License-Identifier: MIT

using System.Net.Http.Json;
using System.Text.Json;
using AetherMesh.Media.LocalLibrary.Interfaces;
using AetherMesh.Media.LocalLibrary.Models;
using Microsoft.Extensions.Logging;

namespace AetherMesh.Media.LocalLibrary;

/// <summary>
/// OpenSubtitles REST API v1 implementation of <see cref="ISubtitleService"/>.
///
/// API documentation: https://opensubtitles.stoplight.io/docs/opensubtitles-api
///
/// The service is designed to degrade gracefully — every network or API error is
/// caught, logged at Warning level, and returns an empty result rather than throwing.
/// </summary>
public sealed class SubtitleService : ISubtitleService
{
    private const string BaseUrl        = "https://api.opensubtitles.com/api/v1";
    private const string AppHeader      = "Aether Media v1.0";
    private const int    MaxResults     = 20;

    private readonly HttpClient              _http;
    private readonly IMovieHasher            _hasher;
    private readonly ILogger<SubtitleService> _logger;
    private readonly string?                 _apiKey;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Constructs the service.
    /// </summary>
    /// <param name="http">Shared <see cref="HttpClient"/>.  Base address is NOT required.</param>
    /// <param name="hasher">Used to compute the movie hash for precise matching.</param>
    /// <param name="logger">Diagnostic logger.</param>
    /// <param name="apiKey">
    /// OpenSubtitles API key from <c>https://www.opensubtitles.com/consumers</c>.
    /// When <c>null</c> or empty the service returns empty results without hitting the API.
    /// </param>
    public SubtitleService(
        HttpClient              http,
        IMovieHasher            hasher,
        ILogger<SubtitleService> logger,
        string?                  apiKey = null)
    {
        _http   = http   ?? throw new ArgumentNullException(nameof(http));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = apiKey;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SubtitleSearchResult>> SearchAsync(
        string  videoFilePath,
        string? titleOverride = null,
        int?    yearOverride  = null,
        string  language     = "en",
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogDebug("SubtitleService: no API key configured — skipping search");
            return [];
        }

        try
        {
            // 1. Try hash-based search first (most accurate)
            var hash = await _hasher.ComputeHashAsync(videoFilePath, ct).ConfigureAwait(false);

            if (hash is not null)
            {
                var hashResults = await SearchByHashAsync(hash, language, ct).ConfigureAwait(false);
                if (hashResults.Count > 0)
                    return hashResults;
            }

            // 2. Fallback: title + year search
            var title = titleOverride ?? Path.GetFileNameWithoutExtension(videoFilePath);
            return await SearchByTitleAsync(title, yearOverride, language, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SubtitleService.SearchAsync failed for {Path}", videoFilePath);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<string> DownloadAsync(
        string videoFilePath,
        string fileId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("OpenSubtitles API key is not configured.");

        if (!int.TryParse(fileId, out var numericFileId))
            throw new ArgumentException($"fileId must be numeric — received: {fileId}", nameof(fileId));

        // 1. Request a temporary download link
        var request = new OsDownloadRequest { FileId = numericFileId };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/download")
        {
            Content = JsonContent.Create(request)
        };
        AddHeaders(req);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var downloadResp = await resp.Content
            .ReadFromJsonAsync<OsDownloadResponse>(JsonOptions, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Received empty download response from OpenSubtitles.");

        if (string.IsNullOrEmpty(downloadResp.Link))
            throw new InvalidOperationException("OpenSubtitles returned an empty download link.");

        // 2. Download the subtitle bytes
        var subtitleBytes = await _http.GetByteArrayAsync(downloadResp.Link, ct)
            .ConfigureAwait(false);

        // 3. Save alongside the video file as <stem>.srt
        var srtPath = Path.ChangeExtension(videoFilePath, ".srt");
        await File.WriteAllBytesAsync(srtPath, subtitleBytes, ct).ConfigureAwait(false);

        _logger.LogInformation("Subtitle saved: {Path} ({Remaining} downloads remaining)",
            srtPath, downloadResp.Remaining);

        return srtPath;
    }

    // ── Private search helpers ─────────────────────────────────────────────

    private async Task<IReadOnlyList<SubtitleSearchResult>> SearchByHashAsync(
        string hash, string language, CancellationToken ct)
    {
        var url = $"{BaseUrl}/subtitles?moviehash={hash}&languages={language}&per_page={MaxResults}";
        return await ExecuteSearchAsync(url, hashMatch: true, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<SubtitleSearchResult>> SearchByTitleAsync(
        string title, int? year, string language, CancellationToken ct)
    {
        var encodedTitle = Uri.EscapeDataString(title);
        var url = $"{BaseUrl}/subtitles?query={encodedTitle}&languages={language}&per_page={MaxResults}";
        if (year.HasValue)
            url += $"&year={year.Value}";

        return await ExecuteSearchAsync(url, hashMatch: false, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<SubtitleSearchResult>> ExecuteSearchAsync(
        string url, bool hashMatch, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(req);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenSubtitles returned {Status} for {Url}",
                (int)resp.StatusCode, url);
            return [];
        }

        var body = await resp.Content
            .ReadFromJsonAsync<OsSearchResponse>(JsonOptions, ct)
            .ConfigureAwait(false);

        if (body is null)
            return [];

        var results = new List<SubtitleSearchResult>(body.Data.Length);

        foreach (var item in body.Data)
        {
            var attr    = item.Attributes;
            var firstFile = attr.Files.FirstOrDefault();
            if (firstFile is null)
                continue;

            results.Add(new SubtitleSearchResult(
                FileId        : firstFile.FileId.ToString(),
                MovieTitle    : attr.FeatureDetails.MovieName,
                Language      : attr.Language,
                ReleaseName   : attr.Release,
                DownloadCount : attr.DownloadCount,
                Rating        : attr.Ratings,
                HashMatch     : hashMatch && attr.MoviehashMatch));
        }

        // Hash-matched results first, then by download count
        results.Sort((a, b) =>
        {
            if (a.HashMatch != b.HashMatch)
                return a.HashMatch ? -1 : 1;
            return b.DownloadCount.CompareTo(a.DownloadCount);
        });

        return results;
    }

    private void AddHeaders(HttpRequestMessage req)
    {
        req.Headers.Add("Api-Key",    _apiKey);
        req.Headers.Add("User-Agent", AppHeader);
    }
}
