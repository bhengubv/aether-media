// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Effects;

/// <summary>
/// Ordered chain of <see cref="IDspEffect"/> stages. The player applies the
/// chain to every PCM buffer before output. Order matters: typically EQ →
/// compressor → limiter → normalisation gain.
/// </summary>
public interface IDspChain
{
    /// <summary>Effects in execution order. Disabled effects are skipped.</summary>
    IReadOnlyList<IDspEffect> Effects { get; }

    /// <summary>Append an effect to the end of the chain.</summary>
    void Add(IDspEffect effect);

    /// <summary>Insert <paramref name="effect"/> at <paramref name="index"/>.</summary>
    void Insert(int index, IDspEffect effect);

    /// <summary>Remove an effect by id; returns <c>true</c> when found.</summary>
    bool Remove(string effectId);

    /// <summary>Move <paramref name="effectId"/> to a new chain position.</summary>
    void Reorder(string effectId, int newIndex);

    /// <summary>
    /// Apply every enabled effect to <paramref name="samples"/> in order.
    /// </summary>
    void Process(Span<float> samples, int sampleRateHz, int channels);
}
