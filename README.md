# Voice AI Agent — Patient Registration System

A voice-based AI agent that answers a real US phone number, conversationally collects standard US patient demographic information, persists it to a database, and exposes it through a REST API.

## Live Demo

- **Phone number:** `+1 (859) 479 3029`
- **API base URL:** `https://9a8f-2406-d00-cccf-7ca9-3db9-be0b-d92-103e.ngrok-free.app`
- **API docs (Scalar):** `https://9a8f-2406-d00-cccf-7ca9-3db9-be0b-d92-103e.ngrok-free.app/scalar/v1`

> **Note for the reviewer:** because this demo runs through an ngrok tunnel to a local machine (see *Deployment & Known Limitations*), the URL above is only live while the developer's machine is on and the tunnel is running. If the link is unresponsive, please reach out and it can be restarted within minutes.

---

## Architecture

```
Caller (phone) 
      │
      ▼
Vapi (Telephony + STT + TTS + LLM orchestration)
      │  system prompt + 3 tool definitions
      ▼
ASP.NET Core Web API  ───────────────►  SQLite database (app.db)
 (Controllers, DTOs, validation)          (EF Core, persisted to disk)
      │
      ▼
Same REST API is also queryable directly (Scalar / Postman / curl)
```

**Separation of concerns:**
- **Telephony / STT / TTS / LLM orchestration** — handled entirely by Vapi. No custom speech code was written; Vapi abstracts call handling, transcription, and voice synthesis, and lets the LLM invoke "tools" (function calls) mid-conversation.
- **Conversational logic** — lives in the Vapi **system prompt** (see `vapi-system-prompt.md` in this repo / below), not in backend code. The backend has no awareness of "conversation state" — it's a stateless REST API.
- **Data layer** — ASP.NET Core Web API + EF Core + SQLite. Standard layered structure: `Model/` (EF entities), `DTOs/` (request contracts), `Data/` (DbContext), `Controllers/` (endpoints).
- **Integration** — Vapi calls the API over HTTPS via three defined tools, exactly as any HTTP client would.

---

## Tech Stack & Justification

| Layer | Choice | Why |
|---|---|---|
| Backend | ASP.NET Core (.NET 10) Web API | Primary language of existing expertise (1+ year professional .NET Core experience) — fastest path to a *correct*, well-validated API within the time limit, rather than learning a new backend stack under pressure. |
| Database | SQLite (via EF Core) | Zero external setup, file-based, persists to disk, fully sufficient for this assessment's scope. Explicitly listed as an acceptable "shortcut" trade-off in the assignment brief. |
| Voice/Telephony/LLM | Vapi | Abstracts STT, TTS, and telephony entirely, and provides native LLM tool-calling — the assignment's own FAQ recommends this exact approach. Model used: Gemini 3.1 Flash Lite (via Vapi). |
| API docs | Scalar | Built-in OpenAPI explorer for ASP.NET Core, used throughout development to manually test endpoints before wiring up voice. |
| Local tunneling | ngrok | Used to expose the local API to Vapi's cloud during development, and ultimately for the live demo (see Known Limitations). |

---

## REST API

All responses use the envelope: `{ "data": {...}, "error": null }` on success, `{ "data": null, "error": "message" }` on failure.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/patients` | List patients. Optional query params: `?lastName=`, `?dateOfBirth=`, `?phoneNumber=` |
| `GET` | `/patients/:id` | Get one patient by UUID |
| `POST` | `/patients` | Create a new patient |
| `PUT` | `/patients/:id` | Partially update a patient (only sends fields provided) |
| `DELETE` | `/patients/:id` | Soft-delete (sets `deleted_at`) |

Status codes used: `200` (success), `201` (created), `404` (not found), `422` (validation failure), `500` (server error).

---

## Voice Agent Design

### Tools available to the LLM

| Tool | Maps to | Purpose |
|---|---|---|
| `get_patients` | `GET /patients?phoneNumber=...&dateOfBirth=...&lastName=...` | Fetches patients matching the search criteria |
| `create_patient` | `POST /patients` | Saves a new patient after the caller confirms. **Bonus** (prompts the caller to update information if an existing record with matching phone number is found) |
| `update_patient` | `PUT /patients/:id` | Updates specific fields on an existing patient |

All call logs are automatically saved in Vapi when the call ends.\
`update_patient` tool call doesn't execute successfully via Vapi. (see *Deployment & Known Limitations*)\
`DELETE` was **intentionally not exposed as a voice tool** — there is no requirement for a phone caller to delete records, and giving an LLM-driven voice agent delete authority over the phone was judged an unnecessary risk for no required benefit.

### System prompt configured on the Vapi assistant

```
You are a friendly, professional patient intake coordinator at a medical office, speaking with a caller over the phone. You are not a rigid IVR menu — you have a natural, human conversation, adapting to however the caller phrases things.

