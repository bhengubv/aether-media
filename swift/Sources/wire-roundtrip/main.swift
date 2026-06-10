// SPDX-License-Identifier: MIT
//
// wire-roundtrip — Aether Media cross-language conformance harness driver.
//
// Reads each canonical golden JSON fixture from tests/cross-language/golden/,
// decodes it via Foundation's JSONDecoder into the Swift model, immediately
// re-encodes via JSONEncoder, and prints `MODEL:JSON` lines to stdout. The
// harness (tests/cross-language/run_all.sh) compares the output against the
// goldens to prove wire-format identity with the C# reference (and Go /
// Python / TypeScript / Rust / Kotlin / C).
//
// Run via:
//   swift run -c release wire-roundtrip
//
// Invoked from the harness which runs from the repo root, so goldens live at
// ../tests/cross-language/golden/ relative to swift/.

import Foundation
import AetherNetMedia

let goldenDir: URL = {
    if let explicit = ProcessInfo.processInfo.environment["AETHERMEDIA_GOLDEN_DIR"], !explicit.isEmpty {
        return URL(fileURLWithPath: explicit, isDirectory: true)
    }
    return URL(fileURLWithPath: "../tests/cross-language/golden", isDirectory: true)
}()

func readGolden(_ name: String) -> Data {
    let url = goldenDir.appendingPathComponent("\(name).json")
    guard let data = try? Data(contentsOf: url) else {
        fatalError("Golden fixture not found: \(url.path)")
    }
    return data
}

let dec = JSONDecoder()
let enc = JSONEncoder()
// Foundation's default encoder emits fields in property declaration order; the
// harness normalises with sort_keys=True so order doesn't affect pass/fail.

do {
    let content = try dec.decode(MediaContent.self, from: readGolden("media_content"))
    let out = try enc.encode(content)
    print("CONTENT:" + (String(data: out, encoding: .utf8) ?? ""))

    let reaction = try dec.decode(MediaReaction.self, from: readGolden("media_reaction"))
    let out2 = try enc.encode(reaction)
    print("REACTION:" + (String(data: out2, encoding: .utf8) ?? ""))

    let profile = try dec.decode(MediaProfile.self, from: readGolden("media_profile"))
    let out3 = try enc.encode(profile)
    print("PROFILE:" + (String(data: out3, encoding: .utf8) ?? ""))
} catch {
    fputs("wire-roundtrip: \(error)\n", stderr)
    exit(1)
}
