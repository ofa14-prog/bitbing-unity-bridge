"""
Port registry — Python side mirror of the Unity PortManager.

Reads/writes %TEMP%/bitbing/ports.json so the chat server (8001) and Unity
bridge (8080) can discover each other's actual ports even after collision
fallback. Adapted from COPLAY's port discovery pattern.
"""
from __future__ import annotations

import json
import os
import socket
import tempfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

DEFAULT_UNITY_PORT = 8080
DEFAULT_CHAT_PORT = 8001
_REGISTRY_DIR = Path(tempfile.gettempdir()) / "bitbing"
_REGISTRY_FILE = _REGISTRY_DIR / "ports.json"


def _read() -> dict:
    try:
        if _REGISTRY_FILE.exists():
            return json.loads(_REGISTRY_FILE.read_text(encoding="utf-8"))
    except Exception:
        pass
    return {}


def _write(data: dict) -> None:
    try:
        _REGISTRY_DIR.mkdir(parents=True, exist_ok=True)
        data["updated_at"] = datetime.now(timezone.utc).isoformat()
        _REGISTRY_FILE.write_text(json.dumps(data, indent=2), encoding="utf-8")
    except Exception:
        pass


def get_unity_port() -> int:
    """Return the Unity bridge port from the registry, or the default."""
    data = _read()
    p = int(data.get("unity_port") or 0)
    return p if p > 0 else DEFAULT_UNITY_PORT


def get_chat_port() -> int:
    data = _read()
    p = int(data.get("chat_port") or 0)
    return p if p > 0 else DEFAULT_CHAT_PORT


def save_chat_port(port: int) -> None:
    data = _read()
    data["chat_port"] = int(port)
    _write(data)


def is_port_available(port: int) -> bool:
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        s.bind(("127.0.0.1", port))
        return True
    except OSError:
        return False
    finally:
        s.close()


def find_available_chat_port(start: int = DEFAULT_CHAT_PORT, attempts: int = 50) -> int:
    for i in range(attempts):
        p = start + i
        if is_port_available(p):
            return p
    raise RuntimeError(f"No free port in {start}..{start + attempts}")
