// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.UI.Shared;

/// <summary>
/// Platform-agnostic folder-picker abstraction.
/// MAUI host: implemented with <c>FolderPicker.PickAsync()</c>.
/// Web host: implemented with <c>&lt;InputFile&gt;</c> / no-op.
/// </summary>
public interface IFilePicker
{
    /// <summary>
    /// Shows the platform folder picker.
    /// Returns the selected path, or <c>null</c> if the user cancelled.
    /// </summary>
    Task<string?> PickFolderAsync();
}
