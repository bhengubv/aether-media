// SPDX-License-Identifier: MIT
using Aether.Space.Core;

namespace Aether.Space.Protocol;

/// <summary>
/// Applies packet routing rules for incoming <see cref="SpaceBreadcrumbPacket"/>
/// frames:
/// <list type="bullet">
///   <item>Normal breadcrumbs are forwarded only within a 3-cell geohash radius.</item>
///   <item>Expired breadcrumbs (TTL elapsed) are pruned and not forwarded.</item>
///   <item><see cref="BreadcrumbType.Emergency"/> breadcrumbs bypass the radius
///   constraint and are flooded to all reachable peers.</item>
/// </list>
/// </summary>
public sealed class SpaceBreadcrumbHandler
{
    private const int DefaultRadiusCells = 3;

    private readonly int _radiusCells;

    /// <summary>
    /// Initialises a new handler.
    /// </summary>
    /// <param name="radiusCells">
    /// Maximum cell distance for non-emergency routing (default 3).
    /// </param>
    public SpaceBreadcrumbHandler(int radiusCells = DefaultRadiusCells)
    {
        if (radiusCells < 1)
            throw new ArgumentOutOfRangeException(nameof(radiusCells), "Must be at least 1.");
        _radiusCells = radiusCells;
    }

    /// <summary>
    /// Determines whether an incoming <paramref name="packet"/> should be
    /// forwarded to peers from the perspective of the local node at
    /// <paramref name="localGeoHash"/>.
    /// </summary>
    /// <param name="packet">The received packet.</param>
    /// <param name="localGeoHash">The local node's current geohash cell.</param>
    /// <param name="utcNow">Current UTC time used for TTL evaluation.</param>
    /// <returns>
    /// <see langword="true"/> if the packet should be forwarded;
    /// <see langword="false"/> if it should be dropped.
    /// </returns>
    public bool ShouldForward(SpaceBreadcrumbPacket packet, GeoHash localGeoHash, DateTime utcNow)
    {
        var breadcrumb = packet.ToBreadcrumb();

        // Always prune expired breadcrumbs.
        if (breadcrumb.IsExpired(utcNow))
            return false;

        // Emergency breadcrumbs flood unconditionally.
        if (breadcrumb.Type == BreadcrumbType.Emergency)
            return true;

        // All other types respect the radius filter.
        return IsCellWithinRadius(
            breadcrumb.GeoHash,
            localGeoHash.Value,
            _radiusCells);
    }

    /// <summary>
    /// Determines whether the <paramref name="targetCell"/> falls within
    /// <paramref name="radiusCells"/> of <paramref name="originCell"/> using
    /// a simple common-prefix heuristic: two geohash cells share the same
    /// parent if they agree on at least
    /// <c>ceil(precision − radiusCells/2)</c> leading characters.
    /// </summary>
    private static bool IsCellWithinRadius(string targetCell, string originCell, int radiusCells)
    {
        if (string.IsNullOrEmpty(targetCell) || string.IsNullOrEmpty(originCell))
            return false;

        var precision = Math.Min(targetCell.Length, originCell.Length);
        var requiredPrefix = Math.Max(1, precision - (int)Math.Ceiling(radiusCells / 2.0));

        var sharedPrefixLength = 0;
        for (var i = 0; i < precision; i++)
        {
            if (targetCell[i] == originCell[i]) sharedPrefixLength++;
            else break;
        }

        return sharedPrefixLength >= requiredPrefix;
    }
}
