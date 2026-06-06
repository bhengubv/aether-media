// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Platform;

/// <summary>
/// Registers file extensions with the OS so double-clicking a music file
/// opens AetherMedia. Implementations are platform-specific (Windows
/// registry, macOS LSItemContentTypes, Linux .desktop files).
/// </summary>
public interface IFileAssociations
{
    /// <summary>Returns true if AetherMedia owns the given extension.</summary>
    bool IsAssociated(string extension);

    /// <summary>Register <paramref name="extension"/> so AetherMedia opens it.</summary>
    void Associate(string extension, string applicationPath, string applicationName);

    /// <summary>Drop our association for <paramref name="extension"/>.</summary>
    void Unassociate(string extension);
}
