#ifndef AETHERMESH_MEDIA_H
#define AETHERMESH_MEDIA_H

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
} AetherMeshMediaContent;

typedef struct {
    char uhid[128];
    char display_name[256];
    char aethermesh_tag[16];
    int  follower_count;
    int  content_count;
} AetherMeshMediaProfile;

/* ── Duration formatting ────────────────────────────────────────────────────── */

/**
 * Format duration_ms into a human-readable string written to out.
 *  - 0  → "Live"
 *  - < 1 hour  → "M:SS"
 *  - >= 1 hour → "H:MM:SS"
 *
 * out_len must be at least 16 bytes.
 */
void aethermesh_format_duration(int64_t duration_ms, char *out, size_t out_len);

/* ── Social graph (opaque handle) ───────────────────────────────────────────── */

typedef struct AetherMeshSocialGraph AetherMeshSocialGraph;

/** Allocate and return a new empty social graph.  Caller must call destroy. */
AetherMeshSocialGraph *aethermesh_social_graph_create(void);

/** Free all memory owned by graph. */
void aethermesh_social_graph_destroy(AetherMeshSocialGraph *graph);

/** Add uhid to the following set.  No-op if already present. */
void aethermesh_social_graph_follow(AetherMeshSocialGraph *graph, const char *uhid);

/** Remove uhid from the following set.  No-op if not present. */
void aethermesh_social_graph_unfollow(AetherMeshSocialGraph *graph, const char *uhid);

/** Returns true when uhid is in the following set. */
bool aethermesh_social_graph_is_following(AetherMeshSocialGraph *graph, const char *uhid);

/**
 * Write all followed UHIDs into out_uhids (array of char* with room for
 * at least max entries).  Returns the number of entries written.
 * out_uhids[i] points into graph-owned memory — do not free.
 */
int  aethermesh_social_graph_list(AetherMeshSocialGraph *graph,
                               const char       **out_uhids,
                               int                max);

/** Returns the number of followed accounts. */
int  aethermesh_social_graph_count(AetherMeshSocialGraph *graph);

/* ── Player (LibVLC wrapper) ────────────────────────────────────────────────── */

typedef struct AetherMeshPlayer AetherMeshPlayer;

typedef enum {
    AETHERMESH_PLAYER_IDLE    = 0,
    AETHERMESH_PLAYER_OPENING = 1,
    AETHERMESH_PLAYER_PLAYING = 2,
    AETHERMESH_PLAYER_PAUSED  = 3,
    AETHERMESH_PLAYER_STOPPED = 4,
    AETHERMESH_PLAYER_ERROR   = 5,
} AetherMeshPlayerState;

AetherMeshPlayer      *aethermesh_player_create(void);
void               aethermesh_player_destroy(AetherMeshPlayer *player);
bool               aethermesh_player_open(AetherMeshPlayer *player, const char *url);
void               aethermesh_player_play(AetherMeshPlayer *player);
void               aethermesh_player_pause(AetherMeshPlayer *player);
void               aethermesh_player_stop(AetherMeshPlayer *player);
AetherMeshPlayerState  aethermesh_player_get_state(AetherMeshPlayer *player);
int64_t            aethermesh_player_get_time_ms(AetherMeshPlayer *player);
void               aethermesh_player_set_volume(AetherMeshPlayer *player, int volume_pct);

/* ── Stream client ──────────────────────────────────────────────────────────── */

typedef struct AetherMeshStreamClient AetherMeshStreamClient;

/**
 * Callback invoked for each segment received.
 * buf is valid only for the duration of the callback.
 */
typedef void (*aethermesh_segment_cb)(const uint8_t *buf, size_t len, void *user_data);

AetherMeshStreamClient *aethermesh_stream_client_create(aethermesh_segment_cb cb, void *user_data);
void                aethermesh_stream_client_destroy(AetherMeshStreamClient *client);
bool                aethermesh_stream_client_connect(AetherMeshStreamClient *client, const char *url);
int                 aethermesh_stream_client_read_segment(AetherMeshStreamClient *client,
                                                       uint8_t *buf, size_t max_len);
void                aethermesh_stream_client_close(AetherMeshStreamClient *client);

#ifdef __cplusplus
}
#endif

#endif /* AETHERMESH_MEDIA_H */
