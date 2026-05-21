// SPDX-License-Identifier: MIT
// wire-roundtrip: reads golden JSON fixtures, round-trips through Go models, prints result.
package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"

	"github.com/bhengubv/aether-media/go/models"
)

func repoRoot() string {
	// Walk up from the current working directory until we find the go.mod
	// that belongs to this module, then return its parent as the repo root.
	dir, err := os.Getwd()
	if err != nil {
		panic(err)
	}
	for {
		if _, err := os.Stat(filepath.Join(dir, "go.mod")); err == nil {
			// go.mod lives inside the "go/" sub-directory; repo root is one level up.
			return filepath.Dir(dir)
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			break
		}
		dir = parent
	}
	// Fall back: assume cwd is already the repo root.
	wd, _ := os.Getwd()
	return wd
}

func main() {
	golden := filepath.Join(repoRoot(), "tests", "cross-language", "golden")

	roundTrip := func(label string, name string, v any) {
		data, err := os.ReadFile(filepath.Join(golden, name+".json"))
		if err != nil {
			fmt.Fprintf(os.Stderr, "read %s: %v\n", name, err)
			return
		}
		if err := json.Unmarshal(data, v); err != nil {
			fmt.Fprintf(os.Stderr, "unmarshal %s: %v\n", name, err)
			return
		}
		out, err := json.Marshal(v)
		if err != nil {
			fmt.Fprintf(os.Stderr, "marshal %s: %v\n", name, err)
			return
		}
		fmt.Printf("%s:%s\n", label, string(out))
	}

	var mc models.MediaContent
	var mr models.MediaReaction
	var mp models.MediaProfile

	roundTrip("CONTENT", "media_content", &mc)
	roundTrip("REACTION", "media_reaction", &mr)
	roundTrip("PROFILE", "media_profile", &mp)
}
