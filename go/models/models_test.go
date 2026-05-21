package models_test

import (
	"strings"
	"testing"
	"time"

	"github.com/bhengubv/aether-media/go/models"
)

// ── helpers ─────────────────────────────────────────────────────────────────

func makeContent(durationMs int64, contentType string) models.MediaContent {
	return models.MediaContent{
		ContentHash: "abc123",
		Title:       "Test",
		DurationMs:  durationMs,
		Codec:       "h264",
		ContentType: contentType,
		CreatorUHID: "u1",
		SizeBytes:   1_000_000,
		CreatedAtMs: time.Now().UnixMilli(),
	}
}

func strPtr(s string) *string { return &s }

// ── FormattedDuration ────────────────────────────────────────────────────────

func TestFormattedDuration_ZeroIsLive(t *testing.T) {
	if got := makeContent(0, "video/mp4").FormattedDuration(); got != "Live" {
		t.Fatalf("want Live, got %q", got)
	}
}

func TestFormattedDuration_NegativeIsLive(t *testing.T) {
	if got := makeContent(-1, "video/mp4").FormattedDuration(); got != "Live" {
		t.Fatalf("want Live, got %q", got)
	}
}

func TestFormattedDuration_SubHour(t *testing.T) {
	// 272 000 ms = 4 min 32 sec → "4:32"
	if got := makeContent(272_000, "video/mp4").FormattedDuration(); got != "4:32" {
		t.Fatalf("want 4:32, got %q", got)
	}
}

func TestFormattedDuration_PadsSeconds(t *testing.T) {
	// 65 000 ms = 1 min 5 sec → "1:05"
	if got := makeContent(65_000, "video/mp4").FormattedDuration(); got != "1:05" {
		t.Fatalf("want 1:05, got %q", got)
	}
}

func TestFormattedDuration_ExactlyOneHour(t *testing.T) {
	if got := makeContent(3_600_000, "video/mp4").FormattedDuration(); got != "1:00:00" {
		t.Fatalf("want 1:00:00, got %q", got)
	}
}

func TestFormattedDuration_OverHour(t *testing.T) {
	// 5 025 000 ms = 1 h 23 min 45 sec → "1:23:45"
	if got := makeContent(5_025_000, "video/mp4").FormattedDuration(); got != "1:23:45" {
		t.Fatalf("want 1:23:45, got %q", got)
	}
}

// ── IsVideo / IsAudio ────────────────────────────────────────────────────────

func TestIsVideo_TrueForVideoMimeType(t *testing.T) {
	if !makeContent(1000, "video/mp4").IsVideo() {
		t.Fatal("expected IsVideo true for video/mp4")
	}
}

func TestIsVideo_FalseForAudioMimeType(t *testing.T) {
	if makeContent(1000, "audio/mp4").IsVideo() {
		t.Fatal("expected IsVideo false for audio/mp4")
	}
}

func TestIsAudio_TrueForAudioMimeType(t *testing.T) {
	if !makeContent(1000, "audio/flac").IsAudio() {
		t.Fatal("expected IsAudio true for audio/flac")
	}
}

func TestIsAudio_FalseForVideoMimeType(t *testing.T) {
	if makeContent(1000, "video/webm").IsAudio() {
		t.Fatal("expected IsAudio false for video/webm")
	}
}

func TestIsVideo_CaseInsensitive(t *testing.T) {
	if !makeContent(1000, "VIDEO/MP4").IsVideo() {
		t.Fatal("expected IsVideo true for VIDEO/MP4")
	}
}

// ── MediaReaction.Validate ───────────────────────────────────────────────────

func makeReaction(rtype models.MediaReactionType, msg *string) models.MediaReaction {
	return models.MediaReaction{
		ReactionID:  "r1",
		ContentHash: "abc",
		FromUHID:    "u1",
		Type:        rtype,
		PositionMs:  0,
		Message:     msg,
		SentAtMs:    time.Now().UnixMilli(),
	}
}

