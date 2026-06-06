// SPDX-License-Identifier: MIT

using AetherMesh.Streaming;
using AetherMesh.Streaming.Models;

namespace AetherMesh.Media.Streaming;

/// <summary>
/// Thin coordination layer on top of <see cref="IStreamingService"/> that manages
/// the full publisher lifecycle (start → segment loop → end) and tracks the current
/// viewer count via <see cref="IStreamingService.SubscriberJoined"/> /
/// <see cref="IStreamingService.SubscriberLeft"/> events.
/// </summary>
public sealed class LiveStreamPublisher : ILiveStreamPublisher
{
    // ── Events ─────────────────────────────────────────────────────────────
    public event EventHandler<int>? ViewerCountChanged;
    public event EventHandler<Exception>? PublishError;

    // ── State ──────────────────────────────────────────────────────────────
    public Guid? ActiveStreamId { get; private set; }
    public bool IsPublishing { get; private set; }
    public int ViewerCount => _viewerCount;

    private volatile int _viewerCount;
    private bool _disposed;

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IStreamingService _streaming;

    public LiveStreamPublisher(IStreamingService streaming)
    {
        _streaming = streaming ?? throw new ArgumentNullException(nameof(streaming));
        _streaming.SubscriberJoined += OnSubscriberJoined;
        _streaming.SubscriberLeft   += OnSubscriberLeft;
    }

    // ── ILiveStreamPublisher ───────────────────────────────────────────────

    public async Task<Guid> StartPublishingAsync(
        string title,
        string codec,
        IReadOnlyList<string> tags,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsPublishing)
            throw new InvalidOperationException("Already publishing. Call StopPublishingAsync first.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("title must not be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(codec))
            throw new ArgumentException("codec must not be empty.", nameof(codec));

        // Determine MIME type from codec heuristic
        var contentType = codec.Equals("opus", StringComparison.OrdinalIgnoreCase)
            ? "audio/opus"
            : $"video/{codec.ToLowerInvariant()}";

        try
        {
            var session = await _streaming.StartStreamAsync(
                title: title,
                contentType: contentType,
                codec: codec,
                segmentDurationMs: AetherMesh.Constants.ProtocolConstants.StreamSegmentDurationMs,
                profile: StreamProfile.ProfileB,
                cancellationToken: ct).ConfigureAwait(false);

            ActiveStreamId = session.Id;
            IsPublishing = true;
            _viewerCount = 0;
            return session.Id;
        }
        catch (Exception ex)
        {
            PublishError?.Invoke(this, ex);
            throw;
        }
    }

    public async Task PublishFrameAsync(
        ReadOnlyMemory<byte> encodedFrame,
        bool isKeyframe,
        uint sequence,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsPublishing || ActiveStreamId is null)
            throw new InvalidOperationException("Not currently publishing. Call StartPublishingAsync first.");

        try
        {
            await _streaming.PublishSegmentAsync(
                streamId: ActiveStreamId.Value,
                encoded: encodedFrame,
                sequence: sequence,
                isKeyframe: isKeyframe,
                cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PublishError?.Invoke(this, ex);
            throw;
        }
    }

    public async Task StopPublishingAsync(CancellationToken ct = default)
    {
        if (!IsPublishing || ActiveStreamId is null)
            return;

        var streamId = ActiveStreamId.Value;
        IsPublishing = false;
        ActiveStreamId = null;
        _viewerCount = 0;

        try
        {
            await _streaming.EndStreamAsync(streamId, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Best-effort — stream already marked ended locally
            PublishError?.Invoke(this, ex);
        }
    }

    // ── IAsyncDisposable ───────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _streaming.SubscriberJoined -= OnSubscriberJoined;
        _streaming.SubscriberLeft   -= OnSubscriberLeft;

        if (IsPublishing)
            await StopPublishingAsync().ConfigureAwait(false);
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void OnSubscriberJoined(object? sender, SubscriberJoinedEventArgs args)
    {
        if (ActiveStreamId is null || args.StreamId != ActiveStreamId.Value)
            return;

        var count = Interlocked.Increment(ref _viewerCount);
        ViewerCountChanged?.Invoke(this, count);
    }

    private void OnSubscriberLeft(object? sender, SubscriberLeftEventArgs args)
    {
        if (ActiveStreamId is null || args.StreamId != ActiveStreamId.Value)
            return;

        var count = Interlocked.Decrement(ref _viewerCount);
        if (count < 0) Interlocked.CompareExchange(ref _viewerCount, 0, count);
        ViewerCountChanged?.Invoke(this, Math.Max(0, count));
    }
}
