"""Tests for aethermedia.plugins.base and aethermedia.plugins.host."""

from __future__ import annotations

import logging
import time
from unittest.mock import MagicMock, patch, call

import pytest

from aethermedia.plugins.base import AetherNetMediaPlugin
from aethermedia.plugins.host import PluginHost
from aethermedia.models import (
    MediaContent,
    MediaReaction,
    MediaReactionType,
    LiveStream,
)


# ── Helpers ───────────────────────────────────────────────────────────────────

def _now_ms() -> int:
    return int(time.time() * 1000)


def _content(**kwargs) -> MediaContent:
    defaults = dict(
        content_hash="abc123",
        title="Test Video",
        duration_ms=180_000,
        codec="h264",
        content_type="video/mp4",
        creator_uhid="uhid-1",
        size_bytes=1_000_000,
        created_at_ms=_now_ms(),
    )
    defaults.update(kwargs)
    return MediaContent(**defaults)


def _reaction(**kwargs) -> MediaReaction:
    defaults = dict(
        reaction_id="r1",
        content_hash="abc123",
        from_uhid="uhid-2",
        type=MediaReactionType.LIKE,
        position_ms=0,
        message=None,
        sent_at_ms=_now_ms(),
    )
    defaults.update(kwargs)
    return MediaReaction(**defaults)


def _stream(**kwargs) -> LiveStream:
    defaults = dict(
        stream_id="s1",
        title="Live Now",
        creator_uhid="uhid-1",
        codec="h264",
        segment_duration_ms=2000,
        started_at_ms=_now_ms(),
        viewer_count=100,
        is_active=True,
    )
    defaults.update(kwargs)
    return LiveStream(**defaults)


# ── Concrete plugin for testing AetherNetMediaPlugin ABC ─────────────────────────

class _GoodPlugin(AetherNetMediaPlugin):
    def __init__(self):
        self.loaded = []
        self.reactions = []
        self.streams = []

    @property
    def name(self) -> str:
        return "good-plugin"

    @property
    def version(self) -> str:
        return "1.0.0"

    def on_content_loaded(self, content: MediaContent) -> None:
        self.loaded.append(content)

    def on_reaction_received(self, reaction: MediaReaction) -> None:
        self.reactions.append(reaction)


class _StreamPlugin(_GoodPlugin):
    """Plugin that also overrides on_stream_started."""

    @property
    def name(self) -> str:
        return "stream-plugin"

    def on_stream_started(self, stream: LiveStream) -> None:
        self.streams.append(stream)


class _BrokenPlugin(AetherNetMediaPlugin):
    """Plugin that raises exceptions in every hook."""

    @property
    def name(self) -> str:
        return "broken-plugin"

    @property
    def version(self) -> str:
        return "0.0.1"

    def on_content_loaded(self, content: MediaContent) -> None:
        raise RuntimeError("on_content_loaded boom")

    def on_reaction_received(self, reaction: MediaReaction) -> None:
        raise RuntimeError("on_reaction_received boom")

    def on_stream_started(self, stream: LiveStream) -> None:
        raise RuntimeError("on_stream_started boom")


# ── AetherNetMediaPlugin — abstract checks ──────────────────────────────────────

def test_plugin_cannot_be_instantiated_directly():
    with pytest.raises(TypeError):
        AetherNetMediaPlugin()  # type: ignore[abstract]


def test_plugin_missing_abstract_methods_raises():
    class _Incomplete(AetherNetMediaPlugin):
        @property
        def name(self) -> str:
            return "x"
        @property
        def version(self) -> str:
            return "1"
        # Missing on_content_loaded and on_reaction_received

    with pytest.raises(TypeError):
        _Incomplete()


def test_plugin_on_stream_started_default_is_noop():
    """Default on_stream_started does nothing and returns None."""
    p = _GoodPlugin()
    stream = _stream()
    result = p.on_stream_started(stream)
    assert result is None
    assert p.streams == []


# ── PluginHost — registration ─────────────────────────────────────────────────

def test_plugin_host_register_and_names():
    host = PluginHost()
    p = _GoodPlugin()
    host.register(p)
    assert "good-plugin" in host.registered_names


def test_plugin_host_register_replaces_existing():
    host = PluginHost()
    p1 = _GoodPlugin()
    p2 = _GoodPlugin()
    host.register(p1)
    host.register(p2)  # same name → replace
    assert len(host.registered_names) == 1


def test_plugin_host_register_multiple_plugins():
    host = PluginHost()
    host.register(_GoodPlugin())
    host.register(_StreamPlugin())
    assert set(host.registered_names) == {"good-plugin", "stream-plugin"}


def test_plugin_host_register_non_plugin_raises():
    host = PluginHost()
    with pytest.raises(TypeError, match="Expected AetherNetMediaPlugin"):
        host.register("not a plugin")  # type: ignore[arg-type]


def test_plugin_host_register_non_plugin_object_raises():
    host = PluginHost()
    with pytest.raises(TypeError):
        host.register(42)  # type: ignore[arg-type]


