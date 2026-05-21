// SPDX-License-Identifier: MIT

using Aether.Models;
using Aether.Protocol;
using Aether.Routing;

namespace Aether.Media.Identity.Tests.Helpers;

/// <summary>Configurable IMeshSender stub that records sent and broadcast packets.</summary>
internal sealed class FakeMeshSender : IMeshSender
{
    public string  LocalUhid    { get; set; } = "local-test-uhid";
    public string? LocalGeohash => null;

    public List<PeerInfo>                          Peers              { get; } = [];
    public List<(MeshPacket Packet, string NextHop)> SentPackets      { get; } = [];
    public List<MeshPacket>                        BroadcastedPackets { get; } = [];

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
