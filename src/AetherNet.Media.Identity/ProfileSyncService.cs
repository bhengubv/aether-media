// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Text.Json;
using AetherNet.Media.Core;
using AetherNet.Media.Core.Models;
using AetherNet.Protocol;
using AetherNet.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherNet.Media.Identity;

/// <summary>
/// Handles inbound ProfileSync (type 23) packets and keeps a local cache of
/// remote profiles updated. Calls <see cref="IProfileService.GetLocalProfileAsync"/>
/// to obtain the local profile for outbound sync broadcasts.
/// </summary>
public sealed class ProfileSyncService : IProfileSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IProfileService _profileService;
    private readonly IMeshSender _sender;
    private readonly ConcurrentDictionary<string, MediaProfile> _remoteProfiles
        = new(StringComparer.Ordinal);
    private readonly ILogger<ProfileSyncService> _logger;
    private readonly FootprintGuard? _guard;

    public event EventHandler<MediaProfile>? ProfileReceived;

    /// <param name="profileService">Local profile access for outbound sync.</param>
    /// <param name="sender">Mesh sender for broadcasting the local profile.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="guard">Optional footprint guard; when present, skips sync in passive mode.</param>
    public ProfileSyncService(
        IProfileService profileService,
        IMeshSender sender,
        ILogger<ProfileSyncService>? logger = null,
        FootprintGuard? guard = null)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? NullLogger<ProfileSyncService>.Instance;
        _guard  = guard;
    }

    /// <inheritdoc/>
    public async Task SyncLocalProfileAsync(CancellationToken ct = default)
    {
        if (_guard is { MeshScanAllowed: false })
        {
            _logger.LogDebug("ProfileSync skipped — passive mode or metered connection");
            return;
        }

        MediaProfile local;
        try
        {
            local = await _profileService.GetLocalProfileAsync(ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "SyncLocalProfile: local profile not available, skipping broadcast");
            return;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(local, JsonOptions);
        var packet = new MeshPacket
        {
            Type = PacketType.ProfileSync,
            SourceUhid = _sender.LocalUhid,
            DestinationUhid = string.Empty,    // broadcast
            Ttl = 7,
            Priority = 0,
            Payload = payload,
        };

        var delivered = await _sender.BroadcastAsync(packet, ct).ConfigureAwait(false);
        _logger.LogDebug("ProfileSync sent local profile to {Count} peer(s)", delivered);
    }

    /// <inheritdoc/>
    public Task HandleSyncPacketAsync(MeshPacket packet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.Type != PacketType.ProfileSync)
        {
            _logger.LogDebug("ProfileSyncService ignoring non-ProfileSync packet type {Type}", packet.Type);
            return Task.CompletedTask;
        }

        if (packet.Payload is null || packet.Payload.Length == 0)
        {
            _logger.LogWarning("ProfileSync packet {Id} from {Source} has empty payload — ignored", packet.Id, packet.SourceUhid);
            return Task.CompletedTask;
        }

        MediaProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<MediaProfile>(packet.Payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "ProfileSync packet {Id} from {Source}: JSON parse failed", packet.Id, packet.SourceUhid);
            return Task.CompletedTask;
        }

        if (profile is null)
        {
            _logger.LogWarning("ProfileSync packet {Id} from {Source} deserialised to null", packet.Id, packet.SourceUhid);
            return Task.CompletedTask;
        }

        // Ignore malformed profiles missing a UHID.
        if (string.IsNullOrWhiteSpace(profile.Uhid))
        {
            _logger.LogWarning("ProfileSync packet {Id}: profile has no UHID — dropped", packet.Id);
            return Task.CompletedTask;
        }

        // Ignore our own profile bounced back from the mesh.
        if (string.Equals(profile.Uhid, _sender.LocalUhid, StringComparison.Ordinal))
            return Task.CompletedTask;

        _remoteProfiles[profile.Uhid] = profile;
        _logger.LogInformation("ProfileSync received {DisplayName} ({Uhid}) tag={Tag}",
            profile.DisplayName, profile.Uhid, profile.AetherNetTagValue);

        ProfileReceived?.Invoke(this, profile);
        return Task.CompletedTask;
    }
}
