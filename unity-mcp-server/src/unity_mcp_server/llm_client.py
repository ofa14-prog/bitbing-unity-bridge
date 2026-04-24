"""
LLM client — OpenAI-compatible chat completions (used with OpenRouter).

Settings resolution order (first hit wins):
  1) Environment variables: ANTHROPIC_BASE_URL / ANTHROPIC_AUTH_TOKEN /
     ANTHROPIC_API_KEY / ANTHROPIC_MODEL
  2) ~/.bitbing/settings.json
  3) Monorepo file:  com.bitbing.unity-bridge/setting_referance.json
     (walked up from this module — only for local dev convenience)

OpenRouter accepts OpenAI-style /v1/chat/completions. We use httpx directly
to avoid pulling another SDK.
"""
from __future__ import annotations

import json
import logging
import os
from pathlib import Path
from typing import Any, Dict, List, Optional

import httpx

logger = logging.getLogger(__name__)


def _load_settings_file() -> Dict[str, str]:
    candidates = []
    home = Path.home() / ".bitbing" / "settings.json"
    candidates.append(home)

    here = Path(__file__).resolve()
    for parent in here.parents:
        candidate = parent / "com.bitbing.unity-bridge" / "setting_referance.json"
        if candidate.exists():
            candidates.append(candidate)
            break

    for path in candidates:
        try:
            if path.exists():
                data = json.loads(path.read_text(encoding="utf-8"))
                env = data.get("env", {}) if isinstance(data, dict) else {}
                if isinstance(env, dict):
                    return {k: str(v) for k, v in env.items() if v}
        except Exception as exc:
            logger.warning("Failed to read %s: %s", path, exc)
    return {}


_FILE_SETTINGS = _load_settings_file()


def _get(key: str, default: str = "") -> str:
    return os.environ.get(key) or _FILE_SETTINGS.get(key, default)


def _normalize_base_url(url: str) -> str:
    url = url.rstrip("/")
    if url.endswith("/v1"):
        return url
    if "openrouter.ai" in url:
        return f"{url}/v1"
    return url


class LLMClient:
    def __init__(self):
        self.base_url = _normalize_base_url(_get("ANTHROPIC_BASE_URL", "https://openrouter.ai/api"))
        self.api_key = _get("ANTHROPIC_AUTH_TOKEN") or _get("ANTHROPIC_API_KEY")
        self.model = _get("ANTHROPIC_MODEL", "minimax/minimax-m2.7")
        self._client: Optional[httpx.AsyncClient] = None

    @property
    def is_configured(self) -> bool:
        return bool(self.api_key and self.model and self.base_url)

    async def _client_get(self) -> httpx.AsyncClient:
        if self._client is None:
            self._client = httpx.AsyncClient(timeout=60.0)
        return self._client

    async def chat(
        self,
        messages: List[Dict[str, str]],
        system: Optional[str] = None,
        temperature: float = 0.3,
        json_mode: bool = False,
    ) -> str:
        """Send chat completion. Returns assistant content string."""
        if not self.is_configured:
            raise RuntimeError(
                "LLM not configured (missing ANTHROPIC_AUTH_TOKEN or ANTHROPIC_MODEL)."
            )

        body: Dict[str, Any] = {
            "model": self.model,
            "messages": ([{"role": "system", "content": system}] if system else []) + messages,
            "temperature": temperature,
        }
        if json_mode:
            body["response_format"] = {"type": "json_object"}

        client = await self._client_get()
        url = f"{self.base_url}/chat/completions"
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
            "HTTP-Referer": "https://github.com/ofa14-prog/bitbing-unity-bridge",
            "X-Title": "BitBing Unity Bridge",
        }
        try:
            r = await client.post(url, json=body, headers=headers)
            r.raise_for_status()
            data = r.json()
            return data["choices"][0]["message"]["content"]
        except httpx.HTTPStatusError as exc:
            logger.error("LLM HTTP %s: %s", exc.response.status_code, exc.response.text[:300])
            raise
        except Exception:
            logger.exception("LLM call failed")
            raise

    async def close(self) -> None:
        if self._client is not None:
            await self._client.aclose()
            self._client = None


_singleton: Optional[LLMClient] = None


def get_llm() -> LLMClient:
    global _singleton
    if _singleton is None:
        _singleton = LLMClient()
    return _singleton
