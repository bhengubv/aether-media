// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// Normalises a segment toward a <see cref="TargetProfile"/> — one segment in, one-or-more ABR
/// rungs out. The identity implementation is <see cref="PassthroughTranscoder"/>; real transcoders
/// (FFmpeg, MediaCodec, VideoToolbox, WebCodecs) are per-platform capability plugins, optional and
/// capability-gated — never required for a node to gateway.
/// </summary>
public interface ITranscoder
{
    /// <summary>True when this transcoder can normalise <paramref name="codec"/> toward <paramref name="target"/>.</summary>
    bool CanNormalize(string codec, TargetProfile target);

    /// <summary>
    /// Normalise one segment into one-or-more rungs. Passthrough returns the segment unchanged as a
    /// single rung.
    /// </summary>
    ValueTask<IReadOnlyList<MediaSegment>> NormalizeAsync(
        MediaSegment segment, TargetProfile target, CancellationToken ct = default);
}
