// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Output;

/// <summary>
/// Real-time audio output sink. Open with an <see cref="AudioFormat"/> + a
/// pull-style sample provider; the output device calls back for buffers as
/// it needs them. Modelled after Winamp's <c>out_</c> plugin contract.
/// </summary>
public interface IAudioOutput : IDisposable
{
    /// <summary>Stable identifier (e.g. <c>"windows-wasapi"</c>).</summary>
    string Id { get; }

    /// <summary>Human-readable name.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Open the device. <paramref name="sampleProvider"/> is invoked from
    /// the audio driver thread to fill the requested span with interleaved
    /// PCM at the format supplied to Open. Return value = number of samples
    /// actually written; 0 signals end-of-stream.
    /// </summary>
    void Open(AudioFormat format, Func<Memory<float>, int> sampleProvider);

    /// <summary>Begin playback.</summary>
    void Play();

    /// <summary>Pause without releasing the device.</summary>
    void Pause();

    /// <summary>Stop playback and close the device.</summary>
    void Stop();

    /// <summary>Output level 0..1.</summary>
    float Volume { get; set; }
}
