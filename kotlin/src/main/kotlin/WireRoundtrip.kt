// SPDX-License-Identifier: MIT
//
// WireRoundtrip — Aether Media cross-language conformance harness driver.
//
// Reads each canonical golden JSON fixture from tests/cross-language/golden/,
// deserialises it into the Kotlin model via kotlinx.serialization, immediately
// re-serialises it, and prints `MODEL:JSON` lines to stdout. The harness
// (tests/cross-language/run_all.sh) compares the output against the goldens
// to prove wire-format identity with the C# reference (and Go / Python / TS /
// Rust / Swift / C).
//
// Run via:
//   ./gradlew wireRoundtrip
//
// Or directly:
//   ./gradlew run -PmainClass=aethermedia.WireRoundtripKt
package aethermedia

import aethermedia.models.MediaContent
import aethermedia.models.MediaProfile
import aethermedia.models.MediaReaction
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.io.File

/**
 * Resolves a golden fixture path relative to the repo root. The Gradle task
 * is invoked from `kotlin/`, so the goldens live at `../tests/cross-language/golden/`.
 */
private fun readGolden(name: String): String {
    // Allow override for non-Gradle invocation (e.g. CI scripts).
    val explicit = System.getenv("AETHERMEDIA_GOLDEN_DIR")
    val dir = if (!explicit.isNullOrBlank()) {
        File(explicit)
    } else {
        File("../tests/cross-language/golden")
    }
    val f = File(dir, "$name.json")
    require(f.exists()) { "Golden fixture not found: ${f.absolutePath}" }
    return f.readText()
}

fun main() {
    // The harness compares with sort_keys=True, so field ORDER in our output
    // doesn't matter — only field NAMES, types, and values. Use the default
    // Json encoder which preserves field declaration order.
    val json = Json { encodeDefaults = true }

    val mc = json.decodeFromString<MediaContent>(readGolden("media_content"))
    println("CONTENT:" + json.encodeToString(mc))

    val mr = json.decodeFromString<MediaReaction>(readGolden("media_reaction"))
    println("REACTION:" + json.encodeToString(mr))

    val mp = json.decodeFromString<MediaProfile>(readGolden("media_profile"))
    println("PROFILE:" + json.encodeToString(mp))
}