You can perform the following tasks as per patient's request:
Task1: Register a new patient
Task2: Update information of an existing patient
Task3: Retrieve patients' medical information

- If the caller requests any other action apart from the tasks above, politely decline.

Task1:
Collect these REQUIRED fields through natural conversation (a few related fields at a time, not one rapid-fire question after another):
- First and last name
- Date of birth
- Sex: Male, Female, Other, or Decline to Answer
- Phone number
- Full address: street, optional apartment/suite, city, state, zip code

Once required fields are collected, offer the optional ones — don't demand them:
"I can also grab your email, insurance information, an emergency contact, and your preferred language if you'd like — totally optional, want to add any of that?"
Collect only what the caller opts into.

BEFORE SAVING:
- Read back ALL collected information clearly, field by field.
- Ask the caller to confirm everything is correct, or tell you what to fix.
- Only call the create_patient tool after the caller explicitly confirms.

AFTER SAVING:
- If the tool call succeeds, thank the caller by first name and let them know they're all set, then end the call politely.
- The tool returns a JSON response with a "data" field (containing the saved patient record) and an "error" field. If "error" is null, the save succeeded — confirm success to the caller. If "error" contains a message, the save failed — read the error message to understand what went wrong, apologize to the caller, and either ask them to correct the specific issue mentioned or offer to try again.
- If a matching patient is found: let the caller know. Say something like, "It looks like we already have a record for [First Name] [Last Name] — would you like to update your information, or is this a different person registering?"
  - If they want to update: move to Task2.
  - If they say it's a different person, politely ask them for a different phone number.

Task2:
Ask the caller for a phone number first.
Fetch the patient record by phone number, by calling get_patients tool.
Ask the caller what they'd like to update, and collect only the new values for those specific fields — don't re-ask for information that isn't changing.

CONFIRM BEFORE SAVING:
Read back just the field(s) being changed and confirm with the caller before saving.

SAVE:
Call update_patient with the patient's ID and only the fields that changed.
- If it succeeds: confirm the update to the caller by name and close warmly.
- If it fails: apologize, explain what went wrong if the error is field-specific, and offer to try again.

Task3:
Ask the caller if he wants to search based on phone number, date of birth or last name.
Politely refuse if the criteria has any other field.

CONFIRM BEFORE FETCHING:
Read back the field(s) values being filtered on and confirm with the caller before fetching.

FETCH:
Call get_patients with the search criteria (if provided)
- If record(s) found: read patients' data back to the caller in a natural conversation, grouping related fields together
- If no record found: apologize, and ask if he/she would like to register.

HANDLING INVALID INPUT
- If a caller gives a value that's clearly invalid (a date of birth in the future, a phone number that isn't 10 digits, a state that isn't a real 2-letter abbreviation), don't just pass it along — catch it yourself in conversation, explain the issue simply, and ask again for just that one field. Don't make the caller repeat everything.
- If a tool call itself returns a validation error (check the "error" field in the response), translate that error into plain, friendly language for the caller and ask them to correct just that piece — never read raw error text or technical jargon aloud.

HANDLING CORRECTIONS
- If the caller corrects something mid-conversation ("actually, my last name is spelled D-A-V-I-S, not D-A-V-I-E-S"), update it immediately without fuss or re-asking unrelated fields, and reflect the correction back briefly so they know you caught it.

