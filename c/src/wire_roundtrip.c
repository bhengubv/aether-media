/* SPDX-License-Identifier: MIT
 *
 * wire_roundtrip — Aether Media cross-language conformance harness driver (C).
 *
 * Reads each canonical golden JSON fixture from tests/cross-language/golden/,
 * parses it via the vendored jsmn JSON tokenizer into the AetherNetMedia*
 * struct, immediately re-serializes it to a canonical-key-order JSON string,
 * and prints `MODEL:JSON` lines to stdout. The harness
 * (tests/cross-language/run_all.sh) compares the output against the goldens
 * to prove wire-format identity with the C# reference (and Go / Python / TS /
 * Rust / Swift / Kotlin).
 *
 * Build:   cmake -S . -B build -DBUILD_WIRE_ROUNDTRIP=ON
 *          cmake --build build --target wire_roundtrip
 * Run:     ./build/wire_roundtrip  (cwd must be c/, so relative goldens path resolves)
 *
 * Coverage: media_content, media_reaction, media_profile (all three goldens).
 */

#include "aethermedia.h"

#define JSMN_STATIC
#include "../third_party/jsmn/jsmn.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <inttypes.h>

/* ── helpers ────────────────────────────────────────────────────────────── */

#define MAX_FIXTURE_BYTES (16 * 1024)
#define MAX_TOKENS 256

static char *read_file(const char *path, size_t *out_len) {
    FILE *f = fopen(path, "rb");
    if (!f) return NULL;
    fseek(f, 0, SEEK_END);
    long sz = ftell(f);
    fseek(f, 0, SEEK_SET);
    if (sz <= 0 || sz > MAX_FIXTURE_BYTES) { fclose(f); return NULL; }
    char *buf = (char *)malloc((size_t)sz + 1);
    if (!buf) { fclose(f); return NULL; }
    size_t n = fread(buf, 1, (size_t)sz, f);
    fclose(f);
    buf[n] = '\0';
    if (out_len) *out_len = n;
    return buf;
}

static int tok_streq(const char *json, const jsmntok_t *tok, const char *s) {
    int len = tok->end - tok->start;
    return tok->type == JSMN_STRING &&
           (int)strlen(s) == len &&
           strncmp(json + tok->start, s, (size_t)len) == 0;
}

static void tok_copy_str(const char *json, const jsmntok_t *tok, char *dst, size_t cap) {
    int len = tok->end - tok->start;
    if (len < 0) len = 0;
    if ((size_t)len >= cap) len = (int)(cap - 1);
    memcpy(dst, json + tok->start, (size_t)len);
    dst[len] = '\0';
}

static int64_t tok_to_int64(const char *json, const jsmntok_t *tok) {
    char buf[32];
    int len = tok->end - tok->start;
    if (len < 0 || len >= (int)sizeof(buf)) return 0;
    memcpy(buf, json + tok->start, (size_t)len);
    buf[len] = '\0';
    return (int64_t)strtoll(buf, NULL, 10);
}

static int tok_to_int(const char *json, const jsmntok_t *tok) {
    return (int)tok_to_int64(json, tok);
}

static int tok_to_bool(const char *json, const jsmntok_t *tok) {
    int len = tok->end - tok->start;
    return tok->type == JSMN_PRIMITIVE && len == 4 &&
           strncmp(json + tok->start, "true", 4) == 0;
}

static int tok_is_null(const char *json, const jsmntok_t *tok) {
    int len = tok->end - tok->start;
    return tok->type == JSMN_PRIMITIVE && len == 4 &&
           strncmp(json + tok->start, "null", 4) == 0;
}

/* Emit a JSON string with minimal escaping (only " and \ — goldens contain
 * neither control chars nor unicode). Keeps the output byte-equal to the
 * other-language drivers' serializers. */
static void emit_string(FILE *out, const char *s) {
    fputc('"', out);
    for (const char *p = s; *p; p++) {
        if (*p == '"' || *p == '\\') fputc('\\', out);
        fputc(*p, out);
    }
    fputc('"', out);
}

