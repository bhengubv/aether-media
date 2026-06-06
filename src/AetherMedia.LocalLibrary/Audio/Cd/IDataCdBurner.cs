// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>Burns a data CD / DVD — Winamp's "MP3 CD" output (every file as a file, not a CDDA session).</summary>
public interface IDataCdBurner
{
    IReadOnlyList<string> EnumerateRecorders();
    Task BurnAsync(DataCdRequest request, IProgress<double>? progress = null, CancellationToken ct = default);
}