func TestReactionValidate_LikeNoMessage_OK(t *testing.T) {
	if err := makeReaction(models.ReactionLike, nil).Validate(); err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestReactionValidate_CommentWithMessage_OK(t *testing.T) {
	if err := makeReaction(models.ReactionComment, strPtr("hello")).Validate(); err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestReactionValidate_CommentWithoutMessage_Error(t *testing.T) {
	if err := makeReaction(models.ReactionComment, nil).Validate(); err == nil {
		t.Fatal("expected error for Comment without message")
	}
}

func TestReactionValidate_CommentBlankMessage_Error(t *testing.T) {
	blank := "   "
	if err := makeReaction(models.ReactionComment, &blank).Validate(); err == nil {
		t.Fatal("expected error for Comment with blank message")
	}
}

func TestReactionValidate_LikeWithMessage_Error(t *testing.T) {
	if err := makeReaction(models.ReactionLike, strPtr("oops")).Validate(); err == nil {
		t.Fatal("expected error for Like with message")
	}
}

func TestReactionValidate_NegativePosition_Error(t *testing.T) {
	r := makeReaction(models.ReactionLike, nil)
	r.PositionMs = -1
	if err := r.Validate(); err == nil {
		t.Fatal("expected error for negative position")
	}
}

func TestReactionValidate_EmptyContentHash_Error(t *testing.T) {
	r := makeReaction(models.ReactionLike, nil)
	r.ContentHash = "   "
	if err := r.Validate(); err == nil {
		t.Fatal("expected error for empty content_hash")
	}
}

// ── MediaProfile.ShortBio ────────────────────────────────────────────────────

func TestShortBio_NilBio_ReturnsEmpty(t *testing.T) {
	p := models.MediaProfile{UHID: "u", DisplayName: "Test"}
	if got := p.ShortBio(); got != "" {
		t.Fatalf("want empty, got %q", got)
	}
}

func TestShortBio_ShortBio_Unchanged(t *testing.T) {
	bio := "Hello world"
	p := models.MediaProfile{UHID: "u", DisplayName: "T", Bio: &bio}
	if got := p.ShortBio(); got != bio {
		t.Fatalf("want %q, got %q", bio, got)
	}
}

func TestShortBio_LongBio_TruncatedWithEllipsis(t *testing.T) {
	bio := strings.Repeat("word ", 30) // well over 120 chars
	p := models.MediaProfile{UHID: "u", DisplayName: "T", Bio: &bio}
	got := p.ShortBio()
	if !strings.HasSuffix(got, "…") {
		t.Fatalf("expected ellipsis, got %q", got)
	}
	runes := []rune(got)
	if len(runes) > 122 { // 120 + "…"
		t.Fatalf("truncated bio too long: %d runes", len(runes))
	}
}

// ── MediaFeedItem helpers ────────────────────────────────────────────────────

func TestReactionTotal(t *testing.T) {
	item := models.MediaFeedItem{
		Content:       makeContent(1000, "video/mp4"),
		LikeCount:    10,
		ShareCount:   5,
		CommentCount: 3,
		PublishedAtMs: time.Now().UnixMilli(),
	}
	if got := item.ReactionTotal(); got != 18 {
		t.Fatalf("want 18, got %d", got)
	}
}

func TestIsNew_RecentItem_True(t *testing.T) {
	item := models.MediaFeedItem{
		Content:       makeContent(1000, "video/mp4"),
		PublishedAtMs: time.Now().Add(-1 * time.Hour).UnixMilli(),
	}
	if !item.IsNew() {
		t.Fatal("expected IsNew true for 1-hour-old item")
	}
}

func TestIsNew_OldItem_False(t *testing.T) {
	item := models.MediaFeedItem{
		Content:       makeContent(1000, "video/mp4"),
		PublishedAtMs: time.Now().Add(-25 * time.Hour).UnixMilli(),
	}
	if item.IsNew() {
		t.Fatal("expected IsNew false for 25-hour-old item")
	}
}

// ── LiveStream.ElapsedMs ─────────────────────────────────────────────────────

func TestElapsedMs_PastStartTime_Positive(t *testing.T) {
	s := models.LiveStream{
		StreamID:    "s1",
		Title:       "Test",
		CreatorUHID: "u1",
		StartedAtMs: time.Now().Add(-5 * time.Second).UnixMilli(),
		IsActive:    true,
	}
	elapsed := s.ElapsedMs()
	if elapsed < 4000 || elapsed > 10000 {
		t.Fatalf("expected ~5000ms elapsed, got %d", elapsed)
	}
}

func TestElapsedMs_FutureStartTime_ReturnsZero(t *testing.T) {
	s := models.LiveStream{
		StreamID:    "s1",
		StartedAtMs: time.Now().Add(10 * time.Second).UnixMilli(),
	}
	if elapsed := s.ElapsedMs(); elapsed != 0 {
		t.Fatalf("expected 0 for future start, got %d", elapsed)
	}
}
