// SPDX-License-Identifier: MIT

using AetherNet.Extensibility;
using AetherNet.Forge;
using AetherNet.Forge.Models;
using AetherNet.Protocol;

namespace AetherMedia.LocalLibrary.Tests.Audio.Mesh;

/// <summary>Test double for <see cref="IForgeService"/>.</summary>
public sealed class InMemoryForgeService : IForgeService
{
    private readonly Dictionary<string, ForgeEntry> _byId = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public event EventHandler<ForgeEntry>? NewEntryAnnounced;

    public Task<ForgeEntry?> QueryAsync(string packageId, CancellationToken ct = default)
    {
        lock (_gate)
            return Task.FromResult(_byId.TryGetValue(packageId, out var e) ? e : null);
    }

    public Task<ForgeEntry> CacheAsync(string packageId, string contentHash, long sizeBytes, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_byId.TryGetValue(packageId, out var existing)) return Task.FromResult(existing);
            var entry = new ForgeEntry
            {
                PackageId = packageId,
                ContentHash = contentHash,
                SizeBytes = sizeBytes,
            };
            _byId[packageId] = entry;
            NewEntryAnnounced?.Invoke(this, entry);
            return Task.FromResult(entry);
        }
    }

    public Task<ForgeEntry?> FetchAsync(string packageId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_byId.TryGetValue(packageId, out var entry)) return Task.FromResult<ForgeEntry?>(entry);
            return Task.FromResult<ForgeEntry?>(null);
        }
    }

    public Task<ForgeStats> GetStatsAsync(CancellationToken ct = default)
    {
        lock (_gate)
            return Task.FromResult(new ForgeStats { CatalogueSize = _byId.Count });
    }
}

/// <summary>Test double for <see cref="IAetherNetIncentiveProvider"/>.</summary>
public sealed class RecordingIncentiveProvider : IAetherNetIncentiveProvider
{
    private readonly object _gate = new();
    private readonly List<(string RelayNode, MeshPacket Packet)> _relays = new();

    public IReadOnlyList<(string RelayNode, MeshPacket Packet)> Relays
    {
        get { lock (_gate) return _relays.ToList(); }
    }

    public Task RecordRelayAsync(string relayNodeUhid, MeshPacket packet, CancellationToken cancellationToken = default)
    {
        lock (_gate) _relays.Add((relayNodeUhid, packet));
        return Task.CompletedTask;
    }

    public Task<bool> ShouldPrioritizeAsync(MeshPacket packet, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
