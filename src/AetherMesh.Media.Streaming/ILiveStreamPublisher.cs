// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Streaming;

/// <summary>
/// Controls a single live stream published by the local node.
///
/// <para>
/// Lifecycle: <see cref="StartPublishingAsync"/> → repeated <see cref="PublishFrameAsync"/>
/// calls → <see cref="StopPublishingAsync"/>.  Disposing the publisher while
/// <see cref="IsPublishing"/> is true automatically calls <see cref="StopPublishingAsync"/>.
/// </para>
/// </summary>
public interface ILiveStreamPublisher : IAsyncDisposable
{
    /// <summary>The stream id returned by <see cref="StartPublishingAsync"/>, or null before publishing begins.</summary>
    Guid? ActiveStreamId { get; }

    /// <summary>True between a successful <see cref="StartPublishingAsync"/> and <see cref="StopPublishingAsync"/>.</summary>
    bool IsPublishing { get; }

    /// <summary>Current count of mesh peers subscribed to this stream.</summary>
    int ViewerCount { get; }

    /// <summary>Raised whenever <see cref="ViewerCount"/> changes.</summary>
    event EventHandler<int>? ViewerCountChanged;

    /// <summary>Raised when an unrecoverable publishing error occurs.</summary>
    event EventHandler<Exception>? PublishError;

    /// <summary>
    /// Begin publishing a new stream with the given metadata.
    /// </summary>
    /// <param name="title">Human-readable stream title.</param>
    /// <param name="codec">Codec name (e.g. "h264", "opus", "av1").</param>
    /// <param name="tags">Searchable tags for discovery.</param>
    /// <returns>The globally unique stream id.</returns>
    Task<Guid> StartPublishingAsync(
        string title,
        string codec,
        IReadOnlyList<string> tags,
        CancellationToken ct = default);

    /// <summary>
    /// Publish a single encoded segment to all current subscribers.
    /// </summary>
    /// <param name="encodedFrame">Codec-encoded bytes.</param>
    /// <param name="isKeyframe">True when this is a random-access (IDR) frame.</param>
    /// <param name="sequence">Monotonically increasing sequence number (wraps at uint32 max).</param>
    Task PublishFrameAsync(
        ReadOnlyMemory<byte> encodedFrame,
        bool isKeyframe,
        uint sequence,
        CancellationToken ct = default);

    /// <summary>Signal end-of-stream and clean up subscriber state.</summary>
    Task StopPublishingAsync(CancellationToken ct = default);
}
