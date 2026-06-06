// SPDX-License-Identifier: MIT

using AetherMesh.Protocol;
using AetherMesh.Streaming;
using AetherMesh.Streaming.Models;

namespace AetherMesh.Media.Social.Tests.Helpers;

/// <summary>
/// Controllable IStreamingService stub. Tests populate <see cref="ActiveStreams"/>
/// and call <see cref="RaiseStreamAnnounced"/> / <see cref="RaiseStreamEnded"/>.
/// </summary>
internal sealed class FakeStreamingService : IStreamingService
{
    public event EventHandler<StreamSession>?             StreamAnnounced;
    public event EventHandler<StreamSession>?             StreamEnded;
    // Required by interface but not exercised in tests
    public event EventHandler<SubscriberJoinedEventArgs>? SubscriberJoined { add { } remove { } }
    public event EventHandler<SubscriberLeftEventArgs>?   SubscriberLeft   { add { } remove { } }
    public event EventHandler<StreamSegment>?             SegmentReceived  { add { } remove { } }

    public List<StreamSession> ActiveStreams { get; } = [];

    public void RaiseStreamAnnounced(StreamSession s) => StreamAnnounced?.Invoke(this, s);
    public void RaiseStreamEnded(StreamSession s)     => StreamEnded?.Invoke(this, s);

    public IReadOnlyList<StreamSession> GetActiveStreams() => ActiveStreams;

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

    public Task SubscribeAsync(Guid streamId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UnsubscribeAsync(Guid streamId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public BitrateRung? GetCurrentBitrateRung(Guid streamId) => null;

    public bool UpdateBandwidthEstimate(Guid streamId, long bandwidthKbps) => false;
}
