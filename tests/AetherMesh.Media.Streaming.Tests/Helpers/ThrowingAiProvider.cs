// SPDX-License-Identifier: MIT

using AetherMesh.Extensibility;
using AetherMesh.Protocol;

namespace AetherMesh.Media.Streaming.Tests.Helpers;

/// <summary>
/// IAetherMeshAiProvider stub that throws on every call.
/// Used to verify AbrController's best-effort fallback to neutral bias.
/// </summary>
internal sealed class ThrowingAiProvider : IAetherMeshAiProvider
{
    public bool IsAvailable => true; // Reports available so AbrController tries to call it

    public Task<AiThreatLevel> AssessThreatAsync(
        MeshPacket packet, CancellationToken ct = default)
        => throw new InvalidOperationException("Simulated AI provider failure");

    public Task<IReadOnlyDictionary<string, double>> GetTransportBiasesAsync(
        int payloadBytes, CancellationToken ct = default)
        => throw new InvalidOperationException("Simulated AI provider failure");
}
