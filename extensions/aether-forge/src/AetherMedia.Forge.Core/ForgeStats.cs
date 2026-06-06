// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace AetherMedia.Forge.Core;

/// <summary>
/// Aggregate statistics for the local Aether Forge cache node.
/// </summary>
/// <param name="TotalBytesSaved">
/// Total bytes served from the local cache (bandwidth saved vs. internet
/// fetches).
/// </param>
/// <param name="TotalPeersServed">
/// Number of distinct peer nodes that have downloaded at least one package
/// from this node.
/// </param>
/// <param name="CatalogueSize">Number of unique package entries in the local cache.</param>
/// <param name="TopPackages">
/// The most-downloaded packages, ordered by <see cref="ForgeEntry.DownloadCount"/>
/// descending.
/// </param>
public sealed record ForgeStats(
    [property: JsonPropertyName("total_bytes_saved")]   long TotalBytesSaved,
    [property: JsonPropertyName("total_peers_served")]  int TotalPeersServed,
    [property: JsonPropertyName("catalogue_size")]      int CatalogueSize,
    [property: JsonPropertyName("top_packages")]        IReadOnlyList<ForgeEntry> TopPackages);
