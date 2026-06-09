# Triggerly

Backoffice workflow automation SaaS built with .NET 8, Temporal, and React.

Define multi-step automated workflows with approval gates, notifications, webhooks, delays, and data transforms. Trigger them manually or via automation rules, then monitor execution in real time.

## Stack

| Layer | Technology |
|---|---|
| Frontend | React 18, Vite, TypeScript, Tailwind CSS, React Query |
| API | ASP.NET Core 8, MediatR (CQRS), FluentValidation, Swagger |
| Workflow engine | Temporal (.NET SDK 1.4) |
| Persistence | EF Core 8 + SQLite (shared `triggerly.db` between API and Worker) |
| Auth | JWT Bearer tokens |
| Email | SMTP via `System.Net.Mail`; MailPit for local dev |

## Architecture

```
Triggerly.Shared        # DTOs, enums, Temporal contracts (WorkflowStepInput, ApprovalSignal)
Triggerly.Domain        # DDD aggregates, domain events, repository interfaces
Triggerly.Application   # CQRS commands/queries via MediatR, validation pipeline
Triggerly.Infrastructure # EF Core + SQLite, repositories, TemporalService / StubTemporalService
Triggerly.Api           # ASP.NET Core REST API (port 5000)
Triggerly.Worker        # Temporal worker — workflow + activity implementations
Triggerly.Tests         # xUnit unit tests
frontend/               # React SPA (port 5173)
```

Key design decisions:
- **CQRS** — all reads via Query handlers, all writes via Command handlers, dispatched through `IMediator`
- **Temporal contracts in Shared** — breaks the circular dependency between Infrastructure (Temporal client) and Worker (Temporal implementation)
- **Workflow steps passed in Temporal input** — API serialises step definitions into `AutomationWorkflowInput` at trigger time so the Worker never queries the database for workflow definitions
- **Tenant isolation** — all entities scoped by `TenantId`
- **Stub mode** — set `Temporal:UseStub=true` to run without a Temporal server; simple workflows auto-complete, approval workflows pause at `WaitingApproval`

## Workflow step types

| Type | Description |
|---|---|
| Action | Generic action step with configurable config |
| Approval | Pauses workflow for human approval (configurable SLA timeout) |
| Notification | Sends email notification (channel, recipient, message configurable per step) |
| Delay | Durable sleep for N seconds |
| DataTransform | Maps/transforms context data between steps |
| Webhook | HTTP call to an external endpoint |
| Condition | Conditional branching logic |

## Prerequisites

| Tool | Version | Install |
|---|---|---|
| .NET 8 SDK | 8.0+ | See note below |
| Node.js | 18+ | `nvm install 18` |
| Temporal CLI | latest | `curl -sSf https://temporal.download/cli.sh \| sh` |
| MailPit | latest | See email section below |
| SQLite CLI (optional) | any | `sudo apt install sqlite3` |

> **Ubuntu 26.04 / WSL2 note** — Ubuntu 26.04 is not in the Microsoft package feed.
> Install .NET via the official script instead:
> ```bash
> curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
> echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc && source ~/.bashrc
> sudo apt install -y libicu-dev
> ```

## Run locally

### Option A — With Temporal (full mode)

**Terminal 1 — Temporal dev server**
```bash
~/.temporalio/bin/temporal server start-dev
# UI at http://localhost:8233
```

**Terminal 2 — API**
```bash
dotnet run --project src/Triggerly.Api
# http://localhost:5000  |  Swagger: http://localhost:5000/swagger
```

**Terminal 3 — Worker**
```bash
dotnet run --project src/Triggerly.Worker
# Connects to Temporal on localhost:7233
```

**Terminal 4 — Frontend**
```bash
cd frontend && npm install && npm run dev
# http://localhost:5173
```

### Option B — Stub mode (no Temporal required)

Set `Temporal:UseStub` to `true` in `src/Triggerly.Api/appsettings.Development.json`:

```json
{
  "Temporal": { "UseStub": true }
}
```

Then run only the API and frontend (no Worker needed). Workflows with no Approval step auto-complete; workflows with an Approval step land on `WaitingApproval` for manual approval via the UI.

### Database

Both the API and Worker share a single SQLite file created automatically at the repo root on first run:

```
triggerly.db
```

The schema is created via `EnsureCreated()` on startup — no migrations needed.

To reset all data:
```bash
rm triggerly.db
```

To inspect data:
```bash
sqlite3 triggerly.db ".tables"
sqlite3 triggerly.db "SELECT Id, Status, StartedAt FROM Executions;"
```

## Email (Notification steps)

### Dev — MailPit (captures emails locally, nothing sent to real inboxes)

```bash
# Install pre-built binary
curl -sL https://github.com/axllent/mailpit/releases/latest/download/mailpit-linux-amd64.tar.gz \
  | tar -xz -C ~/go/bin mailpit

# Run — bind on all interfaces so Windows browser can reach it via WSL2 IP
~/go/bin/mailpit --listen 0.0.0.0:8025 --smtp 0.0.0.0:1025
```

Web inbox: **http://\<wsl-ip\>:8025** (get IP with `hostname -I`)

`src/Triggerly.Worker/appsettings.Development.json` already points at MailPit on port 1025.

### Production — real SMTP (e.g. Gmail)

```json
{
  "Email": {
    "Enabled": true,
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "you@gmail.com",
    "Password": "<app-password>",
    "EnableSsl": true,
    "FromAddress": "you@gmail.com",
    "FromName": "Triggerly"
  }
}
```

## API endpoints

| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login, returns JWT |
| GET | `/api/workflows` | List workflows |
| POST | `/api/workflows` | Create workflow |
| PUT | `/api/workflows/{id}/steps` | Save builder steps |
| POST | `/api/workflows/{id}/activate` | Activate workflow |
| POST | `/api/workflows/{id}/deactivate` | Deactivate workflow |
| POST | `/api/workflows/{id}/trigger` | Trigger execution |
| DELETE | `/api/workflows/{id}` | Delete workflow |
| GET | `/api/executions` | List executions |
| GET | `/api/executions/{id}` | Execution detail (steps + comments) |
| POST | `/api/executions/{id}/approve` | Approve pending step |
| POST | `/api/executions/{id}/reject` | Reject pending step |
| POST | `/api/executions/{id}/cancel` | Cancel execution |
| POST | `/api/executions/{id}/comments` | Add comment |
| GET | `/api/automation-rules` | List automation rules |
| POST | `/api/automation-rules` | Create rule |
| POST | `/api/automation-rules/{id}/enable` | Enable rule |
| POST | `/api/automation-rules/{id}/disable` | Disable rule |
| DELETE | `/api/automation-rules/{id}` | Delete rule |
| GET | `/api/auditlogs` | Paginated audit log |
| GET | `/api/dashboard/stats` | Dashboard stats + trend |

Full interactive docs at `http://localhost:5000/swagger`.

## Run tests

```bash
dotnet test tests/Triggerly.Tests
```
