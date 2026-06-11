# Triggerly — CLAUDE.md

Workflow automation platform for accounting firms. Tenants trigger multi-step workflows against clients, with Temporal.io managing execution, approvals, and SLA enforcement.

---

## Tech Stack

**Backend** (.NET 8, WSL2)
- ASP.NET Core Web API — `src/Triggerly.Api`
- MediatR CQRS — commands in `src/Triggerly.Application/Commands`, queries in `src/Triggerly.Application/Queries`
- EF Core + SQLite — `AppDbContext` uses `EnsureCreated()` (no migrations; new `DbSet<T>` entries create tables automatically on startup)
- Temporal.io — workflow orchestration via `src/Triggerly.Worker`
- JWT auth with tenant-scoped claims (`tenantId`, `role`)

**Frontend** (React 18 + Vite + TypeScript)
- React Query (`@tanstack/react-query`) for all server state
- React Router v6
- Tailwind CSS
- Axios with JWT interceptor — proxy: Vite `:5173` → API `:5000`

---

## Project Layout

```
src/
  Triggerly.Domain/          # Entities, interfaces, domain logic
  Triggerly.Application/     # MediatR commands, queries, validators
  Triggerly.Infrastructure/  # EF repos, email, Temporal client, auth
  Triggerly.Api/             # Controllers, middleware, Program.cs
  Triggerly.Worker/          # Temporal worker, workflow, activities
  Triggerly.Shared/          # DTOs, contracts, enums, TemplateEngine

frontend/src/
  api/          # Axios API clients (one file per domain)
  hooks/        # React Query hooks (one file per domain)
  pages/        # Route-level page components
  components/   # Shared UI, builder, layout
  types/        # Shared TypeScript interfaces
  contexts/     # AuthContext
```

---

## Build Commands

Always run from `/home/za9perrn/Claude/Triggerly`.

```bash
# Build API
dotnet build src/Triggerly.Api/Triggerly.Api.csproj

# Build Worker
dotnet build src/Triggerly.Worker/Triggerly.Worker.csproj

# Run tests
dotnet test tests/Triggerly.Tests/Triggerly.Tests.csproj

# TypeScript check (frontend)
cd frontend && npx tsc --noEmit

# Start frontend dev server
cd frontend && npm run dev
```

**WSL build cache corruption fix** — when you see "could not read existing file" MSBuild errors:
```bash
find src -name "obj" -type d | xargs rm -rf
```
Then rebuild. This is a recurring WSL2 issue, not a code problem.

**Filter build noise:**
```bash
dotnet build ... 2>&1 | grep -E "error CS|Build succeeded|Build FAILED"
```

---

## Running Locally

Three processes needed:
1. **Temporal** — `temporal server start-dev` (port 7233)
2. **API** — `dotnet run --project src/Triggerly.Api` (port 5000)
3. **Worker** — `dotnet run --project src/Triggerly.Worker`
4. **Frontend** — `cd frontend && npm run dev` (port 5173)

Config files: `src/Triggerly.Api/appsettings.json`, `src/Triggerly.Worker/appsettings.json`

Email is disabled by default (`"Enabled": false`). Set SMTP credentials to enable.

---

## Domain Model

### Entities (all in `src/Triggerly.Domain/Entities/`)

| Entity | Purpose |
|---|---|
| `User` | Tenant member; `CreateNew()` generates new TenantId, `CreateForTenant()` joins existing |
| `TenantRole` | User ↔ tenant role mapping (`Preparer=0`, `Reviewer=1`, `Manager=2`) |
| `WorkflowDefinition` | Workflow template with steps and form schema |
| `WorkflowStep` | Step in a workflow; types: `Action`, `Approval`, `Condition`, `Delay`, `Notification`, `DataTransform`, `Webhook` |
| `WorkflowVersion` | Snapshot of a workflow definition |
| `WorkflowExecution` | Live execution instance; holds `InputData`, `ClientId?`, `ClientServiceId?` |
| `ExecutionStep` | Per-step execution record |
| `ExecutionComment` | Audit comments on executions |
| `AutomationRule` | Scheduled/webhook trigger for a workflow |
| `TeamInvite` | Pending invite with cryptographic token (7-day expiry) |
| `EmailTemplate` | Per-tenant email template override (5 keys) |
| `Client` | Client record with IRD number, balance date |
| `ServiceType` | Type of service offered (e.g. "GST Return") |
| `ClientService` | Client ↔ ServiceType assignment with filing period and due dates |
| `TenantSettings` | Per-tenant config (client data source: Internal/Xero/MYOB) |
| `AuditLog` | Immutable audit trail |

**Multi-tenancy**: no Tenant table — `TenantId` is a `string` column on every entity. All queries must filter by `TenantId`.

---

## Roles & Permissions

| Role | Capabilities |
|---|---|
| `Preparer` | Read-only; complete assigned tasks |
| `Reviewer` | Approve/reject steps; trigger workflows; manage clients |
| `Manager` | Full access — workflows, clients, team, settings, email templates |

Controllers use `[Authorize(Roles = "Manager")]` or `[Authorize(Roles = "Manager,Reviewer")]`.

