// SPDX-License-Identifier: MIT

using AetherNet.Media.Core.Models;
using AetherNet.Protocol;

namespace AetherNet.Media.Identity;

/// <summary>
/// Synchronises local and remote creator profiles over the Aether mesh via
/// ProfileSync (packet type 23) packets. Incoming packets are deserialised into
/// <see cref="MediaProfile"/> records and stored in the profile dictionary;
/// outbound sync broadcasts the local node's own profile to all connected peers.
/// </summary>
public interface IProfileSyncService
{
    /// <summary>Broadcast the local node's profile via a ProfileSync MeshPacket.</summary>
    Task SyncLocalProfileAsync(CancellationToken ct = default);

    /// <summary>
    /// Ingest an inbound ProfileSync packet. Deserialises the payload, stores the
    /// profile, and fires <see cref="ProfileReceived"/>.
    /// </summary>
    Task HandleSyncPacketAsync(MeshPacket packet, CancellationToken ct = default);

    /// <summary>Fired each time a remote profile arrives and is stored.</summary>
    event EventHandler<MediaProfile>? ProfileReceived;
}
