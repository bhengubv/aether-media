// SPDX-License-Identifier: MIT
namespace AetherMesh.Vault.Core;

/// <summary>
/// Provides operations for storing, recovering, and managing files in the
/// Aether Vault distributed encrypted storage layer.
/// </summary>
public interface IVaultService
{
    // ── Observable ─────────────────────────────────────────────────────────

    /// <summary>
    /// Hot observable that emits each <see cref="VaultShardRequest"/> as it
    /// arrives from another mesh node requesting a locally-held shard.
    /// </summary>
    IObservable<VaultShardRequest> ShardRequested { get; }

    // ── Mutations ──────────────────────────────────────────────────────────

    /// <summary>
    /// Encrypts and erasure-codes the content stream, distributes the
    /// resulting shards across mesh nodes, and returns the
    /// <see cref="VaultManifest"/> required to recover the file later.
    /// </summary>
    /// <param name="content">Plaintext content stream to store.</param>
    /// <param name="label">Human-readable label for the vault entry.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The manifest describing the stored file.</returns>
    Task<VaultManifest> StoreAsync(Stream content, string label, CancellationToken ct = default);

    /// <summary>
    /// Locates and reassembles the shards described by <paramref name="manifest"/>,
    /// decrypts them, and returns the plaintext content stream.
    /// Requires at least <see cref="VaultManifest.K"/> reachable shards.
    /// </summary>
    /// <param name="manifest">The manifest identifying the stored file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Plaintext content stream.</returns>
    Task<Stream> RecoverAsync(VaultManifest manifest, CancellationToken ct = default);

    /// <summary>
    /// Probes the mesh for shards described by <paramref name="manifest"/> and
    /// returns a <see cref="VaultHealth"/> snapshot.
    /// </summary>
    /// <param name="manifest">The manifest to check.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<VaultHealth> CheckHealthAsync(VaultManifest manifest, CancellationToken ct = default);

    /// <summary>
    /// Ensures that at least <paramref name="targetReplicas"/> copies of each
    /// shard described by <paramref name="manifest"/> are held across distinct
    /// mesh nodes, creating new copies as needed.
    /// </summary>
    /// <param name="manifest">The manifest describing the file to replicate.</param>
    /// <param name="targetReplicas">Desired replication factor per shard.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReplicateAsync(VaultManifest manifest, int targetReplicas, CancellationToken ct = default);
}
