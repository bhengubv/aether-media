# Contributing to Aether Media

Thank you for your interest in contributing to Aether Media. Every contribution matters — whether it is a bug fix, a new codec integration, better documentation, or a test case that catches an edge case we missed.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Git
- (Optional) [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp) for desktop playback

### Build and Test

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet build
dotnet test
```

---

## How to Contribute

### 1. Fork and Branch

1. Fork the repository on GitHub.
2. Create a branch from `main` with a descriptive name:
   ```bash
   git checkout -b feature/watch-together-reactions
   git checkout -b fix/feed-dedup-race
   git checkout -b docs/content-addressing-explainer
   ```

### 2. Make Your Changes

- Write complete, working code. No stubs, no placeholder implementations.
- Include tests for new functionality.
- Update documentation if your change affects the public API or behaviour.

### 3. Test

```bash
# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/AetherMedia.Core.Tests
dotnet test tests/AetherMedia.Social.Tests
```

### 4. Submit a Pull Request

- Push your branch to your fork.
- Open a pull request against `main`.
- Describe what your change does and why.
- Reference any related issues.
- Ensure all tests pass.

---

## Adding a New Media Feature

Aether Media is structured around a clean layered architecture:

- **Core** (`AetherMedia.Core`) — domain models, interfaces, no external dependencies
- **Feature libraries** (`Streaming`, `Social`, `Content`, `Identity`, `AI`) — implement one concern each
- **DI wiring** (`AetherMedia.DependencyInjection`) — `AddAetherNetMedia()` fluent builder
- **Platform apps** (`Desktop`, Android, iOS) — consume feature libraries via DI

To add a new feature:

1. Define the interface in `AetherMedia.Core` (or the appropriate feature library).
2. Implement it in the feature library with full async/cancellation support.
3. Register it in `AetherMedia.DependencyInjection` via the fluent builder.
4. Add unit tests in the corresponding `Tests` project.
5. Update the README feature table if the capability is user-facing.

---

## Code Style

### General

- **Language:** C# (.NET 10), TypeScript (strict mode), Python 3.12, Rust (stable), Go 1.22, Kotlin (JVM 21), Swift 5.10, C (C11)
- **Formatting:** Use `dotnet format` for C#; `prettier` for TypeScript; `black` for Python; `rustfmt` for Rust; `gofmt` for Go.
- **Naming:** Descriptive names. No abbreviations unless universally understood (e.g., `ABR`, `HLS`, `BLE`, `DTN`).

### Documentation

- All public types and members must have XML documentation comments (C#) or equivalent doc-comments in each language.
- Explain *why*, not just *what*:

  ```csharp
  /// <summary>
  /// Caps the feed at <see cref="MaxFeedItems"/> entries to bound memory on
  /// low-end Android Go devices where heap is constrained to 512 MiB.
  /// Oldest entries are evicted when the cap is reached.
  /// </summary>
  private void EnforceCapacity() { ... }
  ```

### Security

- Never log content hashes paired with user identities — this leaks viewing history.
- Never log cryptographic keys, session tokens, or raw packet payloads.
- Use constant-time comparison for any security-critical byte comparisons.
- Never commit test keys or hardcoded credentials.

---

## Areas Where Help Is Wanted

### Codec and Playback
- **LibVLC bindings** — additional platform targets (Linux ARM, Raspberry Pi)
- **ABR tuning** — smarter bitrate rung selection under variable mesh bandwidth
- **Subtitle and audio track switching** — pass-through to LibVLC / ExoPlayer / AVFoundation

### Mesh Integration
- **Watch-together latency compensation** — RTT-based sync adjustment for high-latency transports
- **ChipIn crowdfunding UI** — end-to-end flow from `StartChipInAsync` to Compose / SwiftUI / Avalonia

### Testing
- **Cross-language feed fixture corpus** — extend `tests/cross-language/` with more social-graph scenarios
- **Adversarial content** — malformed content announcements, hash mismatches, oversized chunks
- **Performance benchmarks** — feed aggregation throughput, chunk reassembly speed

### Documentation
- **Social protocol explainer** — how follows, reactions, and announcements travel over DTN
- **Content-addressing guide** — how SHA-256 content identity prevents URL rot
- **Getting-started tutorial** — two C# nodes in the same process discovering and streaming

---

## Reporting Bugs

Open an issue on GitHub with:

1. A clear title describing the problem.
2. Steps to reproduce.
3. Expected behaviour vs. actual behaviour.
4. .NET version and operating system (or device model for mobile).
5. Relevant logs (sanitised — no keys, tokens, or content hashes paired with user identities).

---

## Security Vulnerabilities

Do NOT report security vulnerabilities through public issues. See [SECURITY.md](SECURITY.md) for responsible disclosure instructions.

---

## License

By contributing to Aether Media, you agree that your contributions will be licensed under the [MIT License](LICENSE).
