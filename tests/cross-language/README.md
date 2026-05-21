# Cross-language wire format interop tests

Verifies that all 8 Aether Media SDK implementations produce identical JSON
for the same model data.

## Run

```bash
bash tests/cross-language/run_all.sh
```

## How it works

1. Golden JSON fixtures in `golden/` define the canonical wire format.
2. Each language test reads a golden file, deserialises it into the language's native model, and re-serialises it.
3. The output is normalised (keys sorted) and compared to the normalised golden.
4. Any field name, type, or value mismatch is a test failure.

## Adding a new language

Add a section to `run_all.sh` that reads from `$GOLDEN/` and prints `MODEL:JSON` lines.
