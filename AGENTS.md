# AGENTS.md — Personal Finance Tracker

Guidance for agentic coding agents operating in this repository.

---

## Repository Overview

Full-stack monorepo:
- **Backend:** ASP.NET 10 modular monolith (`backend/`)
- **Frontend:** React + Vite + TypeScript (`frontend/`)
- **Docs:** Architecture and pattern documentation (`docs/`)

---

## Build, Lint, and Test Commands

### Frontend (`frontend/`)

```bash
npm run dev          # Start Vite dev server with HMR
npm run build        # Type-check (tsc -b) then bundle for production
npm run lint         # Run ESLint across all TS/TSX files
npm run preview      # Serve the production build locally
```

**Testing (Vitest — to be added):**
```bash
npm test                                        # Run all tests
npm run test:coverage                           # Run with coverage
npx vitest run src/path/to/file.test.ts         # Run a single test file
npx vitest run -t "test name"                   # Run a single test by name
```

### Backend (`backend/`)

```bash
dotnet build                                                  # Build the solution
dotnet run --project src/Personal.FinanceTracker.Api         # Start the API
dotnet format                                                  # Format all C# code

dotnet test                                                    # Run all tests
dotnet test tests/Finance.UnitTests                           # Run a single test project
dotnet test --filter "FullyQualifiedName~MyMethodName"        # Run a single test by name
dotnet test --filter "DisplayName=My test display name"       # Run by display name
dotnet test --collect:"XPlat Code Coverage"                   # Run with coverage

dotnet ef migrations add <MigrationName>                      # Add EF Core migration
dotnet ef database update                                      # Apply pending migrations
```

---

## Project Structure

```
personal-finance-tracker/
├── backend/
│   ├── src/
│   │   ├── Personal.FinanceTracker.Api/       # ASP.NET minimal API host
│   │   ├── Personal.FinanceTracker.Shared/    # Shared kernel/utilities
│   │   └── Modules/                           # (planned) Finance, Users, Reporting
│   ├── tests/                                 # (planned) xUnit test projects
│   └── Directory.Build.props                  # Shared MSBuild properties
├── frontend/
│   ├── src/
│   │   ├── api/                               # Axios client + API modules
│   │   ├── components/                        # Shared UI components
│   │   ├── features/                          # Feature-based pages/logic
│   │   ├── hooks/                             # Custom React hooks
│   │   ├── types/                             # TypeScript type definitions
│   │   └── utils/                             # Helper functions
│   ├── eslint.config.js
│   ├── tsconfig.app.json
│   └── vite.config.ts
└── docs/                                      # Architecture documentation
```

Each backend module follows Clean Architecture layers:
`Domain` → `Application` → `Infrastructure` → `Api`

---

## TypeScript / Frontend Code Style

### Compiler Settings (strict)
- `strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noFallthroughCasesInSwitch`
- `verbatimModuleSyntax: true` — use `import type` for all type-only imports
- `erasableSyntaxOnly: true` — no `const enum`, no namespaces
- Target: `ES2022`, module resolution: `bundler`

### Imports
- Always use `import type` for type-only imports (enforced by `verbatimModuleSyntax`)
- Group imports: external libraries → internal `@/api` → `@/components` → `@/hooks` → `@/types` → `@/utils`
- Use the `@/` path alias for all imports from `src/`
- Include `.tsx` extension when importing TSX files directly

### Naming Conventions
- **Components:** `PascalCase` function declarations (`function TransactionForm`)
- **Hooks:** `camelCase` with `use` prefix (`useTransactions`, `useCreateTransaction`)
- **Types/Interfaces:** `PascalCase` (`Transaction`, `CreateTransactionRequest`, `PagedResult<T>`)
- **Type unions:** string literal union types (`'Income' | 'Expense'`)
- **API modules:** camelCase object literals (`transactionsApi`, `reportsApi`)
- **Zod schemas:** camelCase with `Schema` suffix (`transactionSchema`)
- **Inferred form types:** `type XyzFormData = z.infer<typeof xyzSchema>`
- **Query key factories:** `const xyzKeys = { all, lists, list, details, detail }` pattern

### Types
- No `any` — use precise types or generics
- Prefer `interface` for object shapes; `type` for unions and aliases
- Use `Partial<T>` for optional defaults in component props
- Mirror backend DTO types exactly (`PagedResult<T>`, `Transaction`, etc.)

### Error Handling (Frontend)
- Use TanStack Query `error` state in components; render a user-facing error message
- Handle form validation errors inline via React Hook Form + Zod (`errors.field.message`)
- Axios interceptor handles 401 → token refresh → retry → redirect to `/login`

---

## React Patterns

- **Feature-based folder structure:** co-locate components, hooks, and types per feature
- **Custom hooks as data layer:** all TanStack Query calls inside custom hooks (`useTransactions`, etc.)
- **Consistent cache invalidation:** use query key factory pattern for `invalidateQueries`
- **No global state library** (no Redux/Zustand) — TanStack Query manages server state
- **Form pattern:** Zod schema → `useForm<FormData>` → controlled inputs → submit handler

