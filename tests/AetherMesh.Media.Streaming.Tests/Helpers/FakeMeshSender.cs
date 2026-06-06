// SPDX-License-Identifier: MIT

using AetherMesh.Models;
using AetherMesh.Protocol;
using AetherMesh.Routing;

namespace AetherMesh.Media.Streaming.Tests.Helpers;

/// <summary>Minimal IMeshSender stub for WatchPartyCoordinator tests.</summary>
internal sealed class FakeMeshSender : IMeshSender
{
    public string  LocalUhid    { get; set; } = "test-host-uhid";
    public string? LocalGeohash => null;

    public List<(MeshPacket Packet, string NextHop)> SentPackets    { get; } = [];
    public List<MeshPacket>                          BroadcastedPackets { get; } = [];

    public IReadOnlyList<PeerInfo> GetConnectedPeers() => Array.Empty<PeerInfo>();

    public Task<bool> SendAsync(MeshPacket packet, string nextHopUhid, CancellationToken ct = default)
    {
        SentPackets.Add((packet, nextHopUhid));
        return Task.FromResult(true);
    }

    public Task<int> BroadcastAsync(MeshPacket packet, CancellationToken ct = default)
    {
        BroadcastedPackets.Add(packet);
        return Task.FromResult(0);
    }
}
