#ifndef AETHERNET_MEDIA_H
#define AETHERNET_MEDIA_H

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Domain structs ─────────────────────────────────────────────────────────── */

typedef struct {
    char    content_hash[65];   /* SHA-256 hex (64 chars) + NUL */
    char    title[256];
    int64_t duration_ms;
    char    codec[64];
    char    content_type[64];
    char    creator_uhid[128];
    int64_t size_bytes;
    int64_t created_at_ms;      /* Unix-epoch ms when content was published.    */
} AetherNetMediaContent;

/*
 * Canonical MediaProfile struct — mirrors the C# / Go / Python / TS / Rust /
 * Swift / Kotlin SDKs so the C wire-roundtrip harness (gated on a JSON lib —
 * see tests/cross-language/README.md) can round-trip identically.
 */
typedef struct {
    char    uhid[128];
    char    display_name[256];
    char    avatar_hash[65];     /* nullable — empty string means null on wire */
    char    bio[512];            /* nullable — empty string means null on wire */
    char    aethernet_tag[16];
    int     follower_count;
    int     following_count;
    int     content_count;
    bool    is_verified;
    int64_t joined_at_ms;
} AetherNetMediaProfile;

/* ── Duration formatting ────────────────────────────────────────────────────── */

/**
 * Format duration_ms into a human-readable string written to out.
 *  - 0  → "Live"
 *  - < 1 hour  → "M:SS"
 *  - >= 1 hour → "H:MM:SS"
 *
 * out_len must be at least 16 bytes.
 */
void aethernet_format_duration(int64_t duration_ms, char *out, size_t out_len);

/* ── Social graph (opaque handle) ───────────────────────────────────────────── */

typedef struct AetherNetSocialGraph AetherNetSocialGraph;

/** Allocate and return a new empty social graph.  Caller must call destroy. */
AetherNetSocialGraph *aethernet_social_graph_create(void);

/** Free all memory owned by graph. */
void aethernet_social_graph_destroy(AetherNetSocialGraph *graph);

/** Add uhid to the following set.  No-op if already present. */
void aethernet_social_graph_follow(AetherNetSocialGraph *graph, const char *uhid);

/** Remove uhid from the following set.  No-op if not present. */
void aethernet_social_graph_unfollow(AetherNetSocialGraph *graph, const char *uhid);

/** Returns true when uhid is in the following set. */
bool aethernet_social_graph_is_following(AetherNetSocialGraph *graph, const char *uhid);

/**
 * Write all followed UHIDs into out_uhids (array of char* with room for
 * at least max entries).  Returns the number of entries written.
 * out_uhids[i] points into graph-owned memory — do not free.
 */
int  aethernet_social_graph_list(AetherNetSocialGraph *graph,
                               const char       **out_uhids,
                               int                max);

/** Returns the number of followed accounts. */
int  aethernet_social_graph_count(AetherNetSocialGraph *graph);

/* ── Player (LibVLC wrapper) ────────────────────────────────────────────────── */

typedef struct AetherNetPlayer AetherNetPlayer;

typedef enum {
    AETHERNET_PLAYER_IDLE    = 0,
    AETHERNET_PLAYER_OPENING = 1,
    AETHERNET_PLAYER_PLAYING = 2,
    AETHERNET_PLAYER_PAUSED  = 3,
    AETHERNET_PLAYER_STOPPED = 4,
    AETHERNET_PLAYER_ERROR   = 5,
} AetherNetPlayerState;

AetherNetPlayer      *aethernet_player_create(void);
void               aethernet_player_destroy(AetherNetPlayer *player);
bool               aethernet_player_open(AetherNetPlayer *player, const char *url);
void               aethernet_player_play(AetherNetPlayer *player);
void               aethernet_player_pause(AetherNetPlayer *player);
void               aethernet_player_stop(AetherNetPlayer *player);
AetherNetPlayerState  aethernet_player_get_state(AetherNetPlayer *player);
int64_t            aethernet_player_get_time_ms(AetherNetPlayer *player);
void               aethernet_player_set_volume(AetherNetPlayer *player, int volume_pct);

/* ── Stream client ──────────────────────────────────────────────────────────── */

typedef struct AetherNetStreamClient AetherNetStreamClient;

/**
 * Callback invoked for each segment received.
 * buf is valid only for the duration of the callback.
 */
typedef void (*aethernet_segment_cb)(const uint8_t *buf, size_t len, void *user_data);

AetherNetStreamClient *aethernet_stream_client_create(aethernet_segment_cb cb, void *user_data);
void                aethernet_stream_client_destroy(AetherNetStreamClient *client);
bool                aethernet_stream_client_connect(AetherNetStreamClient *client, const char *url);
int                 aethernet_stream_client_read_segment(AetherNetStreamClient *client,
                                                       uint8_t *buf, size_t max_len);
void                aethernet_stream_client_close(AetherNetStreamClient *client);

#ifdef __cplusplus
}
#endif

#endif /* AETHERNET_MEDIA_H */