def test_plugin_host_unregister_existing():
    host = PluginHost()
    p = _GoodPlugin()
    host.register(p)
    result = host.unregister("good-plugin")
    assert result is True
    assert "good-plugin" not in host.registered_names


def test_plugin_host_unregister_nonexistent():
    host = PluginHost()
    result = host.unregister("no-such-plugin")
    assert result is False


def test_plugin_host_registered_names_empty_initially():
    host = PluginHost()
    assert host.registered_names == []


def test_plugin_host_registered_names_returns_copy():
    """Mutating the returned list does not affect the host."""
    host = PluginHost()
    host.register(_GoodPlugin())
    names = host.registered_names
    names.clear()
    assert len(host.registered_names) == 1


# ── PluginHost — event dispatch ───────────────────────────────────────────────

def test_notify_content_loaded_dispatches_to_plugin():
    host = PluginHost()
    p = _GoodPlugin()
    host.register(p)
    content = _content()
    host.notify_content_loaded(content)
    assert p.loaded == [content]


def test_notify_content_loaded_dispatches_to_all_plugins():
    host = PluginHost()
    p1 = _GoodPlugin()
    p2 = _StreamPlugin()
    host.register(p1)
    host.register(p2)
    content = _content()
    host.notify_content_loaded(content)
    assert p1.loaded == [content]
    assert p2.loaded == [content]


def test_notify_reaction_received_dispatches():
    host = PluginHost()
    p = _GoodPlugin()
    host.register(p)
    r = _reaction()
    host.notify_reaction_received(r)
    assert p.reactions == [r]


def test_notify_stream_started_dispatches():
    host = PluginHost()
    p = _StreamPlugin()
    host.register(p)
    stream = _stream()
    host.notify_stream_started(stream)
    assert p.streams == [stream]


def test_notify_stream_started_default_noop_does_not_crash():
    """A plugin with the default on_stream_started must not cause host errors."""
    host = PluginHost()
    p = _GoodPlugin()  # uses default no-op
    host.register(p)
    host.notify_stream_started(_stream())
    # No exception → pass


def test_notify_no_plugins_is_a_noop():
    host = PluginHost()
    # Should not raise even with zero plugins
    host.notify_content_loaded(_content())
    host.notify_reaction_received(_reaction())
    host.notify_stream_started(_stream())


# ── PluginHost — fault isolation ──────────────────────────────────────────────

def test_broken_plugin_content_loaded_is_isolated(caplog):
    host = PluginHost()
    broken = _BrokenPlugin()
    good = _GoodPlugin()
    host.register(broken)
    host.register(good)
    content = _content()

    with caplog.at_level(logging.ERROR, logger="aethermedia.plugins.host"):
        host.notify_content_loaded(content)

    # Good plugin must still have received the event
    assert good.loaded == [content]
    # Error was logged for broken plugin
    assert any("broken-plugin" in r.message for r in caplog.records)


def test_broken_plugin_reaction_received_is_isolated(caplog):
    host = PluginHost()
    broken = _BrokenPlugin()
    good = _GoodPlugin()
    host.register(broken)
    host.register(good)
    r = _reaction()

    with caplog.at_level(logging.ERROR, logger="aethermedia.plugins.host"):
        host.notify_reaction_received(r)

    assert good.reactions == [r]
    assert any("broken-plugin" in r.message for r in caplog.records)


def test_broken_plugin_stream_started_is_isolated(caplog):
    host = PluginHost()
    broken = _BrokenPlugin()
    stream_p = _StreamPlugin()
    host.register(broken)
    host.register(stream_p)
    s = _stream()

    with caplog.at_level(logging.ERROR, logger="aethermedia.plugins.host"):
        host.notify_stream_started(s)

    assert stream_p.streams == [s]
    assert any("broken-plugin" in r.message for r in caplog.records)


def test_multiple_broken_plugins_all_isolated(caplog):
    host = PluginHost()

    class _Broken2(_BrokenPlugin):
        @property
        def name(self) -> str:
            return "broken-plugin-2"

    host.register(_BrokenPlugin())
    host.register(_Broken2())
    good = _GoodPlugin()
    host.register(good)

    with caplog.at_level(logging.ERROR, logger="aethermedia.plugins.host"):
        host.notify_content_loaded(_content())

    assert len(good.loaded) == 1


# ── PluginHost — logging on register/unregister ───────────────────────────────

def test_register_logs_debug(caplog):
    host = PluginHost()
    with caplog.at_level(logging.DEBUG, logger="aethermedia.plugins.host"):
        host.register(_GoodPlugin())
    assert any("good-plugin" in r.message for r in caplog.records)


def test_unregister_logs_debug(caplog):
    host = PluginHost()
    host.register(_GoodPlugin())
    with caplog.at_level(logging.DEBUG, logger="aethermedia.plugins.host"):
        host.unregister("good-plugin")
    assert any("good-plugin" in r.message for r in caplog.records)
