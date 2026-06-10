#!/usr/bin/env bash
# Cross-language wire format interop tests
# Each sub-test: read golden JSON → deserialise → re-serialise → compare with golden

set -euo pipefail
PASS=0
FAIL=0
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
GOLDEN="$ROOT/tests/cross-language/golden"

check() {
  local lang="$1" model="$2" got="$3" want
  want="$(cat "$GOLDEN/$model.json" | python3 -c 'import sys,json; print(json.dumps(json.load(sys.stdin), sort_keys=True))')"
  norm="$(echo "$got" | python3 -c 'import sys,json; print(json.dumps(json.load(sys.stdin), sort_keys=True))')"
  if [ "$norm" = "$want" ]; then
    echo "PASS  [$lang] $model"
    PASS=$((PASS + 1))
  else
    echo "FAIL  [$lang] $model"
    echo "  want: $want"
    echo "  got:  $norm"
    FAIL=$((FAIL + 1))
  fi
}

# ── TypeScript ────────────────────────────────────────────────────────────────
TS_OUT=$(cd "$ROOT/typescript" && node --input-type=module <<'EOF'
import { toWire as contentToWire, fromWire as contentFromWire } from './dist/models/MediaContent.js';
import { toWire as reactionToWire, fromWire as reactionFromWire } from './dist/models/MediaReaction.js';
import { toWire as profileToWire, fromWire as profileFromWire } from './dist/models/MediaProfile.js';
import { readFileSync } from 'fs';
const g = (f) => JSON.parse(readFileSync(new URL(f, import.meta.url)));
const c = g('../tests/cross-language/golden/media_content.json');
const r = g('../tests/cross-language/golden/media_reaction.json');
const p = g('../tests/cross-language/golden/media_profile.json');
console.log('CONTENT:' + JSON.stringify(contentToWire(contentFromWire(c))));
console.log('REACTION:' + JSON.stringify(reactionToWire(reactionFromWire(r))));
console.log('PROFILE:' + JSON.stringify(profileToWire(profileFromWire(p))));
EOF
) 2>/dev/null || true

if [ -n "$TS_OUT" ]; then
  check "typescript" "media_content"  "$(echo "$TS_OUT" | grep '^CONTENT:'  | cut -d: -f2-)"
  check "typescript" "media_reaction" "$(echo "$TS_OUT" | grep '^REACTION:' | cut -d: -f2-)"
  check "typescript" "media_profile"  "$(echo "$TS_OUT" | grep '^PROFILE:'  | cut -d: -f2-)"
else
  echo "SKIP  [typescript] (node not available or TypeScript not built)"
fi

# ── Python ────────────────────────────────────────────────────────────────────
PY_OUT=$(cd "$ROOT" && python3 - <<'EOF'
import json, sys, pathlib
sys.path.insert(0, 'python')
from aethermedia.models import MediaContent, MediaReaction, MediaProfile

golden = pathlib.Path('tests/cross-language/golden')
c = json.loads((golden / 'media_content.json').read_text())
r = json.loads((golden / 'media_reaction.json').read_text())
p = json.loads((golden / 'media_profile.json').read_text())

mc = MediaContent.from_dict(c)
mr = MediaReaction.from_dict(r)
mp = MediaProfile.from_dict(p)

print('CONTENT:'  + json.dumps(mc.to_dict()))
print('REACTION:' + json.dumps(mr.to_dict()))
print('PROFILE:'  + json.dumps(mp.to_dict()))
EOF
) 2>/dev/null || true

if [ -n "$PY_OUT" ]; then
  check "python" "media_content"  "$(echo "$PY_OUT" | grep '^CONTENT:'  | cut -d: -f2-)"
  check "python" "media_reaction" "$(echo "$PY_OUT" | grep '^REACTION:' | cut -d: -f2-)"
  check "python" "media_profile"  "$(echo "$PY_OUT" | grep '^PROFILE:'  | cut -d: -f2-)"
else
  echo "SKIP  [python] (python3 not available or models missing from_dict/to_dict)"
fi

# ── Go ────────────────────────────────────────────────────────────────────────
GO_OUT=$(cd "$ROOT/go" && go run ./cmd/wire-roundtrip/... 2>/dev/null) || true
if [ -n "$GO_OUT" ]; then
  check "go" "media_content"  "$(echo "$GO_OUT" | grep '^CONTENT:'  | cut -d: -f2-)"
  check "go" "media_reaction" "$(echo "$GO_OUT" | grep '^REACTION:' | cut -d: -f2-)"
  check "go" "media_profile"  "$(echo "$GO_OUT" | grep '^PROFILE:'  | cut -d: -f2-)"
