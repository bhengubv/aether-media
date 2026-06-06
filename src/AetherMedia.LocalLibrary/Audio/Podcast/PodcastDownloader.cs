// SPDX-License-Identifier: MIT

using System.Net.Http;
using System.Net.Http.Headers;

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>
/// Downloads podcast episode audio to a local directory. Resume-on-failure
/// uses a <c>.partial</c> sidecar file so a crashed download picks up where
/// it left off via HTTP Range.
/// </summary>
public sealed class PodcastDownloader : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public PodcastDownloader() : this(new HttpClient(), ownsHttp: true) { }

    public PodcastDownloader(HttpClient http) : this(http, ownsHttp: false) { }

    private PodcastDownloader(HttpClient http, bool ownsHttp)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _ownsHttp = ownsHttp;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <summary>
    /// Download <paramref name="episode"/> to a file under
    /// <paramref name="destDir"/>. Returns the final file path. No-op if the
    /// file already exists and the length matches.
    /// </summary>
    public async Task<string> DownloadAsync(
        PodcastEpisode episode,
        string destDir,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(episode);
        ArgumentException.ThrowIfNullOrEmpty(destDir);
        Directory.CreateDirectory(destDir);

        var fileName = SafeFileName(episode);
        var finalPath = Path.Combine(destDir, fileName);
        if (System.IO.File.Exists(finalPath))
        {
            if (episode.LengthBytes is null || new FileInfo(finalPath).Length == episode.LengthBytes)
                return finalPath;
        }

        var partialPath = finalPath + ".partial";
        var existing = System.IO.File.Exists(partialPath) ? new FileInfo(partialPath).Length : 0L;

        using var req = new HttpRequestMessage(HttpMethod.Get, episode.AudioUrl);
        if (existing > 0)
            req.Headers.Range = new RangeHeaderValue(existing, null);

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var dest = new FileStream(partialPath, existing > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);

        var total = episode.LengthBytes ?? resp.Content.Headers.ContentLength;
        if (total is null && existing > 0) total = existing + resp.Content.Headers.ContentLength;
        var done = existing;

        var buf = new byte[64 * 1024];
        int n;
        while ((n = await src.ReadAsync(buf, ct).ConfigureAwait(false)) > 0)
        {
            await dest.WriteAsync(buf.AsMemory(0, n), ct).ConfigureAwait(false);
            done += n;
            if (total.HasValue && total.Value > 0) progress?.Report((double)done / total.Value);
        }
        await dest.FlushAsync(ct).ConfigureAwait(false);
        dest.Close();

        System.IO.File.Move(partialPath, finalPath, overwrite: true);
        return finalPath;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }

    private static string SafeFileName(PodcastEpisode episode)
    {
        var ext = Path.GetExtension(episode.AudioUrl.AbsolutePath);
        if (string.IsNullOrEmpty(ext)) ext = ".mp3";
        var baseName = $"{episode.PublishedAtUtc:yyyy-MM-dd} - {episode.Title}";
        Span<char> buf = stackalloc char[baseName.Length];
        for (var i = 0; i < baseName.Length; i++)
            buf[i] = "*?<>|:\"\\/".Contains(baseName[i]) ? '_' : baseName[i];
        return new string(buf) + ext;
    }
}
