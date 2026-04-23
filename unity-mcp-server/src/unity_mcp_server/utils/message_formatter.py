"""
Message formatting utilities for MCP protocol.
"""
import json
from typing import Any, Dict, Optional
from datetime import datetime


class MessageFormatter:
    """Formats messages according to MCP protocol."""

    @staticmethod
    def format_jsonrpc_request(
        method: str,
        params: Optional[Dict[str, Any]] = None,
        request_id: str = "1"
    ) -> str:
        """Format a JSON-RPC 2.0 request."""
        message = {
            "jsonrpc": "2.0",
            "id": request_id,
            "method": method
        }

        if params is not None:
            message["params"] = params

        return json.dumps(message)

    @staticmethod
    def format_jsonrpc_response(
        result: Any,
        request_id: str = "1",
        error: Optional[Dict[str, Any]] = None
    ) -> str:
        """Format a JSON-RPC 2.0 response."""
        message = {
            "jsonrpc": "2.0",
            "id": request_id
        }

        if error is not None:
            message["error"] = error
        else:
            message["result"] = result

        return json.dumps(message)

    @staticmethod
    def format_notification(method: str, params: Optional[Dict[str, Any]] = None) -> str:
        """Format a JSON-RPC 2.0 notification (no id)."""
        message = {
            "jsonrpc": "2.0",
            "method": method
        }

        if params is not None:
            message["params"] = params

        return json.dumps(message)

    @staticmethod
    def parse_message(message: str) -> Dict[str, Any]:
        """Parse a JSON-RPC message."""
        try:
            return json.loads(message)
        except json.JSONDecodeError:
            return {
                "jsonrpc": "2.0",
                "error": {
                    "code": -32700,
                    "message": "Parse error"
                }
            }

    @staticmethod
    def get_timestamp() -> str:
        """Get current timestamp in ISO 8601 format."""
        return datetime.utcnow().isoformat() + "Z"
