// SPDX-License-Identifier: MIT

using AetherMesh.Models;
using AetherMesh.Protocol;
using AetherMesh.Routing;

namespace AetherMesh.Media.Social.Tests.Helpers;

/// <summary>
/// Configurable IMeshSender stub that captures sent packets and broadcasts.
/// Tests can pre-populate <see cref="Peers"/> to simulate connected nodes.
/// </summary>
internal sealed class FakeMeshSender : IMeshSender
{
    public string LocalUhid { get; set; } = "local-test-node";
    public string? LocalGeohash => null;

    /// <summary>Peers reported as connected. Tests may add peers here before calling code under test.</summary>
    public List<PeerInfo> Peers { get; } = [];

    /// <summary>All packets passed to <see cref="SendAsync"/>.</summary>
    public List<(MeshPacket Packet, string NextHop)> SentPackets { get; } = [];

    /// <summary>All packets passed to <see cref="BroadcastAsync"/>.</summary>
    public List<MeshPacket> BroadcastedPackets { get; } = [];

    public IReadOnlyList<PeerInfo> GetConnectedPeers() => Peers;

    public Task<bool> SendAsync(MeshPacket packet, string nextHopUhid,
        CancellationToken cancellationToken = default)
    {
        SentPackets.Add((packet, nextHopUhid));
        return Task.FromResult(true);
    }

    public Task<int> BroadcastAsync(MeshPacket packet, CancellationToken cancellationToken = default)
    {
        BroadcastedPackets.Add(packet);
        return Task.FromResult(Peers.Count);
    }
}
