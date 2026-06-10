//! wire-roundtrip — Aether Media cross-language conformance harness driver
//!
//! Reads each canonical golden JSON fixture from `tests/cross-language/golden/`,
//! deserialises it into the Rust model, immediately re-serialises it, and
//! prints `MODEL:JSON` lines to stdout. The cross-language harness
//! (`tests/cross-language/run_all.sh`) compares the output against the
//! goldens to prove wire-format identity with the C# reference impl
//! (plus Go / Python / TypeScript / Swift / Kotlin / C).
//!
//! Run via: `cargo run --bin wire-roundtrip`

use aethernet_media::models::{MediaContent, MediaProfile, MediaReaction};
use std::fs;
use std::path::PathBuf;

fn golden_path(name: &str) -> PathBuf {
    // CARGO_MANIFEST_DIR = aether-media/rust
    let manifest = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    manifest
        .parent()
        .expect("rust/.. resolves to repo root")
        .join("tests")
        .join("cross-language")
        .join("golden")
        .join(format!("{name}.json"))
}

fn read_golden(name: &str) -> String {
    fs::read_to_string(golden_path(name))
        .unwrap_or_else(|e| panic!("read golden {name}: {e}"))
}

fn main() {
    // MediaContent
    let raw = read_golden("media_content");
    let content: MediaContent = serde_json::from_str(&raw).expect("parse media_content");
    let out = serde_json::to_string(&content).expect("serialise media_content");
    println!("CONTENT:{out}");

    // MediaReaction
    let raw = read_golden("media_reaction");
    let reaction: MediaReaction = serde_json::from_str(&raw).expect("parse media_reaction");
    let out = serde_json::to_string(&reaction).expect("serialise media_reaction");
    println!("REACTION:{out}");

    // MediaProfile
    let raw = read_golden("media_profile");
    let profile: MediaProfile = serde_json::from_str(&raw).expect("parse media_profile");
    let out = serde_json::to_string(&profile).expect("serialise media_profile");
    println!("PROFILE:{out}");
}
