#!/usr/bin/env python3
"""
Command-line interface for Unity MCP Server.
"""
import argparse
import asyncio
import logging
import sys

from .main import main as server_main
from .agents.orchestrator import AgentOrchestrator
from .tools.orchestrator_tools import OrchestratorTools

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(name)s] %(levelname)s: %(message)s",
)
logger = logging.getLogger(__name__)


def parse_args():
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Unity MCP Server - AI-driven Unity game development"
    )

    subparsers = parser.add_subparsers(dest="command", help="Commands")

    # Server command
    server_parser = subparsers.add_parser("server", help="Start MCP server")
    server_parser.add_argument(
        "--port", "-p",
        type=int,
        default=8080,
        help="Port to listen on (default: 8080)"
    )

    # Status command
    status_parser = subparsers.add_parser("status", help="Get pipeline status")
    status_parser.add_argument(
        "--agents",
        action="store_true",
        help="Show agent information"
    )

    # Test command
    test_parser = subparsers.add_parser("test", help="Test pipeline with sample input")
    test_parser.add_argument(
        "--input", "-i",
        type=str,
        default="Create a 2D platformer game",
        help="Input to test with"
    )
    test_parser.add_argument(
        "--game-type", "-g",
        type=str,
        default="2D Platformer",
        help="Game type"
    )

    return parser.parse_args()


async def cmd_status(agents: bool):
    """Handle status command."""
    if agents:
        agent_info = OrchestratorTools.get_agent_info()
        print("\n=== Agent Information ===")
        for agent in agent_info:
            print(f"\n[{agent['id'].upper()}] {agent['name']}")
            print(f"  Color: {agent['color']}")
            print(f"  Description: {agent['description']}")
    else:
        status = OrchestratorTools.get_pipeline_status()
        print("\n=== Pipeline Status ===")
        for key, value in status.items():
            print(f"  {key}: {value}")


async def cmd_test(input_text: str, game_type: str):
    """Handle test command."""
    print(f"\n=== Testing Pipeline ===")
    print(f"Input: {input_text}")
    print(f"Game Type: {game_type}")
    print()

    orchestrator = AgentOrchestrator()
    await orchestrator.initialize()

    result = await orchestrator.handle_tool("orchestrate_pipeline", {
        "input": input_text,
        "gameType": game_type
    })

    for item in result:
        print(item["text"])


async def cmd_server(port: int):
    """Handle server command."""
    from .mcp_server import UnityMcpServer

    print(f"\n=== Starting Unity MCP Server on port {port} ===")
    print("Press Ctrl+C to stop")
    print()

    server = UnityMcpServer(port=port)
    orchestrator = AgentOrchestrator()
    await orchestrator.initialize()
    server.set_orchestrator(orchestrator)

    try:
        await server.run()
    except KeyboardInterrupt:
        logger.info("Shutting down...")
        await server.stop()


async def main():
    """Main entry point for CLI."""
    args = parse_args()

    if args.command is None:
        # Start server by default
        args.port = 8080
        await cmd_server(args.port)
        return

    if args.command == "server":
        await cmd_server(args.port)
    elif args.command == "status":
        await cmd_status(args.agents)
    elif args.command == "test":
        await cmd_test(args.input, args.game_type)


if __name__ == "__main__":
    asyncio.run(main())
