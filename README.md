# Triggerly

Backoffice workflow automation SaaS built with .NET 8, Temporal, and React.

Define multi-step automated workflows with approval gates, notifications, webhooks, delays, and data transforms. Trigger them manually or via automation rules, then monitor execution in real time.

## Stack

| Layer | Technology |
|---|---|
| Frontend | React 18, Vite, TypeScript, Tailwind CSS, React Query |
| API | ASP.NET Core 8, MediatR (CQRS), FluentValidation, Swagger |
| Workflow engine | Temporal (.NET SDK) |
| Persistence | EF Core (InMemory by default, swap to SQL Server) |
| Tests | xUnit, 42 tests covering domain + command/query handlers |

## Architecture

```
Triggerly.Shared        # DTOs, enums, Temporal contracts
Triggerly.Domain        # DDD aggregates, domain events, repository interfaces
Triggerly.Application   # CQRS commands/queries via MediatR, validation pipeline
Triggerly.Infrastructure # EF Core, repositories, TemporalService client
Triggerly.Api           # ASP.NET Core REST API (port 5000)
Triggerly.Worker        # Temporal worker — workflow + activity implementations
Triggerly.Tests         # xUnit unit tests
frontend/               # React SPA (port 5173)
```

Key design decisions:
- **CQRS** — all reads via Query handlers, all writes via Command handlers, dispatched through `IMediator`
- **Temporal contracts in Shared** — breaks the circular dependency between Infrastructure (Temporal client) and Worker (Temporal implementation)
- **Tenant isolation** — all entities scoped by `TenantId`
- **Approval gates** — durable 72-hour `WaitConditionAsync` in the Temporal workflow; signal-based approval/rejection

## Workflow step types

| Type | Description |
|---|---|
| Action | Generic action step with configurable config |
| Approval | Pauses workflow for human approval (72h timeout) |
| Notification | Sends email/Slack/webhook notification |
| Delay | Durable sleep for N seconds |
| DataTransform | Maps/transforms context data between steps |
| Webhook | HTTP call to an external endpoint |
| Condition | Conditional branching logic |

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Temporal CLI](https://docs.temporal.io/cli)
- Node.js 18+ (via [nvm](https://github.com/nvm-sh/nvm) recommended on WSL)

### Run locally

**1. Start Temporal dev server**
```bash
temporal server start-dev
```

**2. Start the API**
```bash
dotnet run --project src/Triggerly.Api
# Listening on http://localhost:5000
# Swagger at http://localhost:5000/swagger
```

**3. Start the Worker**
```bash
dotnet run --project src/Triggerly.Worker
# Connects to Temporal on localhost:7233
```

**4. Start the frontend**
```bash
cd frontend
npm install
npm run dev
# http://localhost:5173
```

### Run tests

```bash
dotnet test tests/Triggerly.Tests
```

## API endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/workflows` | List workflows |
| POST | `/api/workflows` | Create workflow |
| POST | `/api/workflows/{id}/activate` | Activate workflow |
| POST | `/api/workflows/{id}/trigger` | Trigger execution |
| GET | `/api/executions` | List executions |
| POST | `/api/executions/{id}/approve` | Approve pending step |
| POST | `/api/executions/{id}/reject` | Reject pending step |
| GET | `/api/automation-rules` | List automation rules |
| GET | `/api/dashboard/stats` | Dashboard stats + trend |

Full interactive docs at `http://localhost:5000/swagger`.

## Project structure

```
Triggerly/
├── src/
│   ├── Triggerly.Shared/
│   │   ├── Contracts/       # IAutomationWorkflow, ApprovalSignal, TemporalConstants
│   │   ├── DTOs/            # WorkflowDto, ExecutionDto, AutomationRuleDto
│   │   ├── Events/          # Integration events
│   │   └── Models/          # WorkflowStatus, ExecutionStatus, StepType enums
│   ├── Triggerly.Domain/
│   │   ├── Entities/        # WorkflowDefinition, WorkflowExecution, AutomationRule, ...
│   │   ├── Events/          # Domain events
│   │   └── Interfaces/      # Repository + UnitOfWork interfaces
│   ├── Triggerly.Application/
│   │   ├── Commands/        # Create/Update/Trigger/Approve/Reject/Cancel handlers
│   │   ├── Queries/         # List/GetById/Dashboard query handlers
│   │   ├── Behaviors/       # FluentValidation MediatR pipeline
│   │   └── Validators/      # Command validators
│   ├── Triggerly.Infrastructure/
│   │   ├── Persistence/     # AppDbContext, EF configurations
│   │   ├── Repositories/    # EF repository implementations
│   │   └── Temporal/        # TemporalService (start/signal/cancel workflows)
│   ├── Triggerly.Api/
│   │   ├── Controllers/     # Workflows, Executions, AutomationRules, Dashboard
│   │   └── Middleware/      # Global exception handling
│   └── Triggerly.Worker/
│       ├── Workflows/       # AutomationWorkflow (Temporal workflow implementation)
│       └── Activities/      # WorkflowActivities, NotificationActivities, DataActivities
├── tests/
│   └── Triggerly.Tests/     # 42 xUnit tests
└── frontend/
    └── src/
        ├── pages/           # Dashboard, Workflows, Executions, AutomationRules
        ├── components/      # Layout, Sidebar, UI primitives
        ├── hooks/           # React Query hooks with live polling
        └── api/             # Axios API clients
```
