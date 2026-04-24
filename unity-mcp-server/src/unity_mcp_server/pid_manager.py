"""
PID-file manager for the chat server, adapted from COPLAY's PidFileManager.
Lets a freshly-launched server detect a stale predecessor and avoid the
[Errno 10048] "address already in use" trap.
"""
from __future__ import annotations

import os
import tempfile
from pathlib import Path
from typing import Optional

_PID_FILE = Path(tempfile.gettempdir()) / "bitbing" / "chat-server.pid"


def write(pid: int) -> None:
    try:
        _PID_FILE.parent.mkdir(parents=True, exist_ok=True)
        _PID_FILE.write_text(str(int(pid)), encoding="utf-8")
    except Exception:
        pass


def read() -> Optional[int]:
    try:
        if _PID_FILE.exists():
            return int(_PID_FILE.read_text(encoding="utf-8").strip())
    except Exception:
        return None
    return None


def is_alive(pid: int) -> bool:
    if pid <= 0:
        return False
    try:
        if os.name == "nt":
            import ctypes
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
            kernel32 = ctypes.windll.kernel32
            h = kernel32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, pid)
            if not h:
                return False
            try:
                exit_code = ctypes.c_ulong(0)
                kernel32.GetExitCodeProcess(h, ctypes.byref(exit_code))
                return exit_code.value == 259  # STILL_ACTIVE
            finally:
                kernel32.CloseHandle(h)
        else:
            os.kill(pid, 0)
            return True
    except Exception:
        return False


def clear() -> None:
    try:
        if _PID_FILE.exists():
            _PID_FILE.unlink()
    except Exception:
        pass


def claim_or_exit() -> None:
    """
    Called at server startup. If a live PID exists, abort cleanly.
    Otherwise stamp our own PID into the file.
    """
    existing = read()
    if existing and is_alive(existing) and existing != os.getpid():
        raise SystemExit(
            f"Chat server already running (PID {existing}). "
            f"Stop it first or delete {_PID_FILE}."
        )
    write(os.getpid())
