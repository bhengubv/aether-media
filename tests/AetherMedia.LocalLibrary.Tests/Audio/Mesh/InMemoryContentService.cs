// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using AetherNet.Content;
using AetherNet.Content.Models;
using AetherNet.Protocol;

namespace AetherMedia.LocalLibrary.Tests.Audio.Mesh;

/// <summary>
/// Test double for <see cref="IContentService"/>. Stores published payloads
/// keyed by name, exposes the descriptor, simulates the chunk-request
/// protocol synchronously, and fires the
/// <see cref="IContentService.ContentAnnounced"/> /
/// <see cref="IContentService.ContentComplete"/> events the mesh-first
/// fetchers wait on.
/// </summary>
public sealed class InMemoryContentService : IContentService
{
    private readonly Dictionary<string, byte[]> _byHash = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContentDescriptor> _byName = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    /// <inheritdoc/>
    public event EventHandler<ContentDescriptor>? ContentAnnounced;

    /// <inheritdoc/>
    public event EventHandler<ChunkArrivedEventArgs>? ChunkReceived;

    /// <inheritdoc/>
    public event EventHandler<ContentDescriptor>? ContentComplete;

    /// <inheritdoc/>
    public Task<ContentDescriptor> PublishAsync(string name, byte[] data,
        string contentType = "application/octet-stream", int chunkSizeBytes = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(data);
        var descriptor = ContentDescriptor.FromBytes(name, data, contentType, chunkSizeBytes);
        lock (_gate)
        {
            _byHash[descriptor.RootHash] = data;
            _byName[name] = descriptor;
        }
        return Task.FromResult(descriptor);
    }

    /// <inheritdoc/>
    public Task AnnounceAsync(ContentDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        ContentAnnounced?.Invoke(this, descriptor);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task BroadcastBitmapAsync(string rootHashOrName, CancellationToken cancellationToken = default)
    {
        // Application-layer fetchers in this library use a content-key (a
        // SHA-256 of "artist|album|…") as the descriptor name and pass that
        // here, BEFORE they know the descriptor's actual root hash. Match
        // either rootHash OR name so test doubles can simulate "peer
        // announces its catalogue when asked".
        ContentDescriptor? d;
        lock (_gate)
        {
            d = _byName.TryGetValue(rootHashOrName, out var byName)
                ? byName
                : _byName.Values.FirstOrDefault(x => string.Equals(x.RootHash, rootHashOrName, StringComparison.Ordinal));
        }
        if (d is not null) ContentAnnounced?.Invoke(this, d);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RequestChunksAsync(string rootHash, IReadOnlyList<int> chunkIndices,
        string? peerUhid = null, CancellationToken cancellationToken = default)
    {
        ContentDescriptor? d;
        lock (_gate)
            d = _byName.Values.FirstOrDefault(x => string.Equals(x.RootHash, rootHash, StringComparison.Ordinal));
        if (d is null) return Task.CompletedTask;

        var complete = chunkIndices.Count >= d.ChunkCount;
        foreach (var idx in chunkIndices)
            ChunkReceived?.Invoke(this, new ChunkArrivedEventArgs
            {
                RootHash = rootHash,
                ChunkIndex = idx,
                Verified = true,
                ContentComplete = complete,
            });
        if (complete) ContentComplete?.Invoke(this, d);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<byte[]?> AssembleAsync(string rootHash, CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult(_byHash.TryGetValue(rootHash, out var d) ? (byte[]?)d : null);
    }

    /// <summary>
    /// Pre-seed the store with bytes that "another peer" has published —
    /// useful for testing the mesh-first fetch path without first
    /// publishing locally.
    /// </summary>
    public void SeedRemote(string name, byte[] data, string contentType = "application/octet-stream")
    {
        var descriptor = ContentDescriptor.FromBytes(name, data, contentType);
        lock (_gate)
        {
            _byHash[descriptor.RootHash] = data;
            _byName[name] = descriptor;
        }
    }
}
