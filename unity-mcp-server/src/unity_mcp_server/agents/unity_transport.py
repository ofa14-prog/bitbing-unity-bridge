"""
Unity transport — single async HTTP client for talking to the Unity bridge.

Adapted from COPLAY's transport/unity_transport.py: centralizes URL building,
timeouts, retries, and JSON-RPC framing so agent code (ahbab, obsidere, …)
doesn't repeat httpx boilerplate.
"""
from __future__ import annotations

import logging
import uuid
from typing import Any, Dict, Optional

import httpx

from .. import port_registry

logger = logging.getLogger(__name__)


class UnityTransport:
    """JSON-RPC over HTTP client for the Unity McpListener."""

    def __init__(self, timeout: float = 30.0):
        self._timeout = timeout
        self._client: Optional[httpx.AsyncClient] = None

    async def _client_get(self) -> httpx.AsyncClient:
        if self._client is None:
            self._client = httpx.AsyncClient(timeout=self._timeout)
        return self._client

    @property
    def url(self) -> str:
        return f"http://127.0.0.1:{port_registry.get_unity_port()}/mcp"

    async def call_tool(self, name: str, arguments: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """Invoke an MCP tool on the Unity bridge and return its `result` payload."""
        payload = {
            "jsonrpc": "2.0",
            "id": str(uuid.uuid4()),
            "method": "tools/call",
            "params": {"name": name, "arguments": arguments or {}},
        }
        client = await self._client_get()
        try:
            r = await client.post(self.url, json=payload)
            r.raise_for_status()
            body = r.json()
            if "error" in body:
                return {"success": False, "error": body["error"]}
            return body.get("result", {}) or {"success": True}
        except httpx.RequestError as exc:
            logger.warning("Unity transport request error: %s", exc)
            return {"success": False, "error": f"transport: {exc}"}
        except Exception as exc:
            logger.exception("Unity transport unexpected error")
            return {"success": False, "error": str(exc)}

    async def list_tools(self) -> Dict[str, Any]:
        payload = {"jsonrpc": "2.0", "id": str(uuid.uuid4()), "method": "tools/list", "params": {}}
        client = await self._client_get()
        try:
            r = await client.post(self.url, json=payload)
            r.raise_for_status()
            return r.json().get("result", {}) or {}
        except Exception as exc:
            return {"success": False, "error": str(exc)}

    async def close(self) -> None:
        if self._client is not None:
            await self._client.aclose()
            self._client = None


_singleton: Optional[UnityTransport] = None


def get_transport() -> UnityTransport:
    global _singleton
    if _singleton is None:
        _singleton = UnityTransport()
    return _singleton
