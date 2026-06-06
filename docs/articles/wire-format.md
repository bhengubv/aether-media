# Aether Media — Canonical Wire Format

All JSON exchanged between Aether Media SDK implementations must follow this
specification.  Every language SDK is responsible for serialising **to** and
deserialising **from** this format regardless of its internal naming
conventions.

---

## Rules

| Rule | Requirement |
|------|-------------|
| Field names | **snake_case** throughout — no camelCase, no PascalCase |
| Timestamps | **Unix milliseconds as `integer`** — no ISO-8601 strings, no Date/DateTime objects |
| Timestamp suffix | Fields that carry a unix-ms timestamp end in `_ms` (e.g. `sent_at_ms`) |
| Reaction type | The enum field is named `type` (not `reaction_type`) |
| Reaction enum values | Lowercase strings: `"like"`, `"share"`, `"comment"`, `"super_react"` |
| Optional fields | JSON `null` — never omitted |

---

## Models

### MediaContent

```json
{
  "content_hash":   "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "title":          "Aether Launch Keynote",
  "duration_ms":    5025000,
  "codec":          "h264",
  "content_type":   "video/mp4",
  "creator_uhid":   "uhid-alice-0001",
  "size_bytes":     150000000,
  "created_at_ms":  1716249600000,
  "thumbnail_hash": "sha256-of-thumbnail",
  "tags":           ["aether", "launch", "keynote"]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `content_hash` | `string` | SHA-256 hex digest of the raw encoded bytes. Primary key. |
| `title` | `string` | Human-readable title. |
| `duration_ms` | `integer` | Duration in milliseconds. `0` means live. |
| `codec` | `string` | Codec identifier (e.g. `"h264"`, `"av1"`, `"opus"`). |
| `content_type` | `string` | MIME type (e.g. `"video/mp4"`, `"audio/flac"`). |
| `creator_uhid` | `string` | Universal handle ID of the creator. |
| `size_bytes` | `integer` | Encoded size in bytes. |
| `created_at_ms` | `integer` | Unix milliseconds when the content was created. |
| `thumbnail_hash` | `string\|null` | SHA-256 hex of thumbnail, or `null`. |
| `tags` | `string[]` | Zero or more freeform tags. |

---

### MediaReaction

```json
{
  "reaction_id":  "550e8400-e29b-41d4-a716-446655440000",
  "content_hash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "from_uhid":    "uhid-bob-0002",
  "type":         "comment",
  "position_ms":  42000,
  "message":      "This is incredible!",
  "sent_at_ms":   1716249900000
}
```

| Field | Type | Description |
|-------|------|-------------|
| `reaction_id` | `string` | UUID identifying this reaction event. |
| `content_hash` | `string` | SHA-256 hex of the reacted-to content. |
| `from_uhid` | `string` | Viewer's UHID. |
| `type` | `string` | One of `"like"`, `"share"`, `"comment"`, `"super_react"`. |
| `position_ms` | `integer` | Playback position in ms when the reaction occurred. `0` = not anchored. |
| `message` | `string\|null` | Required for `"comment"`; `null` for all other types. |
| `sent_at_ms` | `integer` | Unix milliseconds when the reaction was sent. |

---

### MediaProfile

```json
{
  "uhid":            "uhid-alice-0001",
  "display_name":    "Alice Aether",
  "avatar_hash":     "sha256-of-avatar",
  "bio":             "Building the decentralised video network.",
  "aethernet_tag":      "@alice",
  "follower_count":  12450,
  "following_count": 87,
  "content_count":   234,
  "is_verified":     true,
  "joined_at_ms":    1700000000000
}
```

| Field | Type | Description |
|-------|------|-------------|
| `uhid` | `string` | Universal handle ID. |
| `display_name` | `string` | Public display name. |
| `avatar_hash` | `string\|null` | SHA-256 hex of avatar image, or `null`. |
| `bio` | `string\|null` | Creator bio, or `null`. |
| `aethernet_tag` | `string` | Short @handle on the Aether network. |
| `follower_count` | `integer` | Number of followers. |
| `following_count` | `integer` | Number of accounts followed. |
| `content_count` | `integer` | Number of published content items. |
| `is_verified` | `boolean` | Whether the creator is verified. |
| `joined_at_ms` | `integer` | Unix milliseconds when the account was created. |

---

### MediaFeedItem

```json
{
  "content": {
    "content_hash":   "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
    "title":          "Aether Launch Keynote",
    "duration_ms":    5025000,
    "codec":          "h264",
    "content_type":   "video/mp4",
    "creator_uhid":   "uhid-alice-0001",
    "size_bytes":     150000000,
    "created_at_ms":  1716249600000,
    "thumbnail_hash": null,
    "tags":           ["aether"]
  },
  "like_count":      4821,
  "share_count":     312,
  "comment_count":   98,
  "watch_count":     15230,
  "is_live":         false,
  "stream_id":       null,
  "top_reactions": [
    {
      "reaction_id":  "550e8400-e29b-41d4-a716-446655440000",
      "content_hash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "from_uhid":    "uhid-bob-0002",
      "type":         "like",
      "position_ms":  0,
      "message":      null,
      "sent_at_ms":   1716249900000
    }
  ],
  "published_at_ms": 1716249600000
}
```

| Field | Type | Description |
|-------|------|-------------|
| `content` | `MediaContent` | The embedded content record. |
| `like_count` | `integer` | Total likes (includes super-reacts). |
| `share_count` | `integer` | Total shares. |
| `comment_count` | `integer` | Total comments. |
| `watch_count` | `integer` | Number of distinct watch sessions. |
| `is_live` | `boolean` | `true` when this is an active live stream. |
| `stream_id` | `string\|null` | Stream UUID if live, `null` otherwise. |
| `top_reactions` | `MediaReaction[]` | Up to 5 most recent reactions. |
| `published_at_ms` | `integer` | Unix milliseconds when the item was published to the feed. |

---

## Language Implementation Notes

| Language | Serialisation library | Key config |
|----------|-----------------------|------------|
| **C#** | `System.Text.Json` | `[JsonPropertyName("snake_case")]` on every property |
| **Go** | `encoding/json` | `json:"snake_case"` struct tags; `int64` for timestamps |
| **Rust** | `serde_json` | `#[serde(rename = "type")]` on reaction type field; `#[serde(rename_all = "snake_case")]` on enum |
| **Kotlin** | `kotlinx.serialization` | `@SerialName("snake_case")` on every field |
| **Swift** | `Codable` | `CodingKeys` enum mapping camelCase → snake_case; `Int64` for timestamps |
| **TypeScript** | Native `JSON.stringify` | `WireFormat` interface + `toWire()`/`fromWire()` helpers per model |
| **Python** | `dataclasses.asdict()` / `to_dict()` | `int` fields ending in `_ms`; `to_dict()` method on each model |
| **C** | User-supplied JSON library | Struct fields already snake_case by C convention |
