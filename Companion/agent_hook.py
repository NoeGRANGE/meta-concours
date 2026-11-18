#!/usr/bin/env python3
"""Forwards a coding agent's hook event to the Fretlings companion (see README.md).

As a stdin hook:   python3 agent_hook.py            < event JSON on stdin
As a notify hook:  python3 agent_hook.py '<json>'     (the event JSON as the first argument)

Never blocks or fails the agent: if the companion is not running, the event is dropped.
"""
import json
import os
import sys
import urllib.request

URL = os.environ.get("FRETLINGS_COMPANION", "http://127.0.0.1:7878") + "/v1/hooks/agent"


def main():
    raw = sys.argv[1] if len(sys.argv) > 1 else sys.stdin.read()
    try:
        payload = json.loads(raw or "{}")
    except ValueError:
        return
    payload.setdefault("cwd", os.getcwd())
    request = urllib.request.Request(URL, data=json.dumps(payload).encode(), method="POST",
                                     headers={"Content-Type": "application/json"})
    try:
        urllib.request.urlopen(request, timeout=2).close()
    except OSError:
        pass


if __name__ == "__main__":
    main()
