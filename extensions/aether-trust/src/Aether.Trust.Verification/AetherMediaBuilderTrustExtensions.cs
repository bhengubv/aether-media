// SPDX-License-Identifier: MIT

using Aether.Media.DependencyInjection;
using Aether.Trust.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aether.Trust.Verification;

/// <summary>
/// Extends <see cref="AetherMediaBuilder"/> with aether-trust capability registration.
///
/// <para>Usage:</para>
/// <code>
/// services.AddAetherMedia(aether =>
///     aether
///         .AddContent()
///         .AddTrust());
/// </code>
/// </summary>
public static class AetherMediaBuilderTrustExtensions
{
    /// <summary>
    /// Registers aether-trust services:
    /// <list type="bullet">
    ///   <item><see cref="TrustRingService"/> as <see cref="ITrustRingService"/> —
    ///         the default in-process verification engine (SHA-256 + Ed25519).</item>
    /// </list>
    ///
    /// <para>
    /// No prerequisites — Trust Rings is a standalone extension that integrates
    /// with any content pipeline.  It has no dependency on aether-space, aether-vault,
    /// or aether-market; those extensions may consume <see cref="ITrustRingService"/>
    /// as an optional service-locator dependency.
    /// </para>
    ///
    /// <para>
    /// To wire violation events into the reputation layer, resolve
    /// <see cref="ITrustRingService"/> from the container after build and subscribe
    /// to <see cref="ITrustRingService.ViolationDetected"/>.
    /// </para>
    ///
    /// <para>
    /// Node capability announced during handshake: <c>aether.trust/v1</c>
    /// (see <see cref="TrustCapabilityConstants.V1"/>).
    /// </para>
    /// </summary>
    public static AetherMediaBuilder AddTrust(this AetherMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<ITrustRingService, TrustRingService>();

        return builder;
    }
}
