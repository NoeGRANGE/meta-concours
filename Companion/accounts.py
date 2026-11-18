"""The user's mail, calendar and tasks account: sign-in in the computer's browser, then read-only lists and the two
actions the app offers (mark a mail read, mark a task done). Tokens stay in ~/.fretlings on this computer.
"""
import base64
import hashlib
import http.server
import json
import secrets
import threading
import time
import urllib.parse
import urllib.request
import webbrowser

AUTH_URL = "https://accounts.google.com/o/oauth2/v2/auth"
TOKEN_URL = "https://oauth2.googleapis.com/token"
SCOPES = [
    "https://www.googleapis.com/auth/gmail.modify",       # read unread mail, mark it read
    "https://www.googleapis.com/auth/calendar.readonly",  # upcoming meetings
    "https://www.googleapis.com/auth/tasks",              # open tasks, mark them done
]
GMAIL = "https://gmail.googleapis.com/gmail/v1/users/me"
CALENDAR = "https://www.googleapis.com/calendar/v3/calendars/primary/events"
TASKS = "https://tasks.googleapis.com/tasks/v1/lists/@default/tasks"


class Account:
    def __init__(self, store):
        self.store = store
        self._access = None
        self._expires = 0.0
        self._lock = threading.Lock()

    @property
    def connected(self):
        return bool(self.store.tokens.get("refresh_token"))

    # ---- sign-in: authorization code with PKCE and a loopback redirect, in the computer's browser ----

    def sign_in(self):
        client = self.store.client()
        verifier = secrets.token_urlsafe(64)
        challenge = base64.urlsafe_b64encode(hashlib.sha256(verifier.encode()).digest()).rstrip(b"=").decode()
        received = {}

        class Redirect(http.server.BaseHTTPRequestHandler):
            def do_GET(self):
                received.update(urllib.parse.parse_qs(urllib.parse.urlparse(self.path).query))
                self.send_response(200)
                self.send_header("Content-Type", "text/html; charset=utf-8")
                self.end_headers()
                self.wfile.write("<p>Fretlings is connected. You can close this tab.</p>".encode())

            def log_message(self, *args):
                pass

        server = http.server.HTTPServer(("127.0.0.1", 0), Redirect)
        redirect = f"http://127.0.0.1:{server.server_port}"
        webbrowser.open(AUTH_URL + "?" + urllib.parse.urlencode({
            "client_id": client["client_id"], "redirect_uri": redirect, "response_type": "code",
            "scope": " ".join(SCOPES), "code_challenge": challenge, "code_challenge_method": "S256",
            "access_type": "offline", "prompt": "consent",
        }))
        print("Waiting for you to sign in in the browser…")
        while "code" not in received and "error" not in received:
            server.handle_request()
        server.server_close()
        if "error" in received:
            raise RuntimeError(f"Sign-in refused: {received['error'][0]}")

        tokens = self._token_request({
            "grant_type": "authorization_code", "code": received["code"][0], "redirect_uri": redirect,
            "code_verifier": verifier, "client_id": client["client_id"], "client_secret": client["client_secret"],
        })
        self.store.save_tokens({"refresh_token": tokens["refresh_token"]})
        self._remember(tokens)
        print("Connected.")

    def sign_out(self):
        self.store.save_tokens({})
        self._access = None

    # ---- the three sources ----

    def mail(self):
        """Unread inbox mail of the last two days; 'important' follows the provider's Important label."""
        found = self._get(f"{GMAIL}/messages", {"q": "is:unread in:inbox newer_than:2d", "maxResults": 25})
        items = []
        for message in found.get("messages", []):
            meta = self._get(f"{GMAIL}/messages/{message['id']}",
                             {"format": "metadata", "metadataHeaders": ["Subject", "From"]})
            headers = {h["name"]: h["value"] for h in meta.get("payload", {}).get("headers", [])}
            items.append({
                "id": meta["id"],
                "title": headers.get("Subject", "(no subject)"),
                "detail": _sender(headers.get("From", "")),
                "time": _iso(int(meta.get("internalDate", "0")) / 1000),
                "important": "IMPORTANT" in meta.get("labelIds", []),
                "state": "",
            })
        return items

    def calendar(self):
        """Timed meetings of the next twelve hours."""
        now = time.time()
        found = self._get(CALENDAR, {"timeMin": _iso(now), "timeMax": _iso(now + 12 * 3600), "singleEvents": "true",
                                     "orderBy": "startTime", "maxResults": 10})
        items = []
        for event in found.get("items", []):
            start = event.get("start", {}).get("dateTime")
            if not start:
                continue  # all-day events have no time to worry about
            organizer = event.get("organizer", {})
            items.append({
                "id": event["id"],
                "title": event.get("summary", "Meeting"),
                "detail": "with " + (organizer.get("displayName") or organizer.get("email") or "you"),
                "time": start,
                "important": False,
                "state": "",
            })
        return items

    def tasks(self):
        """Open tasks that have a due date."""
        found = self._get(TASKS, {"showCompleted": "false", "maxResults": 50})
        return [{
            "id": task["id"],
            "title": task.get("title", "Task"),
            "detail": "Tasks",
            "time": task["due"],
            "important": False,
            "state": "",
        } for task in found.get("items", []) if task.get("due")]

    def mark_mail_read(self, message_id):
        self._send("POST", f"{GMAIL}/messages/{message_id}/modify", {"removeLabelIds": ["UNREAD"]})

    def mark_task_done(self, task_id):
        self._send("PATCH", f"{TASKS}/{task_id}", {"status": "completed"})

    # ---- HTTP with a fresh access token ----

    def _get(self, url, params):
        query = urllib.parse.urlencode(params, doseq=True)
        return self._send("GET", f"{url}?{query}")

    def _send(self, method, url, body=None):
        data = json.dumps(body).encode() if body is not None else None
        request = urllib.request.Request(url, data=data, method=method, headers={
            "Authorization": f"Bearer {self._token()}", "Content-Type": "application/json"})
        with urllib.request.urlopen(request, timeout=15) as response:
            content = response.read()
        return json.loads(content) if content else {}

    def _token(self):
        with self._lock:
            if self._access and time.time() < self._expires - 60:
                return self._access
            client = self.store.client()
            self._remember(self._token_request({
                "grant_type": "refresh_token", "refresh_token": self.store.tokens["refresh_token"],
                "client_id": client["client_id"], "client_secret": client["client_secret"]}))
            return self._access

    def _remember(self, tokens):
        self._access = tokens["access_token"]
        self._expires = time.time() + int(tokens.get("expires_in", 3600))

    @staticmethod
    def _token_request(fields):
        request = urllib.request.Request(TOKEN_URL, data=urllib.parse.urlencode(fields).encode(), method="POST")
        with urllib.request.urlopen(request, timeout=15) as response:
            return json.loads(response.read())


def _iso(timestamp):
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime(timestamp))


def _sender(header):
    """'Maya Lindqvist <maya@example.com>' -> 'Maya Lindqvist'."""
    name = header.split("<")[0].strip().strip('"')
    return name or header
