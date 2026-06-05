// SPDX-License-Identifier: MIT

using AetherMesh.Protocol;
using AetherMesh.Streaming;
using AetherMesh.Streaming.Models;

namespace Aether.Media.Streaming.Tests.Helpers;

/// <summary>
/// Minimal IStreamingService stub for AbrController and LiveStreamPublisher tests.
/// Records bandwidth-estimate updates; all other calls are no-ops.
/// </summary>
internal sealed class FakeStreamingService : IStreamingService
{
    // ── Events ──────────────────────────────────────────────────────────────
    public event EventHandler<StreamSession>?             StreamAnnounced;
    public event EventHandler<StreamSession>?             StreamEnded;
    public event EventHandler<SubscriberJoinedEventArgs>? SubscriberJoined { add { } remove { } }
    public event EventHandler<SubscriberLeftEventArgs>?   SubscriberLeft   { add { } remove { } }
    public event EventHandler<StreamSegment>?             SegmentReceived  { add { } remove { } }

    // ── Recorded calls ───────────────────────────────────────────────────────
    public List<(Guid StreamId, long BandwidthKbps)> BandwidthEstimates { get; } = [];
    public List<Guid> SubscribeCalls   { get; } = [];
    public List<Guid> UnsubscribeCalls { get; } = [];

    // ── Helpers ───────────────────────────────────────────────────────────────
    public List<StreamSession> ActiveStreams { get; } = [];

    public void RaiseStreamAnnounced(StreamSession s) => StreamAnnounced?.Invoke(this, s);
    public void RaiseStreamEnded(StreamSession s)     => StreamEnded?.Invoke(this, s);

    // ── IStreamingService ────────────────────────────────────────────────────

    public IReadOnlyList<StreamSession> GetActiveStreams() => ActiveStreams;

    public bool UpdateBandwidthEstimate(Guid streamId, long bandwidthKbps)
    {
        BandwidthEstimates.Add((streamId, bandwidthKbps));
        return true;
    }

    public Task SubscribeAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        SubscribeCalls.Add(streamId);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        UnsubscribeCalls.Add(streamId);
        return Task.CompletedTask;
    }

    public Task<StreamSession> StartStreamAsync(
        string title, string contentType, string codec,
        int segmentDurationMs,
        StreamProfile profile = StreamProfile.ProfileB,
        CancellationToken cancellationToken = default)
    {
        var session = new StreamSession
        {
            Title = title, ContentType = contentType,
            Codec = codec, SegmentDurationMs = segmentDurationMs,
            State = StreamState.Live,
        };
        ActiveStreams.Add(session);
        return Task.FromResult(session);
    }

    public Task PublishSegmentAsync(Guid streamId, ReadOnlyMemory<byte> encoded,
        uint sequence, bool isKeyframe, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task EndStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public BitrateRung? GetCurrentBitrateRung(Guid streamId) => null;
}
