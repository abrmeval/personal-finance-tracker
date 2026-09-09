# AGENTS.md — Personal Finance Tracker

Guidance for agentic coding agents operating in this repository. When unsure about a pattern, read the relevant `docs/` file and existing code first — do not invent approaches.

---

## Project Overview

Full-stack personal finance app (monorepo):
- **Backend** — ASP.NET 10 modular monolith (`backend/`, solution `Personal.FinanceTracker.slnx`). Modules **Users** (JWT auth: register, login, refresh, revoke) and **Finance** (transactions, categories, budgets) are implemented; **Reporting** is planned (Sprint 4).
- **Frontend** — React 19 + Vite 7 + TypeScript (`frontend/`), feature-based structure, TanStack Query for server state.
- **Database** — PostgreSQL; local via Docker Compose (`infrastructure/`), Neon is the production target (Sprint 6).
- Work is sprint-driven — check `docs/ai/sprints/SPRINTS-OVERVIEW.md` for current status before planning changes.

## Tech Stack

- Frontend: React 19, TypeScript 5.9 (strict), Vite 7, Tailwind CSS 4 (Vite plugin), TanStack Query 5, React Hook Form 7 + Zod 4, React Router 7, **native fetch** (not axios), Recharts 3, Lucide React, date-fns, clsx + tailwind-merge
- Backend: ASP.NET 10 Minimal APIs (no MVC controllers), EF Core 10 + Npgsql, FluentValidation 12, JWT Bearer, Microsoft.AspNetCore.OpenApi + Scalar (dev-only API reference)
- Tests: xUnit (`Finance.UnitTests`, `Users.UnitTests`); TestContainers integration tests planned (Sprint 5)
- CI: GitHub Actions — build, lint, format check (warn-only), PR standard enforcement

---

## Commands

### Frontend (`frontend/`)

```bash
copy .env.example .env   # REQUIRED first — dev/build load .env via dotenv-cli and fail without it
npm install
npm run dev              # Vite dev server at http://localhost:3000 (proxy /api/* -> http://localhost:5194)
npm run build            # Type-check (tsc -b) then production build — run before finishing any frontend work
npm run build-lint       # ESLint + type-check + build (full verification)
npm run lint             # ESLint only
npm run serve            # Build, then serve the production build
```

- There is **no `npm test` script yet** (Vitest/MSW/Testing Library are installed as devDependencies; scripts arrive with Sprint 5).
- `.env` contains `VITE_API_URL=/api` and `VITE_ENVIRONMENT`. Never commit `.env` files.

### Backend (`backend/`)

```bash
dotnet build backend/Personal.FinanceTracker.slnx                          # Build solution
dotnet run --project backend/src/Personal.FinanceTracker.Api              # API at http://localhost:5194
dotnet test backend/Personal.FinanceTracker.slnx                          # Run all tests
dotnet test backend/tests/Finance.UnitTests                               # Run one test project
dotnet test backend/Personal.FinanceTracker.slnx --filter "FullyQualifiedName~MyMethodName"  # One test
dotnet format backend/Personal.FinanceTracker.slnx                        # Format C# code
dotnet format backend/Personal.FinanceTracker.slnx --verify-no-changes --severity warn      # What CI checks
```

- API docs: Scalar UI at `http://localhost:5194/scalar` (Development only — not Swagger).
- Health: `http://localhost:5194/health/live` and `/health/ready`.

### EF Core Migrations (two DbContexts — one per module, both must be applied)

Run from `backend/`:

```bash
# Finance module
dotnet ef migrations add <Name> --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj --context FinanceDbContext --output-dir Infrastructure/Data/Migrations
dotnet ef database update --project src/Modules/Finance/Personal.FinanceTracker.Finance.csproj --startup-project src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj --context FinanceDbContext

# Users module: --project src/Modules/Users/Personal.FinanceTracker.Users.csproj --context UsersDbContext
```

Requires the `dotnet-ef` global tool (`dotnet tool install --global dotnet-ef`).

### Local environment

- **Database:** `cd infrastructure && copy .env.example .env` (set `POSTGRES_USER/PASSWORD/DB`), then `docker compose up -d` — postgres:18 on `localhost:5432`.
- **Backend secrets:** `backend/src/Personal.FinanceTracker.Api/appsettings.Local.json` (gitignored, explicitly loaded in `Program.cs`). There is no User Secrets setup — do not use `dotnet user-secrets`.
- **Task runner (Windows + Windows Terminal):** `task local` (frontend preview + backend), `task local-debug` (Vite dev + backend), `task backend-format`. Alternative: `python run.py` opens both apps in terminal tabs.

