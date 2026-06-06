// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Reel;

/// <summary>
/// A single comment on a Reel. Comments are stored locally and gossiped peer-to-peer
/// — there is no central comment database.
///
/// Threading is supported via <see cref="ParentCommentId"/>: a null value indicates
/// a top-level comment; a non-null value is a reply to an existing comment.
/// </summary>
public sealed record ReelComment(
    /// <summary>Locally-generated unique identifier (GUID string).</summary>
    string CommentId,

    /// <summary>Content hash of the Reel being commented on.</summary>
    string ReelHash,

    /// <summary>UHID of the commenter.</summary>
    string AuthorUhid,

    /// <summary>Comment body text.</summary>
    string Text,

    /// <summary>Unix millisecond timestamp when the comment was created.</summary>
    long CreatedAtMs,

    /// <summary>
    /// <see cref="CommentId"/> of the parent comment, or <c>null</c> for a
    /// top-level comment.
    /// </summary>
    string? ParentCommentId
);
