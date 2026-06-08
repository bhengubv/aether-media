// SPDX-License-Identifier: MIT

using AetherNet.Dtn;
using AetherNet.Models;
using AetherNet.Protocol;

namespace AetherMedia.LocalLibrary.Tests.Audio.Mesh;

/// <summary>
/// Test double for <see cref="IDtnService"/>. Stores bundles in an
/// in-memory list and provides <see cref="DeliverAllAsync"/> +
/// <see cref="ExpireAllAsync"/> hooks so tests can drive the lifecycle
/// deterministically and assert against
/// <see cref="LocalLibrary.Audio.Mesh.MeshInvariants"/>.
/// </summary>
public sealed class InMemoryDtnService : IDtnService
{
    private readonly List<DtnBundle> _bundles = new();
    private readonly object _gate = new();

#pragma warning disable CS0067 // event-stub for interface contract; this test double doesn't fire events
    /// <inheritdoc/>
    public event EventHandler<DtnDeliveryReceipt>? BundleDelivered;

    /// <inheritdoc/>
    public event EventHandler<DtnBundleReceivedEventArgs>? BundleReceived;
#pragma warning restore CS0067

    /// <summary>Every bundle ever created — useful for assertions.</summary>
    public IReadOnlyList<DtnBundle> AllBundles
    {
        get { lock (_gate) return _bundles.ToList(); }
    }

    /// <inheritdoc/>
    public Task<DtnBundle> CreateBundleAsync(string recipientUhid, byte[] encryptedPayload,
        BundlePriority priority = BundlePriority.Normal, string? recipientLastGeohash = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(recipientUhid);
        var bundle = new DtnBundle
        {
            SenderUhid = "self",
            RecipientUhid = recipientUhid,
            EncryptedPayload = encryptedPayload ?? Array.Empty<byte>(),
            Priority = priority,
            Status = BundleStatus.Pending,
            RecipientLastGeohash = recipientLastGeohash,
        };
        lock (_gate) _bundles.Add(bundle);
        return Task.FromResult(bundle);
    }

    /// <inheritdoc/>
    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task RunDeliveryScanAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<int> ExpireStaleAsync(CancellationToken cancellationToken = default)
    {
        var expired = 0;
        lock (_gate)
        {
            foreach (var b in _bundles)
                if (b.Status is BundleStatus.Pending or BundleStatus.InCustody && b.IsExpired)
                { b.Status = BundleStatus.Expired; expired++; }
        }
        return Task.FromResult(expired);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DtnBundle>> GetActiveBundlesAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            IReadOnlyList<DtnBundle> active = _bundles
                .Where(b => b.Status is BundleStatus.Pending or BundleStatus.InCustody)
                .ToList();
            return Task.FromResult(active);
        }
    }

    /// <summary>Mark every pending bundle as delivered (simulates a successful mesh scan).</summary>
    public Task DeliverAllAsync()
    {
        DtnBundle[] snapshot;
        lock (_gate)
            snapshot = _bundles.Where(b => b.Status is BundleStatus.Pending or BundleStatus.InCustody).ToArray();
        foreach (var b in snapshot)
        {
            b.Status = BundleStatus.Delivered;
            BundleDelivered?.Invoke(this, new DtnDeliveryReceipt
            {
                BundleId = b.Id,
                RecipientUhid = b.RecipientUhid,
                TotalHops = b.HopCount,
                TotalCustodyTransfers = 0,
            });
        }
        return Task.CompletedTask;
    }

    /// <summary>Force every active bundle into <see cref="BundleStatus.Expired"/>.</summary>
    public Task ExpireAllAsync()
    {
        lock (_gate)
            foreach (var b in _bundles)
                if (b.Status is BundleStatus.Pending or BundleStatus.InCustody)
                    b.Status = BundleStatus.Expired;
        return Task.CompletedTask;
    }
}
