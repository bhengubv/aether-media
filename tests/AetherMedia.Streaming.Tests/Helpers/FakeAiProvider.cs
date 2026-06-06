// SPDX-License-Identifier: MIT

using AetherNet.Extensibility;
using AetherNet.Protocol;

namespace AetherMedia.Streaming.Tests.Helpers;

/// <summary>
/// Minimal IAetherNetAiProvider stub for AbrController AI-bias tests.
/// </summary>
internal sealed class FakeAiProvider : IAetherNetAiProvider
{
    public bool IsAvailable => Available;
    public bool Available   { get; set; } = true;

    /// <summary>Returned by <see cref="GetTransportBiasesAsync"/>.</summary>
    public Dictionary<string, double> TransportBiases { get; } = new();

    public Task<AiThreatLevel> AssessThreatAsync(
        MeshPacket packet, CancellationToken ct = default)
        => Task.FromResult(AiThreatLevel.None);

    public Task<IReadOnlyDictionary<string, double>> GetTransportBiasesAsync(
        int payloadBytes, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, double>>(TransportBiases);
}
