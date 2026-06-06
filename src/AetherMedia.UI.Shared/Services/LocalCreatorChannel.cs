// SPDX-License-Identifier: MIT

using AetherMedia.Core;
using AetherMedia.Core.Models;
using AetherMedia.Identity;

namespace AetherMedia.UI.Shared.Services;

/// <summary>Cross-platform implementation of <see cref="ICreatorChannel"/>.</summary>
public sealed class LocalCreatorChannel : ICreatorChannel
{
    private readonly IProfileService _profiles;
    private readonly IMediaLibrary   _library;
    private readonly HashSet<string> _subscriptions = new(StringComparer.Ordinal);

    public LocalCreatorChannel(IProfileService profiles, IMediaLibrary library)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _library  = library  ?? throw new ArgumentNullException(nameof(library));
    }

    public async Task<MediaProfile> GetProfileAsync(string creatorUhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);
        var p = await _profiles.GetProfileAsync(creatorUhid, ct).ConfigureAwait(false);
        if (p is not null) return p;

        return new MediaProfile(creatorUhid, creatorUhid, null, null, $"@{creatorUhid}", 0, 0, 0, false, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public async Task<IReadOnlyList<MediaContent>> GetContentAsync(
        string creatorUhid, int limit = 20, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);
        var all = await _library.GetByCreatorAsync(creatorUhid, ct).ConfigureAwait(false);
        return limit <= 0 || limit >= all.Count ? all : all.Take(limit).ToList();
    }

    public Task<IReadOnlyList<LiveStream>> GetLiveStreamsAsync(string creatorUhid, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<LiveStream>>(Array.Empty<LiveStream>());

    public Task SubscribeAsync(string creatorUhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);
        lock (_subscriptions) _subscriptions.Add(creatorUhid);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string creatorUhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);
        lock (_subscriptions) _subscriptions.Remove(creatorUhid);
        return Task.CompletedTask;
    }

    public Task<bool> IsSubscribedAsync(string creatorUhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);
        lock (_subscriptions) return Task.FromResult(_subscriptions.Contains(creatorUhid));
    }
}
