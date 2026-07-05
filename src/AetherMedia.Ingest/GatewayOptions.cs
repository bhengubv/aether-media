// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>Runtime options for a single ingest run.</summary>
public sealed record GatewayOptions
{
    /// <summary>Latency/resilience mode.</summary>
    public GatewayMode Mode { get; init; } = GatewayMode.NearLive;

    /// <summary>The normalisation target. Defaults to <see cref="TargetProfile.Passthrough"/>.</summary>
    public TargetProfile Target { get; init; } = TargetProfile.Passthrough;

    /// <summary>What the local node can do. Defaults to <see cref="NodeCapabilities.PassthroughOnly"/>.</summary>
    public NodeCapabilities Capabilities { get; init; } = NodeCapabilities.PassthroughOnly;
}
