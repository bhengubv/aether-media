// Package social provides the SocialGraph for managing follow relationships.
package social

import (
	"sort"
	"sync"
)

// SocialGraph tracks which UHIDs the local user is following.
// All methods are safe for concurrent use.
type SocialGraph struct {
	following map[string]struct{}
	mu        sync.RWMutex
}

// NewSocialGraph constructs a new empty SocialGraph.
func NewSocialGraph() *SocialGraph {
	return &SocialGraph{following: make(map[string]struct{})}
}

// Follow adds uhid to the following set.  No-op if already following.
func (g *SocialGraph) Follow(uhid string) {
	g.mu.Lock()
	defer g.mu.Unlock()
	g.following[uhid] = struct{}{}
}

// Unfollow removes uhid from the following set.  No-op if not following.
func (g *SocialGraph) Unfollow(uhid string) {
	g.mu.Lock()
	defer g.mu.Unlock()
	delete(g.following, uhid)
}

// IsFollowing returns true when uhid is in the following set.
func (g *SocialGraph) IsFollowing(uhid string) bool {
	g.mu.RLock()
	defer g.mu.RUnlock()
	_, ok := g.following[uhid]
	return ok
}

// Following returns a sorted slice of all followed UHIDs.
func (g *SocialGraph) Following() []string {
	g.mu.RLock()
	defer g.mu.RUnlock()
	result := make([]string, 0, len(g.following))
	for k := range g.following {
		result = append(result, k)
	}
	sort.Strings(result)
	return result
}

// Count returns the number of followed accounts.
func (g *SocialGraph) Count() int {
	g.mu.RLock()
	defer g.mu.RUnlock()
	return len(g.following)
}
