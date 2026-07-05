// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// What the local node can do with media — drives the passthrough-vs-transcode decision (and,
/// later, delegation of transcode to a capable peer). Ingress itself is universal: a node with no
/// transcode muscle still gateways by passing bytes through, so the answer to "can this node be the
/// gateway?" is always yes.
/// </summary>
public sealed record NodeCapabilities
{
    /// <summary>Codecs this node carries to subscribers as-is (the mesh baseline it participates in).</summary>
    public IReadOnlySet<string> PassthroughCodecs { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "h264", "hevc", "av1", "vp8", "vp9", "aac", "opus", "mp3", "webvtt",
        };

    /// <summary>True when this node can transcode (FFmpeg / hardware encoders / WebCodecs present).</summary>
    public bool CanTranscode { get; init; }

    /// <summary>Whether <paramref name="codec"/> can be carried through unchanged.</summary>
    public bool CanPassthrough(string codec) => PassthroughCodecs.Contains(codec);

    /// <summary>The universal floor: any node can pass bytes through the mesh, none can transcode.</summary>
    public static NodeCapabilities PassthroughOnly { get; } = new();
}
