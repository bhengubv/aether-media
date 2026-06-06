// SPDX-License-Identifier: MIT

using AetherNet.Extensibility;
using AetherNet.Protocol;

namespace AetherNet.Media.Streaming.Tests.Helpers;

/// <summary>
/// IAetherNetAiProvider stub that throws on every call.
/// Used to verify AbrController's best-effort fallback to neutral bias.
/// </summary>
internal sealed class ThrowingAiProvider : IAetherNetAiProvider
{
    public bool IsAvailable => true; // Reports available so AbrController tries to call it

    public Task<AiThreatLevel> AssessThreatAsync(
        MeshPacket packet, CancellationToken ct = default)
        => throw new InvalidOperationException("Simulated AI provider failure");

    public Task<IReadOnlyDictionary<string, double>> GetTransportBiasesAsync(
        int payloadBytes, CancellationToken ct = default)
        => throw new InvalidOperationException("Simulated AI provider failure");
}
