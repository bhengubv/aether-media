// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Output;

/// <summary>
/// PCM format descriptor handed to audio outputs at open time.
/// </summary>
public sealed record AudioFormat(
    int SampleRateHz,
    int Channels,
    int BitsPerSample = 32,
    bool IsFloat = true);
