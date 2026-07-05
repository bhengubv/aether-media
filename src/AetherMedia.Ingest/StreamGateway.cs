// SPDX-License-Identifier: MIT

using AetherMedia.Streaming;

namespace AetherMedia.Ingest;

/// <summary>
/// The ingest gateway role: pull an external source through a matching <see cref="ISourceAdapter"/>,
/// pass it through (default, zero-cost) or — when the node is capable — transcode it toward the
/// target ladder, and publish it onto the mesh via <see cref="ILiveStreamPublisher"/>. It is a role
/// any node can assume, not a machine.
/// </summary>
public sealed class StreamGateway : IStreamGateway
{
    private readonly IReadOnlyList<ISourceAdapter> _adapters;
    private readonly ILiveStreamPublisher _publisher;
    private readonly ITranscoder _transcoder;

    public StreamGateway(
        IEnumerable<ISourceAdapter> adapters,
        ILiveStreamPublisher publisher,
        ITranscoder transcoder)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        _adapters = adapters as IReadOnlyList<ISourceAdapter> ?? adapters.ToList();
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _transcoder = transcoder ?? throw new ArgumentNullException(nameof(transcoder));
    }

    /// <inheritdoc />
    public async Task<IngestSession> StartAsync(
        SourceDescriptor source, GatewayOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        var adapter = _adapters.FirstOrDefault(a => a.CanHandle(source))
            ?? throw new NotSupportedException(
                $"No source adapter handles this source ({source.Kind}: {source.Uri}).");

        var streamId = await _publisher
            .StartPublishingAsync(source.Title, options.Target.BaselineVideoCodec, source.Tags, ct)
            .ConfigureAwait(false);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pump = PumpAsync(adapter, source, options, cts.Token);
        return new IngestSession(streamId, pump, cts);
    }

    private async Task PumpAsync(
        ISourceAdapter adapter, SourceDescriptor source, GatewayOptions options, CancellationToken ct)
    {
        uint sequence = 0;
        try
        {
            await foreach (var segment in adapter.ReadAsync(source, ct).ConfigureAwait(false))
            {
                foreach (var rung in await NormalizeAsync(segment, options, ct).ConfigureAwait(false))
                {
                    await _publisher.PublishFrameAsync(rung.Payload, rung.IsKeyframe, sequence, ct)
                        .ConfigureAwait(false);
                    sequence++;
                }
            }
        }
        finally
        {
            // Always end the mesh stream, whether the source finished or the pump was cancelled.
            await _publisher.StopPublishingAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async ValueTask<IReadOnlyList<MediaSegment>> NormalizeAsync(
        MediaSegment segment, GatewayOptions options, CancellationToken ct)
    {
        var ladderRequested = options.Target.RungsKbps.Count > 0;
        var codecOk = options.Capabilities.CanPassthrough(segment.Codec);
        var wantTranscode = ladderRequested || !codecOk;

        if (wantTranscode
            && options.Capabilities.CanTranscode
            && _transcoder.CanNormalize(segment.Codec, options.Target))
        {
            return await _transcoder.NormalizeAsync(segment, options.Target, ct).ConfigureAwait(false);
        }

        // Floor: this node lacks transcode muscle (or none is needed) — carry the source's native
        // rendition unchanged. Always watchable; richer rungs light up when a capable node is present.
        return new[] { segment };
    }
}
