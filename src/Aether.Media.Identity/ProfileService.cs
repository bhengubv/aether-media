// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aether.Identity;
using Aether.Media.Core.Models;
using Aether.Protocol;
using Aether.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aether.Media.Identity;

/// <summary>
/// In-process profile store backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Profile creation/update derives the node's AetherTag from its Ed25519 public key
/// and broadcasts a ProfileSync packet so peers can update their own stores.
/// </summary>
public sealed class ProfileService : IProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    private readonly IMeshSender _sender;
    private readonly byte[] _localPublicKey;
    private readonly ConcurrentDictionary<string, MediaProfile> _profiles = new(StringComparer.Ordinal);
    private readonly ILogger<ProfileService> _logger;

    // The UHID of the local node — derived once from the public key at construction.
    private readonly string _localUhid;

    /// <param name="sender">Mesh sender used to broadcast ProfileSync packets.</param>
    /// <param name="localPublicKey">The local node's 32-byte Ed25519 identity public key.</param>
    /// <param name="logger">Optional logger.</param>
    public ProfileService(
        IMeshSender sender,
        byte[] localPublicKey,
        ILogger<ProfileService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(localPublicKey);

        if (localPublicKey.Length != 32)
            throw new ArgumentException("Ed25519 public key must be 32 bytes.", nameof(localPublicKey));

        _sender = sender;
        _localPublicKey = localPublicKey;
        _localUhid = sender.LocalUhid;
        _logger = logger ?? NullLogger<ProfileService>.Instance;
    }

    /// <inheritdoc/>
    public Task<MediaProfile> CreateProfileAsync(
        string displayName,
        string? bio,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var aetherTag = AetherTag.FromPublicKey(_localPublicKey).Value;
        var profile = new MediaProfile(
            Uhid: _localUhid,
            DisplayName: displayName.Trim(),
            AvatarHash: null,
            Bio: bio?.Trim(),
            AetherTagValue: aetherTag,
            FollowerCount: 0,
            FollowingCount: 0,
            ContentCount: 0,
            IsVerified: false,
            JoinedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        _profiles[_localUhid] = profile;
        _logger.LogInformation("Created local profile {DisplayName} tag={Tag}", displayName, aetherTag);

        return BroadcastAndReturnAsync(profile, ct);
    }

    /// <inheritdoc/>
    public Task<MediaProfile> UpdateProfileAsync(
        string uhid,
        string displayName,
        string? bio,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uhid);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (!_profiles.TryGetValue(uhid, out var existing))
            throw new InvalidOperationException($"No profile found for UHID '{uhid}'.");

        var updated = existing with
        {
            DisplayName = displayName.Trim(),
            Bio = bio?.Trim(),
        };

        _profiles[uhid] = updated;
        _logger.LogInformation("Updated profile {Uhid} displayName={Name}", uhid, displayName);

        if (string.Equals(uhid, _localUhid, StringComparison.Ordinal))
            return BroadcastAndReturnAsync(updated, ct);

        return Task.FromResult(updated);
    }

    /// <inheritdoc/>
    public Task<MediaProfile?> GetProfileAsync(string uhid, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uhid);
        _profiles.TryGetValue(uhid, out var profile);
        return Task.FromResult(profile);
    }

    /// <inheritdoc/>
    public Task<MediaProfile?> GetByAetherTagAsync(string aetherTag, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aetherTag);

        // Normalise to canonical XXXXX-XXXXX form before comparing.
        if (!AetherTag.TryParse(aetherTag, out var parsed))
            return Task.FromResult<MediaProfile?>(null);

        var canonicalTag = parsed.Value;

        foreach (var profile in _profiles.Values)
        {
            if (string.Equals(profile.AetherTagValue, canonicalTag, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<MediaProfile?>(profile);
        }

        return Task.FromResult<MediaProfile?>(null);
    }

    /// <inheritdoc/>
    public Task<MediaProfile> GetLocalProfileAsync(CancellationToken ct = default)
    {
        if (_profiles.TryGetValue(_localUhid, out var profile))
            return Task.FromResult(profile);

        throw new InvalidOperationException(
            "Local profile has not been created yet. Call CreateProfileAsync first.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Broadcasts the given profile as a ProfileSync packet and returns it.
    /// Errors during broadcast are logged but do not propagate — the profile
    /// is already stored locally.
    /// </summary>
    private async Task<MediaProfile> BroadcastAndReturnAsync(MediaProfile profile, CancellationToken ct)
    {
        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(profile, JsonOptions);
            var packet = new MeshPacket
            {
                Type = PacketType.ProfileSync,
                SourceUhid = _localUhid,
                DestinationUhid = string.Empty,   // broadcast
                Ttl = 7,
                Priority = 0,
                Payload = payload,
            };
            await _sender.BroadcastAsync(packet, ct).ConfigureAwait(false);
            _logger.LogDebug("ProfileSync broadcast sent for {Uhid}", profile.Uhid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ProfileSync broadcast failed for {Uhid} — profile stored locally", profile.Uhid);
        }

        return profile;
    }
}
