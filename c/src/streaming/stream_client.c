#include "aether_media.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <errno.h>

/*
 * Minimal stream client using POSIX sockets.
 *
 * The client issues a plain HTTP GET to the given URL, then reads the
 * response body in chunks, treating each chunk as a media segment and
 * delivering it via the registered callback.
 *
 * The HTTP parsing is intentionally minimal: it skips headers by looking
 * for the \r\n\r\n separator, then returns body bytes.  This is sufficient
 * for Aether relay endpoints that use plain HTTP/1.1 chunked streaming.
 *
 * On Windows this file compiles using Winsock2.  On POSIX it uses the
 * BSD socket API.
 */

#ifdef _WIN32
#  include <winsock2.h>
#  include <ws2tcpip.h>
#  pragma comment(lib, "ws2_32.lib")
typedef SOCKET socket_t;
#  define INVALID_SOCK INVALID_SOCKET
#  define close_sock(s) closesocket(s)
#  define sock_errno (WSAGetLastError())
#else
#  include <sys/types.h>
#  include <sys/socket.h>
#  include <netdb.h>
#  include <unistd.h>
typedef int socket_t;
#  define INVALID_SOCK (-1)
#  define close_sock(s) close(s)
#  define sock_errno errno
#endif

#define READ_BUF_SIZE 65536

struct AetherStreamClient {
    socket_t           fd;
    aether_segment_cb  callback;
    void              *user_data;
    char               host[256];
    char               path[1024];
    char               port[8];
    bool               connected;
    uint8_t           *read_buf;
    size_t             buf_used;
    size_t             buf_cap;
    /* true once we have consumed the HTTP response headers */
    bool               headers_done;
};

/* ── URL parser ──────────────────────────────────────────────────────────────── */

/*
 * Parse a URL of the form http://host[:port]/path into host, port, path.
 * Returns true on success.
 */
static bool parse_url(const char *url,
                      char *host, size_t host_len,
                      char *port, size_t port_len,
                      char *path, size_t path_len) {
    const char *p = url;

    /* Skip scheme */
    if (strncmp(p, "http://", 7) == 0)  p += 7;
    else if (strncmp(p, "https://", 8) == 0) p += 8;

    /* Host (up to : or /) */
    const char *host_start = p;
    while (*p && *p != ':' && *p != '/') p++;

    size_t host_sz = (size_t)(p - host_start);
    if (host_sz == 0 || host_sz >= host_len) return false;
    memcpy(host, host_start, host_sz);
    host[host_sz] = '\0';

    /* Optional port */
    if (*p == ':') {
        p++;
        const char *port_start = p;
        while (*p && *p != '/') p++;
        size_t port_sz = (size_t)(p - port_start);
        if (port_sz == 0 || port_sz >= port_len) return false;
        memcpy(port, port_start, port_sz);
        port[port_sz] = '\0';
    } else {
        strncpy(port, "80", port_len - 1);
    }

    /* Path */
    if (*p == '/') {
        strncpy(path, p, path_len - 1);
        path[path_len - 1] = '\0';
    } else {
        strncpy(path, "/", path_len - 1);
    }

    return true;
}

/* ── Lifecycle ───────────────────────────────────────────────────────────────── */

AetherStreamClient *aether_stream_client_create(aether_segment_cb cb, void *user_data) {
    AetherStreamClient *c = (AetherStreamClient *)calloc(1, sizeof(AetherStreamClient));
    if (!c) return NULL;
    c->callback  = cb;
    c->user_data = user_data;
    c->fd        = INVALID_SOCK;
    c->read_buf  = (uint8_t *)malloc(READ_BUF_SIZE);
    c->buf_cap   = READ_BUF_SIZE;
    if (!c->read_buf) { free(c); return NULL; }

#ifdef _WIN32
    WSADATA wsa;
    WSAStartup(MAKEWORD(2, 2), &wsa);
#endif

    return c;
}

void aether_stream_client_destroy(AetherStreamClient *client) {
    if (!client) return;
    aether_stream_client_close(client);
    free(client->read_buf);
    free(client);
#ifdef _WIN32
    WSACleanup();
#endif
}

/* ── Connect ─────────────────────────────────────────────────────────────────── */

