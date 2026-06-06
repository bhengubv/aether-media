#ifndef AETHERMESH_SPACE_H
#define AETHERMESH_SPACE_H

#include <stdint.h>
#include <stddef.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Enums ─────────────────────────────────────────────────────────────────── */

typedef enum AetherMeshBreadcrumbType {
    AETHERMESH_BREADCRUMB_POST   = 0,
    AETHERMESH_BREADCRUMB_EVENT  = 1,
    AETHERMESH_BREADCRUMB_ALERT  = 2,
    AETHERMESH_BREADCRUMB_OFFER  = 3,
    AETHERMESH_BREADCRUMB_NOTICE = 4,
    AETHERMESH_BREADCRUMB_PINNED = 5
} AetherMeshBreadcrumbType;

/* ── Structs ────────────────────────────────────────────────────────────────── */

/**
 * AetherMeshSpaceBreadcrumb
 * Represents a geo-pinned noticeboard entry on the mesh.
 */
typedef struct AetherMeshSpaceBreadcrumb {
    char                   id[37];          /* UUID string (36 chars + NUL) */
    char                   space_id[37];
    char                   author_id[37];
    char                   geo_hash[13];    /* GeoHash up to precision-12 + NUL */
    AetherMeshBreadcrumbType   type;
    char                  *title;           /* heap-allocated, freed by aethermesh_space_free */
    char                  *body;            /* heap-allocated */
    int                    is_pinned;
    int                    reaction_count;
    int                    reply_count;
    time_t                 created_at;
    time_t                 updated_at;
    time_t                 expires_at;      /* 0 = no expiry */
} AetherMeshSpaceBreadcrumb;

/**
 * AetherMeshSpaceScanResult
 * Returned by aethermesh_space_scan; caller frees with aethermesh_space_free_scan.
 */
typedef struct AetherMeshSpaceScanResult {
    AetherMeshSpaceBreadcrumb *items;
    size_t                 count;
} AetherMeshSpaceScanResult;

/* ── Function Prototypes ────────────────────────────────────────────────────── */

/**
 * Publish a breadcrumb to the mesh-local noticeboard.
 * Returns a heap-allocated breadcrumb on success, NULL on failure.
 * Caller must free with aethermesh_space_free().
 */
AetherMeshSpaceBreadcrumb *aethermesh_space_drop(const AetherMeshSpaceBreadcrumb *breadcrumb);

/**
 * Scan for breadcrumbs near geo_hash within radius_km.
 * Returns a heap-allocated scan result on success, NULL on failure.
 * Caller must free with aethermesh_space_free_scan().
 */
AetherMeshSpaceScanResult *aethermesh_space_scan(const char *geo_hash, double radius_km);

/**
 * Pin a breadcrumb to the top of its space noticeboard.
 * Returns a heap-allocated updated breadcrumb on success, NULL on failure.
 */
AetherMeshSpaceBreadcrumb *aethermesh_space_pin(const char *breadcrumb_id, const char *space_id);

/**
 * Delete a breadcrumb by ID.
 * Returns 1 if deleted, 0 otherwise.
 */
int aethermesh_space_delete(const char *breadcrumb_id, const char *requester_id);

/**
 * Free a heap-allocated AetherMeshSpaceBreadcrumb returned by this library.
 */
void aethermesh_space_free(AetherMeshSpaceBreadcrumb *breadcrumb);

/**
 * Free a heap-allocated AetherMeshSpaceScanResult returned by aethermesh_space_scan.
 */
void aethermesh_space_free_scan(AetherMeshSpaceScanResult *result);

#ifdef __cplusplus
}
#endif

#endif /* AETHERMESH_SPACE_H */
