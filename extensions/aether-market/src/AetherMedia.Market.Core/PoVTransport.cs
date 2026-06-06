// SPDX-License-Identifier: MIT
namespace AetherMedia.Market.Core;

/// <summary>
/// Short-range transport protocols used to establish Proof-of-Vicinity
/// between two devices. Only transports that require physical proximity
/// are valid for PoV attestation.
/// </summary>
public enum PoVTransport
{
    /// <summary>Bluetooth Low Energy — ~10 m range.</summary>
    BLE      = 0,

    /// <summary>Near Field Communication — ~4 cm range.</summary>
    NFC      = 1,

    /// <summary>Huawei NearLink (formerly SparkLink) — ~10 m range.</summary>
    NearLink = 2,
}
