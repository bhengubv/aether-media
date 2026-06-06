// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>
/// One audio CD burn job.
/// </summary>
/// <param name="RecorderId">
/// Recorder ID from <see cref="ICdBurner.EnumerateRecorders"/>.
/// </param>
/// <param name="Tracks">
/// Lazy stream factories — one per track. Each call must produce a fresh
/// readable seekable stream of 16-bit signed little-endian stereo PCM at
/// 44.1 kHz. The factory pattern lets the burner reopen a track on retry
/// without holding every PCM buffer in memory at once.
/// </param>
public sealed record CdBurnRequest(
    string RecorderId,
    IReadOnlyList<Func<Stream>> Tracks);
