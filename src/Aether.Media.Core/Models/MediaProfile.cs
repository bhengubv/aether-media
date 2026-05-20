namespace Aether.Media.Core.Models;

/// <summary>
/// Public profile of a content creator on the Aether network.
/// </summary>
public sealed record MediaProfile(
    string Uhid,
    string DisplayName,
    string? AvatarHash,
    string? Bio,
    string AetherTagValue,
    int FollowerCount,
    int FollowingCount,
    int ContentCount,
    bool IsVerified,
    DateTime JoinedAt)
{
    private const int ShortBioMaxLength = 120;

    /// <summary>
    /// The creator's bio truncated to 120 characters.  If the bio exceeds the
    /// limit it is cut at the last word boundary before character 120 and an
    /// ellipsis (<c>…</c>) is appended.  Returns an empty string when
    /// <see cref="Bio"/> is null or whitespace.
    /// </summary>
    public string ShortBio
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Bio))
                return string.Empty;

            var trimmed = Bio.Trim();
            if (trimmed.Length <= ShortBioMaxLength)
                return trimmed;

            // Cut at the last space within the limit so we don't break mid-word.
            var cut = trimmed.AsSpan(0, ShortBioMaxLength);
            var lastSpace = cut.LastIndexOf(' ');

            var boundary = lastSpace > 0 ? lastSpace : ShortBioMaxLength;
            return string.Concat(trimmed.AsSpan(0, boundary).TrimEnd(), "…");
        }
    }
}
