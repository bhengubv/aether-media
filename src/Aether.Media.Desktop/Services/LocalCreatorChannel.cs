using Aether.Media.Core;
using Aether.Media.Core.Models;
using Aether.Media.Identity;

namespace Aether.Media.Desktop.Services;

/// <summary>
/// Desktop implementation of <see cref="ICreatorChannel"/>.
/// Delegates profile lookups to <see cref="IProfileService"/> and content lookups
/// to <see cref="IMediaLibrary"/>.  Subscribe/unsubscribe state is held in-process.
/// </summary>
public sealed class LocalCreatorChannel : ICreatorChannel
{
    private readonly IProfileService _profiles;
    private readonly IMediaLibrary _library;
    private readonly HashSet<string> _subscriptions = new(StringComparer.Ordinal);

    public LocalCreatorChannel(IProfileService profiles, IMediaLibrary library)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _library  = library  ?? throw new ArgumentNullException(nameof(library));
    }

    public async Task<MediaProfile> GetProfileAsync(string creatorUhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);

        var profile = await _profiles.GetProfileAsync(creatorUhid, ct).ConfigureAwait(false);

        if (profile is not null)
            return profile;

        // Return a minimal anonymous profile when no record exists
        return new MediaProfile(
            Uhid: creatorUhid,
            DisplayName: creatorUhid,
            AvatarHash: null,
            Bio: null,
            AetherTagValue: $"@{creatorUhid}",
            FollowerCount: 0,
            FollowingCount: 0,
            ContentCount: 0,
            IsVerified: false,
            JoinedAt: DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<MediaContent>> GetContentAsync(
        string creatorUhid,
        int limit = 20,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);

        var all = await _library.GetByCreatorAsync(creatorUhid, ct).ConfigureAwait(false);
        return limit <= 0 || limit >= all.Count
            ? all
            : all.Take(limit).ToList();
    }

    public Task<IReadOnlyList<LiveStream>> GetLiveStreamsAsync(
        string creatorUhid,
        CancellationToken ct = default)
    {
        // No live stream discovery without a mesh; return empty.
        return Task.FromResult<IReadOnlyList<LiveStream>>(Array.Empty<LiveStream>());
    }

    public Task SubscribeAsync(string creatorUhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);
        lock (_subscriptions)
            _subscriptions.Add(creatorUhid);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string creatorUhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);
        lock (_subscriptions)
            _subscriptions.Remove(creatorUhid);
        return Task.CompletedTask;
    }

    public Task<bool> IsSubscribedAsync(string creatorUhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(creatorUhid);
        lock (_subscriptions)
            return Task.FromResult(_subscriptions.Contains(creatorUhid));
    }
}
