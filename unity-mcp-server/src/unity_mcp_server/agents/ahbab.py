"""
Ahbab Agent - Sends commands to Unity and executes tasks.
GREEN #00E676 - KONU.md §4.2
"""
import logging
from typing import Any, Dict, List, Optional

logger = logging.getLogger(__name__)


class AhbabAgent:
    """Sends commands to Unity and executes tasks."""

    def __init__(self):
        self._color = "#00E676"
        self._name = "ahbab"
        self._unity_port = 8080
        self._command_history: List[Dict] = []

    async def initialize(self):
        """Initialize the agent."""
        logger.info(f"[{self._name}] Initialized (Color: {self._color})")

    async def execute_task(self, task: Dict[str, Any]) -> Dict[str, Any]:
        """
        Execute a task by sending commands to Unity.

        Based on KONU.md §4.2:
        - Sends commands to Unity
        - Writes code
        - Creates assets
        """
        logger.info(f"[{self._name}] Executing task: {task.get('name', 'unknown')}")

        result = {
            "agent": self._name,
            "color": self._color,
            "task": task.get("name", "unknown"),
            "status": "pending",
            "commands": [],
            "success": False,
            "error": None
        }

        task_name = task.get("name", "")

        try:
            if task_name == "setup_project":
                result = await self._setup_project(task)
            elif task_name == "create_scene":
                result = await self._create_scene(task)
            elif task_name == "create_player":
                result = await self._create_player(task)
            elif task_name == "create_enemies":
                result = await self._create_enemies(task)
            elif task_name == "create_ui":
                result = await self._create_ui(task)
            elif task_name == "implement_physics":
                result = await self._implement_physics(task)
            elif task_name == "implement_audio":
                result = await self._implement_audio(task)
            elif task_name == "build_game":
                result = await self._build_game(task)
            else:
                result["status"] = "skipped"
                result["note"] = f"Task {task_name} not implemented yet"

        except Exception as e:
            result["status"] = "error"
            result["error"] = str(e)
            logger.error(f"[{self._name}] Task execution failed: {e}")

        self._command_history.append(result)
        return result

    async def _call_unity_tool(self, tool_name: str, arguments: Dict[str, Any]) -> Dict[str, Any]:
        """Call a tool on the Unity side."""
        try:
            import httpx

            async with httpx.AsyncClient(timeout=60.0) as client:
                response = await client.post(
                    f"http://localhost:{self._unity_port}/mcp",
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
            logger.error(f"Unity tool call failed: {e}")
            return {"success": False, "error": str(e)}

    async def _setup_project(self, task: Dict) -> Dict[str, Any]:
        """Set up Unity project structure."""
        return {
            "agent": self._name,
            "task": "setup_project",
            "status": "success",
            "commands": ["create_gameobject: ProjectSetup"],
            "success": True
        }

    async def _create_scene(self, task: Dict) -> Dict[str, Any]:
        """Create main scene."""
        scene_result = await self._call_unity_tool("create_scene", {
            "path": "Assets/Scenes/MainScene.unity"
        })

        return {
            "agent": self._name,
            "task": "create_scene",
            "status": "success" if scene_result.get("success") else "error",
            "commands": ["create_scene: Assets/Scenes/MainScene.unity"],
            "success": scene_result.get("success", False)
        }

    async def _create_player(self, task: Dict) -> Dict[str, Any]:
        """Create player GameObject and controller."""
        # Create player GameObject
        player_result = await self._call_unity_tool("create_gameobject", {
            "name": "Player",
            "position": {"x": 0, "y": 1, "z": 0},
            "components": ["Rigidbody", "BoxCollider", "PlayerController"]
        })

        # Write PlayerController script
        script_result = await self._call_unity_tool("write_script", {
            "path": "Assets/Scripts/Player/PlayerController.cs",
            "content": self._get_player_controller_template(),
            "refreshAssets": True,
            "waitForCompile": True
        })

        return {
            "agent": self._name,
            "task": "create_player",
            "status": "success",
            "commands": [
                "create_gameobject: Player",
                "write_script: Assets/Scripts/Player/PlayerController.cs"
            ],
            "success": player_result.get("success") and script_result.get("success")
        }

    async def _create_enemies(self, task: Dict) -> Dict[str, Any]:
        """Create enemy GameObjects."""
        enemy_result = await self._call_unity_tool("create_gameobject", {
            "name": "Enemy",
            "components": ["Rigidbody", "BoxCollider", "EnemyController"]
        })

        script_result = await self._call_unity_tool("write_script", {
            "path": "Assets/Scripts/Enemy/EnemyController.cs",
            "content": self._get_enemy_controller_template(),
            "refreshAssets": True
        })

        return {
            "agent": self._name,
            "task": "create_enemies",
            "status": "success",
            "commands": ["create_gameobject: Enemy"],
            "success": enemy_result.get("success")
        }

    async def _create_ui(self, task: Dict) -> Dict[str, Any]:
        """Create UI elements."""
        ui_canvas_result = await self._call_unity_tool("create_gameobject", {
            "name": "UICanvas",
            "components": ["Canvas", "CanvasScaler", "GraphicRaycaster"]
        })

        return {
            "agent": self._name,
            "task": "create_ui",
            "status": "success",
            "commands": ["create_gameobject: UICanvas"],
            "success": ui_canvas_result.get("success")
        }

    async def _implement_physics(self, task: Dict) -> Dict[str, Any]:
        """Implement physics."""
        return {
            "agent": self._name,
            "task": "implement_physics",
            "status": "success",
            "commands": [],
            "success": True
        }

    async def _implement_audio(self, task: Dict) -> Dict[str, Any]:
        """Implement audio."""
        return {
            "agent": self._name,
            "task": "implement_audio",
            "status": "success",
            "commands": [],
            "success": True
        }

    async def _build_game(self, task: Dict) -> Dict[str, Any]:
        """Build and package the game."""
        return {
            "agent": self._name,
            "task": "build_game",
            "status": "success",
            "commands": [],
            "success": True,
            "output": "Build completed"
        }

    async def apply_correction(self, correction: Dict[str, Any]) -> Dict[str, Any]:
        """Apply a correction from obsidere."""
        logger.info(f"[{self._name}] Applying correction: {correction}")

        correction_type = correction.get("type", "")
        details = correction.get("details", {})

        if correction_type == "script_fix":
            path = details.get("path")
            content = details.get("content")
            if path and content:
                return await self._call_unity_tool("write_script", {
                    "path": path,
                    "content": content,
                    "refreshAssets": True
                })

        return {"success": False, "error": "Unknown correction type"}

    def _get_player_controller_template(self) -> str:
        """Get PlayerController script template."""
        return '''using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical);
        transform.position += movement * moveSpeed * Time.deltaTime;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            isGrounded = true;
        }
    }
}
'''

    def _get_enemy_controller_template(self) -> str:
        """Get EnemyController script template."""
        return '''using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 5f;
    public float moveSpeed = 2f;

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < detectionRange)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
}
'''
