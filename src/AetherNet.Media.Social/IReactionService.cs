// SPDX-License-Identifier: MIT

using AetherNet.Media.Core.Models;
using AetherNet.Protocol;

namespace AetherNet.Media.Social;

/// <summary>
/// Send and receive <see cref="MediaReaction"/> events over the Aether mesh.
/// Reactions travel as <see cref="PacketType.WatchReaction"/> packets addressed
/// to the content creator's UHID.
/// </summary>
public interface IReactionService
{
    /// <summary>Raised when a reaction packet arrives from any peer for any content.</summary>
    event EventHandler<MediaReaction>? ReactionReceived;

    /// <summary>
    /// Serialise and send <paramref name="reaction"/> to the content creator via the mesh.
    /// </summary>
    Task SendReactionAsync(MediaReaction reaction, CancellationToken ct = default);

    /// <summary>
    /// Returns all reactions stored in memory for <paramref name="contentHash"/>, newest first.
    /// </summary>
    Task<IReadOnlyList<MediaReaction>> GetReactionsAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Deserialise and process an inbound <see cref="PacketType.WatchReaction"/> packet.</summary>
    Task HandlePacketAsync(MeshPacket packet, CancellationToken ct = default);
}
