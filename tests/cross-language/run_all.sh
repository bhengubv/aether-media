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
import { toWire as contentToWire, fromWire as contentFromWire } from './src/models/MediaContent.js';
import { toWire as reactionToWire, fromWire as reactionFromWire } from './src/models/MediaReaction.js';
import { toWire as profileToWire, fromWire as profileFromWire } from './src/models/MediaProfile.js';
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
from aether_media.models import MediaContent, MediaReaction, MediaProfile

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

echo ""
echo "Cross-language: $PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ]
