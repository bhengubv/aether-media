// SPDX-License-Identifier: MIT
package main

import (
	"context"
	"fmt"
	"io"
	"net/http"
	"os"
	"os/signal"
	"syscall"

	"aether.media/extensions/forge/forge"
)

func main() {
	svc := &forge.ForgeService{}
	proxy := newProxyHandler(svc)

	server := &http.Server{
		Addr:    ":2301",
		Handler: proxy,
	}

	fmt.Println("Aether Forge proxy listening on :2301")

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	go func() {
		if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			fmt.Fprintf(os.Stderr, "forge proxy: %v\n", err)
			stop()
		}
	}()

	<-ctx.Done()
	fmt.Println("Aether Forge proxy shutting down…")
	_ = server.Shutdown(context.Background())
}

// proxyHandler dispatches HTTP requests through the Forge cache.
type proxyHandler struct {
	svc    forge.IForgeService
	client *http.Client
}

func newProxyHandler(svc forge.IForgeService) *proxyHandler {
	return &proxyHandler{svc: svc, client: &http.Client{}}
}

func (h *proxyHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	target := r.URL.String()
	if target == "" {
		http.Error(w, "missing target URL", http.StatusBadRequest)
		return
	}

	// Check Forge cache first.
	entry, err := h.svc.Query(r.Context(), target)
	if err == nil && entry != nil {
		stream, fetchErr := h.svc.Fetch(r.Context(), entry.ContentHash)
		if fetchErr == nil && stream != nil {
			defer stream.Close()
			w.Header().Set("X-Aether-Forge-Cache", "HIT")
			w.Header().Set("Content-Type", "application/octet-stream")
			io.Copy(w, stream) //nolint:errcheck
			return
		}
	}

	// Cache miss — forward upstream.
	upstream, err := http.NewRequestWithContext(r.Context(), r.Method, target, r.Body)
	if err != nil {
		http.Error(w, "bad gateway: "+err.Error(), http.StatusBadGateway)
		return
	}

	resp, err := h.client.Do(upstream)
	if err != nil {
		http.Error(w, "bad gateway: "+err.Error(), http.StatusBadGateway)
		return
	}
	defer resp.Body.Close()

	w.Header().Set("X-Aether-Forge-Cache", "MISS")
	w.WriteHeader(resp.StatusCode)
	io.Copy(w, resp.Body) //nolint:errcheck
}
