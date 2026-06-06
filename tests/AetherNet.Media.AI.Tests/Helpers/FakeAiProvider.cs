// SPDX-License-Identifier: MIT

using AetherNet.Extensibility;
using AetherNet.Protocol;

namespace AetherNet.Media.AI.Tests.Helpers;

/// <summary>
/// Controllable IAetherNetAiProvider stub.
/// Tests set <see cref="Available"/>, <see cref="ThreatLevel"/>,
/// <see cref="TransportBiases"/>, and <see cref="RouteSuggestions"/>
/// before calling the code under test.
/// </summary>
internal sealed class FakeAiProvider : IAetherNetAiProvider
{
    public bool IsAvailable => Available;

    public bool Available { get; set; } = true;

    /// <summary>Returned by <see cref="AssessThreatAsync"/> for every packet.</summary>
    public AiThreatLevel ThreatLevel { get; set; } = AiThreatLevel.None;

    /// <summary>Returned by <see cref="GetTransportBiasesAsync"/> — default empty (neutral).</summary>
    public Dictionary<string, double> TransportBiases { get; } = new();

    /// <summary>
    /// destinationUhid → list of suggestions returned by <see cref="SuggestRoutesAsync"/>.
    /// UHIDs absent from this map return an empty list (standard AODV proceeds).
    /// </summary>
    public Dictionary<string, List<AiRouteSuggestion>> RouteSuggestions { get; } = new();

    public Task<AiThreatLevel> AssessThreatAsync(MeshPacket packet, CancellationToken ct = default)
        => Task.FromResult(ThreatLevel);

    public Task<IReadOnlyDictionary<string, double>> GetTransportBiasesAsync(
        int payloadBytes, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, double>>(TransportBiases);

    public Task<IReadOnlyList<AiRouteSuggestion>> SuggestRoutesAsync(
        string destinationUhid,
        int payloadBytes,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AiRouteSuggestion> result =
            RouteSuggestions.TryGetValue(destinationUhid, out var suggestions)
                ? suggestions
                : Array.Empty<AiRouteSuggestion>();

        return Task.FromResult(result);
    }
}
