#include "aethermesh_media.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

/* Simple assertion macro that prints PASS/FAIL and tracks failures */
static int g_failures = 0;

#define ASSERT(cond, msg) \
    do { \
        if (cond) { \
            printf("  PASS: %s\n", (msg)); \
        } else { \
            printf("  FAIL: %s  (line %d)\n", (msg), __LINE__); \
            g_failures++; \
        } \
    } while (0)

/* ── Social graph tests ──────────────────────────────────────────────────────── */

static void test_follow_basic(void) {
    printf("test_follow_basic\n");
    AetherMeshSocialGraph *g = aethermesh_social_graph_create();
    ASSERT(g != NULL, "create returns non-NULL");

    aethermesh_social_graph_follow(g, "alice");
    ASSERT(aethermesh_social_graph_is_following(g, "alice"), "is_following alice after follow");
    ASSERT(!aethermesh_social_graph_is_following(g, "bob"),  "is_following bob returns false");
    ASSERT(aethermesh_social_graph_count(g) == 1, "count is 1 after one follow");

    aethermesh_social_graph_destroy(g);
}

static void test_follow_multiple(void) {
    printf("test_follow_multiple\n");
    AetherMeshSocialGraph *g = aethermesh_social_graph_create();

    aethermesh_social_graph_follow(g, "alice");
    aethermesh_social_graph_follow(g, "bob");
    aethermesh_social_graph_follow(g, "carol");

    ASSERT(aethermesh_social_graph_count(g) == 3, "count is 3 after three follows");
    ASSERT(aethermesh_social_graph_is_following(g, "bob"),   "is_following bob");
    ASSERT(aethermesh_social_graph_is_following(g, "carol"), "is_following carol");

    aethermesh_social_graph_destroy(g);
}

static void test_double_follow_idempotent(void) {
    printf("test_double_follow_idempotent\n");
    AetherMeshSocialGraph *g = aethermesh_social_graph_create();

    aethermesh_social_graph_follow(g, "alice");
    aethermesh_social_graph_follow(g, "alice");
    ASSERT(aethermesh_social_graph_count(g) == 1, "double follow keeps count at 1");

    aethermesh_social_graph_destroy(g);
}

static void test_unfollow(void) {
    printf("test_unfollow\n");
    AetherMeshSocialGraph *g = aethermesh_social_graph_create();

    aethermesh_social_graph_follow(g, "alice");
    aethermesh_social_graph_follow(g, "bob");
    aethermesh_social_graph_unfollow(g, "alice");

    ASSERT(!aethermesh_social_graph_is_following(g, "alice"), "alice removed after unfollow");
    ASSERT(aethermesh_social_graph_is_following(g, "bob"),    "bob still present");
    ASSERT(aethermesh_social_graph_count(g) == 1,             "count is 1 after unfollow");

    aethermesh_social_graph_destroy(g);
}

static void test_unfollow_not_present(void) {
    printf("test_unfollow_not_present\n");
    AetherMeshSocialGraph *g = aethermesh_social_graph_create();

    aethermesh_social_graph_unfollow(g, "ghost"); /* must not crash */
    ASSERT(aethermesh_social_graph_count(g) == 0, "count stays 0 after unfollowing non-existent");

    aethermesh_social_graph_destroy(g);
}

static void test_list(void) {
    printf("test_list\n");
    AetherMeshSocialGraph *g = aethermesh_social_graph_create();

    aethermesh_social_graph_follow(g, "alice");
    aethermesh_social_graph_follow(g, "bob");

    const char *out[8];
    int n = aethermesh_social_graph_list(g, out, 8);
    ASSERT(n == 2, "list returns 2 entries");

    /* Verify both UHIDs appear (order may vary) */
    int found_alice = 0, found_bob = 0;
    for (int i = 0; i < n; i++) {
        if (strcmp(out[i], "alice") == 0) found_alice = 1;
        if (strcmp(out[i], "bob")   == 0) found_bob   = 1;
    }
    ASSERT(found_alice, "alice in list");
    ASSERT(found_bob,   "bob in list");

    aethermesh_social_graph_destroy(g);
}

static void test_list_max_cap(void) {
    printf("test_list_max_cap\n");
    AetherMeshSocialGraph *g = aethermesh_social_graph_create();

    for (int i = 0; i < 10; i++) {
        char uhid[32];
        snprintf(uhid, sizeof(uhid), "user-%02d", i);
        aethermesh_social_graph_follow(g, uhid);
    }
    ASSERT(aethermesh_social_graph_count(g) == 10, "10 follows succeed");

    const char *out[5];
    int n = aethermesh_social_graph_list(g, out, 5);
    ASSERT(n == 5, "list honours max cap of 5");

    aethermesh_social_graph_destroy(g);
}

/* ── Duration formatting tests ───────────────────────────────────────────────── */

static void test_duration_live(void) {
    printf("test_duration_live\n");
    char buf[32];
    aethermesh_format_duration(0, buf, sizeof(buf));
    ASSERT(strcmp(buf, "Live") == 0, "0 ms formats to Live");
    aethermesh_format_duration(-1, buf, sizeof(buf));
    ASSERT(strcmp(buf, "Live") == 0, "-1 ms formats to Live");
}

static void test_duration_sub_hour(void) {
    printf("test_duration_sub_hour\n");
    char buf[32];
    aethermesh_format_duration(272000, buf, sizeof(buf));
    ASSERT(strcmp(buf, "4:32") == 0, "272000ms formats to 4:32");
    aethermesh_format_duration(65000, buf, sizeof(buf));
    ASSERT(strcmp(buf, "1:05") == 0, "65000ms formats to 1:05");
}

static void test_duration_over_hour(void) {
    printf("test_duration_over_hour\n");
    char buf[32];
    aethermesh_format_duration(3600000, buf, sizeof(buf));
    ASSERT(strcmp(buf, "1:00:00") == 0, "3600000ms formats to 1:00:00");
    aethermesh_format_duration(5025000, buf, sizeof(buf));
    ASSERT(strcmp(buf, "1:23:45") == 0, "5025000ms formats to 1:23:45");
}

/* ── Entry point ─────────────────────────────────────────────────────────────── */

int main(void) {
    printf("=== Aether Media C Tests ===\n\n");

    test_follow_basic();
    test_follow_multiple();
    test_double_follow_idempotent();
    test_unfollow();
    test_unfollow_not_present();
    test_list();
    test_list_max_cap();
    test_duration_live();
    test_duration_sub_hour();
    test_duration_over_hour();

    printf("\n");
    if (g_failures == 0) {
        printf("All tests PASSED.\n");
        return 0;
    } else {
        printf("%d test(s) FAILED.\n", g_failures);
        return 1;
    }
}