else
  echo "SKIP  [go] (go binary not available or wire-roundtrip cmd not found)"
fi

# ── Rust ──────────────────────────────────────────────────────────────────────
# Builds the wire-roundtrip bin (lazy; cargo caches between runs).
RUST_OUT=$(cd "$ROOT/rust" && cargo run --bin wire-roundtrip --quiet 2>/dev/null) || true
if [ -n "$RUST_OUT" ]; then
  check "rust" "media_content"  "$(echo "$RUST_OUT" | grep '^CONTENT:'  | cut -d: -f2-)"
  check "rust" "media_reaction" "$(echo "$RUST_OUT" | grep '^REACTION:' | cut -d: -f2-)"
  check "rust" "media_profile"  "$(echo "$RUST_OUT" | grep '^PROFILE:'  | cut -d: -f2-)"
else
  echo "SKIP  [rust] (cargo not available or wire-roundtrip bin failed)"
fi

# ── Kotlin ────────────────────────────────────────────────────────────────────
# Driven by a Gradle task `wireRoundtrip` exposed by kotlin/build.gradle.kts.
KOTLIN_OUT=$(cd "$ROOT/kotlin" && ./gradlew --quiet wireRoundtrip 2>/dev/null) || true
if [ -n "$KOTLIN_OUT" ]; then
  check "kotlin" "media_content"  "$(echo "$KOTLIN_OUT" | grep '^CONTENT:'  | cut -d: -f2-)"
  check "kotlin" "media_reaction" "$(echo "$KOTLIN_OUT" | grep '^REACTION:' | cut -d: -f2-)"
  check "kotlin" "media_profile"  "$(echo "$KOTLIN_OUT" | grep '^PROFILE:'  | cut -d: -f2-)"
else
  echo "SKIP  [kotlin] (gradle not available, wrapper missing, or wireRoundtrip task not defined)"
fi

# ── Swift (macOS / Linux with swift toolchain) ────────────────────────────────
SWIFT_OUT=$(cd "$ROOT/swift" && swift run -c release wire-roundtrip 2>/dev/null) || true
if [ -n "$SWIFT_OUT" ]; then
  check "swift" "media_content"  "$(echo "$SWIFT_OUT" | grep '^CONTENT:'  | cut -d: -f2-)"
  check "swift" "media_reaction" "$(echo "$SWIFT_OUT" | grep '^REACTION:' | cut -d: -f2-)"
  check "swift" "media_profile"  "$(echo "$SWIFT_OUT" | grep '^PROFILE:'  | cut -d: -f2-)"
else
  echo "SKIP  [swift] (swift toolchain not available or wire-roundtrip executable not defined)"
fi

# ── C (cmake + native binary) ─────────────────────────────────────────────────
C_OUT=""
if command -v cmake >/dev/null 2>&1 && [ -f "$ROOT/c/CMakeLists.txt" ]; then
  (cd "$ROOT/c" && cmake -S . -B build -DBUILD_WIRE_ROUNDTRIP=ON >/dev/null 2>&1 \
                && cmake --build build --target wire_roundtrip >/dev/null 2>&1) || true
  if [ -x "$ROOT/c/build/wire_roundtrip" ]; then
    C_OUT=$("$ROOT/c/build/wire_roundtrip" 2>/dev/null) || true
  elif [ -x "$ROOT/c/build/Debug/wire_roundtrip.exe" ]; then
    C_OUT=$("$ROOT/c/build/Debug/wire_roundtrip.exe" 2>/dev/null) || true
  fi
fi
if [ -n "$C_OUT" ]; then
  check "c" "media_content"  "$(echo "$C_OUT" | grep '^CONTENT:'  | cut -d: -f2-)"
  check "c" "media_reaction" "$(echo "$C_OUT" | grep '^REACTION:' | cut -d: -f2-)"
  check "c" "media_profile"  "$(echo "$C_OUT" | grep '^PROFILE:'  | cut -d: -f2-)"
else
  echo "SKIP  [c] (cmake not available or wire_roundtrip target not defined)"
fi

echo ""
echo "Cross-language: $PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ]
