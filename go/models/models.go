// Package models provides the core domain types for Aether Media,
// mirroring the C# Aether.Media.Core.Models namespace.
package models

import (
	"fmt"
	"strings"
	"time"
)

// MediaReactionType classifies the kind of reaction a viewer sends.
type MediaReactionType int

const (
	ReactionLike       MediaReactionType = 1
	ReactionShare      MediaReactionType = 2
	ReactionComment    MediaReactionType = 3
	ReactionSuperReact MediaReactionType = 4
)

func (t MediaReactionType) String() string {
	switch t {
	case ReactionLike:
		return "Like"
	case ReactionShare:
		return "Share"
	case ReactionComment:
		return "Comment"
	case ReactionSuperReact:
		return "SuperReact"
	default:
		return fmt.Sprintf("Unknown(%d)", int(t))
	}
}

// MediaContent is an immutable description of a single piece of media.
// The primary key is ContentHash — a SHA-256 hex digest of the encoded bytes.
type MediaContent struct {
	ContentHash  string
	Title        string
	DurationMs   int64
	Codec        string
	ContentType  string
	CreatorUHID  string
	SizeBytes    int64
	CreatedAt    time.Time
	ThumbnailHash *string
	Tags         []string
}

// FormattedDuration returns a human-readable duration string.
//   - 0  → "Live"
//   - < 1 hour → "M:SS"
//   - >= 1 hour → "H:MM:SS"
func (m MediaContent) FormattedDuration() string {
	if m.DurationMs <= 0 {
		return "Live"
	}
	totalSecs := m.DurationMs / 1000
	hours   := totalSecs / 3600
	minutes := (totalSecs % 3600) / 60
	seconds := totalSecs % 60
	if hours > 0 {
		return fmt.Sprintf("%d:%02d:%02d", hours, minutes, seconds)
	}
	return fmt.Sprintf("%d:%02d", minutes, seconds)
}

// IsVideo returns true when ContentType starts with "video/".
func (m MediaContent) IsVideo() bool {
	return strings.HasPrefix(strings.ToLower(m.ContentType), "video/")
}

// IsAudio returns true when ContentType starts with "audio/".
func (m MediaContent) IsAudio() bool {
	return strings.HasPrefix(strings.ToLower(m.ContentType), "audio/")
}

// MediaReaction is a timestamped reaction sent by a viewer.
type MediaReaction struct {
	ReactionID  string
	ContentHash string
	FromUHID    string
	Type        MediaReactionType
	PositionMs  int64
	Message     *string // required for Comment, nil otherwise
	SentAt      time.Time
}

// Validate checks the reaction business rules and returns an error if violated.
func (r MediaReaction) Validate() error {
	if strings.TrimSpace(r.ContentHash) == "" {
		return fmt.Errorf("content_hash must not be empty")
	}
	if strings.TrimSpace(r.FromUHID) == "" {
		return fmt.Errorf("from_uhid must not be empty")
	}
	if r.PositionMs < 0 {
		return fmt.Errorf("position_ms must be >= 0")
	}
	if r.Type == ReactionComment {
		if r.Message == nil || strings.TrimSpace(*r.Message) == "" {
			return fmt.Errorf("a message is required for Comment reactions")
		}
	} else {
		if r.Message != nil {
			return fmt.Errorf("message must be nil for %s reactions", r.Type)
		}
	}
	return nil
}

// MediaProfile is the public profile of a content creator on Aether.
type MediaProfile struct {
	UHID           string
	DisplayName    string
	AvatarHash     *string
	Bio            *string
	AetherTagValue string
	FollowerCount  int
	FollowingCount int
	ContentCount   int
	IsVerified     bool
	JoinedAt       time.Time
}

const shortBioMax = 120

// ShortBio returns the bio trimmed to 120 chars at the last word boundary
// with "…" appended.  Returns "" when Bio is nil or whitespace.
func (p MediaProfile) ShortBio() string {
	if p.Bio == nil {
		return ""
	}
	bio := strings.TrimSpace(*p.Bio)
	if bio == "" {
		return ""
	}
	runes := []rune(bio)
	if len(runes) <= shortBioMax {
		return bio
	}
	cut := string(runes[:shortBioMax])
	lastSpace := strings.LastIndex(cut, " ")
	if lastSpace <= 0 {
		lastSpace = shortBioMax
	}
	trimmed := strings.TrimRight(string(runes[:lastSpace]), " \t")
	return trimmed + "…"
}

// LiveStream represents an active live broadcast on the Aether mesh.
type LiveStream struct {
	StreamID           string
	Title              string
	CreatorUHID        string
	Codec              string
	SegmentDurationMs  int
	StartedAt          time.Time
	ViewerCount        int
	IsActive           bool
	Tags               []string
}

// ElapsedMs returns wall-clock milliseconds since the stream started.
// Clamped to 0 when StartedAt is in the future.
func (s LiveStream) ElapsedMs() int64 {
	elapsed := time.Since(s.StartedAt).Milliseconds()
	if elapsed < 0 {
		return 0
	}
	return elapsed
}

// ElapsedFormatted returns a human-readable elapsed time (H:MM:SS or M:SS).
func (s LiveStream) ElapsedFormatted() string {
	totalSecs := s.ElapsedMs() / 1000
	hours   := totalSecs / 3600
	minutes := (totalSecs % 3600) / 60
	seconds := totalSecs % 60
	if hours > 0 {
		return fmt.Sprintf("%d:%02d:%02d", hours, minutes, seconds)
	}
	return fmt.Sprintf("%d:%02d", minutes, seconds)
}

// MediaFeedItem combines a piece of content with engagement counters.
type MediaFeedItem struct {
	Content      MediaContent
	LikeCount    int
	ShareCount   int
	CommentCount int
	WatchCount   int
	IsLive       bool
	StreamID     *string
	TopReactions []MediaReaction
	PublishedAt  time.Time
	// WatchedMs is the number of ms the local user has watched this content.
	WatchedMs int64
}

// IsNew returns true when the item was published within the last 24 hours.
func (f MediaFeedItem) IsNew() bool {
	return time.Since(f.PublishedAt) < 24*time.Hour
}

// ReactionTotal returns the sum of likes + shares + comments.
func (f MediaFeedItem) ReactionTotal() int {
	return f.LikeCount + f.ShareCount + f.CommentCount
}
