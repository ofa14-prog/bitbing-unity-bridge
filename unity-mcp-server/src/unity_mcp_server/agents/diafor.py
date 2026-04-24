"""
Diafor Agent - Checks dependencies and prepares environment.
YELLOW #FFD600 - KONU.md §4.2
"""
import logging
from typing import Any, Dict

logger = logging.getLogger(__name__)

_UNITY_URL = "http://127.0.0.1:8080/mcp"
_UNREAL_URL = "http://127.0.0.1:57433/mcp"

# MCP initialize probe — McpListener expects POST JSON-RPC
_INIT_PAYLOAD = {
    "jsonrpc": "2.0",
    "id": "diafor-probe",
    "method": "initialize",
    "params": {},
}


class DiaforAgent:
    """Checks dependencies and prepares environment."""

    def __init__(self):
        self._color = "#FFD600"
        self._name = "diafor"

    async def initialize(self):
        logger.info(f"[{self._name}] Initialized (Color: {self._color})")

    async def check_environment(self, motor: str) -> Dict[str, Any]:
        """
        Verify the target engine bridge is reachable.
        Based on KONU.md §4.2.
        """
        logger.info(f"[{self._name}] Checking environment for motor: {motor}")

        checks: Dict[str, bool] = {}

        if motor == "unity":
            checks["unity_bridge"] = await self._ping(_UNITY_URL)
        elif motor == "unreal":
            checks["unreal_bridge"] = await self._ping(_UNREAL_URL)
        elif motor == "dahili":
            checks["dahili"] = True  # internal engine always ready

        ready = all(checks.values()) if checks else False

        result = {
            "agent": self._name,
            "color": self._color,
            "status": "ready" if ready else "not_ready",
            "motor": motor,
            "checks": checks,
            "ready": ready,
        }
        logger.info(f"[{self._name}] Environment ready: {ready} — {checks}")
        return result

    async def _ping(self, url: str) -> bool:
        """
        Send a JSON-RPC initialize probe.
        McpListener responds with 200 + result when it is up.
        """
        try:
            import httpx
            async with httpx.AsyncClient(timeout=5.0) as client:
                r = await client.post(url, json=_INIT_PAYLOAD)
                body = r.json()
                # Healthy response has "result" key (not "error")
                return r.status_code == 200 and "result" in body
        except Exception as e:
            logger.warning(f"[{self._name}] Probe failed for {url}: {e}")
            return False
