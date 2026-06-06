"""PipProxy — intercepts pip install requests by acting as a PyPI-compatible index mirror.

When a package is requested, it checks ForgeService first (mesh-local cache).
On a cache miss it falls back to the upstream PyPI index and caches the result.

Usage (as a local index server):
    proxy = PipProxy(forge_service=ForgeServiceImpl(), upstream="https://pypi.org/simple")
    await proxy.start(host="127.0.0.1", port=8765)

    # Point pip at it:
    # pip install requests --index-url http://127.0.0.1:8765/simple
"""
from __future__ import annotations

import logging
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from .service import ForgeService

logger = logging.getLogger(__name__)

_ECOSYSTEM = "pypi"


class PipProxy:
    """PyPI index mirror backed by ForgeService."""

    def __init__(
        self,
        forge_service: "ForgeService",
        upstream: str = "https://pypi.org/simple",
    ) -> None:
        self._forge = forge_service
        self._upstream = upstream.rstrip("/")

    async def get_package_index(self, package_name: str) -> str:
        """Return a minimal PyPI Simple API HTML page for *package_name*.

        Checks ForgeService first; falls through to upstream on miss.
        """
        normalized = self._normalize(package_name)
        entry = await self._forge.query(normalized, _ECOSYSTEM)

        if entry is not None:
            logger.debug("Forge cache hit for %s", normalized)
            return self._render_simple_page(package_name, [entry.download_url])

        logger.debug("Forge cache miss for %s — fetching from upstream", normalized)
        upstream_html = await self._fetch_upstream_index(normalized)
        return upstream_html

    async def download_package(self, package_name: str, version: str) -> bytes:
        """Return the raw .whl / .tar.gz bytes for *package_name* at *version*.

        Checks ForgeService first; caches on upstream fallback.
        """
        from .models import ForgeEntry
        import hashlib

        entry = await self._forge.query(package_name, _ECOSYSTEM, version)
        if entry is not None:
            logger.debug("Returning %s==%s from Forge cache", package_name, version)
            return await self._forge.fetch(package_name, _ECOSYSTEM, version)

        logger.debug("Downloading %s==%s from upstream", package_name, version)
        data = await self._download_from_upstream(package_name, version)

        checksum = hashlib.sha256(data).hexdigest()
        new_entry = ForgeEntry(
            package_id=package_name,
            ecosystem=_ECOSYSTEM,
            version=version,
            name=package_name,
            checksum=checksum,
            download_url=f"{self._upstream}/{package_name}/{version}",
            size_bytes=len(data),
        )
        await self._forge.cache(new_entry)
        return data

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------

    @staticmethod
    def _normalize(name: str) -> str:
        return name.lower().replace("_", "-").replace(".", "-")

    def _render_simple_page(self, package_name: str, urls: list[str]) -> str:
        links = "\n".join(f'<a href="{u}">{u.split("/")[-1]}</a>' for u in urls)
        return (
            "<!DOCTYPE html><html><head><title>Links for "
            f"{package_name}</title></head><body>\n"
            f"<h1>Links for {package_name}</h1>\n"
            f"{links}\n"
            "</body></html>"
        )

    async def _fetch_upstream_index(self, package_name: str) -> str:
        """Fetch the Simple API page from upstream (stub)."""
        raise NotImplementedError("HTTP client integration not implemented")

    async def _download_from_upstream(self, package_name: str, version: str) -> bytes:
        """Download a package from upstream PyPI (stub)."""
        raise NotImplementedError("HTTP client integration not implemented")
