// SPDX-License-Identifier: MIT

using AetherMesh.Content;
using AetherMesh.Content.Models;
using AetherMesh.Protocol;

namespace AetherMesh.Media.Identity.Tests.Helpers;

/// <summary>
/// Minimal IContentService stub for AvatarService tests.
/// <see cref="PublishAsync"/> returns a deterministic descriptor; all other
/// methods are no-ops.
/// <see cref="AssembleResult"/> controls what <see cref="AssembleAsync"/> returns.
/// </summary>
internal sealed class FakeContentService : IContentService
{
    public event EventHandler<ContentDescriptor>? ContentAnnounced;
    public event EventHandler<ChunkArrivedEventArgs>? ChunkReceived  { add { } remove { } }
    public event EventHandler<ContentDescriptor>?     ContentComplete { add { } remove { } }

    /// <summary>Byte array returned by <see cref="AssembleAsync"/>. Null = not yet assembled.</summary>
    public byte[]? AssembleResult { get; set; }

    public Task<ContentDescriptor> PublishAsync(
        string name, byte[] data, string contentType = "application/octet-stream",
        int chunkSizeBytes = 0, CancellationToken cancellationToken = default)
    {
        var descriptor = new ContentDescriptor
        {
            RootHash    = Convert.ToHexString(
                              System.Security.Cryptography.SHA256.HashData(data))
                              .ToLowerInvariant(),
            Name        = name,
            TotalBytes  = data.Length,
            ContentType = contentType,
            ChunkCount  = 1,
            ChunkHashes = [],
        };
        ContentAnnounced?.Invoke(this, descriptor);
        return Task.FromResult(descriptor);
    }

    public Task AnnounceAsync(ContentDescriptor descriptor, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RequestChunksAsync(string rootHash, IReadOnlyList<int> chunkIndices,
        string? peerUhid = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<byte[]?> AssembleAsync(string rootHash, CancellationToken cancellationToken = default)
        => Task.FromResult(AssembleResult);

    public Task BroadcastBitmapAsync(string rootHash, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