Frontend: `useRole()` hook exposes `isManager`, `isReviewer`, `canEdit`, `canTrigger`, etc.

---

## Token System (TemplateEngine)

`src/Triggerly.Shared/Utils/TemplateEngine.cs` resolves tokens in step `config` dictionaries before each step runs.

Regex: `\{\{(input|client|service)\.([^}]+)\}\}`

| Token | Source | Example |
|---|---|---|
| `{{input.fieldId}}` | Trigger form fields | `{{input.periodEndDate}}` |
| `{{client.name}}` | Client record | `{{client.irdNumber}}` |
| `{{client.email}}` | Client record | `{{client.balanceDate}}` |
| `{{service.name}}` | ServiceType name | `{{service.filingPeriod}}` |
| `{{service.nextDueAt}}` | ClientService | `{{service.nextDueAt}}` |

Client/service tokens are injected into `InputData` by `TriggerWorkflowCommandHandler` when a client is selected at trigger time.

---

## Email Templates

Five fixed template keys, all in `src/Triggerly.Infrastructure/Services/EmailTemplateService.cs`:

| Key | Triggered by |
|---|---|
| `approval_request` | Approval step starts |
| `approval_reminder` | SLA % elapsed |
| `escalation` | SLA nearing breach |
| `sla_breach` | SLA breached |
| `notification` | Notification step executes |

Tenants can override any template via Settings → Email Templates. `GetRenderedAsync()` prefers the DB row over the hardcoded default.

---

## Workflow Execution Flow

1. `POST /api/workflows/{id}/trigger` → `TriggerWorkflowCommand`
2. Handler enriches `InputData` with `client.*`/`service.*` keys, saves `WorkflowExecution`, starts Temporal workflow
3. `AutomationWorkflow.cs` iterates steps; before each step `TemplateEngine.Resolve()` substitutes tokens in config
4. Activities dispatch by step type (`Approval`, `Notification`, `Webhook`, `DataTransform`, `Delay`, `Condition`; `Action` is a stub)
5. On completion, `CompleteExecutionAsync` sets status and auto-updates `ClientService.LastFiledAt` if the execution had a `ClientServiceId`

**Approval signal**: Temporal waits for `ApprovalSignal` with `{ Approved: bool, Reason?: string }`. Posted via `POST /api/executions/{id}/approve` or `/reject`.

---

## API Controllers

| Controller | Base route |
|---|---|
| `AuthController` | `/api/auth` — register, login, accept-invite |
| `WorkflowsController` | `/api/workflows` — CRUD, trigger, steps, form |
| `ExecutionsController` | `/api/executions` — list, detail, approve, reject, cancel, comment |
| `ClientsController` | `/api/clients` — CRUD + client services |
| `ServiceTypesController` | `/api/service-types` — CRUD |
| `EmailTemplatesController` | `/api/email-templates` — list, upsert, delete (reset) |
| `AutomationRulesController` | `/api/automation-rules` |
| `TeamController` | `/api/team` — members, invites, roles |
| `DashboardController` | `/api/dashboard` |
| `AuditLogsController` | `/api/audit-logs` |
| `WebhooksController` | `/api/webhooks/{token}` — inbound webhook trigger |

---

## Frontend Conventions

- **API clients**: `frontend/src/api/{domain}.ts` — thin wrappers over axios returning typed data
- **Hooks**: `frontend/src/hooks/use{Domain}.ts` — React Query `useQuery`/`useMutation` wrappers
- **Pages**: default exports; route-level only, no logic — delegate to hooks
- **Auth**: `localStorage` key `triggerly_auth` → `{ token, user }`. `AuthContext` exposes `user`, `login`, `logout`, `isAuthenticated`
- **Role checks**: always use `useRole()`, never read `user.role` directly in components

---

## Feature Branch Workflow

```bash
git checkout -b feat/<name> origin/main
# implement
git add <specific files>
git commit -m "feat: ..."
git push -u origin feat/<name>
gh pr create --title "..." --body "..."  # include "Closes #<issue>"
```

Never commit `triggerly.db`, `triggerly.db-shm`, `triggerly.db-wal`, or `dotnet-install.sh`.

---

## Known Issues / Gotchas

- **`EnsureCreated()`**: adds new tables but never alters existing ones. If you add a column to an existing entity, delete `triggerly.db` locally to recreate the schema.
- **`Dictionary<string, object>` from Temporal**: values arrive as `JsonElement`, not raw strings. `TemplateEngine` and all activity code handles this explicitly — don't assume `.ToString()` is safe without checking `ValueKind`.
- **`Action` step type**: currently a stub (logs + delays 100ms). Falls through as the `default` case in `AutomationWorkflow.cs`.
- **Client tokens in email templates**: `EmailTemplateService` builds its own token dict from activity params — it does not read `InputData` from the execution. Client tokens (`{{client.*}}`) are not available in email template bodies yet; only in step configs.
- **WSL2 MSBuild cache**: stale `obj/` folders cause spurious build failures. Run `find src -name "obj" -type d | xargs rm -rf` first.
