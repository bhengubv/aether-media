// SPDX-License-Identifier: MIT

using AetherMesh.Extensibility;
using AetherMesh.Handshake;
using AetherMesh.Protocol;

namespace Aether.Media.Social.Tests.Helpers;

/// <summary>
/// Controllable IHandshakeService stub. Tests call <see cref="RaisePeerNegotiated"/>
/// and pre-populate <see cref="NegotiatedPeers"/> for the seed path.
/// </summary>
internal sealed class FakeHandshakeService : IHandshakeService
{
    public event EventHandler<PeerCapabilities>?          PeerNegotiated;
    // Required by interface but not exercised in tests
    public event EventHandler<IncompatiblePeerEventArgs>? IncompatiblePeer { add { } remove { } }

    /// <summary>Pre-populated set returned by GetAllNegotiated() — simulates already-connected peers.</summary>
    public List<PeerCapabilities> NegotiatedPeers { get; } = [];

    public void RaisePeerNegotiated(PeerCapabilities caps) =>
        PeerNegotiated?.Invoke(this, caps);

    public IReadOnlyList<PeerCapabilities> GetAllNegotiated() => NegotiatedPeers;

    public Task InitiateAsync(string peerUhid, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task HandleHelloAsync(MeshPacket helloPacket, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task HandleHelloAckAsync(MeshPacket helloAckPacket, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<PeerCapabilities?> GetPeerCapabilitiesAsync(
        string peerUhid, CancellationToken cancellationToken = default)
        => Task.FromResult<PeerCapabilities?>(NegotiatedPeers.FirstOrDefault(p => p.PeerUhid == peerUhid));

    public Task RenegotiateAsync(string peerUhid, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<BiometricVerificationResult> VerifyCoPresenceAsync(
        byte[] localFaceFrameRgbHwc, int width, int height,
        FaceEmbedding referenceEmbedding, CancellationToken cancellationToken = default)
        => Task.FromResult(BiometricVerificationResult.Failed);
}
