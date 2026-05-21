// SPDX-License-Identifier: MIT

using Aether.Media.UI.Shared;

namespace Aether.Media.Web;

/// <summary>
/// Web implementation of <see cref="IFilePicker"/>.
/// Folder picking is not directly supported in the browser; returns null
/// so the caller can show its own &lt;InputFile&gt; based alternative.
/// </summary>
internal sealed class WebFilePicker : IFilePicker
{
    public Task<string?> PickFolderAsync()
    {
        // Browser security does not allow folder path access.
        // The Library page falls back to an <InputFile> upload flow.
        return Task.FromResult<string?>(null);
    }
}
