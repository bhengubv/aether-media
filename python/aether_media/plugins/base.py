"""Abstract base class for Aether Media plugins."""

from __future__ import annotations

from abc import ABC, abstractmethod
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from aether_media.models import MediaContent, MediaReaction, LiveStream


class AetherMediaPlugin(ABC):
    """
    Base class for all Aether Media plugins.

    Subclass this and implement the required hooks, then register the
    plugin with a PluginHost instance.
    """

    @property
    @abstractmethod
    def name(self) -> str:
        """Unique plugin identifier (e.g. "my-plugin")."""
        ...

    @property
    @abstractmethod
    def version(self) -> str:
        """Semantic version string (e.g. "1.2.3")."""
        ...

    @abstractmethod
    def on_content_loaded(self, content: "MediaContent") -> None:
        """Called when a new piece of media content is loaded by the player."""
        ...

    @abstractmethod
    def on_reaction_received(self, reaction: "MediaReaction") -> None:
        """Called when a reaction arrives for the currently playing content."""
        ...

    def on_stream_started(self, stream: "LiveStream") -> None:
        """
        Optional hook — called when a live stream begins.

        The default implementation does nothing.  Override to add behaviour.
        """
        # Intentionally empty: this is a valid no-op default for optional hooks.
        return