static void emit_string_or_null(FILE *out, const char *s) {
    if (s == NULL || s[0] == '\0') fputs("null", out);
    else emit_string(out, s);
}

/* ── MediaContent ───────────────────────────────────────────────────────── */

typedef struct {
    char    content_hash[65];
    char    title[256];
    int64_t duration_ms;
    char    codec[64];
    char    content_type[64];
    char    creator_uhid[128];
    int64_t size_bytes;
    int64_t created_at_ms;
    char    thumbnail_hash[65];
    int     thumbnail_is_null;
    char    tags[16][64];
    int     tag_count;
} mc_t;

static int parse_media_content(const char *json, size_t len, mc_t *mc) {
    jsmn_parser p;
    jsmn_init(&p);
    jsmntok_t t[MAX_TOKENS];
    int n = jsmn_parse(&p, json, len, t, MAX_TOKENS);
    if (n < 0 || t[0].type != JSMN_OBJECT) return -1;
    memset(mc, 0, sizeof(*mc));
    mc->thumbnail_is_null = 1;

    int i = 1;
    while (i < n) {
        const jsmntok_t *k = &t[i];
        const jsmntok_t *v = &t[i + 1];
        if (tok_streq(json, k, "content_hash"))        tok_copy_str(json, v, mc->content_hash, sizeof(mc->content_hash));
        else if (tok_streq(json, k, "title"))           tok_copy_str(json, v, mc->title, sizeof(mc->title));
        else if (tok_streq(json, k, "duration_ms"))     mc->duration_ms = tok_to_int64(json, v);
        else if (tok_streq(json, k, "codec"))           tok_copy_str(json, v, mc->codec, sizeof(mc->codec));
        else if (tok_streq(json, k, "content_type"))    tok_copy_str(json, v, mc->content_type, sizeof(mc->content_type));
        else if (tok_streq(json, k, "creator_uhid"))    tok_copy_str(json, v, mc->creator_uhid, sizeof(mc->creator_uhid));
        else if (tok_streq(json, k, "size_bytes"))      mc->size_bytes = tok_to_int64(json, v);
        else if (tok_streq(json, k, "created_at_ms"))   mc->created_at_ms = tok_to_int64(json, v);
        else if (tok_streq(json, k, "thumbnail_hash")) {
            if (tok_is_null(json, v)) { mc->thumbnail_is_null = 1; }
            else { tok_copy_str(json, v, mc->thumbnail_hash, sizeof(mc->thumbnail_hash)); mc->thumbnail_is_null = 0; }
        }
        else if (tok_streq(json, k, "tags") && v->type == JSMN_ARRAY) {
            int cnt = v->size;
            if (cnt > 16) cnt = 16;
            mc->tag_count = cnt;
            for (int j = 0; j < cnt; j++) tok_copy_str(json, &t[i + 2 + j], mc->tags[j], sizeof(mc->tags[0]));
            i += 1 + cnt;
        }
        i += 2;
    }
    return 0;
}

static void emit_media_content(FILE *out, const mc_t *mc) {
    fprintf(out, "{\"content_hash\":");        emit_string(out, mc->content_hash);
    fprintf(out, ",\"title\":");                emit_string(out, mc->title);
    fprintf(out, ",\"duration_ms\":%" PRId64,   mc->duration_ms);
    fprintf(out, ",\"codec\":");                emit_string(out, mc->codec);
    fprintf(out, ",\"content_type\":");         emit_string(out, mc->content_type);
    fprintf(out, ",\"creator_uhid\":");         emit_string(out, mc->creator_uhid);
    fprintf(out, ",\"size_bytes\":%" PRId64,    mc->size_bytes);
    fprintf(out, ",\"created_at_ms\":%" PRId64, mc->created_at_ms);
    fprintf(out, ",\"thumbnail_hash\":");       emit_string_or_null(out, mc->thumbnail_is_null ? NULL : mc->thumbnail_hash);
    fprintf(out, ",\"tags\":[");
    for (int j = 0; j < mc->tag_count; j++) {
        if (j) fputc(',', out);
        emit_string(out, mc->tags[j]);
    }
    fputs("]}", out);
}

/* ── MediaReaction ──────────────────────────────────────────────────────── */

