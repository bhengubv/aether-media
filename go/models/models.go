// Package models provides the core domain types for Aether Media,
// mirroring the C# Aether.Media.Core.Models namespace.
package models

import (
	"encoding/json"
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
		return "like"
	case ReactionShare:
		return "share"
	case ReactionComment:
		return "comment"
	case ReactionSuperReact:
		return "super_react"
	default:
		return fmt.Sprintf("Unknown(%d)", int(t))
	}
}

// MarshalJSON serialises MediaReactionType as a lowercase string (wire format).
func (t MediaReactionType) MarshalJSON() ([]byte, error) {
	s := t.String()
	if strings.HasPrefix(s, "Unknown(") {
		return nil, fmt.Errorf("models: cannot marshal unknown MediaReactionType %d", int(t))
	}
	return json.Marshal(s)
}

// UnmarshalJSON deserialises a lowercase string into a MediaReactionType.
func (t *MediaReactionType) UnmarshalJSON(data []byte) error {
	var s string
	if err := json.Unmarshal(data, &s); err != nil {
		return fmt.Errorf("models: MediaReactionType must be a string: %w", err)
	}
	switch s {
	case "like":
		*t = ReactionLike
	case "share":
		*t = ReactionShare
	case "comment":
		*t = ReactionComment
	case "super_react":
		*t = ReactionSuperReact
	default:
		return fmt.Errorf("models: unknown MediaReactionType wire value %q", s)
	}
	return nil
}

// MediaContent is an immutable description of a single piece of media.
// The primary key is ContentHash — a SHA-256 hex digest of the encoded bytes.
type MediaContent struct {
	ContentHash   string   `json:"content_hash"`
	Title         string   `json:"title"`
	DurationMs    int64    `json:"duration_ms"`
	Codec         string   `json:"codec"`
	ContentType   string   `json:"content_type"`
	CreatorUHID   string   `json:"creator_uhid"`
	SizeBytes     int64    `json:"size_bytes"`
	CreatedAtMs   int64    `json:"created_at_ms"`
	ThumbnailHash *string  `json:"thumbnail_hash"`
	Tags          []string `json:"tags"`
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
	ReactionID  string            `json:"reaction_id"`
	ContentHash string            `json:"content_hash"`
	FromUHID    string            `json:"from_uhid"`
	Type        MediaReactionType `json:"type"`
	PositionMs  int64             `json:"position_ms"`
	Message     *string           `json:"message"`
	SentAtMs    int64             `json:"sent_at_ms"`
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
	UHID           string  `json:"uhid"`
	DisplayName    string  `json:"display_name"`
	AvatarHash     *string `json:"avatar_hash"`
	Bio            *string `json:"bio"`
	AetherTag      string  `json:"aether_tag"`
	FollowerCount  int     `json:"follower_count"`
	FollowingCount int     `json:"following_count"`
	ContentCount   int     `json:"content_count"`
	IsVerified     bool    `json:"is_verified"`
	JoinedAtMs     int64   `json:"joined_at_ms"`
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
	StreamID          string   `json:"stream_id"`
	Title             string   `json:"title"`
	CreatorUHID       string   `json:"creator_uhid"`
	Codec             string   `json:"codec"`
	SegmentDurationMs int      `json:"segment_duration_ms"`
	StartedAtMs       int64    `json:"started_at_ms"`
	ViewerCount       int      `json:"viewer_count"`
	IsActive          bool     `json:"is_active"`
	Tags              []string `json:"tags"`
}

// ElapsedMs returns wall-clock milliseconds since the stream started.
// Clamped to 0 when StartedAtMs is in the future.
func (s LiveStream) ElapsedMs() int64 {
	startedAt := time.UnixMilli(s.StartedAtMs)
	elapsed := time.Since(startedAt).Milliseconds()
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
	Content      MediaContent   `json:"content"`
	LikeCount    int            `json:"like_count"`
	ShareCount   int            `json:"share_count"`
	CommentCount int            `json:"comment_count"`
	WatchCount   int            `json:"watch_count"`
	IsLive       bool           `json:"is_live"`
	StreamID     *string        `json:"stream_id"`
	TopReactions []MediaReaction `json:"top_reactions"`
	PublishedAtMs int64         `json:"published_at_ms"`
	// WatchedMs is the number of ms the local user has watched this content.
	WatchedMs int64 `json:"watched_ms"`
}

// IsNew returns true when the item was published within the last 24 hours.
func (f MediaFeedItem) IsNew() bool {
	publishedAt := time.UnixMilli(f.PublishedAtMs)
	return time.Since(publishedAt) < 24*time.Hour
}

// ReactionTotal returns the sum of likes + shares + comments.
func (f MediaFeedItem) ReactionTotal() int {
	return f.LikeCount + f.ShareCount + f.CommentCount
}
