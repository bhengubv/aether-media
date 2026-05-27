// SPDX-License-Identifier: MIT

using Aether.Media.DependencyInjection;
using Aether.Vault.ErasureCoding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aether.Vault.ErasureCoding;

/// <summary>
/// Extends <see cref="AetherMediaBuilder"/> with aether-vault capability registration.
///
/// <para>Usage:</para>
/// <code>
/// services.AddAetherMedia(aether =>
///     aether
///         .AddContent()
///         .AddVault());
/// </code>
/// </summary>
public static class AetherMediaBuilderVaultExtensions
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
    /// Shard hosting incentives are settled via <c>IAetherIncentiveProvider</c> (ZAR →
    /// SDPKT wallet) when an incentive provider is registered in the container.
    /// </para>
    ///
    /// <para>
    /// Node capability announced during handshake: <c>aether.vault/v1</c>
    /// (see <see cref="Aether.Vault.Core.VaultCapabilityConstants.V1"/>).
    /// </para>
    /// </summary>
    public static AetherMediaBuilder AddVault(this AetherMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IErasureCoder, ReedSolomonEncoder>();

        return builder;
    }
}
