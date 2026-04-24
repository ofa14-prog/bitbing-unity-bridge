"""
Vates Agent — gatekeeper + planner.

RED #FF3B3B (KONU.md §4.2)

Two responsibilities, both backed by the LLM:

1) classify_intent(prompt, history)
   Decides whether the user's message should kick off the 6-agent pipeline
   or stay a normal chat reply. Returns:

       {
         "trigger_pipeline": bool,
         "reason": str,            # short rationale, shown in panel logs
         "chat_reply": str|None,   # used when trigger_pipeline=False
       }

   The orchestrator hands chat_reply straight back to the user without
   running diafor/ahbab/etc.

2) plan(prompt, game_type)
   Legacy-compatible analyze() — only called when classify_intent already
   said yes. Asks the LLM to emit the task list in the same shape ahbab
   knows how to dispatch.
"""
from __future__ import annotations

import json
import logging
import re
from typing import Any, Dict, List, Optional

from ..llm_client import get_llm

logger = logging.getLogger(__name__)


# Tasks ahbab knows how to execute — keep the model on the rails.
KNOWN_TASKS: List[str] = [
    "setup_project",
    "create_scene",
    "create_player",
    "create_enemies",
    "create_ui",
    "implement_physics",
    "implement_audio",
    "build_game",
]


_GATE_SYSTEM = """Sen "Vates" adında bir router'sın. BitBing Unity Bridge'in 6-ajanlı oyun
üretim hattının önünde duruyorsun. Sadece JSON döndürürsün.

KARAR KURALI ÇOK BASİT:
- pipeline_start  → Kullanıcı somut bir Unity üretim/değişiklik fiili kullandıysa
  (oluştur/yap/ekle/kur/yaz/build/derle/üret/kodla/sil/değiştir vb.).
  Sahne, oyuncu, düşman, UI, prefab, script, build gibi somut bir şey istiyorsa pipeline başlar.
  Ahbab gerçekten Unity'de iş yapacak — sen "Unity'da nasıl yaparsın" anlatma, sadece tetikle.

- chat_only → Selamlama, soru, açıklama isteme, "evet/hayır/tamam", küçük sohbet,
  "ne yapabilirsin" gibi meta sorular. Bu durumda Türkçe doğal cevap yaz.

ÖRNEKLER:
1) "merhaba" → {"decision":"chat_only","reason":"selamlama","chat_reply":"Merhaba! Sana nasıl yardımcı olabilirim?"}
2) "evet" → {"decision":"chat_only","reason":"kısa onay","chat_reply":"Anladım."}
3) "ne yapabilirsin" → {"decision":"chat_only","reason":"meta soru","chat_reply":"Sana Unity'de sahne, oyuncu, düşman, UI, script üretebilirim. Söyle yeter."}
4) "Sahneye bir küp ekle" → {"decision":"pipeline_start","reason":"somut Unity üretim isteği (küp ekle)","chat_reply":""}
5) "2D platformer sahnesi oluştur" → {"decision":"pipeline_start","reason":"yeni sahne üretim isteği","chat_reply":""}
6) "PlayerController script yaz" → {"decision":"pipeline_start","reason":"script yazma isteği","chat_reply":""}
7) "Build alabilir miyiz?" → {"decision":"chat_only","reason":"soru, henüz emir değil","chat_reply":"Tabii, build başlatmamı ister misin? 'build al' dersen başlatırım."}
8) "build al" → {"decision":"pipeline_start","reason":"build emri","chat_reply":""}

KURAL: Önceki cevabında bir şey önermiş olabilirsin ama kullanıcı sadece "evet" derse
yine chat_only'dir — tek başına onay pipeline tetiklemez, kullanıcı somut emir vermeli.

ÇIKTI FORMATI (sadece bu, başka hiçbir şey):
{"decision":"pipeline_start"|"chat_only","reason":"...","chat_reply":"..."}"""


_PLAN_SYSTEM = f"""Sen "Vates" adlı planlayıcı ajansın. Kullanıcının oyun isteğini, ahbab ajanının
çalıştırabileceği görev DAG'ına dönüştür.

KULLANILABİLİR GÖREV İSİMLERİ (sadece bunlardan seç):
{", ".join(KNOWN_TASKS)}

Cevabını SADECE şu JSON şemasıyla ver:
{{
  "tasks": [
    {{"name": "<KNOWN_TASKS'tan biri>", "description": "kısa açıklama", "priority": 1, "dependencies": []}}
  ]
}}

Öncelik (priority) 1-6 arasında olsun. setup_project hep priority 1, build_game hep
en yüksek priority olsun. Bağımlılıkları doğru kur (örn. create_enemies için
create_player gerekir)."""


def _extract_json(text: str) -> Optional[dict]:
    """Models sometimes wrap JSON in ```json fences. Strip and parse."""
    if not text:
        return None
    text = text.strip()
    fence = re.search(r"```(?:json)?\s*(\{.*?\})\s*```", text, re.DOTALL)
    candidate = fence.group(1) if fence else text
    try:
        return json.loads(candidate)
    except Exception:
        match = re.search(r"\{.*\}", candidate, re.DOTALL)
        if match:
            try:
                return json.loads(match.group(0))
            except Exception:
                return None
    return None