bool aether_stream_client_connect(AetherStreamClient *client, const char *url) {
    if (!client || !url) return false;

    if (!parse_url(url, client->host, sizeof(client->host),
                       client->port, sizeof(client->port),
                       client->path, sizeof(client->path))) {
        fprintf(stderr, "[AetherStream] Failed to parse URL: %s\n", url);
        return false;
    }

    struct addrinfo hints, *res;
    memset(&hints, 0, sizeof hints);
    hints.ai_family   = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;

    int rc = getaddrinfo(client->host, client->port, &hints, &res);
    if (rc != 0) {
        fprintf(stderr, "[AetherStream] getaddrinfo error: %s\n", gai_strerror(rc));
        return false;
    }

    socket_t sock = INVALID_SOCK;
    for (struct addrinfo *r = res; r != NULL; r = r->ai_next) {
        sock = socket(r->ai_family, r->ai_socktype, r->ai_protocol);
        if (sock == INVALID_SOCK) continue;
        if (connect(sock, r->ai_addr, (int)r->ai_addrlen) == 0) break;
        close_sock(sock);
        sock = INVALID_SOCK;
    }
    freeaddrinfo(res);

    if (sock == INVALID_SOCK) {
        fprintf(stderr, "[AetherStream] Could not connect to %s:%s\n",
                client->host, client->port);
        return false;
    }

    client->fd        = sock;
    client->connected = true;
    client->headers_done = false;
    client->buf_used  = 0;

    /* Send minimal HTTP GET request */
    char req[2048];
    snprintf(req, sizeof(req),
             "GET %s HTTP/1.1\r\n"
             "Host: %s\r\n"
             "Accept: application/octet-stream\r\n"
             "Connection: keep-alive\r\n"
             "\r\n",
             client->path, client->host);

    size_t req_len = strlen(req);
    ssize_t sent = send(sock, req, (int)req_len, 0);
    if (sent != (ssize_t)req_len) {
        fprintf(stderr, "[AetherStream] send failed\n");
        aether_stream_client_close(client);
        return false;
    }

    printf("[AetherStream] Connected to %s:%s%s\n",
           client->host, client->port, client->path);
    return true;
}

/* ── Read segment ────────────────────────────────────────────────────────────── */

int aether_stream_client_read_segment(AetherStreamClient *client,
                                       uint8_t *buf, size_t max_len) {
    if (!client || !buf || max_len == 0 || !client->connected) return -1;

    /* Read raw bytes from the socket */
    ssize_t n = recv(client->fd, (char *)buf, (int)max_len, 0);
    if (n <= 0) {
        if (n == 0) {
            printf("[AetherStream] Server closed connection\n");
        } else {
            fprintf(stderr, "[AetherStream] recv error: %d\n", (int)sock_errno);
        }
        aether_stream_client_close(client);
        return -1;
    }

    /* Skip HTTP headers on first read */
    if (!client->headers_done) {
        /* Search for \r\n\r\n */
        const uint8_t *body_start = NULL;
        for (ssize_t i = 0; i <= n - 4; i++) {
            if (buf[i]   == '\r' && buf[i+1] == '\n' &&
                buf[i+2] == '\r' && buf[i+3] == '\n') {
                body_start = buf + i + 4;
                break;
            }
        }
        if (body_start) {
            client->headers_done = true;
            ssize_t body_len = n - (ssize_t)(body_start - buf);
            if (body_len <= 0) return 0;
            /* Shift body bytes to front of buf */
            memmove(buf, body_start, (size_t)body_len);
            n = body_len;
        } else {
            /* Haven't seen end-of-headers yet — drop this chunk */
            return 0;
        }
    }

    /* Fire the callback with the segment data */
    if (client->callback) {
        client->callback(buf, (size_t)n, client->user_data);
    }

    return (int)n;
}

/* ── Close ───────────────────────────────────────────────────────────────────── */

void aether_stream_client_close(AetherStreamClient *client) {
    if (!client) return;
    if (client->fd != INVALID_SOCK) {
        close_sock(client->fd);
        client->fd = INVALID_SOCK;
    }
    client->connected    = false;
    client->headers_done = false;
    printf("[AetherStream] Closed\n");
}
