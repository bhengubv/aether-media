// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace Aether.Market.Core;

/// <summary>
/// Aggregated Proof-of-Vicinity reputation score for a mesh node, derived
/// from the total number of distinct witnessed co-location events.
/// Scores decay over time via a 6-month half-life applied at query time.
/// </summary>
/// <param name="Uhid">Universal host ID of the node this score applies to.</param>
/// <param name="UniqueWitnesses">Number of distinct UHIDs that have witnessed proximity with this node.</param>
/// <param name="WeightedScore">
/// Decay-adjusted composite score.  Each witness contributes a decayed weight
/// based on the time since the most recent co-location event with that witness.
/// </param>
/// <param name="LastUpdated">UTC timestamp of the most recent PoV token that was factored into this score.</param>
public sealed record PoVScore(
    [property: JsonPropertyName("uhid")]             string   Uhid,
    [property: JsonPropertyName("unique_witnesses")] int      UniqueWitnesses,
    [property: JsonPropertyName("weighted_score")]   double   WeightedScore,
    [property: JsonPropertyName("last_updated")]     DateTime LastUpdated);
