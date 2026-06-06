import Foundation

// MARK: - MediaReactionType

public enum MediaReactionType: String, Codable, CustomStringConvertible {
    case like       = "like"
    case share      = "share"
    case comment    = "comment"
    case superReact = "super_react"

    public var description: String {
        switch self {
        case .like:       return "Like"
        case .share:      return "Share"
        case .comment:    return "Comment"
        case .superReact: return "SuperReact"
        }
    }
}

// MARK: - MediaContent

public struct MediaContent: Codable, Identifiable, Hashable {
    public var id: String { contentHash }

    public let contentHash: String
    public let title: String
    public let durationMs: Int64
    public let codec: String
    public let contentType: String
    public let creatorUhid: String
    public let sizeBytes: Int64
    public let createdAtMs: Int64
    public let thumbnailHash: String?
    public let tags: [String]

    public init(
        contentHash: String,
        title: String,
        durationMs: Int64,
        codec: String,
        contentType: String,
        creatorUhid: String,
        sizeBytes: Int64,
        createdAtMs: Int64,
        thumbnailHash: String? = nil,
        tags: [String] = []
    ) {
        self.contentHash   = contentHash
        self.title         = title
        self.durationMs    = durationMs
        self.codec         = codec
        self.contentType   = contentType
        self.creatorUhid   = creatorUhid
        self.sizeBytes     = sizeBytes
        self.createdAtMs   = createdAtMs
        self.thumbnailHash = thumbnailHash
        self.tags          = tags
    }

    private enum CodingKeys: String, CodingKey {
        case contentHash   = "content_hash"
        case title
        case durationMs    = "duration_ms"
        case codec
        case contentType   = "content_type"
        case creatorUhid   = "creator_uhid"
        case sizeBytes     = "size_bytes"
        case createdAtMs   = "created_at_ms"
        case thumbnailHash = "thumbnail_hash"
        case tags
    }

    /// Human-readable duration:
    /// - 0 ms → "Live"
    /// - < 1 hour → "M:SS"
    /// - >= 1 hour → "H:MM:SS"
    public var formattedDuration: String {
        guard durationMs > 0 else { return "Live" }
        let totalSeconds = Int(durationMs / 1000)
        let hours   = totalSeconds / 3600
        let minutes = (totalSeconds % 3600) / 60
        let seconds = totalSeconds % 60
        if hours > 0 {
            return String(format: "%d:%02d:%02d", hours, minutes, seconds)
        }
        return String(format: "%d:%02d", minutes, seconds)
    }

    public var isVideo: Bool { contentType.lowercased().hasPrefix("video/") }
    public var isAudio: Bool { contentType.lowercased().hasPrefix("audio/") }
}

// MARK: - MediaReaction

public enum MediaReactionError: Error, LocalizedError {
    case emptyContentHash
    case emptyFromUhid
    case commentRequiresMessage
    case nonCommentMustNotHaveMessage(MediaReactionType)

    public var errorDescription: String? {
        switch self {
        case .emptyContentHash:
            return "contentHash must not be empty"
        case .emptyFromUhid:
            return "fromUhid must not be empty"
        case .commentRequiresMessage:
            return "A message is required for Comment reactions"
        case .nonCommentMustNotHaveMessage(let type):
            return "message must be nil for \(type) reactions"
        }
    }
}

public struct MediaReaction: Codable, Identifiable {
    public var id: String { reactionId }

    public let reactionId: String
    public let contentHash: String
    public let fromUhid: String
    public let type: MediaReactionType
    public let positionMs: Int64
    public let message: String?
    public let sentAtMs: Int64

    private enum CodingKeys: String, CodingKey {
        case reactionId  = "reaction_id"
        case contentHash = "content_hash"
        case fromUhid    = "from_uhid"
        case type
        case positionMs  = "position_ms"
        case message
        case sentAtMs    = "sent_at_ms"
    }

    /// Failable init that validates business rules.
    public init(
        reactionId: String,
        contentHash: String,
        fromUhid: String,
        type: MediaReactionType,
        positionMs: Int64,
        message: String?,
        sentAtMs: Int64
    ) throws {
        guard !contentHash.trimmingCharacters(in: .whitespaces).isEmpty else {
            throw MediaReactionError.emptyContentHash
        }
        guard !fromUhid.trimmingCharacters(in: .whitespaces).isEmpty else {
            throw MediaReactionError.emptyFromUhid
        }
        if type == .comment {
            guard let msg = message, !msg.trimmingCharacters(in: .whitespaces).isEmpty else {
                throw MediaReactionError.commentRequiresMessage
            }
        } else {
            if message != nil {
                throw MediaReactionError.nonCommentMustNotHaveMessage(type)
            }
        }
        self.reactionId  = reactionId
        self.contentHash = contentHash
        self.fromUhid    = fromUhid
        self.type        = type
        self.positionMs  = positionMs
        self.message     = message
        self.sentAtMs    = sentAtMs
    }
}

// MARK: - MediaProfile

