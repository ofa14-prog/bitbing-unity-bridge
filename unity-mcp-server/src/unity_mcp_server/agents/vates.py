"""
Vates Agent - Analyzes user input and creates task DAG.
RED #FF3B3B - KONU.md §4.2
"""
import logging
from typing import Any, Dict, List

logger = logging.getLogger(__name__)


class VatesAgent:
    """Analyzes user input and creates task DAG."""

    def __init__(self):
        self._color = "#FF3B3B"
        self._name = "vates"

    async def initialize(self):
        """Initialize the agent."""
        logger.info(f"[{self._name}] Initialized (Color: {self._color})")

    async def analyze(self, user_input: str, game_type: str) -> Dict[str, Any]:
        """
        Analyze user input and create a task DAG.

        Based on KONU.md §4.2:
        - Analyzes user input
        - Creates DAG task schema
        """
        logger.info(f"[{self._name}] Analyzing input: {user_input[:50]}...")

        # Task templates based on game type
        task_templates = self._get_task_templates(game_type)

        # Analyze input to determine which tasks are needed
        tasks = self._analyze_input_and_select_tasks(user_input, task_templates)

        result = {
            "agent": self._name,
            "color": self._color,
            "status": "success",
            "task_dag": tasks,
            "game_type": game_type,
            "input_analyzed": user_input
        }

        logger.info(f"[{self._name}] Created {len(tasks)} tasks")
        return result

    def _get_task_templates(self, game_type: str) -> List[Dict[str, Any]]:
        """Get task templates based on game type."""
        base_templates = [
            {
                "name": "setup_project",
                "description": "Set up Unity project structure",
                "priority": 1,
                "dependencies": []
            },
            {
                "name": "create_scene",
                "description": "Create and configure main scene",
                "priority": 2,
                "dependencies": ["setup_project"]
            },
            {
                "name": "create_player",
                "description": "Create player GameObject and controller",
                "priority": 3,
                "dependencies": ["setup_project"]
            },
            {
                "name": "create_enemies",
                "description": "Create enemy GameObjects",
                "priority": 3,
                "dependencies": ["setup_project", "create_player"]
            },
            {
                "name": "create_ui",
                "description": "Create UI elements (HUD, menus)",
                "priority": 4,
                "dependencies": ["setup_project"]
            },
            {
                "name": "implement_physics",
                "description": "Implement physics and collisions",
                "priority": 3,
                "dependencies": ["create_player"]
            },
            {
                "name": "implement_audio",
                "description": "Add audio effects and music",
                "priority": 5,
                "dependencies": ["create_player"]
            },
            {
                "name": "build_game",
                "description": "Build and package the game",
                "priority": 6,
                "dependencies": ["create_player", "create_enemies", "create_ui"]
            }
        ]

        if "platformer" in game_type.lower():
            return base_templates
        elif "puzzle" in game_type.lower():
            return base_templates + [
                {
                    "name": "create_puzzle_mechanics",
                    "description": "Create puzzle logic",
                    "priority": 3,
                    "dependencies": ["setup_project"]
                }
            ]
        elif "fps" in game_type.lower() or "first-person" in game_type.lower():
            return base_templates + [
                {
                    "name": "create_weapons",
                    "description": "Create weapon systems",
                    "priority": 3,
                    "dependencies": ["create_player"]
                }
            ]

        return base_templates

    def _analyze_input_and_select_tasks(
        self,
        user_input: str,
        templates: List[Dict[str, Any]]
    ) -> List[Dict[str, Any]]:
        """Analyze input and select relevant tasks."""
        # Simple keyword-based selection
        input_lower = user_input.lower()

        selected_tasks = []
        for template in templates:
            task_name = template["name"].lower()

            # Always include essential tasks
            if template["priority"] <= 2:
                selected_tasks.append(template)
                continue

            # Check keywords for optional tasks
            keywords = {
                "create_enemies": ["enemy", "düşman", "mob", "creature"],
                "create_ui": ["ui", "interface", "menu", "hud", "arayüz"],
                "implement_physics": ["physics", "fizik", "collision", "çarpışma"],
                "implement_audio": ["audio", "sound", "music", "ses"],
                "build_game": ["build", "package", "derle", "paketle"],
            }

            if task_name in keywords:
                for keyword in keywords[task_name]:
                    if keyword in input_lower:
                        selected_tasks.append(template)
                        break

        # Sort by priority
        selected_tasks.sort(key=lambda x: x["priority"])

        return selected_tasks
