// SPDX-License-Identifier: MIT
namespace AetherMesh.Forge.Core;

/// <summary>
/// Parses fully-qualified Aether Forge package identifiers of the form
/// <c>ecosystem:name@version</c>.
/// </summary>
/// <remarks>
/// Supported ecosystems: <c>npm</c>, <c>git</c>, <c>pip</c>, <c>cargo</c>,
/// <c>go</c>.
/// </remarks>
public static class PackageIdParser
{
    private static readonly HashSet<string> KnownEcosystems =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "npm", "git", "pip", "cargo", "go",
        };

    /// <summary>
    /// Parses <paramref name="packageId"/> into its component parts.
    /// </summary>
    /// <param name="packageId">
    /// A package identifier in the form <c>ecosystem:name@version</c>,
    /// e.g. <c>npm:react@18.2.0</c> or <c>go:github.com/gin-gonic/gin@v1.9.1</c>.
    /// </param>
    /// <returns>
    /// A tuple of <c>(ecosystem, name, version)</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="packageId"/> is null, empty, or does not
    /// match the expected format.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the ecosystem prefix is not one of the supported values.
    /// </exception>
    public static (string Ecosystem, string Name, string Version) Parse(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        var colonIdx = packageId.IndexOf(':', StringComparison.Ordinal);
        if (colonIdx <= 0)
            throw new ArgumentException(
                $"Package ID '{packageId}' must contain an ecosystem prefix separated by ':'.",
                nameof(packageId));

        var ecosystem = packageId[..colonIdx];
        var remainder = packageId[(colonIdx + 1)..];

        if (!KnownEcosystems.Contains(ecosystem))
            throw new NotSupportedException(
                $"Ecosystem '{ecosystem}' is not supported. " +
                $"Supported values: {string.Join(", ", KnownEcosystems)}.");

        if (string.IsNullOrWhiteSpace(remainder))
            throw new ArgumentException(
                $"Package ID '{packageId}' must specify a name after ':'.",
                nameof(packageId));

        var atIdx = remainder.LastIndexOf('@');
        if (atIdx <= 0)
            throw new ArgumentException(
                $"Package ID '{packageId}' must contain a version separated by '@'.",
                nameof(packageId));

        var name    = remainder[..atIdx];
        var version = remainder[(atIdx + 1)..];

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                $"Package ID '{packageId}' has an empty name.", nameof(packageId));

        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException(
                $"Package ID '{packageId}' has an empty version.", nameof(packageId));

        return (ecosystem.ToLowerInvariant(), name, version);
    }
}
