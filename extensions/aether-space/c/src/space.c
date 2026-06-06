#include "aethernet_space.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/* ── Internal helpers ──────────────────────────────────────────────────────── */

static void log_not_implemented(const char *fn_name) {
    fprintf(stderr, "[aether-space] %s: not implemented\n", fn_name);
}

static AetherNetSpaceBreadcrumb *copy_breadcrumb(const AetherNetSpaceBreadcrumb *src) {
    if (!src) return NULL;
    AetherNetSpaceBreadcrumb *dst = (AetherNetSpaceBreadcrumb *)calloc(1, sizeof(AetherNetSpaceBreadcrumb));
    if (!dst) return NULL;
    memcpy(dst, src, sizeof(AetherNetSpaceBreadcrumb));
    /* deep-copy heap strings */
    dst->title = src->title ? strdup(src->title) : NULL;
    dst->body  = src->body  ? strdup(src->body)  : NULL;
    return dst;
}

/* ── Public API stubs ──────────────────────────────────────────────────────── */

AetherNetSpaceBreadcrumb *aethernet_space_drop(const AetherNetSpaceBreadcrumb *breadcrumb) {
    log_not_implemented("aethernet_space_drop");
    (void)breadcrumb;
    return NULL;
}

AetherNetSpaceScanResult *aethernet_space_scan(const char *geo_hash, double radius_km) {
    log_not_implemented("aethernet_space_scan");
    (void)geo_hash;
    (void)radius_km;
    return NULL;
}

AetherNetSpaceBreadcrumb *aethernet_space_pin(const char *breadcrumb_id, const char *space_id) {
    log_not_implemented("aethernet_space_pin");
    (void)breadcrumb_id;
    (void)space_id;
    return NULL;
}

int aethernet_space_delete(const char *breadcrumb_id, const char *requester_id) {
    log_not_implemented("aethernet_space_delete");
    (void)breadcrumb_id;
    (void)requester_id;
    return 0;
}

void aethernet_space_free(AetherNetSpaceBreadcrumb *breadcrumb) {
    if (!breadcrumb) return;
    free(breadcrumb->title);
    free(breadcrumb->body);
    free(breadcrumb);
}

void aethernet_space_free_scan(AetherNetSpaceScanResult *result) {
    if (!result) return;
    for (size_t i = 0; i < result->count; ++i) {
        free(result->items[i].title);
        free(result->items[i].body);
    }
    free(result->items);
    free(result);
}
