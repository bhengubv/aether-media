"""Plugin host — manages registered plugins and dispatches events."""

from __future__ import annotations

import logging
from typing import TYPE_CHECKING

from aethermedia.plugins.base import AetherNetMediaPlugin

if TYPE_CHECKING:
    from aethermedia.models import MediaContent, MediaReaction, LiveStream

logger = logging.getLogger(__name__)


class PluginHost:
    """
    Maintains a registry of AetherNetMediaPlugin instances and broadcasts
    lifecycle events to all of them.

    Exceptions raised by individual plugins are caught and logged so that
    one misbehaving plugin cannot disrupt the others.
    """

    def __init__(self) -> None:
        self._plugins: dict[str, AetherNetMediaPlugin] = {}

    # ── Registration ──────────────────────────────────────────────────────

    def register(self, plugin: AetherNetMediaPlugin) -> None:
        """Add a plugin to the registry.  Replaces any existing plugin with the same name."""
        if not isinstance(plugin, AetherNetMediaPlugin):
            raise TypeError(f"Expected AetherNetMediaPlugin, got {type(plugin).__name__}")
        self._plugins[plugin.name] = plugin
        logger.debug("Registered plugin: %s v%s", plugin.name, plugin.version)

    def unregister(self, plugin_name: str) -> bool:
        """
        Remove a plugin by name.

        Returns True if the plugin was found and removed, False if it was
        not registered.
        """
        removed = self._plugins.pop(plugin_name, None)
        if removed is not None:
            logger.debug("Unregistered plugin: %s", plugin_name)
            return True
        return False

    @property
    def registered_names(self) -> list[str]:
        """Names of all currently registered plugins."""
        return list(self._plugins.keys())

    # ── Event dispatch ─────────────────────────────────────────────────────

    def notify_content_loaded(self, content: "MediaContent") -> None:
        """Broadcast on_content_loaded to all registered plugins."""
        for plugin in self._plugins.values():
            try:
                plugin.on_content_loaded(content)
            except Exception:
                logger.exception(
                    "Plugin '%s' raised an exception in on_content_loaded", plugin.name
                )

    def notify_reaction_received(self, reaction: "MediaReaction") -> None:
        """Broadcast on_reaction_received to all registered plugins."""
        for plugin in self._plugins.values():
            try:
                plugin.on_reaction_received(reaction)
            except Exception:
                logger.exception(
                    "Plugin '%s' raised an exception in on_reaction_received", plugin.name
                )

    def notify_stream_started(self, stream: "LiveStream") -> None:
        """Broadcast on_stream_started to all registered plugins."""
        for plugin in self._plugins.values():
            try:
                plugin.on_stream_started(stream)
            except Exception:
                logger.exception(
                    "Plugin '%s' raised an exception in on_stream_started", plugin.name
                )
