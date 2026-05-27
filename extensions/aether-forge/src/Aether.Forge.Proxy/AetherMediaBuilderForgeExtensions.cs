// SPDX-License-Identifier: MIT

using Aether.Media.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aether.Forge.Proxy;

/// <summary>
/// Extends <see cref="AetherMediaBuilder"/> with aether-forge capability registration.
///
/// <para>Usage:</para>
/// <code>
/// services.AddAetherMedia(aether =>
///     aether
///         .AddContent()
///         .AddForge());
/// </code>
/// </summary>
public static class AetherMediaBuilderForgeExtensions
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
    /// Node capability announced during handshake: <c>aether.forge/v1</c>
    /// (see <see cref="ForgeCapabilityConstants.V1"/>).
    /// </para>
    /// </summary>
    public static AetherMediaBuilder AddForge(this AetherMediaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<ForgeProxy>();
        builder.Services.TryAddSingleton<ForgeProxyServer>();

        return builder;
    }
}
