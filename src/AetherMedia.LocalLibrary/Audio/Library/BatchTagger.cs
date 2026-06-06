// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Tags;

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// Apply a tag patch across many files in one pass — the Winamp
/// <c>Auto-Tag &gt; Selected Files</c> equivalent. Only the non-null fields
/// on the supplied <see cref="AudioTags"/> patch overwrite the existing
/// tags; everything else is preserved.
/// </summary>
public sealed class BatchTagger
{
    private readonly IAudioTagWriter _writer;

    public BatchTagger(IAudioTagWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <summary>Result of one file in the batch.</summary>
    public sealed record Result(string FilePath, bool Succeeded, string? Error);

    /// <summary>
    /// Apply <paramref name="patch"/> to every path in <paramref name="filePaths"/>.
    /// Reports a result per file; errors don't abort the batch.
    /// </summary>
    public async Task<IReadOnlyList<Result>> ApplyAsync(
        IReadOnlyList<string> filePaths,
        AudioTags patch,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ArgumentNullException.ThrowIfNull(patch);

        var results = new List<Result>(filePaths.Count);
        for (var i = 0; i < filePaths.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var path = filePaths[i];
            try
            {
                await _writer.WriteAsync(path, patch, ct).ConfigureAwait(false);
                results.Add(new Result(path, Succeeded: true, Error: null));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException)
            {
                results.Add(new Result(path, Succeeded: false, Error: ex.Message));
            }
            progress?.Report((double)(i + 1) / filePaths.Count);
        }
        return results;
    }
}