---

## C# / Backend Code Style

### Project Settings (`Directory.Build.props`)
- `<Nullable>enable</Nullable>` — nullable reference types required everywhere
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — zero warnings policy
- `<ImplicitUsings>enable</ImplicitUsings>` — no explicit BCL `using` statements
- Roslyn analyzers (`Microsoft.CodeAnalysis.NetAnalyzers`) enforced

### Naming Conventions
- **Classes/Records:** `PascalCase`
- **Private fields:** `_camelCase` with underscore prefix (`_repository`, `_logger`)
- **Async methods:** `Async` suffix (`GetAllAsync`, `CreateAsync`)
- **Endpoints:** static classes with extension methods (`TransactionEndpoints.MapTransactionEndpoints`)
- **DTOs:** `XxxRequest` / `XxxResponse` suffix

### Architecture Rules
- Dependencies always flow inward: `Api` → `Application` → `Domain`; `Infrastructure` implements `Application` interfaces
- Domain entities use private constructors + static `Create(...)` factory methods
- Repositories return `T?` (nullable) for single-entity lookups — never throw for not-found
- Modules register themselves via `AddXxxModule(IServiceCollection, IConfiguration)` and `MapXxxEndpoints(IEndpointRouteBuilder)`

### Error Handling (Backend)
- Global `ExceptionHandlingMiddleware` maps exceptions to RFC 7807 `ProblemDetails`:
  - `ValidationException` → 400
  - `UnauthorizedAccessException` → 401
  - `NotFoundException` → 404
  - Unhandled → 500
- `ValidationFilter<T>` endpoint filter handles request validation before handlers are reached
- Throw `ArgumentException` only in domain entities for truly invalid state
- Use `null` / `bool` return values for expected not-found or failure cases — not exceptions

### C# Patterns
- Minimal APIs with endpoint extension methods (no MVC controllers)
- Repository pattern: domain-defined interfaces in `Application`, EF Core implementations in `Infrastructure`
- Specification pattern (`ISpecification<T>`) for composable query filters
- `record` types for immutable query params
- Background jobs with TickerQ using `[TickerFunction("Name", "cron")]` attribute

---

## Testing Conventions

### Backend (xUnit + TestContainers)
- Unit tests: `tests/<Module>.UnitTests/` — pure domain/application logic, no I/O
- Integration tests: `tests/<Module>.IntegrationTests/` — use TestContainers for real DB
- Test class name mirrors the class under test: `TransactionServiceTests`
- Test method name: `MethodName_Scenario_ExpectedResult`
- Use `Assert.Equal`, `Assert.NotNull`, `Assert.Throws<T>` from xUnit

### Frontend (Vitest — planned)
- Test files: `*.test.ts` / `*.test.tsx` co-located with source files
- Use `@testing-library/react` for component tests
- Mock API calls with `msw` (Mock Service Worker)

---

## Sprint Documentation

All sprint planning and execution documents live at:

```
docs/ai/sprints/
├── SPRINTS-OVERVIEW.md   # Master plan — all sprints, goals, sequencing
├── sprint-0.md           # Foundation & Tooling
├── sprint-1.md           # Users Module / Authentication
├── sprint-2.md           # Finance Module: Transactions & Categories
├── sprint-3.md           # Finance Module: Budgets
├── sprint-4.md           # Reporting Module & Dashboard
├── sprint-5.md           # Testing
└── sprint-6.md           # DevOps & Infrastructure
```

### Rules for Sprint Documents

- **Never create sprint docs outside `docs/ai/sprints/`** — this is the canonical location
- Each sprint file is named `sprint-N.md` (lowercase, hyphenated)
- The `SPRINTS-OVERVIEW.md` must be updated when any sprint's status changes
- Sprint status values: `New` | `In Progress` | `Done`
- When a sprint executor completes a sprint, it must update the status in both the sprint file header and in `SPRINTS-OVERVIEW.md`
- The `designer-enforcer` agent must be invoked at the end of every sprint before marking it Done

---

## Key Dependencies

### Frontend
| Package | Purpose |
|---------|---------|
| `react` + `react-dom` | UI framework |
| `typescript` + `vite` | Build toolchain |
| `@tanstack/react-query` | Server state management |
| `axios` | HTTP client |
| `react-hook-form` + `zod` | Form handling and validation |
| `react-router-dom` | Client-side routing |
| `recharts` / `chart.js` | Data visualization |

### Backend
| Package | Purpose |
|---------|---------|
| ASP.NET 10 Minimal APIs | HTTP host and routing |
| Entity Framework Core | ORM with PostgreSQL (`Npgsql`) |
| FluentValidation | Request validation |
| xUnit + TestContainers | Testing |
| OpenTelemetry | Traces, metrics, logs (OTLP export) |
| TickerQ | Cron-based background jobs |
