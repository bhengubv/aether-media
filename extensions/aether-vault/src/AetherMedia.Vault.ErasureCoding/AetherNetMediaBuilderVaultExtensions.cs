// SPDX-License-Identifier: MIT

using AetherMedia.DependencyInjection;
using AetherMedia.Vault.ErasureCoding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AetherMedia.Vault.ErasureCoding;

/// <summary>
/// Extends <see cref="AetherNetMediaBuilder"/> with aether-vault capability registration.
///
/// <para>Usage:</para>
/// <code>
/// services.AddAetherNetMedia(aether =>
///     aether
///         .AddContent()
///         .AddVault());
/// </code>
/// </summary>
public static class AetherNetMediaBuilderVaultExtensions
{
    /// <summary>
    /// Registers aether-vault services:
    /// <list type="bullet">
    ///   <item><see cref="ReedSolomonEncoder"/> as <see cref="IErasureCoder"/> — the
    ///         default Reed-Solomon erasure coding implementation (k=10, m=4 by default;
    ///         any 10 of 14 shards can reconstruct the original data).</item>
    /// </list>
    ///
    /// <para>
    /// An <see cref="IVaultService"/> implementation must be registered separately — either
    /// by a platform-specific package or the host application — before any vault
    /// store/recover operations are requested.
    /// </para>
    ///
    /// <para>
    /// Shard hosting incentives are settled via <c>IAetherNetIncentiveProvider</c> (ZAR →
    /// SDPKT wallet) when an incentive provider is registered in the container.
    /// </para>
    ///
    /// <para>
    /// Node capability announced during handshake: <c>aethernet.vault/v1</c>
    /// (see <see cref="AetherMedia.Vault.Core.VaultCapabilityConstants.V1"/>).
    /// </para>
    /// </summary>
    public static AetherNetMediaBuilder AddVault(this AetherNetMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IErasureCoder, ReedSolomonEncoder>();

        return builder;
    }
}
