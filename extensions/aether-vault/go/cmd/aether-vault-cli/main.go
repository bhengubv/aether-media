// SPDX-License-Identifier: MIT
package main

import (
	"encoding/json"
	"fmt"
	"os"

	"aether.media/extensions/vault/vault"
)

const usage = `aether-vault-cli — Aether Vault command-line interface

Usage:
  aether-vault-cli store <file>             Store a file in the Aether Vault.
  aether-vault-cli recover <manifest-json>  Recover a file using a vault manifest.
  aether-vault-cli health <manifest-json>   Check the health of a stored file.

Arguments:
  <file>           Path to the file to store.
  <manifest-json>  Path to a JSON file containing a VaultManifest.
`

func main() {
	if len(os.Args) < 2 {
		fmt.Fprint(os.Stderr, usage)
		os.Exit(1)
	}

	cmd := os.Args[1]

	switch cmd {
	case "store":
		if len(os.Args) < 3 {
			fmt.Fprintf(os.Stderr, "error: store requires <file> argument\n\n%s", usage)
			os.Exit(1)
		}
		runStore(os.Args[2])

	case "recover":
		if len(os.Args) < 3 {
			fmt.Fprintf(os.Stderr, "error: recover requires <manifest-json> argument\n\n%s", usage)
			os.Exit(1)
		}
		runRecover(os.Args[2])

	case "health":
		if len(os.Args) < 3 {
			fmt.Fprintf(os.Stderr, "error: health requires <manifest-json> argument\n\n%s", usage)
			os.Exit(1)
		}
		runHealth(os.Args[2])

	default:
		fmt.Fprintf(os.Stderr, "error: unknown command %q\n\n%s", cmd, usage)
		os.Exit(1)
	}
}

func runStore(filePath string) {
	f, err := os.Open(filePath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: cannot open %q: %v\n", filePath, err)
		os.Exit(1)
	}
	defer f.Close()

	svc := &vault.VaultService{}
	manifest, err := svc.Store(nil, f, filePath) //nolint:staticcheck // context unused in stub
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: store failed: %v\n", err)
		os.Exit(1)
	}

	enc := json.NewEncoder(os.Stdout)
	enc.SetIndent("", "  ")
	if err := enc.Encode(manifest); err != nil {
		fmt.Fprintf(os.Stderr, "error: cannot encode manifest: %v\n", err)
		os.Exit(1)
	}
}

func runRecover(manifestPath string) {
	manifest, err := loadManifest(manifestPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: %v\n", err)
		os.Exit(1)
	}

	svc := &vault.VaultService{}
	rc, err := svc.Recover(nil, manifest) //nolint:staticcheck // context unused in stub
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: recover failed: %v\n", err)
		os.Exit(1)
	}
	defer rc.Close()

	fmt.Printf("Recovered manifest %q — plaintext stream ready.\n", manifest.FileId)
}

func runHealth(manifestPath string) {
	manifest, err := loadManifest(manifestPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: %v\n", err)
		os.Exit(1)
	}

	svc := &vault.VaultService{}
	health, err := svc.CheckHealth(nil, manifest) //nolint:staticcheck // context unused in stub
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: health check failed: %v\n", err)
		os.Exit(1)
	}

	enc := json.NewEncoder(os.Stdout)
	enc.SetIndent("", "  ")
	if err := enc.Encode(health); err != nil {
		fmt.Fprintf(os.Stderr, "error: cannot encode health: %v\n", err)
		os.Exit(1)
	}
}

func loadManifest(path string) (*vault.VaultManifest, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("cannot read manifest %q: %w", path, err)
	}
	var m vault.VaultManifest
	if err := json.Unmarshal(data, &m); err != nil {
		return nil, fmt.Errorf("cannot parse manifest %q: %w", path, err)
	}
	return &m, nil
}
