// SPDX-License-Identifier: MIT

using AetherNet.Dtn;
using AetherNet.Models;

namespace AetherMedia.LocalLibrary.Audio.Mesh;

/// <summary>
/// Runtime predicates that mirror the safety + liveness properties proved
/// by the Petri net models in <c>aether-protocol/formal/</c>. Designed for
/// xUnit assertions: every wave-16 integration test calls into these
/// predicates after exercising the integration, so the model and the code
/// stay coupled.
///
/// <para>
/// Each predicate maps to a specific formal model:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="DtnCustodyEventuallyTerminates"/> ←
///     <c>formal/dtn-custody</c>: a bundle must reach <see cref="BundleStatus.Delivered"/>,
///     <see cref="BundleStatus.Expired"/>, or <see cref="BundleStatus.Failed"/>
///     — never stuck in <see cref="BundleStatus.Pending"/> or
///     <see cref="BundleStatus.InCustody"/> indefinitely.</description></item>
///   <item><description><see cref="MultiDeviceSyncConverges"/> ←
///     <c>formal/multi-device-sync</c>: after both devices process the
///     same mutation set, their observable state is identical (set
///     equality, not list equality).</description></item>
///   <item><description><see cref="ContentBitmapEventuallyComplete"/> ←
///     <c>formal/content-bitmap</c>: every requested chunk of a content
///     descriptor must eventually arrive and verify.</description></item>
///   <item><description><see cref="ForgeIntegrity"/> ←
///     <c>formal/forge-integrity</c>: cached payload bytes must hash to
///     the recorded content hash.</description></item>
///   <item><description><see cref="StreamSequenceMonotonic"/> ←
///     <c>formal/stream-abr</c> + <c>formal/watch-together-timed</c>:
///     segment sequence numbers issued by a publisher are strictly
///     increasing.</description></item>
/// </list>
/// </summary>
public static class MeshInvariants
{
    /// <summary>
    /// DTN custody: no bundle remains active forever. Returns true iff
    /// every bundle reaches a terminal state (Delivered / Expired / Failed)
    /// within the given deadline.
    /// </summary>
    public static async Task<bool> DtnCustodyEventuallyTerminates(
        IDtnService dtn,
        Func<Task> driveDelivery,
        int maxScans = 10,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dtn);
        ArgumentNullException.ThrowIfNull(driveDelivery);
        for (var i = 0; i < maxScans; i++)
        {
            var active = await dtn.GetActiveBundlesAsync(ct).ConfigureAwait(false);
            var stuck = active.Where(b => b.Status is BundleStatus.Pending or BundleStatus.InCustody).ToList();
            if (stuck.Count == 0) return true;
            await driveDelivery().ConfigureAwait(false);
        }
        var remaining = await dtn.GetActiveBundlesAsync(ct).ConfigureAwait(false);
        return !remaining.Any(b => b.Status is BundleStatus.Pending or BundleStatus.InCustody);
    }

    /// <summary>
    /// Multi-device sync: device B's observed state matches device A's
    /// after the same mutation set has been applied.
    /// </summary>
    public static bool MultiDeviceSyncConverges<T>(IEnumerable<T> deviceA, IEnumerable<T> deviceB) =>
        new HashSet<T>(deviceA).SetEquals(deviceB);

    /// <summary>
    /// Content bitmap: returns true iff every chunk of <paramref name="rootHash"/>
    /// has been verified locally after at most <paramref name="maxScans"/>
    /// requests.
    /// </summary>
    public static async Task<bool> ContentBitmapEventuallyComplete(
        AetherNet.Content.IContentService content,
        string rootHash,
        int expectedChunks,
        Func<Task> driveDelivery,
        int maxScans = 10,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(rootHash);
        ArgumentNullException.ThrowIfNull(driveDelivery);
        for (var i = 0; i < maxScans; i++)
        {
            var assembled = await content.AssembleAsync(rootHash, ct).ConfigureAwait(false);
            if (assembled is not null) return true;
            await driveDelivery().ConfigureAwait(false);
        }
        return await content.AssembleAsync(rootHash, ct).ConfigureAwait(false) is not null;
    }

    /// <summary>
    /// Forge integrity: returns true iff <paramref name="payload"/>'s SHA-256
    /// hash matches <paramref name="expectedHashHex"/>.
    /// </summary>
    public static bool ForgeIntegrity(byte[] payload, string expectedHashHex)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrEmpty(expectedHashHex);
        var actual = MeshPackageDistributor.IntegrityHash(payload);
        return string.Equals(actual, expectedHashHex, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Stream sequence: returns true iff every published sequence number
    /// is strictly greater than its predecessor.
    /// </summary>
    public static bool StreamSequenceMonotonic(IEnumerable<uint> publishedSequences)
    {
        ArgumentNullException.ThrowIfNull(publishedSequences);
        uint? prev = null;
        foreach (var s in publishedSequences)
        {
            if (prev is not null && s <= prev.Value) return false;
            prev = s;
        }
        return true;
    }
}
