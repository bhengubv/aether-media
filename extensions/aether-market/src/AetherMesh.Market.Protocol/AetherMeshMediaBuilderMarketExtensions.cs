// SPDX-License-Identifier: MIT

using AetherMesh.Media.DependencyInjection;
using AetherMesh.Space.Protocol;
using AetherMesh.Vault.ErasureCoding;
using Microsoft.Extensions.DependencyInjection;

namespace AetherMesh.Market.Protocol;

/// <summary>
/// Extends <see cref="AetherMeshMediaBuilder"/> with aether-market capability registration.
///
/// <para>
/// aether-market is the convergence layer: it synthesises aether-space (geo-pinned listings),
/// aether-vault (document escrow), and Proof-of-Vicinity trust into a fully offline-capable
/// peer-to-peer commerce platform.
/// </para>
///
/// <para>Usage:</para>
/// <code>
/// services.AddAetherMeshMedia(aether =>
///     aether
///         .AddContent()
///         .AddSpace()     // required
///         .AddVault()     // required
///         .AddMarket());
/// </code>
/// </summary>
public static class AetherMeshMediaBuilderMarketExtensions
{
    /// <summary>
    /// Registers aether-market services and validates prerequisites.
    ///
    /// <para><b>Prerequisites (throws <see cref="InvalidOperationException"/> if missing):</b></para>
    /// <list type="bullet">
    ///   <item>aether-space — <c>.AddSpace()</c> must be called first
    ///         (aether-market uses <see cref="SpaceBreadcrumbHandler"/> to pin listings
    ///         to physical geo-coordinates).</item>
    ///   <item>aether-vault — <c>.AddVault()</c> must be called first
    ///         (aether-market uses <see cref="IErasureCoder"/> for document escrow on
    ///         land-deed and certificate sales).</item>
    /// </list>
    ///
    /// <para>
    /// <see cref="IPoVService"/> and <see cref="IMarketService"/> implementations must be
    /// registered separately — either by a platform-specific package or the host application.
    /// </para>
    ///
    /// <para>
    /// Node capability announced during handshake: <c>aethermesh.market/v1</c>
    /// (see <see cref="AetherMesh.Market.Core.MarketCapabilityConstants.V1"/>).
    /// </para>
    ///
    /// <para><b>eKYC pathway:</b> 10+ unique Proof-of-Vicinity witnesses satisfy SARB Exemption 17
    /// (Directive 1 of 2017) simplified due-diligence requirements for low-value accounts,
    /// enabling mobile-money onboarding without a phone number.
    /// See <c>extensions/aether-market/docs/ekyc-pathway.md</c>.</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <c>AddSpace()</c> or <c>AddVault()</c> has not been called before
    /// <c>AddMarket()</c>.
    /// </exception>
    public static AetherMeshMediaBuilder AddMarket(this AetherMeshMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Validate Space prerequisite
        if (!builder.Services.Any(d => d.ServiceType == typeof(SpaceBreadcrumbHandler)))
            throw new InvalidOperationException(
                "aether-market requires aether-space. Call .AddSpace() before .AddMarket().");

        // Validate Vault prerequisite
        if (!builder.Services.Any(d => d.ServiceType == typeof(IErasureCoder)))
            throw new InvalidOperationException(
                "aether-market requires aether-vault. Call .AddVault() before .AddMarket().");

        // PoV token + market packet handlers are registered by the host app via IPoVService / IMarketService.
        // Protocol-level packet type constants are in MarketProtocolConstants.

        return builder;
    }
}
