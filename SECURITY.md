# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in Aether Media, please report it
responsibly.

**Email:** security@thegeeknetwork.co.za
**PGP:** available on request — ask in the initial email if you prefer
encrypted correspondence.

Please include:

- Description of the vulnerability
- Steps to reproduce (ideally a failing test or PoC against a tagged commit)
- Potential impact
- Suggested fix, if any
- Whether you would like public credit on disclosure

Do **not** file vulnerabilities as public GitHub issues.

## Disclosure Timeline

- We will acknowledge receipt within **48 hours**.
- We will provide an initial assessment within **7 days**.
- We aim to release a fix within **30 days** for critical issues.
- We follow a **90-day coordinated disclosure** timeline. If we have not
  shipped a fix within that window we will agree on an extension or
  publish jointly.

## Scope

In scope for security reports:

- Content integrity — hash verification, tampered chunk detection, false
  content-address claims
- Identity spoofing — AetherNetTag forgery, creator impersonation, unsigned
  profile updates
- Social graph manipulation — follow-graph poisoning, forged reactions,
  reputation gaming via fake content announcements
- Feed injection — inserting malicious content into a peer's feed via
  crafted mesh packets
- Privacy leaks — unintended disclosure of watch history, social graph,
  or location via mesh metadata
- AI layer — adversarial inputs that cause `IAetherNetAiProvider` to misclassify
  clean content as threats, or vice versa
- Cryptographic issues inherited from the aether-protocol layer — report
  those here if discovered via Aether Media; we will coordinate with the
  protocol team
- Memory safety in the C embedded player — buffer handling, missing
  zeroisation of decrypted content

## Out of Scope

- Issues in upstream dependencies (aether-protocol, LibVLC, ExoPlayer,
  AVFoundation, HLS.js, Shaka) — report those to the relevant project.
- Social engineering.
- Issues requiring physical access to an unlocked device.
- Side-channel attacks against the media codec layer (timing attacks on
  LibVLC, etc.).
- Quantum attacks against X25519 / Ed25519 — these are inherited from the
  protocol layer.
- Content moderation policy disputes — these are operational, not security.

## Recognition

We will credit reporters in the release notes for the fix unless the
reporter prefers to remain anonymous. Indicate your preference in the
initial email.
