# Fretlings companion

A small program for your computer that gives the headset your real data in **Live mode**: unread mail, upcoming meetings, open tasks, and the state of your AI coding sessions. Python 3.9+, standard library only. Everything stays on your computer and your local network.

## 1. Connect your mail, calendar and tasks

1. Create an OAuth client of type **Desktop app** in your account provider's developer console, with the Mail, Calendar and Tasks APIs enabled.
2. Put it in `~/.fretlings/config.json` (the file is created on first run):

   ```json
   {
     "pairing_code": "123456",
     "client": { "client_id": "…", "client_secret": "…" }
   }
   ```

3. Sign in once, in your computer's browser:

   ```bash
   python3 fretlings_companion.py login
   ```

Scopes: read mail and mark it read, read the calendar, read tasks and mark them done. Nothing else.

## 2. Connect your coding agents

Each agent reports its sessions through hooks. Point them at `agent_hook.py`:

- **Agents with JSON hooks** (events such as `UserPromptSubmit`, `Notification`, `Stop`, `StopFailure`, `SessionEnd`, sent on stdin): add the same command for each of those events in the agent's settings file:

  ```json
  {
    "hooks": {
      "UserPromptSubmit": [{ "hooks": [{ "type": "command", "command": "python3 /path/to/Companion/agent_hook.py" }] }],
      "Notification":     [{ "hooks": [{ "type": "command", "command": "python3 /path/to/Companion/agent_hook.py" }] }],
      "Stop":             [{ "hooks": [{ "type": "command", "command": "python3 /path/to/Companion/agent_hook.py" }] }],
      "StopFailure":      [{ "hooks": [{ "type": "command", "command": "python3 /path/to/Companion/agent_hook.py" }] }],
      "SessionEnd":       [{ "hooks": [{ "type": "command", "command": "python3 /path/to/Companion/agent_hook.py" }] }]
    }
  }
  ```

- **Agents with a notify command** (the event JSON passed as an argument, e.g. `agent-turn-complete`):

  ```toml
  notify = ["python3", "/path/to/Companion/agent_hook.py"]
  ```

Only the session id, the project folder name, the state and the time reach the headset. Prompts and transcripts never leave the hook.

| Event | Session state on the creature |
|---|---|
| prompt submitted, tool use | running (grey bead) |
| notification, permission or approval request | waiting for you (orange bead, lamp on) |
| turn complete | done (blue bead, lamp blinks twice) |
| failure | failed (red bead) |
| session end | removed |

## 3. Run it

```bash
python3 fretlings_companion.py
```

It prints its **pairing code** (also `python3 fretlings_companion.py pair`) and announces itself on the local network, so the headset finds it on its own.

## 4. Switch the headset to Live mode

In Unity, select `Assets/_Project/Settings/Data/DataSettings.asset`:

- **Mode**: `Live`
- **Pairing Code**: the code printed by the companion
- **Companion Host**: leave empty to find it automatically, or enter your computer's IP address

Build and run. Mail, Calendar, Tasks and Agents now follow your real data; Messages stays calm. Switch **Mode** back to `Demo` for the scripted demo.

## API

| Request | Answer |
|---|---|
| `GET /v1/sources/{mail,calendar,tasks,agents}` | `{"source": "...", "items": [{"id", "title", "detail", "time" (ISO 8601), "important", "state"}]}` |
| `POST /v1/sources/{id}/items/{item}/read` | mail: mark read; agents: clear a finished session |
| `POST /v1/sources/tasks/items/{item}/done` | mark the task done |
| `POST /v1/hooks/agent` | hook event JSON, from this computer only |

Every source request carries the `X-Fretlings-Pairing` header. Tests: `python3 -m unittest test_companion`.
