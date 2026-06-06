// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Sync;

/// <summary>What a sync operation would do — produced by a dry-run pass.</summary>
public sealed record SyncPlan(
    IReadOnlyList<string> ToCopy,
    IReadOnlyList<string> ToDelete,
    long TotalBytesToCopy);
