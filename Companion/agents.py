"""Coding-agent sessions, fed by the agents' hooks (see README.md).

Only what the headset needs leaves this module: a session id, its project folder name, its state and since when.
Prompts, tool calls and transcripts are never stored or forwarded.
"""
import os
import threading
import time

RUNNING, WAITING, DONE, FAILED = "running", "waiting", "done", "failed"

DETAILS = {
    RUNNING: "Working",
    WAITING: "Needs your reply",
    DONE: "Finished",
    FAILED: "Stopped with an error",
}

# Hook event names, as sent by the agents, mapped to a session state. None ends the session.
EVENTS = {
    "UserPromptSubmit": RUNNING,
    "PreToolUse": RUNNING,
    "PostToolUse": RUNNING,
    "Notification": WAITING,
    "PermissionRequest": WAITING,
    "approval-requested": WAITING,
    "Stop": DONE,
    "agent-turn-complete": DONE,
    "StopFailure": FAILED,
    "error": FAILED,
    "SessionEnd": None,
}


class Sessions:
    def __init__(self):
        self._lock = threading.Lock()
        self._sessions = {}

    def on_hook(self, payload):
        """Applies one hook event. Returns False when the event is not one we track."""
        event = payload.get("hook_event_name") or payload.get("type") or ""
        if event not in EVENTS:
            return False
        session = str(payload.get("session_id") or payload.get("thread-id") or payload.get("turn-id") or "")
        if not session:
            return False
        state = EVENTS[event]
        with self._lock:
            if state is None:
                self._sessions.pop(session, None)
                return True
            previous = self._sessions.get(session)
            if previous and previous["state"] == state:
                return True
            folder = os.path.basename(os.path.normpath(payload.get("cwd") or "")) or "session"
            self._sessions[session] = {"id": session, "title": folder, "state": state, "since": time.time()}
        return True

    def items(self):
        with self._lock:
            sessions = sorted(self._sessions.values(), key=lambda s: s["since"])
        return [{
            "id": s["id"],
            "title": s["title"],
            "detail": DETAILS[s["state"]],
            "time": iso(s["since"]),
            "important": s["state"] == WAITING,
            "state": s["state"],
        } for s in sessions]

    def act(self, session, action):
        """'read' (seen) clears a finished or failed session; a running or waiting one needs the user's reply."""
        with self._lock:
            current = self._sessions.get(session)
            if current is None:
                return False
            if action == "read" and current["state"] in (DONE, FAILED):
                del self._sessions[session]
            return True


def iso(timestamp):
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime(timestamp))
