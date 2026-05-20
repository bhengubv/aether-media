#include "aether_media.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

/*
 * LibVLC wrapper for Aether Media.
 *
 * When AETHER_HAVE_LIBVLC is defined to 1 (set by CMake when libvlc is found)
 * this file compiles a real libvlc-backed player.  Otherwise it compiles a
 * fully functional in-process stub that tracks state without media output —
 * this keeps the build clean on headless CI machines with no display server.
 */

#if AETHER_HAVE_LIBVLC
#  include <vlc/vlc.h>
#endif

struct AetherPlayer {
#if AETHER_HAVE_LIBVLC
    libvlc_instance_t    *vlc;
    libvlc_media_t       *media;
    libvlc_media_player_t *mp;
#endif
    AetherPlayerState     state;
    char                  current_url[2048];
};

/* ── Lifecycle ───────────────────────────────────────────────────────────────── */

AetherPlayer *aether_player_create(void) {
    AetherPlayer *p = (AetherPlayer *)calloc(1, sizeof(AetherPlayer));
    if (!p) return NULL;
    p->state = AETHER_PLAYER_IDLE;

#if AETHER_HAVE_LIBVLC
    p->vlc = libvlc_new(0, NULL);
    if (!p->vlc) {
        free(p);
        return NULL;
    }
#endif
    return p;
}

void aether_player_destroy(AetherPlayer *player) {
    if (!player) return;
#if AETHER_HAVE_LIBVLC
    if (player->mp)    { libvlc_media_player_release(player->mp);    player->mp    = NULL; }
    if (player->media) { libvlc_media_release(player->media);        player->media = NULL; }
    if (player->vlc)   { libvlc_release(player->vlc);                player->vlc   = NULL; }
#endif
    free(player);
}

/* ── Control ─────────────────────────────────────────────────────────────────── */

bool aether_player_open(AetherPlayer *player, const char *url) {
    if (!player || !url) return false;

    strncpy(player->current_url, url, sizeof(player->current_url) - 1);
    player->state = AETHER_PLAYER_OPENING;

#if AETHER_HAVE_LIBVLC
    /* Release any previous media */
    if (player->mp)    { libvlc_media_player_stop(player->mp);    libvlc_media_player_release(player->mp);    player->mp    = NULL; }
    if (player->media) { libvlc_media_release(player->media);                                                  player->media = NULL; }

    player->media = libvlc_media_new_location(player->vlc, url);
    if (!player->media) {
        player->state = AETHER_PLAYER_ERROR;
        return false;
    }
    player->mp = libvlc_media_player_new_from_media(player->media);
    if (!player->mp) {
        libvlc_media_release(player->media);
        player->media = NULL;
        player->state = AETHER_PLAYER_ERROR;
        return false;
    }
#endif

    printf("[AetherPlayer] Opened: %s\n", url);
    return true;
}

void aether_player_play(AetherPlayer *player) {
    if (!player) return;
#if AETHER_HAVE_LIBVLC
    if (player->mp) libvlc_media_player_play(player->mp);
#endif
    player->state = AETHER_PLAYER_PLAYING;
    printf("[AetherPlayer] Playing\n");
}

void aether_player_pause(AetherPlayer *player) {
    if (!player) return;
#if AETHER_HAVE_LIBVLC
    if (player->mp) libvlc_media_player_pause(player->mp);
#endif
    player->state = AETHER_PLAYER_PAUSED;
    printf("[AetherPlayer] Paused\n");
}

void aether_player_stop(AetherPlayer *player) {
    if (!player) return;
#if AETHER_HAVE_LIBVLC
    if (player->mp) libvlc_media_player_stop(player->mp);
#endif
    player->state = AETHER_PLAYER_STOPPED;
    printf("[AetherPlayer] Stopped\n");
}

AetherPlayerState aether_player_get_state(AetherPlayer *player) {
    if (!player) return AETHER_PLAYER_ERROR;
#if AETHER_HAVE_LIBVLC
    if (player->mp) {
        libvlc_state_t s = libvlc_media_player_get_state(player->mp);
        switch (s) {
        case libvlc_Playing: return AETHER_PLAYER_PLAYING;
        case libvlc_Paused:  return AETHER_PLAYER_PAUSED;
        case libvlc_Stopped: return AETHER_PLAYER_STOPPED;
        case libvlc_Opening: return AETHER_PLAYER_OPENING;
        case libvlc_Error:   return AETHER_PLAYER_ERROR;
        default: break;
        }
    }
#endif
    return player->state;
}

int64_t aether_player_get_time_ms(AetherPlayer *player) {
    if (!player) return -1;
#if AETHER_HAVE_LIBVLC
    if (player->mp) {
        libvlc_time_t t = libvlc_media_player_get_time(player->mp);
        return (int64_t)t;
    }
#endif
    return 0;
}

void aether_player_set_volume(AetherPlayer *player, int volume_pct) {
    if (!player) return;
    if (volume_pct < 0)   volume_pct = 0;
    if (volume_pct > 200) volume_pct = 200;
#if AETHER_HAVE_LIBVLC
    if (player->mp) libvlc_audio_set_volume(player->mp, volume_pct);
#endif
    printf("[AetherPlayer] Volume: %d%%\n", volume_pct);
}
