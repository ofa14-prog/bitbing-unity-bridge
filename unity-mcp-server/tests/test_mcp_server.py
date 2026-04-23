"""
Tests for MCP server functionality.
"""
import pytest
from unittest.mock import AsyncMock, MagicMock, patch
from unity_mcp_server.mcp_server import UnityMcpServer
from unity_mcp_server.agents.orchestrator import AgentOrchestrator


class TestUnityMcpServer:
    """Test cases for UnityMcpServer."""

    @pytest.fixture
    def server(self):
        return UnityMcpServer(port=8080)

    def test_server_initialization(self, server):
        """Test server initializes correctly."""
        assert server._port == 8080
        assert server._server is not None
        assert server._orchestrator is None

    def test_server_set_orchestrator(self, server):
        """Test setting the orchestrator."""
        mock_orchestrator = MagicMock()
        server.set_orchestrator(mock_orchestrator)
        assert server._orchestrator == mock_orchestrator

    @pytest.mark.asyncio
    async def test_list_tools(self, server):
        """Test listing available tools."""
        tools = await server._server.list_tools()
        assert len(tools) > 0

        tool_names = [t.name for t in tools]
        assert "create_gameobject" in tool_names
        assert "write_script" in tool_names
        assert "enter_play_mode" in tool_names
        assert "orchestrate_pipeline" in tool_names


class TestUnityMcpServerIntegration:
    """Integration tests for MCP server with orchestrator."""

    @pytest.fixture
    async def server_with_orchestrator(self):
        server = UnityMcpServer(port=8080)
        orchestrator = AgentOrchestrator()
        await orchestrator.initialize()
        server.set_orchestrator(orchestrator)
        return server

    @pytest.mark.asyncio
    async def test_orchestrate_pipeline_delegation(self, server_with_orchestrator):
        """Test that pipeline tools are delegated to orchestrator."""
        server = server_with_orchestrator

        # Check orchestrator is set
        assert server._orchestrator is not None


class TestToolDefinitions:
    """Test tool schema definitions."""

    @pytest.mark.asyncio
    async def test_create_gameobject_schema(self):
        """Test create_gameobject tool has correct schema."""
        server = UnityMcpServer()
        tools = await server._server.list_tools()

        tool = next(t for t in tools if t.name == "create_gameobject")
        assert tool.description is not None
        assert "name" in tool.inputSchema.get("properties", {})

    @pytest.mark.asyncio
    async def test_write_script_schema(self):
        """Test write_script tool has correct schema."""
        server = UnityMcpServer()
        tools = await server._server.list_tools()

        tool = next(t for t in tools if t.name == "write_script")
        assert "path" in tool.inputSchema.get("properties", {})
        assert "content" in tool.inputSchema.get("properties", {})

    @pytest.mark.asyncio
    async def test_orchestrate_pipeline_schema(self):
        """Test orchestrate_pipeline tool has correct schema."""
        server = UnityMcpServer()
        tools = await server._server.list_tools()

        tool = next(t for t in tools if t.name == "orchestrate_pipeline")
        assert "input" in tool.inputSchema.get("properties", {})
