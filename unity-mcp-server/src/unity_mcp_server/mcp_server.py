"""
MCP Server implementation for Unity bridge.
Handles MCP protocol communication with AI clients (Claude, Cursor, Copilot).
"""
import asyncio
import json
import logging
from typing import Any, Dict, List, Optional

from mcp.server import Server
from mcp.server.stdio import stdio_server
from mcp.types import Tool, TextContent

logger = logging.getLogger(__name__)


class UnityMcpServer:
    """MCP Server that bridges AI assistants to Unity Editor."""

    def __init__(self, port: int = 8080):
        self._port = port
        self._server = Server("unity-bridge")
        self._orchestrator = None
        self._setup_handlers()

    def set_orchestrator(self, orchestrator):
        """Set the agent orchestrator for processing requests."""
        self._orchestrator = orchestrator

    def _setup_handlers(self):
        """Set up MCP request handlers."""
        server = self._server

        @server.list_tools()
        async def list_tools() -> List[Tool]:
            """List available MCP tools."""
            return [
                Tool(
                    name="create_gameobject",
                    description="Creates a new GameObject in the Unity scene",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "name": {"type": "string", "description": "GameObject name"},
                            "parentPath": {"type": "string", "description": "Parent path in hierarchy"},
                            "position": {
                                "type": "object",
                                "description": "World position {x, y, z}",
                                "properties": {
                                    "x": {"type": "number"},
                                    "y": {"type": "number"},
                                    "z": {"type": "number"},
                                }
                            },
                            "components": {
                                "type": "array",
                                "description": "Component type names to add",
                                "items": {"type": "string"}
                            }
                        },
                        "required": ["name"]
                    }
                ),
                Tool(
                    name="write_script",
                    description="Writes a C# script file to Assets folder",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "path": {"type": "string", "description": "Script path (must be in Assets/)"},
                            "content": {"type": "string", "description": "Script content"},
                            "refreshAssets": {"type": "boolean", "description": "Refresh asset database", "default": True},
                            "waitForCompile": {"type": "boolean", "description": "Wait for compilation", "default": False}
                        },
                        "required": ["path", "content"]
                    }
                ),
                Tool(
                    name="add_component",
                    description="Adds a component to a GameObject",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "gameObjectPath": {"type": "string", "description": "GameObject path"},
                            "componentType": {"type": "string", "description": "Component type name"}
                        },
                        "required": ["gameObjectPath", "componentType"]
                    }
                ),
                Tool(
                    name="delete_gameobject",
                    description="Deletes a GameObject from the Unity scene",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "path": {"type": "string", "description": "GameObject path in hierarchy"}
                        },
                        "required": ["path"]
                    }
                ),
                Tool(
                    name="enter_play_mode",
                    description="Enters Unity Play Mode",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "waitForLoad": {"type": "boolean", "default": True},
                            "timeoutSeconds": {"type": "number", "default": 30}
                        }
                    }
                ),
                Tool(
                    name="exit_play_mode",
                    description="Exits Unity Play Mode",
                    inputSchema={"type": "object", "properties": {}}
                ),
                Tool(
                    name="take_screenshot",
                    description="Takes a screenshot of the Game view",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "outputPath": {"type": "string", "description": "Output file path"},
                            "width": {"type": "number", "default": 1920},
                            "height": {"type": "number", "default": 1080},
                            "includeUI": {"type": "boolean", "default": True}
                        },
                        "required": ["outputPath"]
                    }
                ),
                Tool(
                    name="run_tests",
                    description="Runs Unity Test Runner tests",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "testSuite": {"type": "string", "enum": ["EditMode", "PlayMode"], "default": "EditMode"},
                            "category": {"type": "string"}
                        }
                    }
                ),
                Tool(
                    name="create_scene",
                    description="Creates a new Unity scene",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "path": {"type": "string", "description": "Scene path"}
                        },
                        "required": ["path"]
                    }
                ),
                Tool(
                    name="open_scene",
                    description="Opens an existing Unity scene",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "path": {"type": "string", "description": "Scene path"}
                        },
                        "required": ["path"]
                    }
                ),
                Tool(
                    name="analyze_input",
                    description="Analyzes user input and creates a task plan (vates agent)",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "input": {"type": "string", "description": "User's natural language input"},
                            "gameType": {"type": "string", "description": "Type of game to create"}
                        },
                        "required": ["input"]
                    }
                ),
                Tool(
                    name="check_environment",
                    description="Checks dependencies and prepares environment (diafor agent)",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "motor": {"type": "string", "enum": ["unity", "unreal", "dahili"]}
                        }
                    }
                ),
                Tool(
                    name="orchestrate_pipeline",
                    description="Runs the full 6-agent pipeline (vates→Diafor→ahbab→obsidere→patientia→magnumpus)",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "input": {"type": "string", "description": "User input"},
                            "gameType": {"type": "string"}
                        },
                        "required": ["input"]
                    }
                ),
            ]

        @server.call_tool()
        async def call_tool(name: str, arguments: Dict[str, Any]) -> List[TextContent]:
            """Handle tool call requests."""
            logger.info(f"Tool call: {name} with args: {arguments}")

            # If orchestrator is set, delegate to it for agent-based processing
            if self._orchestrator and name in ["analyze_input", "check_environment", "orchestrate_pipeline"]:
                return await self._orchestrator.handle_tool(name, arguments)

            # Otherwise, forward directly to Unity via HTTP
            result = await self._call_unity_tool(name, arguments)

            return [TextContent(type="text", text=json.dumps(result, indent=2))]

    async def _call_unity_tool(self, tool_name: str, arguments: Dict[str, Any]) -> Dict[str, Any]:
        """Call a tool on the Unity side via HTTP."""
        try:
            import httpx

            async with httpx.AsyncClient(timeout=30.0) as client:
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
            return {
                "success": False,
                "error": str(e)
            }

    async def run(self):
        """Run the MCP server."""
        logger.info(f"Starting MCP server on port {self._port}")

        async with stdio_server() as (read_stream, write_stream):
            await self._server.run(
                read_stream,
                write_stream,
                self._server.create_initialization_options()
            )

    async def stop(self):
        """Stop the MCP server."""
        logger.info("Stopping MCP server")
        # Cleanup if needed
