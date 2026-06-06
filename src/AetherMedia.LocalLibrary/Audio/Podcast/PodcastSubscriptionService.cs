// SPDX-License-Identifier: MIT

using System.Net.Http;
using System.Net.Http.Headers;

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>
/// Default <see cref="IPodcastSubscriptionService"/>. Holds the subscription
/// list in memory; the host shell wires this to a persistent SQLite /
/// LiteDB store as needed. Feed refresh uses <see cref="HttpClient"/> +
/// <see cref="PodcastRssParser"/>.
/// </summary>
public sealed class PodcastSubscriptionService : IPodcastSubscriptionService, IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly PodcastRssParser _parser = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, PodcastSubscription> _subs = new(StringComparer.OrdinalIgnoreCase);

    public PodcastSubscriptionService() : this(new HttpClient(), ownsHttp: true) { }

    public PodcastSubscriptionService(HttpClient http) : this(http, ownsHttp: false) { }

    private PodcastSubscriptionService(HttpClient http, bool ownsHttp)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _ownsHttp = ownsHttp;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AetherMedia", "1.0"));
    }

    /// <inheritdoc/>
    public async Task<PodcastSubscription> SubscribeAsync(Uri feedUrl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(feedUrl);
        var feed = await FetchAsync(feedUrl, ct).ConfigureAwait(false);
        var sub = new PodcastSubscription(
            FeedUrl: feedUrl,
            Title: feed.Title,
            SubscribedAtUtc: DateTimeOffset.UtcNow,
            LastRefreshedUtc: DateTimeOffset.UtcNow,
            LastSeenEpisodeGuid: feed.Episodes.FirstOrDefault()?.Guid);
        lock (_gate) _subs[feedUrl.ToString()] = sub;
        return sub;
    }

    /// <inheritdoc/>
    public Task UnsubscribeAsync(Uri feedUrl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(feedUrl);
        lock (_gate) _subs.Remove(feedUrl.ToString());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PodcastSubscription>> ListAsync(CancellationToken ct = default)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<PodcastSubscription>>(_subs.Values.ToList());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(PodcastSubscription Subscription, IReadOnlyList<PodcastEpisode> NewEpisodes)>>
        RefreshAllAsync(CancellationToken ct = default)
    {
        PodcastSubscription[] snapshot;
        lock (_gate) snapshot = _subs.Values.ToArray();

        var result = new List<(PodcastSubscription, IReadOnlyList<PodcastEpisode>)>(snapshot.Length);
        foreach (var sub in snapshot)
        {
            ct.ThrowIfCancellationRequested();
            PodcastFeed feed;
            try { feed = await FetchAsync(sub.FeedUrl, ct).ConfigureAwait(false); }
            catch (HttpRequestException) { continue; }

            var fresh = new List<PodcastEpisode>();
            foreach (var ep in feed.Episodes)
            {
                if (ep.Guid == sub.LastSeenEpisodeGuid) break; // hit the last-seen marker → stop
                fresh.Add(ep);
            }

            var updated = sub with
            {
                Title = feed.Title,
                LastRefreshedUtc = DateTimeOffset.UtcNow,
                LastSeenEpisodeGuid = feed.Episodes.FirstOrDefault()?.Guid ?? sub.LastSeenEpisodeGuid,
            };
            lock (_gate) _subs[sub.FeedUrl.ToString()] = updated;
            result.Add((updated, fresh));
        }
        return result;
    }

    private async Task<PodcastFeed> FetchAsync(Uri feedUrl, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(feedUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await _parser.ParseAsync(stream, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }
}
