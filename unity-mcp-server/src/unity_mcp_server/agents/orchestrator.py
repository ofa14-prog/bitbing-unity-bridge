"""
Agent orchestrator - coordinates the 6-agent pipeline.
Based on KONU.md §4.1 architecture.
"""
import logging
from typing import Any, Callable, Coroutine, Dict, List, Optional

from .vates import VatesAgent
from .diafor import DiaforAgent
from .ahbab import AhbabAgent
from .obsidere import ObsidereAgent
from .patientia import PatientiaAgent
from .magnumpus import MagnumpusAgent

logger = logging.getLogger(__name__)


class AgentOrchestrator:
    """Orchestrates the 6-agent pipeline for AI-driven game development."""

    def __init__(self):
        self._vates = VatesAgent()
        self._diafor = DiaforAgent()
        self._ahbab = AhbabAgent()
        self._obsidere = ObsidereAgent()
        self._patientia = PatientiaAgent()
        self._magnumpus = MagnumpusAgent()

        self._current_pipeline = None
        self._iteration_count = 0
        self._max_iterations = 5

    async def initialize(self):
        """Initialize all agents."""
        logger.info("Initializing 6-agent orchestrator...")

        # Initialize each agent
        await self._vates.initialize()
        await self._diafor.initialize()
        await self._ahbab.initialize()
        await self._obsidere.initialize()
        await self._patientia.initialize()
        await self._magnumpus.initialize()

        logger.info("All agents initialized successfully")

    async def handle_tool(self, tool_name: str, arguments: Dict[str, Any]) -> List[Dict]:
        """Handle tool calls from the MCP server."""
        logger.info(f"Orchestrator handling tool: {tool_name}")

        if tool_name == "analyze_input":
            return await self._handle_analyze_input(arguments)
        elif tool_name == "check_environment":
            return await self._handle_check_environment(arguments)
        elif tool_name == "orchestrate_pipeline":
            return await self._handle_orchestrate_pipeline(arguments)
        else:
            return [{"type": "text", "text": f"Unknown tool: {tool_name}"}]

    async def _handle_analyze_input(self, arguments: Dict[str, Any]) -> List[Dict]:
        """Handle analyze_input tool call."""
        user_input = arguments.get("input", "")
        game_type = arguments.get("gameType", "2D Platformer")

        result = await self._vates.analyze(user_input, game_type)

        return [{
            "type": "text",
            "text": f"## Vates Analysis\n\n**Input:** {user_input}\n**Game Type:** {game_type}\n\n**Task DAG:**\n{self._format_task_dag(result)}"
        }]

    async def _handle_check_environment(self, arguments: Dict[str, Any]) -> List[Dict]:
        """Handle check_environment tool call."""
        motor = arguments.get("motor", "unity")

        result = await self._diafor.check_environment(motor)

        return [{
            "type": "text",
            "text": f"## Diafor Environment Check\n\n**Motor:** {motor}\n\n**Status:** {result.get('status', 'unknown')}\n\n{self._format_check_result(result)}"
        }]

    async def _handle_orchestrate_pipeline(self, arguments: Dict[str, Any]) -> List[Dict]:
        """Handle the full 6-agent pipeline."""
        user_input = arguments.get("input", "")
        game_type = arguments.get("gameType", "2D Platformer")

        logger.info("Starting full 6-agent pipeline...")

        # Phase 1: Vates - Analyze input
        logger.info("[1/6] Vates analyzing input...")
        vates_result = await self._vates.analyze(user_input, game_type)
        task_dag = vates_result.get("task_dag", [])
        logger.info(f"Vates created {len(task_dag)} tasks")

        # Phase 2: Diafor - Check dependencies
        logger.info("[2/6] Diafor checking environment...")
        diafor_result = await self._diafor.check_environment("unity")
        if not diafor_result.get("ready", False):
            return [{
                "type": "text",
                "text": f"## Pipeline Failed\n\n**Reason:** Environment not ready\n{diafor_result}"
            }]

        # Phase 3: Ahbab - Execute commands
        logger.info("[3/6] Ahbab executing commands...")
        ahbab_results = []
        for task in task_dag:
            result = await self._ahbab.execute_task(task)
            ahbab_results.append(result)

        # Phase 4: Obsidere - Monitor and correct
        logger.info("[4/6] Obsidere monitoring outputs...")
        obsidere_result = await self._obsidere.monitor(ahbab_results)

        # Loop if errors detected
        self._iteration_count = 0
        while obsidere_result.get("has_errors", False) and self._iteration_count < self._max_iterations:
            self._iteration_count += 1
            logger.info(f"[4/6] Obsidere found errors, iteration {self._iteration_count}")

            # Apply patches
            for correction in obsidere_result.get("corrections", []):
                await self._ahbab.apply_correction(correction)

            # Re-check
            obsidere_result = await self._obsidere.monitor(ahbab_results)

        # Phase 5: Patientia - Test and score
        logger.info("[5/6] Patientia testing and scoring...")
        patientia_result = await self._patientia.test_and_score(ahbab_results)

        # Phase 6: Magnumpus - Package output
        logger.info("[6/6] Magnumpus packaging output...")
        magnumpus_result = await self._magnumpus.package(
            task_dag,
            ahbab_results,
            patientia_result
        )

        return [{
            "type": "text",
            "text": self._format_pipeline_summary(
                vates_result,
                diafor_result,
                ahbab_results,
                obsidere_result,
                patientia_result,
                magnumpus_result
            )
        }]

    async def run_with_progress(
        self,
        user_input: str,
        game_type: str,
        progress: Callable[[Dict[str, Any]], Coroutine],
    ) -> None:
        """Run the full pipeline, emitting progress events to the chat UI."""
        self._iteration_count = 0

        async def emit(agent_id: str, status: str, message: str) -> None:
            await progress({"type": "agent_event", "agentId": agent_id, "status": status, "message": message})

        # Phase 1: vates
        await emit("vates", "running", "Kullanıcı girdisi analiz ediliyor…")
        vates_result = await self._vates.analyze(user_input, game_type)
        task_dag = vates_result.get("task_dag", [])
        await emit("vates", "done", f"{len(task_dag)} görev oluşturuldu")

        # Phase 2: diafor
        await emit("diafor", "running", "Ortam ve Unity bağlantısı kontrol ediliyor…")
        diafor_result = await self._diafor.check_environment("unity")
        if not diafor_result.get("ready", False):
            # Warn but continue — ahbab will surface any real connection error
            await emit("diafor", "error", "Unity bridge yanıt vermedi (komutlar yine de deneniyor)")
        else:
            await emit("diafor", "done", "Bağlantı doğrulandı")

        # Phase 3: ahbab
        await emit("ahbab", "running", f"{len(task_dag)} komut Unity'e gönderiliyor…")
        ahbab_results = []
        for task in task_dag:
            result = await self._ahbab.execute_task(task)
            ahbab_results.append(result)
        await emit("ahbab", "done", f"{len(ahbab_results)} komut tamamlandı")

        # Phase 4: obsidere
        await emit("obsidere", "running", "Çıktılar denetleniyor…")
        obsidere_result = await self._obsidere.monitor(ahbab_results)

        while obsidere_result.get("has_errors", False) and self._iteration_count < self._max_iterations:
            self._iteration_count += 1
            await emit("obsidere", "running", f"Hata düzeltme — iterasyon {self._iteration_count}")
            for correction in obsidere_result.get("corrections", []):
                await self._ahbab.apply_correction(correction)
            obsidere_result = await self._obsidere.monitor(ahbab_results)

        error_count = obsidere_result.get("error_count", 0)
        obs_status = "error" if obsidere_result.get("has_errors") else "done"
        await emit("obsidere", obs_status, f"{error_count} hata tespit edildi")

        # Phase 5: patientia
        await emit("patientia", "running", "Build kalitesi puanlanıyor…")
        patientia_result = await self._patientia.test_and_score(ahbab_results)
        score = patientia_result.get("score", 0)
        grade = patientia_result.get("grade", "F")
        await emit("patientia", "done", f"Puan: {score}/100 — Not: {grade}")

        # Phase 6: magnumpus
        await emit("magnumpus", "running", "Çıktılar paketleniyor…")
        magnumpus_result = await self._magnumpus.package(task_dag, ahbab_results, patientia_result)
        await emit("magnumpus", "done", magnumpus_result.get("delivery_message", "Teslim tamamlandı"))

        await progress({
            "type": "pipeline_complete",
            "summary": self._format_pipeline_summary(
                vates_result, diafor_result, ahbab_results, obsidere_result, patientia_result, magnumpus_result
            ),
            "score": score,
            "grade": grade,
        })

    def _format_task_dag(self, result: Dict[str, Any]) -> str:
        """Format task DAG for display."""
        tasks = result.get("task_dag", [])
        lines = []
        for i, task in enumerate(tasks):
            lines.append(f"- **{task.get('name', f'Task {i+1}')}**: {task.get('description', '')}")
        return "\n".join(lines) if lines else "- No tasks generated"

    def _format_check_result(self, result: Dict[str, Any]) -> str:
        """Format environment check result."""
        checks = result.get("checks", {})
        lines = []
        for key, value in checks.items():
            status = "✅" if value else "❌"
            lines.append(f"  {status} {key}")
        return "\n".join(lines) if lines else str(result)

    def _format_pipeline_summary(
        self,
        vates_result: Dict,
        diafor_result: Dict,
        ahbab_results: List[Dict],
        obsidere_result: Dict,
        patientia_result: Dict,
        magnumpus_result: Dict
    ) -> str:
        """Format the full pipeline summary."""
        score = patientia_result.get("score", 0)
        grade = "A" if score >= 80 else "B" if score >= 60 else "C" if score >= 40 else "F"

        summary = f"""## Pipeline Complete

### Agent Results

| Agent | Status | Output |
|-------|--------|--------|
| **vates** (RED) | ✅ | {len(vates_result.get('task_dag', []))} tasks created |
| **Diafor** (YEL) | ✅ | Environment ready |
| **ahbab** (GRN) | ✅ | {len(ahbab_results)} commands executed |
| **obsidere** (BLU) | {"✅" if not obsidere_result.get('has_errors') else "🔄"} | {obsidere_result.get('error_count', 0)} errors detected |
| **patientia** (PUR) | ✅ | Score: **{score}/100** (Grade: {grade}) |
| **magnumpus** (ORN) | ✅ | Output packaged |

### Final Score: {score}/100 ({grade})

{magnumpus_result.get('summary', 'No summary available')}
"""
        return summary
