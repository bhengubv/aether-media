// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>How the gateway trades latency against resilience.</summary>
public enum GatewayMode
{
    /// <summary>Segmented, chunk-addressed, resilient (~seconds of glass-to-glass latency).</summary>
    NearLive,

    /// <summary>Smaller frames, lower latency, less loss tolerance (~1–3s).</summary>
    LowLatency,
}
