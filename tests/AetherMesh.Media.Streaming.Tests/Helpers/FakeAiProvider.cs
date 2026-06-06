// SPDX-License-Identifier: MIT

using AetherMesh.Extensibility;
using AetherMesh.Protocol;

namespace AetherMesh.Media.Streaming.Tests.Helpers;

/// <summary>
/// Minimal IAetherMeshAiProvider stub for AbrController AI-bias tests.
/// </summary>
internal sealed class FakeAiProvider : IAetherMeshAiProvider
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