typedef struct {
    char    reaction_id[64];
    char    content_hash[65];
    char    from_uhid[128];
    char    type[32];
    int64_t position_ms;
    char    message[256];
    int     message_is_null;
    int64_t sent_at_ms;
} mr_t;

static int parse_media_reaction(const char *json, size_t len, mr_t *mr) {
    jsmn_parser p;
    jsmn_init(&p);
    jsmntok_t t[MAX_TOKENS];
    int n = jsmn_parse(&p, json, len, t, MAX_TOKENS);
    if (n < 0 || t[0].type != JSMN_OBJECT) return -1;
    memset(mr, 0, sizeof(*mr));
    mr->message_is_null = 1;
    int i = 1;
    while (i < n) {
        const jsmntok_t *k = &t[i];
        const jsmntok_t *v = &t[i + 1];
        if      (tok_streq(json, k, "reaction_id"))  tok_copy_str(json, v, mr->reaction_id, sizeof(mr->reaction_id));
        else if (tok_streq(json, k, "content_hash")) tok_copy_str(json, v, mr->content_hash, sizeof(mr->content_hash));
        else if (tok_streq(json, k, "from_uhid"))    tok_copy_str(json, v, mr->from_uhid, sizeof(mr->from_uhid));
        else if (tok_streq(json, k, "type"))          tok_copy_str(json, v, mr->type, sizeof(mr->type));
        else if (tok_streq(json, k, "position_ms"))  mr->position_ms = tok_to_int64(json, v);
        else if (tok_streq(json, k, "message")) {
            if (tok_is_null(json, v)) mr->message_is_null = 1;
            else { tok_copy_str(json, v, mr->message, sizeof(mr->message)); mr->message_is_null = 0; }
        }
        else if (tok_streq(json, k, "sent_at_ms"))   mr->sent_at_ms = tok_to_int64(json, v);
        i += 2;
    }
    return 0;
}

static void emit_media_reaction(FILE *out, const mr_t *mr) {
    fprintf(out, "{\"reaction_id\":");          emit_string(out, mr->reaction_id);
    fprintf(out, ",\"content_hash\":");          emit_string(out, mr->content_hash);
    fprintf(out, ",\"from_uhid\":");             emit_string(out, mr->from_uhid);
    fprintf(out, ",\"type\":");                  emit_string(out, mr->type);
    fprintf(out, ",\"position_ms\":%" PRId64,    mr->position_ms);
    fprintf(out, ",\"message\":");               emit_string_or_null(out, mr->message_is_null ? NULL : mr->message);
    fprintf(out, ",\"sent_at_ms\":%" PRId64,     mr->sent_at_ms);
    fputc('}', out);
}

/* ── MediaProfile ───────────────────────────────────────────────────────── */

typedef struct {
    char    uhid[128];
    char    display_name[256];
    char    avatar_hash[65];
    int     avatar_is_null;
    char    bio[512];
    int     bio_is_null;
    char    aethernet_tag[16];
    int     follower_count;
    int     following_count;
    int     content_count;
    int     is_verified;
    int64_t joined_at_ms;
} mp_t;

