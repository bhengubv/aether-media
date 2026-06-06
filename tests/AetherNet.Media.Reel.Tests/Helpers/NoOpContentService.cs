// SPDX-License-Identifier: MIT

using AetherNet.Content;
using AetherNet.Content.Models;
using AetherNet.Protocol;

namespace AetherNet.Media.Reel.Tests.Helpers;

/// <summary>
/// Minimal no-op <see cref="IContentService"/> stub for unit tests.
/// Records calls to <see cref="Published"/> and <see cref="Announced"/> for
/// assertion purposes.
/// </summary>
internal sealed class NoOpContentService : IContentService
{
    public List<ContentDescriptor> Published { get; } = [];
    public List<ContentDescriptor> Announced { get; } = [];

    // Byte array to return from AssembleAsync — null means "not found"
    public byte[]? AssembleResult { get; set; }

#pragma warning disable CS0067
    public event EventHandler<ChunkArrivedEventArgs>? ChunkReceived;
    public event EventHandler<ContentDescriptor>?     ContentAnnounced;
    public event EventHandler<ContentDescriptor>?     ContentComplete;
#pragma warning restore CS0067

    public Task<ContentDescriptor> PublishAsync(
        string name, byte[] data, string contentType = "application/octet-stream",
        int chunkSizeBytes = 0, CancellationToken cancellationToken = default)
    {
        var descriptor = ContentDescriptor.FromBytes(name, data, contentType);
        Published.Add(descriptor);
        return Task.FromResult(descriptor);
    }

    public Task AnnounceAsync(ContentDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        Announced.Add(descriptor);
        return Task.CompletedTask;
    }

    public Task RequestChunksAsync(string rootHash, IReadOnlyList<int> chunkIndices,
        string? peerUhid = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<byte[]?> AssembleAsync(string rootHash,
        CancellationToken cancellationToken = default)
        => Task.FromResult(AssembleResult);

    public Task BroadcastBitmapAsync(string rootHash, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
