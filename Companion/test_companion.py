"""python3 -m unittest test_companion   (from this folder; uses a temporary home, needs no account)"""
import json
import os
import tempfile
import threading
import unittest
import urllib.error
import urllib.request

os.environ["HOME"] = tempfile.mkdtemp()  # before importing: the store lives in ~/.fretlings

from http.server import ThreadingHTTPServer  # noqa: E402

import fretlings_companion as fc  # noqa: E402
from agents import Sessions  # noqa: E402


class SessionsTest(unittest.TestCase):
    def test_hooks_drive_the_session_state(self):
        sessions = Sessions()
        sessions.on_hook({"hook_event_name": "UserPromptSubmit", "session_id": "a", "cwd": "/code/refactor-auth"})
        sessions.on_hook({"hook_event_name": "Notification", "session_id": "a", "cwd": "/code/refactor-auth"})
        self.assertFalse(sessions.on_hook({"hook_event_name": "SomethingElse", "session_id": "a"}))
        [item] = sessions.items()
        self.assertEqual(("refactor-auth", "waiting", True), (item["title"], item["state"], item["important"]))

        self.assertTrue(sessions.act("a", "read"))
        self.assertEqual(1, len(sessions.items()), "A waiting session needs a reply, not a glance.")
        sessions.on_hook({"type": "agent-turn-complete", "thread-id": "a"})
        self.assertEqual("done", sessions.items()[0]["state"])
        sessions.act("a", "read")
        self.assertEqual([], sessions.items(), "A finished session is cleared once seen.")


class ServerTest(unittest.TestCase):
    def setUp(self):
        self.store = fc.Store()
        self.companion = fc.Companion(self.store)
        self.server = ThreadingHTTPServer(("127.0.0.1", 0), fc.make_handler(self.companion, self.store.config["pairing_code"]))
        threading.Thread(target=self.server.serve_forever, daemon=True).start()
        self.base = f"http://127.0.0.1:{self.server.server_port}"

    def tearDown(self):
        self.server.shutdown()

    def call(self, method, path, body=None, code=None):
        request = urllib.request.Request(self.base + path, method=method,
                                         data=json.dumps(body).encode() if body is not None else None,
                                         headers={"X-Fretlings-Pairing": code or self.store.config["pairing_code"]})
        try:
            with urllib.request.urlopen(request) as response:
                return response.status, json.loads(response.read() or b"{}")
        except urllib.error.HTTPError as error:
            return error.code, {}

    def test_headset_reads_agents_and_acts(self):
        self.call("POST", "/v1/hooks/agent", {"hook_event_name": "Stop", "session_id": "s1", "cwd": "/w/docs-site"})
        status, body = self.call("GET", "/v1/sources/agents")
        self.assertEqual(200, status)
        self.assertEqual(["docs-site"], [i["title"] for i in body["items"]])
        self.assertEqual(401, self.call("GET", "/v1/sources/agents", code="000000")[0], "The pairing code is checked.")
        self.assertEqual(200, self.call("POST", "/v1/sources/agents/items/s1/read")[0])
        self.assertEqual([], self.call("GET", "/v1/sources/agents")[1]["items"])
        self.assertEqual(200, self.call("GET", "/v1/sources/mail")[0], "Mail answers even before sign-in.")
        self.assertEqual(404, self.call("GET", "/v1/sources/unknown")[0])


if __name__ == "__main__":
    unittest.main()
