"""
Magnumpus Agent - Packages all outputs and delivers to user.
ORANGE #FF6D00 - KONU.md §4.2
"""
import logging
from typing import Any, Dict, List

logger = logging.getLogger(__name__)


class MagnumpusAgent:
    """Packages outputs and delivers to user."""

    def __init__(self):
        self._color = "#FF6D00"
        self._name = "magnumpus"

    async def initialize(self):
        """Initialize the agent."""
        logger.info(f"[{self._name}] Initialized (Color: {self._color})")

    async def package(
        self,
        task_dag: List[Dict],
        ahbab_results: List[Dict],
        patientia_result: Dict
    ) -> Dict[str, Any]:
        """
        Package all outputs and prepare for delivery.

        Based on KONU.md §4.2:
        - Combines all outputs
        - Delivers to user
        """
        logger.info(f"[{self._name}] Packaging outputs...")

        result = {
            "agent": self._name,
            "color": self._color,
            "status": "packaging",
            "package": {},
            "summary": ""
        }

        # Collect all created assets
        created_assets = self._collect_created_assets(ahbab_results)

        # Create package summary
        package = {
            "project_name": "AIGeneratedGame",
            "version": "0.1.0",
            "created_at": self._get_timestamp(),
            "tasks_completed": len(task_dag),
            "final_score": patientia_result.get("score", 0),
            "grade": patientia_result.get("grade", "F"),
            "created_assets": created_assets,
            "files_created": self._count_files(created_assets),
            "components_created": self._count_components(created_assets)
        }

        result["package"] = package
        result["summary"] = self._generate_summary(package)

        # If score is good enough, mark as ready for delivery
        if package["final_score"] >= 50:
            result["status"] = "ready_for_delivery"
            result["delivery_message"] = "Your game is ready! Check the Unity project."
        else:
            result["status"] = "needs_revision"
            result["delivery_message"] = "Your game needs some revisions before delivery."

        logger.info(f"[{self._name}] Packaging complete: {package['files_created']} files")
        return result

    def _collect_created_assets(self, results: List[Dict]) -> List[Dict[str, Any]]:
        """Collect all assets created by ahbab."""
        assets = []

        for result in results:
            task_name = result.get("task", "unknown")
            commands = result.get("commands", [])

            for cmd in commands:
                if isinstance(cmd, str) and ":" in cmd:
                    parts = cmd.split(":", 1)
                    asset_type = parts[0]
                    asset_path = parts[1] if len(parts) > 1 else ""

                    assets.append({
                        "type": asset_type,
                        "path": asset_path,
                        "task": task_name
                    })

        return assets

    def _count_files(self, assets: List[Dict]) -> int:
        """Count total files created."""
        return len([a for a in assets if a.get("path")])

    def _count_components(self, assets: List[Dict]) -> int:
        """Count total components created."""
        return len([a for a in assets if a.get("type") in ["create_gameobject", "add_component"]])

    def _generate_summary(self, package: Dict) -> str:
        """Generate human-readable summary."""
        score = package["final_score"]
        grade = package["grade"]
        files = package["files_created"]
        components = package["components_created"]

        summary = f"""
## Game Package Ready!

### Score: {score}/100 ({grade})

### Created Assets:
- **Files:** {files}
- **Components:** {components}
- **Tasks Completed:** {package['tasks_completed']}

### Created Files:
"""
        for asset in package.get("created_assets", []):
            if asset.get("path"):
                summary += f"- `{asset['path']}` ({asset['type']})\n"

        return summary

    def _get_timestamp(self) -> str:
        """Get current timestamp."""
        from datetime import datetime
        return datetime.now().isoformat()
