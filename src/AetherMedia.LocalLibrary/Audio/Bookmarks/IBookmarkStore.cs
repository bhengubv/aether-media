// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Bookmarks;

/// <summary>Persists and queries <see cref="Bookmark"/> records.</summary>
public interface IBookmarkStore
{
    Task AddAsync(Bookmark bookmark, CancellationToken ct = default);
    Task<IReadOnlyList<Bookmark>> ListAsync(string? filePath = null, CancellationToken ct = default);
    Task<Bookmark?> ResumeFor(string filePath, CancellationToken ct = default);
    Task RemoveAsync(Bookmark bookmark, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
