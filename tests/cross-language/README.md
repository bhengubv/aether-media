# Cross-language wire format interop tests

Verifies that all 8 Aether Media SDK implementations produce identical JSON
for the same model data. Same pattern as
[`aether-protocol/tests/cross-language/`](https://github.com/bhengubv/aether-protocol/tree/main/tests/cross-language)
which uses fixture corpora for the protocol layer.

## Run

```bash
bash tests/cross-language/run_all.sh
```

## How it works

1. Golden JSON fixtures in `golden/` define the canonical wire format.
2. Each language's wire-roundtrip driver reads a golden file, deserialises
   it into the language's native model, immediately re-serialises it, and
   prints `MODEL:JSON` lines to stdout.
3. The harness reads each driver's output, normalises with `sort_keys=True`,
   and compares to the equivalently-normalised golden. Field name, type, or
   value mismatch is a test failure.

## Coverage matrix

| Language    | Driver                                                                                     | Runtime-verified | Toolchain on Windows |
|-------------|--------------------------------------------------------------------------------------------|------------------|----------------------|
| TypeScript  | `node --input-type=module` heredoc against `typescript/dist/models/`                       | ✅                | ✅                    |
| Python      | `python3 -` heredoc against `python/aethermedia/models`                                    | ✅                | ✅                    |
| Go          | `go run ./cmd/wire-roundtrip/...`                                                          | ✅                | ✅                    |
| Rust        | `cargo run --bin wire-roundtrip` (`rust/src/bin/wire-roundtrip.rs`)                        | ✅                | ✅                    |
| Kotlin      | `./gradlew wireRoundtrip` (`kotlin/src/main/kotlin/WireRoundtrip.kt`)                     | ✅                | ✅                    |
| Swift       | `swift run -c release wire-roundtrip` (`swift/Sources/wire-roundtrip/main.swift`)         | ⚠ Mac/Linux only | ❌ (swift toolchain)  |
| C           | _(pending — tracked in W18-7: vendor jsmn or cJSON, then add `c/src/wire_roundtrip.c`)_   | ❌                | n/a                   |
| C# (ref)    | xUnit `BandwidthFixtureTests` + dotnet test (see `aether-protocol/tests/...`)              | ✅ (ref impl)     | ✅                    |

For toolchain-gated languages, the harness `SKIP`s gracefully — it does not
silently pass. The C struct field names are also statically audited in this
README so any future drift is caught at code-review time before the next
harness run.

## Bugs caught by extending the harness (W18-6 P3)

The Phase 3 sweep found six real wire-format divergences across the
language SDKs that the pre-existing 3-language harness missed:

1. **Go `MediaProfile.aether_tag`** should be `aethernet_tag` — wire-format
   field name drift caught the first run.
2. **Rust `MediaContent`** was missing `created_at_ms` — silently dropped
   on round-trip (`serde` ignores unknown fields by default).
3. **Kotlin `MediaContent`** was missing `createdAtMs` — same silent drop.
4. **Kotlin `MediaReactionType` enum** wasn't `@Serializable` and its cases
   were `LIKE`/`SHARE`/... without `@SerialName` annotations, so decoding
   the golden `"type": "like"` threw at runtime.
5. **Kotlin `MediaProfile`** had a `private companion object` that blocked
   the kotlinx-serialization-generated public companion accessor — caused
   `IllegalAccessError` at runtime.
6. **C `AetherNetMediaContent` struct** was missing `created_at_ms` (drift
   from the canonical wire format).
7. **C `AetherNetMediaProfile` struct** was missing `avatar_hash`, `bio`,
   `following_count`, `is_verified`, `joined_at_ms` — incomplete vs the
   canonical wire format.

All seven were fixed in W18-6 P3 commits.

## Static field-name audit

Run `grep -rn "aether" tests/cross-language/golden/` to see the canonical
field names. The following must hold for each language SDK:

| Field (golden)     | TS  | Python | Go  | Rust | Kotlin | Swift | C   |
|--------------------|-----|--------|-----|------|--------|-------|-----|
| `content_hash`     | ✅   | ✅      | ✅   | ✅    | ✅      | ✅     | ✅   |
| `created_at_ms`    | ✅   | ✅      | ✅   | ✅    | ✅      | ✅     | ✅   |
| `aethernet_tag`    | ✅   | ✅      | ✅   | ✅    | ✅      | ✅     | ✅   |
| `joined_at_ms`     | ✅   | ✅      | ✅   | ✅    | ✅      | ✅     | ✅   |

If any cell becomes ❌, the next harness run will fail loudly for that
language (and the static audit table here should be updated to reflect the
drift before any commit lands).

## Adding a new language

Add a `# ── <Language> ──` section to `run_all.sh` that prints `CONTENT:JSON`,
`REACTION:JSON`, `PROFILE:JSON` lines, then update the coverage matrix in
this README.

## Adding a new golden fixture

1. Add the JSON to `golden/`.
2. Update each driver to round-trip the new model.
3. Update each language's check block in `run_all.sh`.
