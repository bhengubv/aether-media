package social_test

import (
	"testing"

	"github.com/bhengubv/aether-media/go/social"
)

func TestFollow_AddsToSet(t *testing.T) {
	g := social.NewSocialGraph()
	g.Follow("alice")
	if !g.IsFollowing("alice") {
		t.Fatal("expected alice to be followed")
	}
}

func TestFollow_Multiple(t *testing.T) {
	g := social.NewSocialGraph()
	g.Follow("alice")
	g.Follow("bob")
	if g.Count() != 2 {
		t.Fatalf("expected count 2, got %d", g.Count())
	}
}

func TestFollow_Idempotent(t *testing.T) {
	g := social.NewSocialGraph()
	g.Follow("alice")
	g.Follow("alice")
	if g.Count() != 1 {
		t.Fatalf("expected count 1 (idempotent), got %d", g.Count())
	}
}

func TestUnfollow_RemovesFromSet(t *testing.T) {
	g := social.NewSocialGraph()
	g.Follow("alice")
	g.Unfollow("alice")
	if g.IsFollowing("alice") {
		t.Fatal("expected alice to be unfollowed")
	}
	if g.Count() != 0 {
		t.Fatalf("expected count 0, got %d", g.Count())
	}
}

func TestUnfollow_NonFollowing_Noop(t *testing.T) {
	g := social.NewSocialGraph()
	g.Unfollow("ghost") // must not panic
	if g.Count() != 0 {
		t.Fatalf("expected count 0, got %d", g.Count())
	}
}

func TestFollowing_ReturnsSorted(t *testing.T) {
	g := social.NewSocialGraph()
	g.Follow("charlie")
	g.Follow("alice")
	g.Follow("bob")
	list := g.Following()
	expected := []string{"alice", "bob", "charlie"}
	if len(list) != len(expected) {
		t.Fatalf("expected %v, got %v", expected, list)
	}
	for i, v := range expected {
		if list[i] != v {
			t.Fatalf("at index %d: want %q, got %q", i, v, list[i])
		}
	}
}

func TestIsFollowing_False_ForUnknown(t *testing.T) {
	g := social.NewSocialGraph()
	if g.IsFollowing("unknown") {
		t.Fatal("expected false for unknown uhid")
	}
}

func TestCount_StartsAtZero(t *testing.T) {
	g := social.NewSocialGraph()
	if g.Count() != 0 {
		t.Fatalf("expected count 0, got %d", g.Count())
	}
}
