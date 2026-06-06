// SPDX-License-Identifier: MIT

using AetherNet.Media.Core.Models;

namespace AetherNet.Media.UI.Shared.ViewModels;

/// <summary>Wraps a <see cref="MediaProfile"/> for display in profile and nearby views.</summary>
public sealed class MediaProfileViewModel : ViewModelBase
{
    public MediaProfile Source { get; }

    public string  DisplayName    => Source.DisplayName;
    public string  AetherNetTagValue => Source.AetherNetTagValue;
    public string  ShortBio       => Source.ShortBio;
    public int     FollowerCount  => Source.FollowerCount;
    public int     FollowingCount => Source.FollowingCount;
    public int     ContentCount   => Source.ContentCount;
    public bool    IsVerified     => Source.IsVerified;
    public string? AvatarHash     => Source.AvatarHash;

    public MediaProfileViewModel(MediaProfile source)
    {
        Source = source;
    }
}
