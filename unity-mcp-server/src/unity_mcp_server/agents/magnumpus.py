"""
Magnumpus Agent - Packages all outputs and delivers to user.
ORANGE #FF6D00 - KONU.md §4.2

Uses the LLM to write a natural-language Türkçe summary at the end of the
pipeline so the user gets a conversational reply instead of a raw scoreboard.
"""
import logging
from typing import Any, Dict, List

from ..llm_client import get_llm

logger = logging.getLogger(__name__)


_SUMMARY_SYSTEM = """Sen "Magnumpus" adlı teslimat ajansın. Az önce 6-ajanlı bir Unity oyun
üretim hattı çalıştı. Sana JSON olarak: kullanıcının orijinal isteği,
çalıştırılan görevler, ahbab'ın komut sonuçları, hata sayısı ve patientia'nın
puanı verilecek.

Görevin: kullanıcıya 2-4 cümlelik, samimi, Türkçe bir özet yaz.
- Markdown tablo, başlık, bullet KULLANMA. Düz akıcı metin yaz.
- Ne yapıldığını insancıl bir dille özetle.
- Puanı doğal bir cümlenin içinde geçir.
- Hata varsa bunu yumuşak bir dille söyle ve bir sonraki adım için kısa öneri ver.
- Maksimum 4 cümle. Emoji 1 tane yeterli, başta olabilir."""


class MagnumpusAgent:
    """Packages outputs and delivers to user."""

    def __init__(self):
        self._color = "#FF6D00"
        self._name = "magnumpus"
        self._llm = get_llm()

    async def initialize(self):
        """Initialize the agent."""
        logger.info(f"[{self._name}] Initialized (Color: {self._color})")

    async def package(
        self,
        task_dag: List[Dict],
        ahbab_results: List[Dict],
        patientia_result: Dict,
        user_input: str = "",
        obsidere_result: Dict | None = None,
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
        else:
            result["status"] = "needs_revision"

        # Natural-language Türkçe özet — kullanıcının chat baloncuğunda göreceği metin
        result["delivery_message"] = await self._llm_summary(
            user_input=user_input,
            task_dag=task_dag,
            ahbab_results=ahbab_results,
            patientia_result=patientia_result,
            obsidere_result=obsidere_result or {},
            package=package,
        )

        logger.info(f"[{self._name}] Packaging complete: {package['files_created']} files")
        return result

    async def _llm_summary(
        self,
        user_input: str,
        task_dag: List[Dict],
        ahbab_results: List[Dict],
        patientia_result: Dict,
        obsidere_result: Dict,
        package: Dict,
    ) -> str:
        """Ask the LLM to write a 2-4 sentence Türkçe summary. Falls back to a
        warm template when the model is unreachable."""
        score = package.get("final_score", 0)
        grade = package.get("grade", "F")
        files = package.get("files_created", 0)
        components = package.get("components_created", 0)
        error_count = obsidere_result.get("error_count", 0)
        executed = [r.get("task") for r in ahbab_results if r.get("task")]

        if self._llm.is_configured:
            try:
                payload = {
                    "kullanici_istegi": user_input,
                    "calistirilan_gorevler": executed,
                    "olusturulan_dosya_sayisi": files,
                    "olusturulan_bilesen_sayisi": components,
                    "tespit_edilen_hata": error_count,
                    "puan": score,
                    "not": grade,
                }
                import json as _json
                user_msg = (
                    "Pipeline tamamlandı. Aşağıdaki sonuçları samimi bir Türkçe "
                    "cevaba dönüştür:\n\n```json\n"
                    + _json.dumps(payload, ensure_ascii=False, indent=2)
                    + "\n```"
                )
                text = await self._llm.chat(
                    [{"role": "user", "content": user_msg}],
                    system=_SUMMARY_SYSTEM,
                    temperature=0.6,
                )
                text = (text or "").strip()
                if text:
                    return text
            except Exception as exc:
                logger.warning("[magnumpus] LLM summary failed (%s) — using fallback", exc)

        return self._fallback_summary(user_input, executed, score, grade, error_count)

    @staticmethod
    def _fallback_summary(user_input: str, executed: List[str], score: int, grade: str, errors: int) -> str:
        head = "🎉" if score >= 80 else "✅" if score >= 60 else "⚠️" if score >= 40 else "🛟"
        task_list = ", ".join(executed) if executed else "birkaç temel adım"
        err_part = (
            f" Yolda {errors} küçük pürüz çıktı, gerekirse tekrar denetleyebilirim."
            if errors > 0 else ""
        )
        return (
            f"{head} İsteğin için {task_list} adımlarını çalıştırdım ve sonuçlandırdım. "
            f"Genel kalite puanı {score}/100 ({grade}).{err_part} "
            f"Detayları Unity Console'dan da takip edebilirsin."
        )

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
