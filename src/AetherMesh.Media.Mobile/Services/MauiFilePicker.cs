// SPDX-License-Identifier: MIT

using AppIFilePicker = AetherMesh.Media.UI.Shared.IFilePicker;

namespace AetherMesh.Media.Mobile.Services;

/// <summary>
/// MAUI implementation of <see cref="AppIFilePicker"/>.
/// Presents a file picker and returns the directory containing the chosen file,
/// which becomes the scan root for the media library.
/// </summary>
internal sealed class MauiFilePicker : AppIFilePicker
{
    public async Task<string?> PickFolderAsync()
    {
        var result = await FilePicker.PickAsync();
        return result is null ? null : Path.GetDirectoryName(result.FullPath);
    }
}
