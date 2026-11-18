#!/usr/bin/env python3
"""Fretlings companion: runs on the user's computer and feeds the headset in Live mode.

    python3 fretlings_companion.py login    # connect the mail, calendar and tasks account (once)
    python3 fretlings_companion.py          # run: serve the headset on the local network
    python3 fretlings_companion.py pair     # show the pairing code to enter in the app's Data Settings
    python3 fretlings_companion.py logout   # forget the account

The headset finds the companion through a UDP announcement on the local network, then reads each source with
GET /v1/sources/<mail|calendar|tasks|agents> and acts with POST /v1/sources/<id>/items/<item>/<read|done|snooze>,
always with the pairing code. Coding agents report to POST /v1/hooks/agent, from this computer only.
Python 3.9+, standard library only.
"""
import json
import pathlib
import secrets
import socket
import sys
import threading
import time
import traceback
import urllib.parse
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

from accounts import Account
from agents import Sessions

HOME = pathlib.Path.home() / ".fretlings"
PORT = 7878
DISCOVERY_PORT = 7879
REFRESH_SECONDS = 60


class Store:
    """~/.fretlings: config.json (pairing code, account client) and tokens.json (the account's refresh token)."""

    def __init__(self):
        HOME.mkdir(mode=0o700, exist_ok=True)
        self.config_path, self.tokens_path = HOME / "config.json", HOME / "tokens.json"
        self.config = self._load(self.config_path)
        self.tokens = self._load(self.tokens_path)
        if "pairing_code" not in self.config:
            self.config["pairing_code"] = f"{secrets.randbelow(1_000_000):06d}"
            self._write(self.config_path, self.config)

    def client(self):
        client = self.config.get("client")
        if not client or not client.get("client_id"):
            raise SystemExit("Add your OAuth client to ~/.fretlings/config.json first (see README.md).")
        return client

    def save_tokens(self, tokens):
        self.tokens = tokens
        self._write(self.tokens_path, tokens)

    @staticmethod
    def _load(path):
        return json.loads(path.read_text()) if path.exists() else {}

    @staticmethod
    def _write(path, data):
        path.write_text(json.dumps(data, indent=2))
        path.chmod(0o600)


class Companion:
    def __init__(self, store):
        self.store = store
        self.account = Account(store)
        self.sessions = Sessions()
        self.cache = {"mail": [], "calendar": [], "tasks": []}

    def items(self, source):
        return self.sessions.items() if source == "agents" else self.cache.get(source)

    def act(self, source, item, action):
        if source == "agents":
            return self.sessions.act(item, action)
        if source == "mail" and action == "read":
            self.account.mark_mail_read(item)
        elif source == "tasks" and action == "done":
            self.account.mark_task_done(item)
        else:
            return False
        self.cache[source] = [i for i in self.cache[source] if i["id"] != item]
        return True

    def refresh_forever(self):
        while True:
            if self.account.connected:
                for source, read in (("mail", self.account.mail), ("calendar", self.account.calendar),
                                     ("tasks", self.account.tasks)):
                    try:
                        self.cache[source] = read()
                    except Exception as error:  # keep the last good list; try again next round
                        print(f"[{source}] refresh failed: {error}")
            time.sleep(REFRESH_SECONDS)


def make_handler(companion, pairing_code):
    class Handler(BaseHTTPRequestHandler):
        def do_GET(self):
            parts = self._parts()
            if len(parts) == 3 and parts[:2] == ["v1", "sources"] and self._paired():
                items = companion.items(parts[2])
                return self._json(404, {}) if items is None else self._json(200, {"source": parts[2], "items": items})
            self._json(404, {})

        def do_POST(self):
            parts = self._parts()
            if parts == ["v1", "hooks", "agent"]:
                if self.client_address[0] not in ("127.0.0.1", "::1"):
                    return self._json(403, {})
                length = int(self.headers.get("Content-Length") or 0)
                payload = json.loads(self.rfile.read(length) or b"{}")
                return self._json(200, {"tracked": companion.sessions.on_hook(payload)})
            if len(parts) == 6 and parts[:2] == ["v1", "sources"] and parts[3] == "items" and self._paired():
                try:
                    done = companion.act(parts[2], urllib.parse.unquote(parts[4]), parts[5])
                except Exception:
                    traceback.print_exc()
                    return self._json(502, {})
                return self._json(200 if done else 409, {})
            self._json(404, {})

        def _paired(self):
            if secrets.compare_digest(self.headers.get("X-Fretlings-Pairing", ""), pairing_code):
                return True
            self._json(401, {"error": "wrong pairing code"})
            return False

        def _parts(self):
            return [p for p in urllib.parse.urlparse(self.path).path.split("/") if p]

        def _json(self, status, body):
            data = json.dumps(body).encode()
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)

        def log_message(self, *args):
            pass

    return Handler


def announce_forever():
    beacon = f"FRETLINGS {PORT}".encode()
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sender:
        sender.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        while True:
            try:
                sender.sendto(beacon, ("255.255.255.255", DISCOVERY_PORT))
            except OSError:
                pass  # no network for now; keep announcing
            time.sleep(2)


def main():
    store = Store()
    command = sys.argv[1] if len(sys.argv) > 1 else "run"
    if command == "login":
        Account(store).sign_in()
    elif command == "logout":
        Account(store).sign_out()
        print("Account forgotten.")
    elif command == "pair":
        print(f"Pairing code: {store.config['pairing_code']}")
    elif command == "run":
        companion = Companion(store)
        threading.Thread(target=companion.refresh_forever, daemon=True).start()
        threading.Thread(target=announce_forever, daemon=True).start()
        server = ThreadingHTTPServer(("0.0.0.0", PORT), make_handler(companion, store.config["pairing_code"]))
        state = "connected" if companion.account.connected else "not connected (run: login)"
        print(f"Fretlings companion on port {PORT}. Account: {state}. Pairing code: {store.config['pairing_code']}")
        server.serve_forever()
    else:
        raise SystemExit(__doc__)


if __name__ == "__main__":
    main()
