// SPDX-License-Identifier: MIT
namespace AetherMedia.Vault.ErasureCoding;

/// <summary>
/// Provides Reed-Solomon erasure coding operations for splitting data into
/// shards and reconstructing data from a sufficient subset of those shards.
/// </summary>
public interface IErasureCoder
{
    /// <summary>
    /// Splits <paramref name="data"/> into <paramref name="k"/> data shards
    /// plus <paramref name="m"/> parity shards using Reed-Solomon encoding.
    /// </summary>
    /// <param name="data">The raw data bytes to encode.</param>
    /// <param name="k">Number of data shards.</param>
    /// <param name="m">Number of parity shards.</param>
    /// <returns>
    /// An array of <paramref name="k"/> + <paramref name="m"/> shards; each
    /// shard is a byte array of equal length (padded to align if necessary).
    /// </returns>
    byte[][] Encode(byte[] data, int k, int m);

    /// <summary>
    /// Reconstructs the original data from a subset of shards.
    /// </summary>
    /// <param name="shards">
    /// Array of shards, length == k + m.  Set missing shards to
    /// <see langword="null"/> to indicate they are unavailable.
    /// At least <paramref name="k"/> entries must be non-null.
    /// </param>
    /// <param name="k">Number of data shards.</param>
    /// <param name="m">Number of parity shards.</param>
    /// <returns>The reconstructed original data bytes (without padding).</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when fewer than <paramref name="k"/> shards are available.
    /// </exception>
    byte[] Decode(byte[]?[] shards, int k, int m);
}