public struct MediaProfile: Codable, Identifiable {
    public var id: String { uhid }

    public let uhid: String
    public let displayName: String
    public let avatarHash: String?
    public let bio: String?
    public let aethermeshTag: String
    public let followerCount: Int
    public let followingCount: Int
    public let contentCount: Int
    public let isVerified: Bool
    public let joinedAtMs: Int64

    private enum CodingKeys: String, CodingKey {
        case uhid
        case displayName   = "display_name"
        case avatarHash    = "avatar_hash"
        case bio
        case aethermeshTag     = "aethermesh_tag"
        case followerCount = "follower_count"
        case followingCount = "following_count"
        case contentCount  = "content_count"
        case isVerified    = "is_verified"
        case joinedAtMs    = "joined_at_ms"
    }

    private static let shortBioMax = 120

    /// Bio trimmed to 120 chars at the last word boundary, with "…" appended.
    /// Returns "" when bio is nil or whitespace.
    public var shortBio: String {
        guard let bio, !bio.trimmingCharacters(in: .whitespaces).isEmpty else { return "" }
        let trimmed = bio.trimmingCharacters(in: .whitespaces)
        guard trimmed.count > MediaProfile.shortBioMax else { return trimmed }

        let cutIndex = trimmed.index(trimmed.startIndex, offsetBy: MediaProfile.shortBioMax)
        let cut = String(trimmed[..<cutIndex])
        if let lastSpaceRange = cut.range(of: " ", options: .backwards) {
            let boundary = String(cut[..<lastSpaceRange.lowerBound]).trimmingCharacters(in: .whitespaces)
            return boundary + "…"
        }
        return cut.trimmingCharacters(in: .whitespaces) + "…"
    }
}

// MARK: - LiveStream

public struct LiveStream: Codable, Identifiable {
    public var id: String { streamId }

    public let streamId: String
    public let title: String
    public let creatorUhid: String
    public let codec: String
    public let segmentDurationMs: Int
    public let startedAtMs: Int64
    public let viewerCount: Int
    public let isActive: Bool
    public let tags: [String]

    private enum CodingKeys: String, CodingKey {
        case streamId          = "stream_id"
        case title
        case creatorUhid       = "creator_uhid"
        case codec
        case segmentDurationMs = "segment_duration_ms"
        case startedAtMs       = "started_at_ms"
        case viewerCount       = "viewer_count"
        case isActive          = "is_active"
        case tags
    }

    /// Wall-clock milliseconds since the broadcast started.  Always >= 0.
    public var elapsedMs: Int64 {
        let nowMs = Int64(Date().timeIntervalSince1970 * 1000)
        return max(0, nowMs - startedAtMs)
    }

    /// Human-readable elapsed time (H:MM:SS or M:SS).
    public var elapsedFormatted: String {
        let totalSeconds = Int(elapsedMs / 1000)
        let hours   = totalSeconds / 3600
        let minutes = (totalSeconds % 3600) / 60
        let seconds = totalSeconds % 60
        if hours > 0 {
            return String(format: "%d:%02d:%02d", hours, minutes, seconds)
        }
        return String(format: "%d:%02d", minutes, seconds)
    }
}

// MARK: - MediaFeedItem

public struct MediaFeedItem: Codable, Identifiable {
    public var id: String { content.contentHash }

    public let content: MediaContent
    public let likeCount: Int
    public let shareCount: Int
    public let commentCount: Int
    public let watchCount: Int
    public let isLive: Bool
    public let streamId: String?
    public let topReactions: [MediaReaction]
    public let publishedAtMs: Int64
    public let watchedMs: Int64

    private enum CodingKeys: String, CodingKey {
        case content
        case likeCount    = "like_count"
        case shareCount   = "share_count"
        case commentCount = "comment_count"
        case watchCount   = "watch_count"
        case isLive       = "is_live"
        case streamId     = "stream_id"
        case topReactions = "top_reactions"
        case publishedAtMs = "published_at_ms"
        case watchedMs    = "watched_ms"
    }

    public init(
        content: MediaContent,
        likeCount: Int,
        shareCount: Int,
        commentCount: Int,
        watchCount: Int,
        isLive: Bool,
        streamId: String?,
        topReactions: [MediaReaction],
        publishedAtMs: Int64,
        watchedMs: Int64 = 0
    ) {
        self.content      = content
        self.likeCount    = likeCount
        self.shareCount   = shareCount
        self.commentCount = commentCount
        self.watchCount   = watchCount
        self.isLive       = isLive
        self.streamId     = streamId
        self.topReactions = topReactions
        self.publishedAtMs = publishedAtMs
        self.watchedMs    = watchedMs
    }

    /// True when published within the last 24 hours.
    public var isNew: Bool {
        let nowMs = Int64(Date().timeIntervalSince1970 * 1000)
        return (nowMs - publishedAtMs) < 86_400_000
    }

    /// Likes + shares + comments.
    public var reactionTotal: Int { likeCount + shareCount + commentCount }
}
