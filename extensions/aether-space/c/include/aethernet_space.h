#ifndef AETHERNET_SPACE_H
#define AETHERNET_SPACE_H

#include <stdint.h>
#include <stddef.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Enums ─────────────────────────────────────────────────────────────────── */

typedef enum AetherNetBreadcrumbType {
    AETHERNET_BREADCRUMB_POST   = 0,
    AETHERNET_BREADCRUMB_EVENT  = 1,
    AETHERNET_BREADCRUMB_ALERT  = 2,
    AETHERNET_BREADCRUMB_OFFER  = 3,
    AETHERNET_BREADCRUMB_NOTICE = 4,
    AETHERNET_BREADCRUMB_PINNED = 5
} AetherNetBreadcrumbType;

/* ── Structs ────────────────────────────────────────────────────────────────── */

/**
 * AetherNetSpaceBreadcrumb
 * Represents a geo-pinned noticeboard entry on the mesh.
 */
typedef struct AetherNetSpaceBreadcrumb {
    char                   id[37];          /* UUID string (36 chars + NUL) */
    char                   space_id[37];
    char                   author_id[37];
    char                   geo_hash[13];    /* GeoHash up to precision-12 + NUL */
    AetherNetBreadcrumbType   type;
    char                  *title;           /* heap-allocated, freed by aethernet_space_free */
    char                  *body;            /* heap-allocated */
    int                    is_pinned;
    int                    reaction_count;
    int                    reply_count;
    time_t                 created_at;
    time_t                 updated_at;
    time_t                 expires_at;      /* 0 = no expiry */
} AetherNetSpaceBreadcrumb;

/**
 * AetherNetSpaceScanResult
 * Returned by aethernet_space_scan; caller frees with aethernet_space_free_scan.
 */
typedef struct AetherNetSpaceScanResult {
    AetherNetSpaceBreadcrumb *items;
    size_t                 count;
} AetherNetSpaceScanResult;

/* ── Function Prototypes ────────────────────────────────────────────────────── */

/**
 * Publish a breadcrumb to the mesh-local noticeboard.
 * Returns a heap-allocated breadcrumb on success, NULL on failure.
 * Caller must free with aethernet_space_free().
 */
AetherNetSpaceBreadcrumb *aethernet_space_drop(const AetherNetSpaceBreadcrumb *breadcrumb);

/**
 * Scan for breadcrumbs near geo_hash within radius_km.
 * Returns a heap-allocated scan result on success, NULL on failure.
 * Caller must free with aethernet_space_free_scan().
 */
AetherNetSpaceScanResult *aethernet_space_scan(const char *geo_hash, double radius_km);

/**
 * Pin a breadcrumb to the top of its space noticeboard.
 * Returns a heap-allocated updated breadcrumb on success, NULL on failure.
 */
AetherNetSpaceBreadcrumb *aethernet_space_pin(const char *breadcrumb_id, const char *space_id);

/**
 * Delete a breadcrumb by ID.
 * Returns 1 if deleted, 0 otherwise.
 */
int aethernet_space_delete(const char *breadcrumb_id, const char *requester_id);

/**
 * Free a heap-allocated AetherNetSpaceBreadcrumb returned by this library.
 */
void aethernet_space_free(AetherNetSpaceBreadcrumb *breadcrumb);

/**
 * Free a heap-allocated AetherNetSpaceScanResult returned by aethernet_space_scan.
 */
void aethernet_space_free_scan(AetherNetSpaceScanResult *result);

#ifdef __cplusplus
}
#endif

#endif /* AETHERNET_SPACE_H */
