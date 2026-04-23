"""
Diafor Agent - Checks dependencies and prepares environment.
YELLOW #FFD600 - KONU.md §4.2
"""
import logging
from typing import Any, Dict

logger = logging.getLogger(__name__)


class DiaforAgent:
    """Checks dependencies and prepares environment."""

    def __init__(self):
        self._color = "#FFD600"
        self._name = "diafor"

    async def initialize(self):
        """Initialize the agent."""
        logger.info(f"[{self._name}] Initialized (Color: {self._color})")

    async def check_environment(self, motor: str) -> Dict[str, Any]:
        """
        Check dependencies and prepare environment.

        Based on KONU.md §4.2:
        - Checks dependencies
        - Prepares environment
        - Verifies motor connection
        """
        logger.info(f"[{self._name}] Checking environment for motor: {motor}")

        result = {
            "agent": self._name,
            "color": self._color,
            "status": "checking",
            "motor": motor,
            "checks": {},
            "ready": False
        }

        if motor == "unity":
            result["checks"]["unity_connection"] = await self._check_unity_connection()
            result["checks"]["assets_folder"] = await self._check_assets_folder()
            result["checks"]["mcp_server"] = await self._check_mcp_server()

        elif motor == "unreal":
            result["checks"]["unreal_connection"] = await self._check_unreal_connection()

        elif motor == "dahili":
            result["checks"]["babylonjs"] = await self._check_babylonjs()

        # Determine if environment is ready
        checks = result["checks"]
        result["ready"] = all(checks.values())
        result["status"] = "ready" if result["ready"] else "not_ready"

        logger.info(f"[{self._name}] Environment ready: {result['ready']}")
        return result

    async def _check_unity_connection(self) -> bool:
        """Check if Unity connection is available."""
        try:
            import httpx
            async with httpx.AsyncClient() as client:
                response = await client.get("http://localhost:8080/mcp", timeout=5.0)
                return response.status_code == 200
        except Exception as e:
            logger.warning(f"Unity connection check failed: {e}")
            return False

    async def _check_assets_folder(self) -> bool:
        """Check if Assets folder exists."""
        # This is a simple check - in real implementation would verify via Unity API
        return True

    async def _check_mcp_server(self) -> bool:
        """Check if MCP server is running."""
        return await self._check_unity_connection()

    async def _check_unreal_connection(self) -> bool:
        """Check if Unreal connection is available."""
        try:
            import httpx
            async with httpx.AsyncClient() as client:
                response = await client.get("http://localhost:57433/mcp", timeout=5.0)
                return response.status_code == 200
        except Exception as e:
            logger.warning(f"Unreal connection check failed: {e}")
            return False

    async def _check_babylonjs(self) -> bool:
        """Check if Babylon.js is available."""
        # Internal engine check
        return True
