#include "aethernet_media.h"
#include <stdio.h>
#include <string.h>

static void on_segment(const uint8_t *buf, size_t len, void *user_data) {
    (void)buf;
    (void)user_data;
    printf("[Demo] Received segment: %zu bytes\n", len);
}

static const char *state_name(AetherNetPlayerState s) {
    switch (s) {
    case AETHERNET_PLAYER_IDLE:    return "idle";
    case AETHERNET_PLAYER_OPENING: return "opening";
    case AETHERNET_PLAYER_PLAYING: return "playing";
    case AETHERNET_PLAYER_PAUSED:  return "paused";
    case AETHERNET_PLAYER_STOPPED: return "stopped";
    case AETHERNET_PLAYER_ERROR:   return "error";
    default:                    return "unknown";
    }
}

int main(void) {
    printf("=== Aether Media C Demo ===\n\n");

    /* ── Social graph ─────────────────────────────────────────────────────── */
    printf("-- Social Graph --\n");
    AetherNetSocialGraph *graph = aethernet_social_graph_create();
    if (!graph) {
        fprintf(stderr, "Failed to create social graph\n");
        return 1;
    }

    aethernet_social_graph_follow(graph, "alice-uhid-0001");
    aethernet_social_graph_follow(graph, "bob-uhid-0002");
    aethernet_social_graph_follow(graph, "carol-uhid-0003");
    printf("Following %d account(s)\n", aethernet_social_graph_count(graph));

    const char *list[16];
    int n = aethernet_social_graph_list(graph, list, 16);
    for (int i = 0; i < n; i++) {
        printf("  • %s\n", list[i]);
    }

    printf("Is following alice? %s\n",
           aethernet_social_graph_is_following(graph, "alice-uhid-0001") ? "yes" : "no");

    aethernet_social_graph_unfollow(graph, "bob-uhid-0002");
    printf("After unfollow: %d account(s)\n", aethernet_social_graph_count(graph));

    /* ── Duration formatting ──────────────────────────────────────────────── */
    printf("\n-- Duration Formatting --\n");
    char dur_buf[32];
    int64_t durations[] = { 0, 272000, 65000, 3600000, 5025000 };
    const char *expected[] = { "Live", "4:32", "1:05", "1:00:00", "1:23:45" };
    for (int i = 0; i < 5; i++) {
        aethernet_format_duration(durations[i], dur_buf, sizeof(dur_buf));
        printf("  %lldms → \"%s\" (expected \"%s\") %s\n",
               (long long)durations[i], dur_buf, expected[i],
               strcmp(dur_buf, expected[i]) == 0 ? "OK" : "FAIL");
    }

    /* ── Player ───────────────────────────────────────────────────────────── */
    printf("\n-- Player --\n");
    AetherNetPlayer *player = aethernet_player_create();
    if (!player) {
        fprintf(stderr, "Failed to create player\n");
        aethernet_social_graph_destroy(graph);
        return 1;
    }

    const char *test_uri = "http://relay.aethernet.network/media/demo.mp4";
    bool opened = aethernet_player_open(player, test_uri);
    printf("Open result: %s\n", opened ? "success" : "failed (no LibVLC)");
    printf("State: %s\n", state_name(aethernet_player_get_state(player)));

    aethernet_player_play(player);
    printf("State after play: %s\n", state_name(aethernet_player_get_state(player)));

    aethernet_player_pause(player);
    printf("State after pause: %s\n", state_name(aethernet_player_get_state(player)));

    aethernet_player_stop(player);
    printf("State after stop: %s\n", state_name(aethernet_player_get_state(player)));
    aethernet_player_set_volume(player, 80);

    /* ── Stream client (connect will fail in CI — shows the API works) ──── */
    printf("\n-- Stream Client --\n");
    AetherNetStreamClient *sc = aethernet_stream_client_create(on_segment, NULL);
    if (sc) {
        /* This will fail unless a live relay is reachable — that's expected */
        bool ok = aethernet_stream_client_connect(sc, "http://127.0.0.1:9999/demo-stream");
        printf("Stream connect: %s\n", ok ? "connected" : "not reachable (expected in CI)");
        aethernet_stream_client_destroy(sc);
    }

    /* ── Cleanup ──────────────────────────────────────────────────────────── */
    aethernet_player_destroy(player);
    aethernet_social_graph_destroy(graph);

    printf("\n=== Demo complete ===\n");
    return 0;
}