---

## Boundaries

**Always do:**
- Read the sprint doc (`docs/ai/sprints/sprint-N.md`) and relevant `docs/` file before starting work; read `docs/ai/ui-design-rules.md` before any frontend UI work.
- Verify with `dotnet build` + `npm run build` (and `npm run lint`) before finishing — every sprint's success criteria require clean builds.
- Register new modules/endpoints via `AddXxxModule(...)` / `MapXxxEndpoints(...)` in the module's `DependencyInjection.cs`, wired in `Program.cs`.
- When completing a sprint, update its status in both the sprint file header and `SPRINTS-OVERVIEW.md`.

**Ask first:**
- Changing auth token storage — the current `localStorage` approach is a known deferred audit finding (C-1, tracked in `SPRINTS-OVERVIEW.md` Known Gaps); refactor is owner-scheduled, not ad hoc.
- Re-enabling `TreatWarningsAsErrors` (commented out in `Directory.Build.props`; 6 pre-existing warnings in test projects — fix warnings when touched, don't let new warnings in).

**Never do:**
- Store sensitive data (JWTs, refresh tokens, passwords, full user objects) in `localStorage`/`sessionStorage` in new code.
- Create sprint documents outside `docs/ai/sprints/`.
- Commit secrets (`appsettings.Local.json`, `.env` files are gitignored — keep it that way).
- Use `axios` for new API code — the fetch-based `apiClient` in `src/api/client.ts` is the standard (the `axios` dependency is unused; do not add usages).
- Put business logic in endpoints — endpoints are thin; logic belongs in `Application` services.

---

## Project Structure

```
backend/
├── Personal.FinanceTracker.slnx              # Solution (XML format)
├── Directory.Build.props                     # Nullable, ImplicitUsings, NetAnalyzers
├── src/
│   ├── Personal.FinanceTracker.Api/          # Host — Program.cs, appsettings.Local.json (gitignored)
│   ├── Personal.FinanceTracker.Shared/       # ExceptionHandlingMiddleware, ValidationFilter<T>,
│   │                                         # Entity, NotFoundException, ApiResponse<T>, ApiErrorCode
│   └── Modules/
│       ├── Users/                            # Auth (users.* schema)
│       └── Finance/                          # Transactions, Categories, Budgets (finances.* schema)
│           └── Domain / Application / Infrastructure / Api + DependencyInjection.cs
└── tests/                                    # Finance.UnitTests, Users.UnitTests (xUnit)

frontend/src/
├── api/          # client.ts (fetch apiClient) + auth/budgets/categories/transactions modules
├── components/   # Shared UI (auth/ — AuthProvider, layout/ — Header/Sidebar/MainLayout)
├── features/     # auth/, transactions/, categories/, budgets/ — co-located pages/hooks/components/schemas
├── hooks/  pages/  routes/  types/  utils/

infrastructure/   # docker-compose.yml + .env.example (local PostgreSQL)
docs/             # Architecture docs, sprint plans (see Documentation below)
.opencode/        # opencode config: commands/role.md, skills/dotnet-best-practices
.github/skills/   # finance-tracker-expert skill
```

Each backend module follows Clean Architecture with dependencies flowing inward only:
`Api` → `Application` → `Domain`; `Infrastructure` implements `Application`/`Domain` interfaces.

---

## Code Style — Frontend

### Compiler settings (verified in `tsconfig.app.json`)
- `strict`, `noUnusedLocals`, `noUnusedParameters`, `noFallthroughCasesInSwitch`
- `verbatimModuleSyntax: true` — always `import type` for type-only imports
- `erasableSyntaxOnly: true` — no `const enum`, no namespaces
- Target ES2022, moduleResolution `bundler`, `@/*` path alias to `src/` (use it for all src imports)

```ts
import type { Transaction } from "@/types/finance";  // type-only import
import { transactionsApi } from "@/api/transactions";
```

- Import order: external libraries → `@/api` → `@/components` → `@/hooks` → `@/types` → `@/utils`.
- No `.tsx` extensions in import specifiers (existing code imports without them).
- No `any` — precise types, generics, or `unknown` with guards. `interface` for object shapes; `type` for unions.
- Mirror backend DTOs exactly in `src/types/` (e.g. `PagedResult<T>`, `BudgetWithSpending`).

### Naming
- Components: `PascalCase` function declarations. Hooks: `useXxx`. API modules: `xxxApi`. Zod schemas: `xxxSchema`. Query key factories: `xxxKeys`. Types: `PascalCase`. String unions for enums (`'Income' | 'Expense'`).

### Patterns
- All data access via custom hooks (TanStack Query); never fetch inside components. Use the query key factory and invalidate via it — never hardcode key strings. Cross-feature dependency example: transaction mutations invalidate `budgetKeys.all` because budget spending depends on transactions.

```ts
export const transactionKeys = {
  all: ["transactions"] as const,
  lists: () => [...transactionKeys.all, "list"] as const,
  // list(filters), details(), detail(id) ...
};
```

- Forms: Zod schema (co-located with the feature, e.g. `features/budgets/schemas.ts`) → `type XyzFormData = z.infer<typeof xyzSchema>` → `useForm<XyzFormData>({ resolver: zodResolver(xyzSchema) })` → inline errors via `errors.field.message`.
- HTTP: use `apiClient.get/post/put/patch/delete` from `src/api/client.ts`. It handles bearer tokens, 401 → refresh → retry, and logout redirect. Pass `anonymous: true` for unauthenticated endpoints (login/register).
- Styling: Tailwind CSS exclusively; `clsx` + `tailwind-merge` for conditional classes; follow `docs/ai/ui-design-rules.md`.
- Shared formatters live in `src/utils/formatters.ts` (`formatCurrency`, `formatDate`).

---

## Code Style — Backend

### Project settings
- `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, NetAnalyzers enforced.
- `TreatWarningsAsErrors` is currently **commented out** — do not introduce new warnings; fix the 6 pre-existing test-project warnings when touched.

### Naming
- Classes/records `PascalCase`; private fields `_camelCase`; async methods `Async` suffix; DTOs `XxxRequest`/`XxxResponse`; validators `CreateXxxValidator`/`UpdateXxxValidator`.

### Domain entities (verified pattern)
- Private constructors + static `Create(...)` factory; `private set` on all properties; extend `Entity` from Shared. `ArgumentException` only for truly invalid state inside factories/update methods. Soft-delete via `IsActive` + `Deactivate()`.

```csharp
public sealed class Budget : Entity
{
    public string Name { get; private set; } = string.Empty;
    private Budget() { }
    public static Budget Create(Guid userId, Guid categoryId, string name, decimal limitAmount, BudgetPeriod period)
    { /* validate, throw ArgumentException, return new Budget { ... } */ }
}
```

### Endpoints (Minimal APIs)
- Static `XxxEndpoints` classes with `MapXxxEndpoints(this IEndpointRouteBuilder)` extension methods; `MapGroup("/api/xxx").WithTags(...).RequireAuthorization()`; `AddEndpointFilter<ValidationFilter<TRequest>>()` on mutating endpoints; return `TypedResults` (not `Results`).

```csharp
var group = app.MapGroup("/api/budgets").WithTags("Budgets").RequireAuthorization();
group.MapPost("/", CreateAsync).AddEndpointFilter<ValidationFilter<CreateBudgetRequest>>();
```

### EF Core / Infrastructure
- One `DbContext` per module, isolated to its schema (`HasDefaultSchema("finances")` / `"users"`).
- Fluent API via `IEntityTypeConfiguration<T>` — never data annotations. snake_case columns (`HasColumnName("created_at")`), `timestamptz`, `HasPrecision(18, 2)` on decimals, partial unique indexes for soft-delete (`WHERE is_active`).
- `EnableRetryOnFailure` is configured in each module's `DependencyInjection.cs` (Npgsql transient fault handling).
- Migrations live in `Infrastructure/Data/Migrations` (see commands above for exact flags).
- Repositories: interfaces in Domain/Application, EF implementations in Infrastructure; single-entity lookups return `T?` — never throw for not-found.
- Services return `Result<T>` for failure cases; throw `NotFoundException` only where a 404 should surface.

### Error handling
- `ExceptionHandlingMiddleware` maps exceptions to RFC 7807 `ProblemDetails` (`ValidationException` → 400, `UnauthorizedAccessException` → 401, `NotFoundException` → 404, unhandled → 500). Never catch-and-swallow in services; never return raw exception messages.
- Error codes: add constants to `Shared/Constants/ApiErrorCode.cs` (e.g. `BUDGET_NOT_FOUND`, `DUPLICATE_BUDGET_CATEGORY`).
- Enums serialize as JSON strings (`"Income"`/`"Expense"`) via `JsonStringEnumConverter` in `Program.cs`.

---

## Security Rules

- **Never store sensitive data in `localStorage`, `sessionStorage`, or JS-accessible cookies** in new code: no JWT access/refresh tokens, passwords, or full user objects. Acceptable: UI preferences and purely cosmetic display values.
- Note: the current `src/api/client.ts` persists tokens in `localStorage` — a **known CRITICAL audit finding (C-1) deferred by owner decision** to a dedicated auth-hardening task. Do not spread the pattern; target state is access token in memory and refresh token in `HttpOnly`, `Secure`, `SameSite=Strict` cookies set by the server.
- Never write sensitive values from JavaScript (`document.cookie`); cookies for sessions must be `HttpOnly` + `Secure`.
- Test: "If an attacker injects a `<script>` tag, can they read this value?" If yes — do not store it client-side.
- Never commit secrets: local secrets go in `appsettings.Local.json` or `.env` files (both gitignored); docs use placeholders only.

---

## Testing Conventions

### Backend (xUnit — exists today)
- Unit tests in `backend/tests/<Module>.UnitTests/` — pure domain/application logic, no I/O.
- Mock dependencies with NSubstitute (`Substitute.For<T>()`); assert with FluentAssertions (global using in `Usings.cs`).
- Test class mirrors the class under test (`TransactionServiceTests`); method names `MethodName_Scenario_ExpectedResult`.
- Integration tests with TestContainers are planned (Sprint 5) — not present yet.

### Frontend (Vitest — planned)
- Test files `*.test.ts(x)` co-located with source; `@testing-library/react` for components; mock API with `msw`; query by role/label, not class names.
- No test scripts exist yet — do not claim `npm test` works.

---

## Workflow & CI

- **Commits:** Conventional Commits (`feat:`, `fix:`, `docs:`, ...).
- **PRs to `main` are validated by `pr-standard.yml` (required for merge):**
  - Title: `PR: [Area] Title` — optional area in brackets (`[Finance]`, `[Users]`, `[CI]`, `[Docs]`), imperative mood, at most 150 chars.
  - Body must contain `# Summary:` (2-3 sentences) before `# Key Changes:` (bullet list).
- **Dev CI** (`dev.yml`, on push/PR to `main`): backend restore + Release build + `dotnet format --verify-no-changes --severity warn`; frontend `npm ci` + lint + build. Node 20 / .NET 10.
- Commenting `/oc` or `/opencode` on an issue/PR triggers an automated code-reviewer agent run.
- Sprint status values: `New` | `In Progress` | `Done` — update in both the sprint file header and `SPRINTS-OVERVIEW.md` when status changes. The `designer-enforcer` agent must run at the end of every sprint before marking it Done.

---

## Key Dependencies

- Frontend: `react`/`react-dom` 19, `typescript` 5.9, `vite` 7, `tailwindcss` 4, `@tanstack/react-query` 5, `react-hook-form` 7 + `zod` 4 + `@hookform/resolvers`, `react-router-dom` 7, `recharts` 3, `lucide-react`, `date-fns` 4, `clsx` + `tailwind-merge`, `dotenv-cli` (env loading in scripts)
- Backend: EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL`, `FluentValidation` 12, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore`, `AspNetCore.HealthChecks.NpgSql` (referenced, not yet wired)
- Planned, not installed: TickerQ (background jobs, Sprint 4), OpenTelemetry wiring (packages installed, Sprint 6)

---

## Documentation (read only when relevant)

| File | Read when |
|------|-----------|
| `docs/01-Project-Structure.md` | Monorepo layout, module organization |
| `docs/02-Backend-Documentation.md` | Clean Architecture, EF Core, endpoint/validation patterns |
| `docs/03-Frontend-Documentation.md` | React/TanStack Query/form/routing patterns |
| `docs/04-DevOps-Deployment.md` | CI/CD pipelines, environment configuration |
| `docs/05-Infrastructure.md` | Neon PostgreSQL, deployment targets, secrets |
| `docs/06-Local-Development.md` | Local setup (partially stale — trust this file's Commands section) |
| `docs/DEPENDENCIES.md` | NuGet/npm dependency catalogue |
| `docs/DESIGN_PATTERNS.md` | Backend patterns catalogue (Result, Repository, Options) |
| `docs/ai/ui-design-rules.md` | Any frontend UI work (colors, spacing, component conventions) |
| `docs/ai/sprints/SPRINTS-OVERVIEW.md` | Sprint scope, sequencing, status, known gaps |
| `docs/ai/sprints/sprint-N.md` | The current sprint's detailed task list |
| `.github/skills/finance-tracker-expert/SKILL.md` | Repo-expert agent skill |
