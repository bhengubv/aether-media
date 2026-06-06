#include "aethermesh_media.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

/* Initial capacity; doubles on overflow */
#define INITIAL_CAP 16

struct AetherMeshSocialGraph {
    char   **uhids;     /* heap-allocated array of heap-allocated strings */
    int      count;
    int      capacity;
};

/* ── Lifecycle ──────────────────────────────────────────────────────────────── */

AetherMeshSocialGraph *aethermesh_social_graph_create(void) {
    AetherMeshSocialGraph *g = (AetherMeshSocialGraph *)malloc(sizeof(AetherMeshSocialGraph));
    if (!g) return NULL;
    g->uhids    = (char **)malloc(INITIAL_CAP * sizeof(char *));
    g->count    = 0;
    g->capacity = INITIAL_CAP;
    if (!g->uhids) {
        free(g);
        return NULL;
    }
    return g;
}

void aethermesh_social_graph_destroy(AetherMeshSocialGraph *graph) {
    if (!graph) return;
    for (int i = 0; i < graph->count; i++) {
        free(graph->uhids[i]);
    }
    free(graph->uhids);
    free(graph);
}

/* ── Internal helpers ────────────────────────────────────────────────────────── */

/** Returns the index of uhid in the array, or -1 if not found. */
static int _find_index(AetherMeshSocialGraph *graph, const char *uhid) {
    for (int i = 0; i < graph->count; i++) {
        if (strcmp(graph->uhids[i], uhid) == 0)
            return i;
    }
    return -1;
}

/** Grow the backing array by doubling capacity.  Returns false on OOM. */
static bool _grow(AetherMeshSocialGraph *graph) {
    int new_cap = graph->capacity * 2;
    char **new_arr = (char **)realloc(graph->uhids, new_cap * sizeof(char *));
    if (!new_arr) return false;
    graph->uhids    = new_arr;
    graph->capacity = new_cap;
    return true;
}

/* ── Public operations ───────────────────────────────────────────────────────── */

void aethermesh_social_graph_follow(AetherMeshSocialGraph *graph, const char *uhid) {
    if (!graph || !uhid || uhid[0] == '\0') return;
    if (_find_index(graph, uhid) >= 0) return;  /* already following */

    if (graph->count >= graph->capacity) {
        if (!_grow(graph)) return;  /* OOM — silently drop */
    }
    graph->uhids[graph->count] = strdup(uhid);
    if (graph->uhids[graph->count]) {
        graph->count++;
    }
}

void aethermesh_social_graph_unfollow(AetherMeshSocialGraph *graph, const char *uhid) {
    if (!graph || !uhid) return;
    int idx = _find_index(graph, uhid);
    if (idx < 0) return;  /* not present — no-op */

    free(graph->uhids[idx]);

    /* Shift remaining elements left to close the gap */
    for (int i = idx; i < graph->count - 1; i++) {
        graph->uhids[i] = graph->uhids[i + 1];
    }
    graph->count--;
}

bool aethermesh_social_graph_is_following(AetherMeshSocialGraph *graph, const char *uhid) {
    if (!graph || !uhid) return false;
    return _find_index(graph, uhid) >= 0;
}

int aethermesh_social_graph_list(AetherMeshSocialGraph *graph,
                              const char       **out_uhids,
                              int                max) {
    if (!graph || !out_uhids || max <= 0) return 0;
    int written = graph->count < max ? graph->count : max;
    for (int i = 0; i < written; i++) {
        out_uhids[i] = graph->uhids[i];
    }
    return written;
}

int aethermesh_social_graph_count(AetherMeshSocialGraph *graph) {
    if (!graph) return 0;
    return graph->count;
}

/* ── Duration formatting ─────────────────────────────────────────────────────── */

void aethermesh_format_duration(int64_t duration_ms, char *out, size_t out_len) {
    if (!out || out_len == 0) return;
    if (duration_ms <= 0) {
        snprintf(out, out_len, "Live");
        return;
    }
    int64_t total_secs = duration_ms / 1000;
    int64_t hours   = total_secs / 3600;
    int64_t minutes = (total_secs % 3600) / 60;
    int64_t seconds = total_secs % 60;
    if (hours > 0) {
        snprintf(out, out_len, "%lld:%02lld:%02lld",
                 (long long)hours, (long long)minutes, (long long)seconds);
    } else {
        snprintf(out, out_len, "%lld:%02lld",
                 (long long)minutes, (long long)seconds);
    }
}
