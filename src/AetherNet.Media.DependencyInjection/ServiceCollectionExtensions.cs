// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;

namespace AetherNet.Media.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> that provide the
/// Aether Media service registration entry point.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Aether Media services to <paramref name="services"/> using a fluent
    /// builder delegate.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// A delegate that receives an <see cref="AetherNetMediaBuilder"/> and selects
    /// which subsystems to activate (e.g. <c>.AddSocial().AddStreaming().AddAI()</c>).
    /// </param>
    /// <returns>
    /// The original <paramref name="services"/> collection, enabling further
    /// chaining on the host's service configuration.
    /// </returns>
    /// <example>
    /// <code>
    /// services.AddAetherNetMedia(media => media
    ///     .AddIdentity()
    ///     .AddContent()
    ///     .AddSocial()
    ///     .AddStreaming()
    ///     .AddAI());
    /// </code>
    /// </example>
    public static IServiceCollection AddAetherNetMedia(
        this IServiceCollection services,
        Action<AetherNetMediaBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AetherNetMediaBuilder(services);
        configure(builder);
        return services;
    }
}
