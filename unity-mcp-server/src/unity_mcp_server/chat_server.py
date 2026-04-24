"""Web UI chat server — serves the chatbot frontend and runs the 6-agent pipeline."""
import asyncio
import json
import logging
from contextlib import asynccontextmanager
from pathlib import Path
from typing import Any, Dict

import uvicorn
from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.responses import FileResponse, StreamingResponse
from pydantic import BaseModel

from .agents.orchestrator import AgentOrchestrator

logger = logging.getLogger(__name__)

STATIC_DIR = Path(__file__).parent.parent.parent / "static"

_orchestrator: AgentOrchestrator | None = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    global _orchestrator
    _orchestrator = AgentOrchestrator()
    await _orchestrator.initialize()
    logger.info("Orchestrator initialized — chat server ready.")
    yield


app = FastAPI(title="BitBing Unity Bridge", lifespan=lifespan)


class RunRequest(BaseModel):
    prompt: str
    gameType: str = "2D Platformer"


@app.get("/")
async def index():
    return FileResponse(STATIC_DIR / "index.html")


@app.post("/api/run")
async def api_run(req: RunRequest):
    """NDJSON streaming endpoint for Unity Agent Panel chat."""
    queue: asyncio.Queue = asyncio.Queue()

    async def produce() -> None:
        try:
            await _orchestrator.run_with_progress(req.prompt, req.gameType, queue.put)
        except Exception as exc:
            await queue.put({"type": "pipeline_failed", "reason": str(exc)})
        finally:
            await queue.put(None)

    asyncio.create_task(produce())

    async def ndjson():
        while True:
            event = await queue.get()
            if event is None:
                break
            yield json.dumps(event, ensure_ascii=False) + "\n"

    return StreamingResponse(ndjson(), media_type="application/x-ndjson")


@app.websocket("/ws")
async def ws_endpoint(websocket: WebSocket):
    await websocket.accept()
    logger.info("WebSocket client connected")
    try:
        while True:
            data = await websocket.receive_json()
            prompt: str = data.get("prompt", "").strip()
            game_type: str = data.get("gameType", "2D Platformer")

            if not prompt:
                continue

            async def send(event: Dict[str, Any]) -> None:
                await websocket.send_json(event)

            try:
                await _orchestrator.run_with_progress(prompt, game_type, send)
            except Exception as exc:
                logger.exception("Pipeline error")
                await websocket.send_json({"type": "pipeline_failed", "reason": str(exc)})
    except WebSocketDisconnect:
        logger.info("WebSocket client disconnected")


def start() -> None:
    """Entry point for the unity-mcp-chat CLI command."""
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s [%(name)s] %(levelname)s: %(message)s",
    )
    uvicorn.run(
        "unity_mcp_server.chat_server:app",
        host="127.0.0.1",
        port=8001,
        reload=False,
    )
