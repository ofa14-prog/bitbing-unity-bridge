"""
Unity tools for MCP - wrapper functions for Unity commands.
"""
import logging
from typing import Any, Dict, Optional

import httpx

logger = logging.getLogger(__name__)


class UnityTools:
    """Wrapper for Unity MCP tools."""

    def __init__(self, port: int = 8080):
        self._port = port

    async def create_gameobject(
        self,
        name: str,
        parent_path: Optional[str] = None,
        position: Optional[Dict[str, float]] = None,
        components: Optional[list] = None
    ) -> Dict[str, Any]:
        """Create a GameObject in Unity."""
        return await self._call_tool("create_gameobject", {
            "name": name,
            "parentPath": parent_path,
            "position": position,
            "components": components or []
        })

    async def delete_gameobject(self, path: str) -> Dict[str, Any]:
        """Delete a GameObject from Unity."""
        return await self._call_tool("delete_gameobject", {"path": path})

    async def add_component(
        self,
        game_object_path: str,
        component_type: str
    ) -> Dict[str, Any]:
        """Add a component to a GameObject."""
        return await self._call_tool("add_component", {
            "gameObjectPath": game_object_path,
            "componentType": component_type
        })

    async def write_script(
        self,
        path: str,
        content: str,
        refresh_assets: bool = True,
        wait_for_compile: bool = False
    ) -> Dict[str, Any]:
        """Write a C# script to Assets folder."""
        return await self._call_tool("write_script", {
            "path": path,
            "content": content,
            "refreshAssets": refresh_assets,
            "waitForCompile": wait_for_compile
        })

    async def create_scene(self, path: str) -> Dict[str, Any]:
        """Create a new Unity scene."""
        return await self._call_tool("create_scene", {"path": path})

    async def open_scene(self, path: str) -> Dict[str, Any]:
        """Open an existing Unity scene."""
        return await self._call_tool("open_scene", {"path": path})

    async def enter_play_mode(
        self,
        wait_for_load: bool = True,
        timeout_seconds: float = 30.0
    ) -> Dict[str, Any]:
        """Enter Unity Play Mode."""
        return await self._call_tool("enter_play_mode", {
            "waitForLoad": wait_for_load,
            "timeoutSeconds": timeout_seconds
        })

    async def exit_play_mode(self) -> Dict[str, Any]:
        """Exit Unity Play Mode."""
        return await self._call_tool("exit_play_mode", {})

    async def take_screenshot(
        self,
        output_path: str,
        width: int = 1920,
        height: int = 1080,
        include_ui: bool = True
    ) -> Dict[str, Any]:
        """Take a screenshot of the Game view."""
        return await self._call_tool("take_screenshot", {
            "outputPath": output_path,
            "width": width,
            "height": height,
            "includeUI": include_ui
        })

    async def run_tests(
        self,
        test_suite: str = "EditMode",
        category: Optional[str] = None
    ) -> Dict[str, Any]:
        """Run Unity Test Runner tests."""
        return await self._call_tool("run_tests", {
            "testSuite": test_suite,
            "category": category
        })

    async def _call_tool(self, tool_name: str, arguments: Dict[str, Any]) -> Dict[str, Any]:
        """Call a tool on the Unity side."""
        try:
            async with httpx.AsyncClient(timeout=60.0) as client:
                response = await client.post(
                    f"http://localhost:{self._port}/mcp",
                    json={
                        "jsonrpc": "2.0",
                        "id": "1",
                        "method": "tools/call",
                        "params": {
                            "name": tool_name,
                            "arguments": arguments
                        }
                    }
                )
                response.raise_for_status()
                return response.json()
        except Exception as e:
            logger.error(f"Failed to call Unity tool {tool_name}: {e}")
            return {"success": False, "error": str(e)}
