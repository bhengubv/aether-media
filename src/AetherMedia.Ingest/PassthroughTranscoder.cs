// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// Identity transcoder: the segment passes through unchanged as a single rung. Always available on
/// every node and every platform — it is the guaranteed floor beneath the optional real transcoders.
/// </summary>
public sealed class PassthroughTranscoder : ITranscoder
{
    /// <inheritdoc />
    public bool CanNormalize(string codec, TargetProfile target) => true;

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<MediaSegment>> NormalizeAsync(
        MediaSegment segment, TargetProfile target, CancellationToken ct = default)
        => new(new[] { segment });
}
