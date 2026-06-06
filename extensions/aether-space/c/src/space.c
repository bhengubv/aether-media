#include "aethermesh_space.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/* ── Internal helpers ──────────────────────────────────────────────────────── */

static void log_not_implemented(const char *fn_name) {
    fprintf(stderr, "[aether-space] %s: not implemented\n", fn_name);
}

static AetherMeshSpaceBreadcrumb *copy_breadcrumb(const AetherMeshSpaceBreadcrumb *src) {
    if (!src) return NULL;
    AetherMeshSpaceBreadcrumb *dst = (AetherMeshSpaceBreadcrumb *)calloc(1, sizeof(AetherMeshSpaceBreadcrumb));
    if (!dst) return NULL;
    memcpy(dst, src, sizeof(AetherMeshSpaceBreadcrumb));
    /* deep-copy heap strings */
    dst->title = src->title ? strdup(src->title) : NULL;
    dst->body  = src->body  ? strdup(src->body)  : NULL;
    return dst;
}

/* ── Public API stubs ──────────────────────────────────────────────────────── */

AetherMeshSpaceBreadcrumb *aethermesh_space_drop(const AetherMeshSpaceBreadcrumb *breadcrumb) {
    log_not_implemented("aethermesh_space_drop");
    (void)breadcrumb;
    return NULL;
}

AetherMeshSpaceScanResult *aethermesh_space_scan(const char *geo_hash, double radius_km) {
    log_not_implemented("aethermesh_space_scan");
    (void)geo_hash;
    (void)radius_km;
    return NULL;
}

AetherMeshSpaceBreadcrumb *aethermesh_space_pin(const char *breadcrumb_id, const char *space_id) {
    log_not_implemented("aethermesh_space_pin");
    (void)breadcrumb_id;
    (void)space_id;
    return NULL;
}

int aethermesh_space_delete(const char *breadcrumb_id, const char *requester_id) {
    log_not_implemented("aethermesh_space_delete");
    (void)breadcrumb_id;
    (void)requester_id;
    return 0;
}

void aethermesh_space_free(AetherMeshSpaceBreadcrumb *breadcrumb) {
    if (!breadcrumb) return;
    free(breadcrumb->title);
    free(breadcrumb->body);
    free(breadcrumb);
}

void aethermesh_space_free_scan(AetherMeshSpaceScanResult *result) {
    if (!result) return;
    for (size_t i = 0; i < result->count; ++i) {
        free(result->items[i].title);
        free(result->items[i].body);
    }
    free(result->items);
    free(result);
}
