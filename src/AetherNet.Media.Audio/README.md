# AetherNet.Media.Audio

The audio depth Winamp had and most "modern" media apps don't —
loudness normalization, parametric EQ, DSP chain, equal-power crossfade —
rebuilt for the streaming-and-mesh age. Pure managed C#, no native
dependencies, MIT.

```bash
dotnet add package AetherNet.Media.Audio
```

## What's in the box

### Loudness — `Loudness/`

A real ITU-R BS.1770-4 / EBU R128 integrated-loudness analyzer
(`Itu1770Analyzer`) — the same algorithm Spotify, YouTube, Apple Music,
Tidal, and every broadcaster use. K-weighted, gated, with true-peak and
loudness-range. Targets in `LoudnessTargets`: −14 LUFS (Spotify / YouTube
/ Tidal), −16 LUFS (Apple Music), −23 LUFS (EBU R128), −24 LUFS (ATSC).

```csharp
using AetherNet.Media.Audio.Loudness;

var analyzer = new Itu1770Analyzer();
var measurement = analyzer.Measure(pcmSamples, sampleRateHz: 48000, channels: 2);

// "How much should I scale this so it sits at the Spotify target?"
var gain = measurement.GainToTarget(LoudnessTargets.Spotify);
//  → never exceeds the −1.0 dBFS true-peak ceiling, so no clipping.
```

Persist measurements per-content with `ILoudnessStore` /
`InMemoryLoudnessStore`. A mesh-backed adapter that gossips a track's
measurement to peers (so each device doesn't redo the analysis) is the
obvious next step.

### Equalizer — `Equalizer/`

10-band parametric EQ at the ISO 1/3-octave centres
(32 / 64 / 125 / 250 / 500 / 1k / 2k / 4k / 8k / 16k Hz), implemented as
a cascade of peaking biquads via the RBJ Audio EQ Cookbook formulas.
Eight built-in presets (`Flat`, `BassBoost`, `TrebleBoost`, `Rock`,
`Pop`, `Jazz`, `Classical`, `Vocal`) — same band layout Winamp 5 / iTunes /
most streaming-app EQs use.

```csharp
using AetherNet.Media.Audio.Equalizer;

var eq = new BiquadEqualizer();
eq.ApplyPreset(EqualizerPresets.Rock);
eq.Process(pcmSamples, sampleRateHz: 48000, channels: 2);
```

### DSP Chain — `Effects/`

`IDspEffect` (any plugin that mutates a PCM buffer) chained into `IDspChain`.
The Winamp `dsp_` plugin contract, modernised. `BiquadEqualizer`
implements `IDspEffect`; future compressors, limiters, room-correction,
or replay-gain appliers all slot in the same way.

```csharp
var chain = new DspChain();
chain.Add(new BiquadEqualizer());
chain.Process(pcmSamples, sampleRateHz: 48000, channels: 2);
```

### Crossfade — `Crossfade/`

Three transition modes: `Off` (hard cut), `Gapless` (sample-accurate
album playback), `Crossfade` (equal-power S-curve over a configurable
window). `CrossfadeController` produces the per-sample gain ramp so the
combined energy of A + B stays roughly constant through the transition
— the standard equal-power choice for music players.

```csharp
var fade = new CrossfadeController
{
    Mode = CrossfadeMode.Crossfade,
    FadeDurationMs = 4000,
};
fade.ComputeGainRamp(positionMs, sampleCount, sampleRateHz, fadingOut: true, gains);
```

## What this is not

- A codec — feed it 32-bit float PCM and it does the rest.
- An output backend — call `Process` on every buffer you decode, before
  you hand it to the OS audio device.
- Native — no LibVLC / FFmpeg dependency. Wrappers around those decoders
  live in `AetherNet.Media.Desktop` / future platform projects.

## Why this exists

Read [`docs/research/winamp-postmortem.md`](https://github.com/bhengubv/aether-media/blob/master/docs/research/winamp-postmortem.md) —
this library implements the four things that audit identified as the
audio engineering depth users actually feel and AetherMedia was missing.
