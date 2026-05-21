// SPDX-License-Identifier: MIT

using Aether.Extensibility;
using Aether.Protocol;

namespace Aether.Media.AI.Tests.Helpers;

/// <summary>
/// Controllable IAetherAiProvider stub.
/// Tests set <see cref="Available"/>, <see cref="ThreatLevel"/>, and
/// <see cref="TransportBiases"/> before calling the code under test.
/// </summary>
internal sealed class FakeAiProvider : IAetherAiProvider
{
    public bool IsAvailable => Available;

    public bool Available { get; set; } = true;

    /// <summary>Returned by <see cref="AssessThreatAsync"/> for every packet.</summary>
    public AiThreatLevel ThreatLevel { get; set; } = AiThreatLevel.None;

    /// <summary>Returned by <see cref="GetTransportBiasesAsync"/> — default empty (neutral).</summary>
    public Dictionary<string, double> TransportBiases { get; } = new();

    public Task<AiThreatLevel> AssessThreatAsync(MeshPacket packet, CancellationToken ct = default)
        => Task.FromResult(ThreatLevel);

    public Task<IReadOnlyDictionary<string, double>> GetTransportBiasesAsync(
        int payloadBytes, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, double>>(TransportBiases);
}