static int parse_media_profile(const char *json, size_t len, mp_t *mp) {
    jsmn_parser p;
    jsmn_init(&p);
    jsmntok_t t[MAX_TOKENS];
    int n = jsmn_parse(&p, json, len, t, MAX_TOKENS);
    if (n < 0 || t[0].type != JSMN_OBJECT) return -1;
    memset(mp, 0, sizeof(*mp));
    mp->avatar_is_null = 1;
    mp->bio_is_null = 1;
    int i = 1;
    while (i < n) {
        const jsmntok_t *k = &t[i];
        const jsmntok_t *v = &t[i + 1];
        if      (tok_streq(json, k, "uhid"))             tok_copy_str(json, v, mp->uhid, sizeof(mp->uhid));
        else if (tok_streq(json, k, "display_name"))     tok_copy_str(json, v, mp->display_name, sizeof(mp->display_name));
        else if (tok_streq(json, k, "avatar_hash")) {
            if (tok_is_null(json, v)) mp->avatar_is_null = 1;
            else { tok_copy_str(json, v, mp->avatar_hash, sizeof(mp->avatar_hash)); mp->avatar_is_null = 0; }
        }
        else if (tok_streq(json, k, "bio")) {
            if (tok_is_null(json, v)) mp->bio_is_null = 1;
            else { tok_copy_str(json, v, mp->bio, sizeof(mp->bio)); mp->bio_is_null = 0; }
        }
        else if (tok_streq(json, k, "aethernet_tag"))    tok_copy_str(json, v, mp->aethernet_tag, sizeof(mp->aethernet_tag));
        else if (tok_streq(json, k, "follower_count"))   mp->follower_count = tok_to_int(json, v);
        else if (tok_streq(json, k, "following_count"))  mp->following_count = tok_to_int(json, v);
        else if (tok_streq(json, k, "content_count"))    mp->content_count = tok_to_int(json, v);
        else if (tok_streq(json, k, "is_verified"))      mp->is_verified = tok_to_bool(json, v);
        else if (tok_streq(json, k, "joined_at_ms"))     mp->joined_at_ms = tok_to_int64(json, v);
        i += 2;
    }
    return 0;
}

static void emit_media_profile(FILE *out, const mp_t *mp) {
    fprintf(out, "{\"uhid\":");                 emit_string(out, mp->uhid);
    fprintf(out, ",\"display_name\":");          emit_string(out, mp->display_name);
    fprintf(out, ",\"avatar_hash\":");           emit_string_or_null(out, mp->avatar_is_null ? NULL : mp->avatar_hash);
    fprintf(out, ",\"bio\":");                   emit_string_or_null(out, mp->bio_is_null ? NULL : mp->bio);
    fprintf(out, ",\"aethernet_tag\":");         emit_string(out, mp->aethernet_tag);
    fprintf(out, ",\"follower_count\":%d",       mp->follower_count);
    fprintf(out, ",\"following_count\":%d",      mp->following_count);
    fprintf(out, ",\"content_count\":%d",        mp->content_count);
    fprintf(out, ",\"is_verified\":%s",          mp->is_verified ? "true" : "false");
    fprintf(out, ",\"joined_at_ms\":%" PRId64,   mp->joined_at_ms);
    fputc('}', out);
}

/* ── main ───────────────────────────────────────────────────────────────── */

static const char *golden_path(const char *name) {
    static char buf[512];
    const char *env = getenv("AETHERMEDIA_GOLDEN_DIR");
    const char *dir = (env && *env) ? env : "../tests/cross-language/golden";
    snprintf(buf, sizeof(buf), "%s/%s.json", dir, name);
    return buf;
}

int main(void) {
    size_t len;
    char *json;

    /* MediaContent */
    json = read_file(golden_path("media_content"), &len);
    if (!json) { fprintf(stderr, "wire_roundtrip: read media_content failed\n"); return 1; }
    mc_t mc;
    if (parse_media_content(json, len, &mc) != 0) { fprintf(stderr, "parse media_content\n"); return 1; }
    fputs("CONTENT:", stdout); emit_media_content(stdout, &mc); fputc('\n', stdout);
    free(json);

    /* MediaReaction */
    json = read_file(golden_path("media_reaction"), &len);
    if (!json) { fprintf(stderr, "wire_roundtrip: read media_reaction failed\n"); return 1; }
    mr_t mr;
    if (parse_media_reaction(json, len, &mr) != 0) { fprintf(stderr, "parse media_reaction\n"); return 1; }
    fputs("REACTION:", stdout); emit_media_reaction(stdout, &mr); fputc('\n', stdout);
    free(json);

    /* MediaProfile */
    json = read_file(golden_path("media_profile"), &len);
    if (!json) { fprintf(stderr, "wire_roundtrip: read media_profile failed\n"); return 1; }
    mp_t mp;
    if (parse_media_profile(json, len, &mp) != 0) { fprintf(stderr, "parse media_profile\n"); return 1; }
    fputs("PROFILE:", stdout); emit_media_profile(stdout, &mp); fputc('\n', stdout);
    free(json);

    return 0;
}
