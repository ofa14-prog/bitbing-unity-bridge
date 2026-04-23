"""
Patientia Agent - Runs tests, scores 1-100, and decides direction.
PURPLE #AA00FF - KONU.md §4.2
"""
import logging
from typing import Any, Dict, List

logger = logging.getLogger(__name__)


class PatientiaAgent:
    """Runs tests and scores the build."""

    def __init__(self):
        self._color = "#AA00FF"
        self._name = "patientia"

    async def initialize(self):
        """Initialize the agent."""
        logger.info(f"[{self._name}] Initialized (Color: {self._color})")

    async def test_and_score(self, results: List[Dict[str, Any]]) -> Dict[str, Any]:
        """
        Run tests and score the build 1-100.

        Based on KONU.md §4.2:
        - Runs tests
        - Scores 1-100
        - Decides whether to send back to obsidere or proceed to magnumpus
        """
        logger.info(f"[{self._name}] Testing and scoring {len(results)} tasks")

        result = {
            "agent": self._name,
            "color": self._color,
            "status": "testing",
            "score": 0,
            "grade": "F",
            "test_results": [],
            "decision": "pending"
        }

        # Run tests on Unity side
        test_result = await self._run_unity_tests()

        # Calculate score
        score = self._calculate_score(results, test_result)
        result["score"] = score
        result["grade"] = self._score_to_grade(score)

        # Make decision based on score
        if score < 50:
            result["decision"] = "send_back_to_obsidere"
            result["decision_reason"] = "Score below 50 - needs revision"
        else:
            result["decision"] = "proceed_to_magnumpus"
            result["decision_reason"] = "Score acceptable - proceed to packaging"

        result["status"] = "complete"

        logger.info(f"[{self._name}] Final score: {score}/100 ({result['grade']})")
        return result

    async def _run_unity_tests(self) -> Dict[str, Any]:
        """Run tests on Unity side."""
        try:
            import httpx

            async with httpx.AsyncClient(timeout=120.0) as client:
                response = await client.post(
                    "http://localhost:8080/mcp",
                    json={
                        "jsonrpc": "2.0",
                        "id": "1",
                        "method": "tools/call",
                        "params": {
                            "name": "run_tests",
                            "arguments": {"testSuite": "EditMode"}
                        }
                    }
                )
                response.raise_for_status()
                return response.json()
        except Exception as e:
            logger.warning(f"Unity tests failed: {e}")
            return {"success": False, "error": str(e)}

    def _calculate_score(self, results: List[Dict], test_result: Dict) -> int:
        """
        Calculate score from 1-100 based on multiple factors.

        Based on KONU.md §4.3:
        - 1-50: Insufficient - send back to obsidere
        - 51-100: Sufficient - proceed to magnumpus
        """
        score = 100

        # Deduct for failed tasks
        failed_count = sum(1 for r in results if not r.get("success", False))
        score -= failed_count * 10

        # Deduct for test failures
        if not test_result.get("success", True):
            score -= 20

        # Deduct for errors
        error_count = sum(
            len(r.get("errors", []))
            for r in results
        )
        score -= error_count * 5

        # Deduct for skipped tasks
        skipped_count = sum(1 for r in results if r.get("status") == "skipped")
        score -= skipped_count * 5

        return max(0, min(100, score))

    def _score_to_grade(self, score: int) -> str:
        """Convert score to letter grade."""
        if score >= 90:
            return "A"
        elif score >= 80:
            return "B"
        elif score >= 70:
            return "C"
        elif score >= 60:
            return "D"
        elif score >= 50:
            return "E"
        else:
            return "F"
