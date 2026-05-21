// SPDX-License-Identifier: MIT

using Aether.Extensibility;
using Aether.Protocol;

namespace Aether.Media.Streaming.Tests.Helpers;

/// <summary>
/// IAetherAiProvider stub that throws on every call.
/// Used to verify AbrController's best-effort fallback to neutral bias.
/// </summary>
internal sealed class ThrowingAiProvider : IAetherAiProvider
{
    public bool IsAvailable => true; // Reports available so AbrController tries to call it

    public Task<AiThreatLevel> AssessThreatAsync(
        MeshPacket packet, CancellationToken ct = default)
        => throw new InvalidOperationException("Simulated AI provider failure");

    public Task<IReadOnlyDictionary<string, double>> GetTransportBiasesAsync(
        int payloadBytes, CancellationToken ct = default)
        => throw new InvalidOperationException("Simulated AI provider failure");
}
