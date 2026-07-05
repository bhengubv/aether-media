// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// The normalisation target: the baseline codecs and the ABR ladder a gateway should produce.
/// <see cref="Passthrough"/> is the always-available floor — no transcode, carry the source's own
/// rendition unchanged.
/// </summary>
public sealed record TargetProfile
{
    /// <summary>Baseline video codec every subscriber can decode.</summary>
    public string BaselineVideoCodec { get; init; } = "h264";

    /// <summary>Baseline audio codec every subscriber can decode.</summary>
    public string BaselineAudioCodec { get; init; } = "aac";

    /// <summary>Bitrate rungs (Kbps) to emit. Empty means keep the source's own rung(s).</summary>
    public IReadOnlyList<int> RungsKbps { get; init; } = [];

    /// <summary>The floor: carry the source rendition as-is, no transcode.</summary>
    public static TargetProfile Passthrough { get; } = new();
}
