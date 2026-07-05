// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// The ingest seam: pull an external source and yield <see cref="MediaSegment"/>s. One contract —
/// any protocol (HLS today; DASH, LL-HLS, WebRTC-WHEP, RTMP/SRT, file, capture later) implements it
/// with zero change to the gateway or the mesh. Portable to every SDK language: it is platform I/O
/// producing the shared segment IR, not new wire bytes.
/// </summary>
public interface ISourceAdapter
{
    /// <summary>True when this adapter can read the given source.</summary>
    bool CanHandle(SourceDescriptor source);

    /// <summary>
    /// Read the source, yielding segments until it ends (VOD / end-of-list) or
    /// <paramref name="ct"/> is cancelled.
    /// </summary>
    IAsyncEnumerable<MediaSegment> ReadAsync(SourceDescriptor source, CancellationToken ct = default);
}
