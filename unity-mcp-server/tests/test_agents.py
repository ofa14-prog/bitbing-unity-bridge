"""
Tests for 6-agent system.
"""
import pytest
from unittest.mock import AsyncMock, MagicMock, patch
from unity_mcp_server.agents.vates import VatesAgent
from unity_mcp_server.agents.diafor import DiaforAgent
from unity_mcp_server.agents.ahbab import AhbabAgent
from unity_mcp_server.agents.obsidere import ObsidereAgent
from unity_mcp_server.agents.patientia import PatientiaAgent
from unity_mcp_server.agents.magnumpus import MagnumpusAgent
from unity_mcp_server.agents.orchestrator import AgentOrchestrator


class TestVatesAgent:
    """Test cases for VatesAgent."""

    @pytest.fixture
    def agent(self):
        return VatesAgent()

    @pytest.mark.asyncio
    async def test_initialize(self, agent):
        """Test agent initializes with correct color."""
        await agent.initialize()
        assert agent._color == "#FF3B3B"
        assert agent._name == "vates"

    @pytest.mark.asyncio
    async def test_analyze_creates_tasks(self, agent):
        """Test analyze creates task DAG."""
        result = await agent.analyze("Create a 2D platformer with enemies", "2D Platformer")

        assert result["agent"] == "vates"
        assert result["color"] == "#FF3B3B"
        assert "task_dag" in result
        assert len(result["task_dag"]) > 0

    @pytest.mark.asyncio
    async def test_task_priorities_sorted(self, agent):
        """Test tasks are sorted by priority."""
        result = await agent.analyze("Create any game", "2D Platformer")
        tasks = result["task_dag"]

        priorities = [t["priority"] for t in tasks]
        assert priorities == sorted(priorities)


class TestDiaforAgent:
    """Test cases for DiaforAgent."""

    @pytest.fixture
    def agent(self):
        return DiaforAgent()

    @pytest.mark.asyncio
    async def test_initialize(self, agent):
        """Test agent initializes with correct color."""
        await agent.initialize()
        assert agent._color == "#FFD600"
        assert agent._name == "diafor"

    @pytest.mark.asyncio
    async def test_check_environment_unity(self, agent):
        """Test environment check for Unity."""
        with patch("httpx.AsyncClient") as mock_client:
            mock_response = MagicMock()
            mock_response.status_code = 200
            mock_client.return_value.__aenter__.return_value.get = AsyncMock(
                return_value=mock_response
            )

            result = await agent.check_environment("unity")

            assert result["agent"] == "diafor"
            assert "checks" in result
            assert "ready" in result


class TestAhbabAgent:
    """Test cases for AhbabAgent."""

    @pytest.fixture
    def agent(self):
        return AhbabAgent()

    @pytest.mark.asyncio
    async def test_initialize(self, agent):
        """Test agent initializes with correct color."""
        await agent.initialize()
        assert agent._color == "#00E676"
        assert agent._name == "ahbab"

    @pytest.mark.asyncio
    async def test_execute_task_returns_result(self, agent):
        """Test execute_task returns proper structure."""
        task = {"name": "setup_project", "description": "Test task"}
        result = await agent.execute_task(task)

        assert result["agent"] == "ahbab"
        assert result["task"] == "setup_project"
        assert "status" in result
        assert "commands" in result


class TestObsidereAgent:
    """Test cases for ObsidereAgent."""

    @pytest.fixture
    def agent(self):
        return ObsidereAgent()

    @pytest.mark.asyncio
    async def test_initialize(self, agent):
        """Test agent initializes with correct color."""
        await agent.initialize()
        assert agent._color == "#2979FF"
        assert agent._name == "obsidere"

    @pytest.mark.asyncio
    async def test_monitor_detects_errors(self, agent):
        """Test monitor detects errors."""
        results = [
            {"task": "task1", "success": True, "status": "success"},
            {"task": "task2", "success": False, "error": "Something went wrong", "status": "error"}
        ]

        result = await agent.monitor(results)

        assert result["agent"] == "obsidere"
        assert result["has_errors"] == True
        assert result["error_count"] == 1


class TestPatientiaAgent:
    """Test cases for PatientiaAgent."""

    @pytest.fixture
    def agent(self):
        return PatientiaAgent()

    @pytest.mark.asyncio
    async def test_initialize(self, agent):
        """Test agent initializes with correct color."""
        await agent.initialize()
        assert agent._color == "#AA00FF"
        assert agent._name == "patientia"

    @pytest.mark.asyncio
    async def test_score_to_grade(self, agent):
        """Test score to grade conversion."""
        assert agent._score_to_grade(95) == "A"
        assert agent._score_to_grade(85) == "B"
        assert agent._score_to_grade(75) == "C"
        assert agent._score_to_grade(65) == "D"
        assert agent._score_to_grade(55) == "E"
        assert agent._score_to_grade(45) == "F"

    @pytest.mark.asyncio
    async def test_calculate_score_full_marks(self, agent):
        """Test score calculation with all success."""
        results = [
            {"success": True, "status": "success", "errors": []},
            {"success": True, "status": "success", "errors": []}
        ]
        test_result = {"success": True}

        score = agent._calculate_score(results, test_result)
        assert score == 100


class TestMagnumpusAgent:
    """Test cases for MagnumpusAgent."""

    @pytest.fixture
    def agent(self):
        return MagnumpusAgent()

    @pytest.mark.asyncio
    async def test_initialize(self, agent):
        """Test agent initializes with correct color."""
        await agent.initialize()
        assert agent._color == "#FF6D00"
        assert agent._name == "magnumpus"

    @pytest.mark.asyncio
    async def test_package_creates_package(self, agent):
        """Test package creates output."""
        task_dag = [{"name": "task1"}, {"name": "task2"}]
        ahbab_results = [
            {"task": "task1", "success": True, "commands": ["create_gameobject: Test"]}
        ]
        patientia_result = {"score": 80, "grade": "B"}

        result = await agent.package(task_dag, ahbab_results, patientia_result)

        assert result["agent"] == "magnumpus"
        assert "package" in result
        assert result["package"]["final_score"] == 80


class TestAgentOrchestrator:
    """Test cases for AgentOrchestrator."""

    @pytest.fixture
    async def orchestrator(self):
        orch = AgentOrchestrator()
        await orch.initialize()
        return orch

    @pytest.mark.asyncio
    async def test_initialize_all_agents(self, orchestrator):
        """Test all agents are initialized."""
        assert orchestrator._vates is not None
        assert orchestrator._diafor is not None
        assert orchestrator._ahbab is not None
        assert orchestrator._obsidere is not None
        assert orchestrator._patientia is not None
        assert orchestrator._magnumpus is not None

    @pytest.mark.asyncio
    async def test_handle_tool_analyze_input(self, orchestrator):
        """Test analyze_input tool handling."""
        result = await orchestrator.handle_tool("analyze_input", {
            "input": "Create a platformer",
            "gameType": "2D Platformer"
        })

        assert len(result) > 0
        assert result[0]["type"] == "text"

    @pytest.mark.asyncio
    async def test_handle_tool_unknown(self, orchestrator):
        """Test unknown tool returns error message."""
        result = await orchestrator.handle_tool("unknown_tool", {})

        assert len(result) > 0
        assert "Unknown tool" in result[0]["text"]
