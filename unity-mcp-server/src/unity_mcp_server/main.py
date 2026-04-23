"""
Main entry point for the Unity MCP Server.
Based on COPLAY unity-mcp architecture, extended with 6-agent orchestration.
"""
import asyncio
import logging
import sys
from typing import Optional

from .mcp_server import UnityMcpServer
from .agents.orchestrator import AgentOrchestrator

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(name)s] %(levelname)s: %(message)s",
)
logger = logging.getLogger(__name__)


async def main():
    """Main entry point."""
    logger.info("Starting Unity MCP Server v0.1.0")

    # Initialize the MCP server
    server = UnityMcpServer()

    # Initialize the agent orchestrator
    orchestrator = AgentOrchestrator()
    await orchestrator.initialize()

    # Connect orchestrator to server
    server.set_orchestrator(orchestrator)

    # Run the server
    try:
        await server.run()
    except KeyboardInterrupt:
        logger.info("Shutting down...")
        await server.stop()


if __name__ == "__main__":
    asyncio.run(main())
