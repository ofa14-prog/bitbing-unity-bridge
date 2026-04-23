"""
Obsidere Agent - Monitors outputs, detects errors, and loops back for corrections.
BLUE #2979FF - KONU.md §4.2
"""
import logging
from typing import Any, Dict, List

logger = logging.getLogger(__name__)


class ObsidereAgent:
    """Monitors outputs, detects errors, and triggers corrections."""

    def __init__(self):
        self._color = "#2979FF"
        self._name = "obsidere"

    async def initialize(self):
        """Initialize the agent."""
        logger.info(f"[{self._name}] Initialized (Color: {self._color})")

    async def monitor(self, results: List[Dict[str, Any]]) -> Dict[str, Any]:
        """
        Monitor outputs from ahbab and detect errors.

        Based on KONU.md §4.2:
        - Monitors outputs from ahbab
        - Detects errors
        - Triggers loop back to ahbab if needed
        """
        logger.info(f"[{self._name}] Monitoring {len(results)} task results")

        result = {
            "agent": self._name,
            "color": self._color,
            "status": "analyzing",
            "has_errors": False,
            "error_count": 0,
            "corrections": [],
            "analyzed_results": []
        }

        errors = []
        corrections = []

        for task_result in results:
            analyzed = self._analyze_single_result(task_result)
            result["analyzed_results"].append(analyzed)

            if not analyzed["success"]:
                errors.append(analyzed)
                result["error_count"] += 1
                result["has_errors"] = True

                # Generate correction
                correction = self._generate_correction(analyzed)
                if correction:
                    corrections.append(correction)

        result["errors"] = errors
        result["corrections"] = corrections
        result["status"] = "complete"

        logger.info(f"[{self._name}] Found {result['error_count']} errors")
        return result

    def _analyze_single_result(self, task_result: Dict[str, Any]) -> Dict[str, Any]:
        """Analyze a single task result."""
        success = task_result.get("success", False)
        task_name = task_result.get("task", "unknown")

        analyzed = {
            "task": task_name,
            "success": success,
            "status": task_result.get("status", "unknown"),
            "errors": [],
            "warnings": []
        }

        # Check for specific error patterns
        if task_result.get("error"):
            analyzed["errors"].append(task_result["error"])

        # Check for compilation errors
        commands = task_result.get("commands", [])
        for cmd in commands:
            if "compile" in str(cmd).lower():
                # Would check compilation log in real implementation
                pass

        # Task-specific analysis
        if task_name == "create_player" and success:
            # Verify player components
            pass

        elif task_name == "create_enemies" and success:
            # Verify enemy AI
            pass

        analyzed["success"] = len(analyzed["errors"]) == 0
        return analyzed

    def _generate_correction(self, analyzed: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Generate a correction for an error."""
        task_name = analyzed["task"]
        errors = analyzed["errors"]

        if not errors:
            return None

        correction = {
            "type": "unknown",
            "task": task_name,
            "details": {}
        }

        for error in errors:
            error_str = str(error).lower()

            if "not found" in error_str or "does not exist" in error_str:
                correction["type"] = "missing_reference"
                correction["details"] = {
                    "action": "create_missing",
                    "description": f"Missing component in {task_name}"
                }

            elif "compilation" in error_str or "syntax" in error_str:
                correction["type"] = "script_fix"
                correction["details"] = {
                    "action": "fix_syntax",
                    "description": f"Fix syntax error in {task_name}"
                }

            elif "null" in error_str or "reference" in error_str:
                correction["type"] = "null_reference"
                correction["details"] = {
                    "action": "check_references",
                    "description": f"Fix null reference in {task_name}"
                }

        return correction if correction["type"] != "unknown" else None
