// SPDX-License-Identifier: MIT

using AetherMesh.Content;
using AetherMesh.Content.Models;
using AetherMesh.Protocol;

namespace Aether.Media.Social.Tests.Helpers;

/// <summary>
/// No-op IContentService stub with a raiseable <see cref="ContentAnnounced"/> event.
/// </summary>
internal sealed class FakeContentService : IContentService
{
    public event EventHandler<ContentDescriptor>? ContentAnnounced;
    // Required by interface but not exercised in tests
    public event EventHandler<ChunkArrivedEventArgs>? ChunkReceived { add { } remove { } }
    public event EventHandler<ContentDescriptor>?     ContentComplete { add { } remove { } }

    public void RaiseContentAnnounced(ContentDescriptor d) =>
        ContentAnnounced?.Invoke(this, d);

    public Task<ContentDescriptor> PublishAsync(
        string name, byte[] data, string contentType = "application/octet-stream",
        int chunkSizeBytes = 0, CancellationToken cancellationToken = default)
    {
        var descriptor = new ContentDescriptor
        {
            RootHash    = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data)).ToLowerInvariant(),
            Name        = name,
            TotalBytes  = data.Length,
            ContentType = contentType,
            ChunkCount  = 1,
            ChunkHashes = [],
        };
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
        => Task.FromResult<byte[]?>(null);

    public Task BroadcastBitmapAsync(string rootHash, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
