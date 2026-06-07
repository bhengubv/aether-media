// SPDX-License-Identifier: MIT

using AetherNet.Protocol;
using AetherNet.Streaming;
using AetherNet.Streaming.Models;

namespace AetherMedia.LocalLibrary.Tests.Audio.Mesh;

/// <summary>
/// Test double for <see cref="IStreamingService"/>. Records every segment
/// published per session and exposes them as
/// <see cref="GetPublishedSegments"/> so tests can run the
/// <see cref="LocalLibrary.Audio.Mesh.MeshInvariants.StreamSequenceMonotonic"/>
/// assertion.
/// </summary>
public sealed class InMemoryStreamingService : IStreamingService
{
    private readonly Dictionary<Guid, StreamSession> _sessions = new();
    private readonly Dictionary<Guid, List<RecordedSegment>> _segments = new();
    private readonly object _gate = new();

    // The test double only emits StreamAnnounced + StreamEnded. The other
    // events are part of the contract but irrelevant to invariants we
    // assert here — suppress the "never used" diagnostic.
#pragma warning disable CS0067
    public event EventHandler<StreamSession>? StreamAnnounced;
    public event EventHandler<SubscriberJoinedEventArgs>? SubscriberJoined;
    public event EventHandler<SubscriberLeftEventArgs>? SubscriberLeft;
    public event EventHandler<StreamSegment>? SegmentReceived;
    public event EventHandler<StreamSession>? StreamEnded;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public Task<StreamSession> StartStreamAsync(string title, string contentType, string codec,
        int segmentDurationMs, StreamProfile profile = StreamProfile.ProfileB,
        CancellationToken cancellationToken = default)
    {
        var session = new StreamSession
        {
            Id = Guid.NewGuid(),
            PublisherUhid = "self",
            Title = title,
            ContentType = contentType,
            Codec = codec,
            SegmentDurationMs = segmentDurationMs,
            State = StreamState.Live,
        };
        lock (_gate)
        {
            _sessions[session.Id] = session;
            _segments[session.Id] = new List<RecordedSegment>();
        }
        StreamAnnounced?.Invoke(this, session);
        return Task.FromResult(session);
    }

    /// <inheritdoc/>
    public Task PublishSegmentAsync(Guid streamId, ReadOnlyMemory<byte> encoded, uint sequence,
        bool isKeyframe, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_segments.TryGetValue(streamId, out var list))
                throw new InvalidOperationException("Stream not started.");
            list.Add(new RecordedSegment(sequence, isKeyframe, encoded.ToArray()));
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EndStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        StreamSession? session;
        lock (_gate)
        {
            if (!_sessions.TryGetValue(streamId, out session)) return Task.CompletedTask;
            session.State = StreamState.Ended;
        }
        StreamEnded?.Invoke(this, session);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SubscribeAsync(Guid streamId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task UnsubscribeAsync(Guid streamId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public IReadOnlyList<StreamSession> GetActiveStreams()
    {
        lock (_gate) return _sessions.Values.Where(s => s.State == StreamState.Live).ToList();
    }

    /// <inheritdoc/>
    public BitrateRung? GetCurrentBitrateRung(Guid streamId) => null;

    /// <inheritdoc/>
    public bool UpdateBandwidthEstimate(Guid streamId, long bandwidthKbps) => false;

    /// <summary>All segments published for a stream — in publish order.</summary>
    public IReadOnlyList<RecordedSegment> GetPublishedSegments(Guid streamId)
    {
        lock (_gate)
            return _segments.TryGetValue(streamId, out var list) ? list.ToList() : Array.Empty<RecordedSegment>();
    }

    public sealed record RecordedSegment(uint Sequence, bool IsKeyframe, byte[] Encoded);
}
