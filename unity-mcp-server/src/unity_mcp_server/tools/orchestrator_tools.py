"""
Orchestrator tools - tools for managing the 6-agent pipeline.
"""
import logging
from typing import Any, Dict, List

logger = logging.getLogger(__name__)


class OrchestratorTools:
    """Tools for managing the agent orchestrator."""

    @staticmethod
    def get_pipeline_status() -> Dict[str, Any]:
        """Get current pipeline status."""
        return {
            "status": "idle",
            "current_agent": None,
            "iteration": 0,
            "max_iterations": 5
        }

    @staticmethod
    def get_agent_info() -> List[Dict[str, str]]:
        """Get information about all agents."""
        return [
            {
                "id": "vates",
                "name": "Vates",
                "color": "#FF3B3B",
                "description": "Analyzes user input and creates task DAG"
            },
            {
                "id": "diafor",
                "name": "Diafor",
                "color": "#FFD600",
                "description": "Checks dependencies and prepares environment"
            },
            {
                "id": "ahbab",
                "name": "Ahbab",
                "color": "#00E676",
                "description": "Sends commands to Unity"
            },
            {
                "id": "obsidere",
                "name": "Obsidere",
                "color": "#2979FF",
                "description": "Monitors outputs and detects errors"
            },
            {
                "id": "patientia",
                "name": "Patientia",
                "color": "#AA00FF",
                "description": "Runs tests and scores 1-100"
            },
            {
                "id": "magnumpus",
                "name": "Magnumpus",
                "color": "#FF6D00",
                "description": "Packages outputs and delivers to user"
            }
        ]

    @staticmethod
    def get_available_game_types() -> List[str]:
        """Get available game types."""
        return [
            "2D Platformer",
            "2D Top-Down",
            "2D Puzzle",
            "3D First-Person",
            "3D Third-Person",
            "3D Strategy",
            "Card / Board",
            "Visual Novel"
        ]
