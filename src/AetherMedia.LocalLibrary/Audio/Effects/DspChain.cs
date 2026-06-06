// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Effects;

/// <summary>
/// Default in-process <see cref="IDspChain"/>. Threadsafe for reads while the
/// player processes; mutations should happen on the UI thread.
/// </summary>
public sealed class DspChain : IDspChain
{
    private readonly List<IDspEffect> _effects = [];

    /// <inheritdoc/>
    public IReadOnlyList<IDspEffect> Effects => _effects;

    /// <inheritdoc/>
    public void Add(IDspEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _effects.Add(effect);
    }

    /// <inheritdoc/>
    public void Insert(int index, IDspEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _effects.Insert(index, effect);
    }

    /// <inheritdoc/>
    public bool Remove(string effectId)
    {
        ArgumentException.ThrowIfNullOrEmpty(effectId);
        var idx = _effects.FindIndex(e => e.Id == effectId);
        if (idx < 0) return false;
        _effects.RemoveAt(idx);
        return true;
    }

    /// <inheritdoc/>
    public void Reorder(string effectId, int newIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(effectId);
        var idx = _effects.FindIndex(e => e.Id == effectId);
        if (idx < 0) return;
        var e = _effects[idx];
        _effects.RemoveAt(idx);
        _effects.Insert(Math.Clamp(newIndex, 0, _effects.Count), e);
    }

    /// <inheritdoc/>
    public void Process(Span<float> samples, int sampleRateHz, int channels)
    {
        foreach (var e in _effects)
        {
            if (e.IsEnabled)
                e.Process(samples, sampleRateHz, channels);
        }
    }
}