class VatesAgent:
    def __init__(self):
        self._color = "#FF3B3B"
        self._name = "vates"
        self._llm = get_llm()

    async def initialize(self):
        logger.info(
            f"[{self._name}] Initialized (Color: {self._color}, "
            f"llm_configured={self._llm.is_configured})"
        )

    # ── 1) Gatekeeper ───────────────────────────────────────────────────
    async def classify_intent(
        self,
        user_input: str,
        history: Optional[List[Dict[str, str]]] = None,
    ) -> Dict[str, Any]:
        """Return {trigger_pipeline, reason, chat_reply}."""
        history = history or []

        if not self._llm.is_configured:
            return self._heuristic_gate(user_input)

        messages = list(history) + [{"role": "user", "content": user_input}]
        try:
            raw = await self._llm.chat(
                messages, system=_GATE_SYSTEM, temperature=0.1, json_mode=True
            )
        except Exception as exc:
            logger.warning("[vates] LLM gate failed (%s) — falling back to heuristic", exc)
            return self._heuristic_gate(user_input)

        parsed = _extract_json(raw) or {}
        decision = (parsed.get("decision") or "chat_only").lower()
        trigger = decision == "pipeline_start"
        return {
            "trigger_pipeline": trigger,
            "reason": parsed.get("reason") or ("Pipeline gerekli" if trigger else "Sadece sohbet"),
            "chat_reply": (parsed.get("chat_reply") or "").strip() if not trigger else "",
        }

    def _heuristic_gate(self, user_input: str) -> Dict[str, Any]:
        text = user_input.lower().strip()
        bare_acks = {"evet", "hayır", "hayir", "olur", "tamam", "yes", "no", "ok", "okey", "iptal"}
        if text in bare_acks or len(text) <= 3:
            return {"trigger_pipeline": False, "reason": "kısa onay/red — sohbet", "chat_reply": "Anladım."}

        triggers = ("yap", "oluştur", "olustur", "build", "create", "üret", "uret",
                    "kur", "ekle", "tasarla", "geliştir", "gelistir", "code", "kodla")
        if any(t in text for t in triggers):
            return {"trigger_pipeline": True, "reason": "üretim fiili tespit edildi (heuristic)", "chat_reply": ""}
        return {
            "trigger_pipeline": False,
            "reason": "üretim isteği tespit edilmedi (heuristic)",
            "chat_reply": "LLM bağlantısı yok — somut bir üretim isteği yazarsan (ör: 'sahne oluştur', 'oyuncu kontrolü ekle') ajanları başlatabilirim.",
        }

    # ── 2) Planner ──────────────────────────────────────────────────────
    async def plan(self, user_input: str, game_type: str) -> Dict[str, Any]:
        if self._llm.is_configured:
            try:
                raw = await self._llm.chat(
                    [{"role": "user", "content": f"Oyun tipi: {game_type}\nİstek: {user_input}"}],
                    system=_PLAN_SYSTEM,
                    temperature=0.2,
                    json_mode=True,
                )
                parsed = _extract_json(raw) or {}
                tasks = parsed.get("tasks") or []
                tasks = [t for t in tasks if t.get("name") in KNOWN_TASKS]
                if tasks:
                    tasks.sort(key=lambda x: x.get("priority", 99))
                    return self._wrap(tasks, user_input, game_type)
            except Exception as exc:
                logger.warning("[vates] LLM plan failed (%s) — using template", exc)

        return self._wrap(self._template_dag(user_input, game_type), user_input, game_type)

    # Legacy alias for orchestrator compatibility
    async def analyze(self, user_input: str, game_type: str) -> Dict[str, Any]:
        return await self.plan(user_input, game_type)

    def _wrap(self, tasks: List[Dict[str, Any]], user_input: str, game_type: str) -> Dict[str, Any]:
        return {
            "agent": self._name,
            "color": self._color,
            "status": "success",
            "task_dag": tasks,
            "game_type": game_type,
            "input_analyzed": user_input,
        }

    def _template_dag(self, user_input: str, game_type: str) -> List[Dict[str, Any]]:
        text = user_input.lower()
        plan: List[Dict[str, Any]] = [
            {"name": "setup_project", "description": "Unity proje yapısı kur", "priority": 1, "dependencies": []},
            {"name": "create_scene", "description": "Ana sahne", "priority": 2, "dependencies": ["setup_project"]},
            {"name": "create_player", "description": "Oyuncu kontrolü", "priority": 3, "dependencies": ["create_scene"]},
        ]
        if any(k in text for k in ["enemy", "düşman", "dusman", "mob"]):
            plan.append({"name": "create_enemies", "description": "Düşmanlar", "priority": 4, "dependencies": ["create_player"]})
        if any(k in text for k in ["ui", "menu", "hud", "arayüz", "arayuz"]):
            plan.append({"name": "create_ui", "description": "UI elemanları", "priority": 4, "dependencies": ["setup_project"]})
        if any(k in text for k in ["physics", "fizik", "collision"]):
            plan.append({"name": "implement_physics", "description": "Fizik", "priority": 4, "dependencies": ["create_player"]})
        if any(k in text for k in ["audio", "ses", "müzik", "muzik", "sound"]):
            plan.append({"name": "implement_audio", "description": "Ses", "priority": 5, "dependencies": ["create_player"]})
        if any(k in text for k in ["build", "derle", "paketle"]):
            plan.append({"name": "build_game", "description": "Build", "priority": 6, "dependencies": ["create_player"]})
        return plan
