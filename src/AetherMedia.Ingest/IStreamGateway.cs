// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// The ingest gateway role: pull a source through a matching <see cref="ISourceAdapter"/>, pass it
/// through or (when the node is capable) transcode it, and publish it onto the mesh. It is a role,
/// not a machine — any node holding both an internet path and the mesh can run it, on any platform.
/// </summary>
public interface IStreamGateway
{
    /// <summary>
    /// Begin ingesting <paramref name="source"/> and publishing it to the mesh. Returns immediately
    /// with a session whose <see cref="IngestSession.StreamId"/> is already live; the pump runs until
    /// the source ends or the session is stopped.
    /// </summary>
    Task<IngestSession> StartAsync(
        SourceDescriptor source, GatewayOptions options, CancellationToken ct = default);
}
