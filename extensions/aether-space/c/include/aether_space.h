#ifndef AETHER_SPACE_H
#define AETHER_SPACE_H

#include <stdint.h>
#include <stddef.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Enums ─────────────────────────────────────────────────────────────────── */

typedef enum AetherBreadcrumbType {
    AETHER_BREADCRUMB_POST   = 0,
    AETHER_BREADCRUMB_EVENT  = 1,
    AETHER_BREADCRUMB_ALERT  = 2,
    AETHER_BREADCRUMB_OFFER  = 3,
    AETHER_BREADCRUMB_NOTICE = 4,
    AETHER_BREADCRUMB_PINNED = 5
} AetherBreadcrumbType;

/* ── Structs ────────────────────────────────────────────────────────────────── */

/**
 * AetherSpaceBreadcrumb
 * Represents a geo-pinned noticeboard entry on the mesh.
 */
typedef struct AetherSpaceBreadcrumb {
    char                   id[37];          /* UUID string (36 chars + NUL) */
    char                   space_id[37];
    char                   author_id[37];
    char                   geo_hash[13];    /* GeoHash up to precision-12 + NUL */
    AetherBreadcrumbType   type;
    char                  *title;           /* heap-allocated, freed by aether_space_free */
    char                  *body;            /* heap-allocated */
    int                    is_pinned;
    int                    reaction_count;
    int                    reply_count;
    time_t                 created_at;
    time_t                 updated_at;
    time_t                 expires_at;      /* 0 = no expiry */
} AetherSpaceBreadcrumb;

/**
 * AetherSpaceScanResult
 * Returned by aether_space_scan; caller frees with aether_space_free_scan.
 */
typedef struct AetherSpaceScanResult {
    AetherSpaceBreadcrumb *items;
    size_t                 count;
} AetherSpaceScanResult;

/* ── Function Prototypes ────────────────────────────────────────────────────── */

/**
 * Publish a breadcrumb to the mesh-local noticeboard.
 * Returns a heap-allocated breadcrumb on success, NULL on failure.
 * Caller must free with aether_space_free().
 */
AetherSpaceBreadcrumb *aether_space_drop(const AetherSpaceBreadcrumb *breadcrumb);

/**
 * Scan for breadcrumbs near geo_hash within radius_km.
 * Returns a heap-allocated scan result on success, NULL on failure.
 * Caller must free with aether_space_free_scan().
 */
AetherSpaceScanResult *aether_space_scan(const char *geo_hash, double radius_km);

/**
 * Pin a breadcrumb to the top of its space noticeboard.
 * Returns a heap-allocated updated breadcrumb on success, NULL on failure.
 */
AetherSpaceBreadcrumb *aether_space_pin(const char *breadcrumb_id, const char *space_id);

/**
 * Delete a breadcrumb by ID.
 * Returns 1 if deleted, 0 otherwise.
 */
int aether_space_delete(const char *breadcrumb_id, const char *requester_id);

/**
 * Free a heap-allocated AetherSpaceBreadcrumb returned by this library.
 */
void aether_space_free(AetherSpaceBreadcrumb *breadcrumb);

/**
 * Free a heap-allocated AetherSpaceScanResult returned by aether_space_scan.
 */
void aether_space_free_scan(AetherSpaceScanResult *result);

#ifdef __cplusplus
}
#endif

#endif /* AETHER_SPACE_H */
