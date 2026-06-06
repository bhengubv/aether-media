// SPDX-License-Identifier: MIT

namespace AetherNet.Trust.Core;

/// <summary>
/// Classifies content by intended use so that different trust rings can be
/// configured per category (e.g. a developer-key ring for APKs is stricter
/// than the author-key ring for user-generated media).
/// </summary>
public enum ContentCategory
{
    /// <summary>
    /// Wildcard sentinel — keys added to this category are checked for every
    /// content type when no category-specific key matches.
    /// </summary>
    Any,

    /// <summary>
    /// Installable application binaries: APK, IPA, EXE, DEB, RPM.
    /// Typically requires a developer signing key from the publisher's ring.
    /// </summary>
    AppBinary,

    /// <summary>
    /// User-generated audio, video, and image files (MIME: video/*, audio/*, image/*).
    /// The author's own Ed25519 key is the natural trust anchor.
    /// </summary>
    MediaContent,

    /// <summary>
    /// Short-form social posts, reactions, and text announcements.
    /// </summary>
    SocialPost,

    /// <summary>
    /// High-value documents: PDFs, land deeds, academic certificates, medical records.
    /// Often combined with aether-vault escrow.
    /// </summary>
    Document,

    /// <summary>
    /// Avatar blobs and profile-sync payloads originating from identity packets.
    /// </summary>
    ProfileData,
}
