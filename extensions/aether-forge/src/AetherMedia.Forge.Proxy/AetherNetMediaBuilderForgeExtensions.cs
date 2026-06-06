// SPDX-License-Identifier: MIT

using AetherMedia.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AetherMedia.Forge.Proxy;

/// <summary>
/// Extends <see cref="AetherNetMediaBuilder"/> with aether-forge capability registration.
///
/// <para>Usage:</para>
/// <code>
/// services.AddAetherNetMedia(aether =>
///     aether
///         .AddContent()
///         .AddForge());
/// </code>
/// </summary>
public static class AetherNetMediaBuilderForgeExtensions
{
    /// <summary>
    /// Registers aether-forge services:
    /// <list type="bullet">
    ///   <item><see cref="ForgeProxy"/> — handles HTTP CONNECT proxy requests, serving
    ///         cached package artifacts from the mesh before falling back to the internet.</item>
    ///   <item><see cref="ForgeProxyServer"/> — starts the HTTP proxy listener on
    ///         <c>localhost:2301</c> (default). Configure the port via
    ///         <c>ForgeProxyServer.DefaultPort</c> before calling <c>AddForge()</c>.</item>
    /// </list>
    ///
    /// <para>
    /// An <see cref="IForgeService"/> implementation must be registered separately — either
    /// by a platform-specific package or by the host application — before the first
    /// proxy request is handled.
    /// </para>
    ///
    /// <para>
    /// Node capability announced during handshake: <c>aethernet.forge/v1</c>
    /// (see <see cref="ForgeCapabilityConstants.V1"/>).
    /// </para>
    /// </summary>
    public static AetherNetMediaBuilder AddForge(this AetherNetMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<ForgeProxy>();
        builder.Services.TryAddSingleton<ForgeProxyServer>();

        return builder;
    }
}