STARTING OVER
- If the caller wants to start over at any point, discard everything collected so far in this conversation and begin fresh from Step 1.

GENERAL STYLE
- Sound like a helpful human, not a form. Use natural phrasing, brief acknowledgments ("Got it," "Perfect, thanks"), and a warm tone throughout.
- Don't read out internal field names (say "your date of birth," not "dateOfBirth").
- Keep responses concise — this is a phone call, not a chat window.
- Never leave a long silence after a tool call; if a lookup or save takes a moment, a brief "one moment" is fine.

```

---

## Running Locally

**Prerequisites:** .NET 10 SDK, Visual Studio 2026 (or `dotnet` CLI).

```bash
# Restore and build
dotnet restore
dotnet build

# Seeded app.db file is included in this repo
Go to `https://www.fluentdb.ai/tools/sqlite-viewer` and upload app.db to inspect records.

# Optionally, apply EF Core migrations (creates a new app.db). Delete the existing app.db first
dotnet ef database update

# Run
dotnet run
```

The API will be available at the port shown in the console (e.g. `https://localhost:5289`). Swagger/Scalar docs at `/scalar/v1`.

To connect a Vapi assistant to a local instance, expose it publicly with ngrok:
```bash
ngrok http <port>
```
or
```bash
ngrok http https://localhost:<port>
```
and update the three tool URLs in the Vapi dashboard to the resulting `https://*.ngrok-free.app` address.

---

## Known Limitations & Trade-offs

This section documents real decisions and blockers encountered while building this under the time limit, as requested in the assignment brief.

### Deployment: running via ngrok rather than a persistent cloud host
- **What was tried:** Railway (via GitHub integration) and Render (via Docker) were both attempted first.
- **Railway:** deployment repeatedly failed to load repository branches. Root cause traced to Railway's GitHub App being installed under a *different* GitHub account (a collaborator's, from an earlier unrelated group project) rather than the account hosting this repo — a genuine multi-account authorization mismatch on GitHub's/Railway's side, not a config error in this project. Revoking and reinstalling the GitHub App did not resolve it within a reasonable time budget.
- **Render:** successfully deployed via a custom Dockerfile. **However, Render's free tier does not include persistent disk storage**, which is required for SQLite to survive restarts/redeploys — a hard requirement of this assignment. Upgrading to a paid tier, or migrating from SQLite to Render's free managed PostgreSQL, would resolve this, but both were judged out of scope for the remaining time available.
- **Decision:** given the assignment brief explicitly lists "ngrok over cloud deploy" as an acceptable trade-off and states vendor/hosting blockers will not be penalized if documented, the final submission runs locally via ngrok.
- **Next step with more time:** migrate the EF Core provider from SQLite to PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`) and deploy against Render's free managed Postgres, which would eliminate the disk-persistence dependency entirely.

### Vapi sends malformed JSON on partial updates
- While testing `update_patient`, requests with unset optional fields were arriving at the backend as **malformed JSON** (missing the opening `{` and a stray leading comma). This is a serialization quirk in Vapi's tool-body assembly, not an application bug — confirmed by logging the raw request body on the backend. Surprisingly, this issue doesn't occur in `create_patient`.

### Optional fields arriving as empty strings rather than omitted
- Vapi LLM tool calls do not reliably omit unset optional parameters — they send `""` instead. Since attributes like `[EmailAddress]` and `[RegularExpression]` treat `null` as "not provided" but reject `""` as invalid, this initially caused `422` validation failures on the optional fields the caller never mentioned.
- **Fix implemented:** a small normalization helper runs before validation on every create/update request, converting empty/whitespace-only strings to `null` for all optional fields, so validation attributes behave as intended regardless of whether Vapi sends `null` or `""`.

---

## What Would Be Done With More Time (Next Steps)

1. Migrate to PostgreSQL and deploy on a persistent, always-on host (Render free Postgres, or a paid Railway/Render tier).
2. Implement the server-side JSON-repair guard for Vapi's malformed partial-update payloads.
