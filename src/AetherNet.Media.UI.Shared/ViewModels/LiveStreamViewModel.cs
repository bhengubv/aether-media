// SPDX-License-Identifier: MIT

using AetherNet.Media.Core.Models;

namespace AetherNet.Media.UI.Shared.ViewModels;

/// <summary>Wraps a <see cref="LiveStream"/> for display in the home and nearby views.</summary>
public sealed class LiveStreamViewModel : ViewModelBase
{
    public LiveStream Source { get; }

    public Guid   StreamId         => Source.StreamId;
    public string Title            => Source.Title;
    public string CreatorUhid      => Source.CreatorUhid;
    public int    ViewerCount      => Source.ViewerCount;
    public string ElapsedFormatted => Source.ElapsedFormatted;
    public string Codec            => Source.Codec;

    public LiveStreamViewModel(LiveStream source)
    {
        Source = source;
    }
}
