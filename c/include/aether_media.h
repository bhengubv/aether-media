#ifndef AETHER_MEDIA_H
#define AETHER_MEDIA_H

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
} AetherMediaContent;

typedef struct {
    char uhid[128];
    char display_name[256];
    char aether_tag[16];
    int  follower_count;
    int  content_count;
} AetherMediaProfile;

/* ── Duration formatting ────────────────────────────────────────────────────── */

/**
 * Format duration_ms into a human-readable string written to out.
 *  - 0  → "Live"
 *  - < 1 hour  → "M:SS"
 *  - >= 1 hour → "H:MM:SS"
 *
 * out_len must be at least 16 bytes.
 */
void aether_format_duration(int64_t duration_ms, char *out, size_t out_len);

/* ── Social graph (opaque handle) ───────────────────────────────────────────── */

typedef struct AetherSocialGraph AetherSocialGraph;

/** Allocate and return a new empty social graph.  Caller must call destroy. */
AetherSocialGraph *aether_social_graph_create(void);

/** Free all memory owned by graph. */
void aether_social_graph_destroy(AetherSocialGraph *graph);

/** Add uhid to the following set.  No-op if already present. */
void aether_social_graph_follow(AetherSocialGraph *graph, const char *uhid);

/** Remove uhid from the following set.  No-op if not present. */
void aether_social_graph_unfollow(AetherSocialGraph *graph, const char *uhid);

/** Returns true when uhid is in the following set. */
bool aether_social_graph_is_following(AetherSocialGraph *graph, const char *uhid);

/**
 * Write all followed UHIDs into out_uhids (array of char* with room for
 * at least max entries).  Returns the number of entries written.
 * out_uhids[i] points into graph-owned memory — do not free.
 */
int  aether_social_graph_list(AetherSocialGraph *graph,
                               const char       **out_uhids,
                               int                max);

/** Returns the number of followed accounts. */
int  aether_social_graph_count(AetherSocialGraph *graph);

/* ── Player (LibVLC wrapper) ────────────────────────────────────────────────── */

typedef struct AetherPlayer AetherPlayer;

typedef enum {
    AETHER_PLAYER_IDLE    = 0,
    AETHER_PLAYER_OPENING = 1,
    AETHER_PLAYER_PLAYING = 2,
    AETHER_PLAYER_PAUSED  = 3,
    AETHER_PLAYER_STOPPED = 4,
    AETHER_PLAYER_ERROR   = 5,
} AetherPlayerState;

AetherPlayer      *aether_player_create(void);
void               aether_player_destroy(AetherPlayer *player);
bool               aether_player_open(AetherPlayer *player, const char *url);
void               aether_player_play(AetherPlayer *player);
void               aether_player_pause(AetherPlayer *player);
void               aether_player_stop(AetherPlayer *player);
AetherPlayerState  aether_player_get_state(AetherPlayer *player);
int64_t            aether_player_get_time_ms(AetherPlayer *player);
void               aether_player_set_volume(AetherPlayer *player, int volume_pct);

/* ── Stream client ──────────────────────────────────────────────────────────── */

typedef struct AetherStreamClient AetherStreamClient;

/**
 * Callback invoked for each segment received.
 * buf is valid only for the duration of the callback.
 */
typedef void (*aether_segment_cb)(const uint8_t *buf, size_t len, void *user_data);

AetherStreamClient *aether_stream_client_create(aether_segment_cb cb, void *user_data);
void                aether_stream_client_destroy(AetherStreamClient *client);
bool                aether_stream_client_connect(AetherStreamClient *client, const char *url);
int                 aether_stream_client_read_segment(AetherStreamClient *client,
                                                       uint8_t *buf, size_t max_len);
void                aether_stream_client_close(AetherStreamClient *client);

#ifdef __cplusplus
}
#endif

#endif /* AETHER_MEDIA_H */
