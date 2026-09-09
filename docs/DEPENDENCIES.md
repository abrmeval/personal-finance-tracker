# Dependencies

This document describes the runtime, design-time, and test dependencies for the backend and frontend projects, including their general purpose and their specific role in this application. All entries are verified against the project files (`backend/**/*.csproj`, `frontend/package.json`).

Status shorthand:
- **Active** — declared and used in code today
- **Referenced** — declared but not yet wired (planned for a later sprint)
- **Unused** — declared but not used anywhere; removal candidate

---

## Backend

The backend is an ASP.NET 10 Minimal API solution targeting .NET 10. Dependencies are declared in the `.csproj` files under `backend/src/` and `backend/tests/`.

### Runtime Dependencies (src)

| Package | Version | Project(s) | General Purpose | Role in Project |
|---------|---------|------------|-----------------|-----------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.8 | Api | JWT bearer authentication middleware | Validates JWT access tokens on protected endpoints; integrates with the authorization pipeline |
| `Microsoft.AspNetCore.OpenApi` + `Microsoft.OpenApi` | 10.0.8 / 2.7.5 | Api | OpenAPI document generation for Minimal APIs | Generates the OpenAPI spec served in Development |
| `Scalar.AspNetCore` | 2.14.14 | Api | Interactive API reference UI | Serves the Scalar UI at `/scalar` (Development only) — this project does **not** use Swashbuckle/Swagger |
| `Microsoft.EntityFrameworkCore` | 10.0.8 | Api, Users, Finance | .NET ORM for strongly-typed database access | Core ORM across all modules via the repository pattern |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.1 | Api, Users, Finance | EF Core provider for PostgreSQL | Connects the module DbContexts to PostgreSQL (local Docker today; Neon in production); `EnableRetryOnFailure` configured per module |
| `FluentValidation` | 12.1.1 | Shared, Users, Finance, Api | Fluent validation rules for .NET objects | Validates request DTOs via `AbstractValidator<T>`; executed by the `ValidationFilter<T>` endpoint filter |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Users, Finance | DI registration helpers for FluentValidation | Enables `services.AddValidatorsFromAssemblyContaining<T>()` automatic validator discovery |
| `BCrypt.Net-Next` | 4.2.0 | Users | BCrypt password hashing | Hashes passwords on register and verifies them on login (`UserService`) |
| `System.IdentityModel.Tokens.Jwt` + `Microsoft.IdentityModel.Tokens` | 8.18.0 | Users | JWT creation and validation primitives | Generates and reads JWTs in `TokenService` (access tokens, refresh token claims) |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | Api | PostgreSQL health check for ASP.NET Core | **Referenced — not yet wired.** `/health/live` and `/health/ready` currently register no DB check |
| `OpenTelemetry.Extensions.Hosting` / `.Exporter.OpenTelemetryProtocol` / `.Instrumentation.AspNetCore` / `.Instrumentation.Http` | 1.15.x | Api | Traces, metrics, and logs with OTLP export | **Referenced — wiring planned (Sprint 6)** |

### Design-Time Dependencies

| Package | Version | Project(s) | General Purpose | Role in Project |
|---------|---------|------------|-----------------|-----------------|
| `Microsoft.EntityFrameworkCore.Design` | 10.0.8 | Api, Users, Finance | EF Core design-time tooling (scaffolding, migrations) | Required by `dotnet ef migrations add` and `dotnet ef database update`; excluded from the published output |

### Test Dependencies (Finance.UnitTests and Users.UnitTests)

| Package | Version | General Purpose | Role in Project |
|---------|---------|----------------|-----------------|
| `Microsoft.NET.Test.Sdk` | 17.13.0 | Test platform adapter for .NET | Runs the xUnit tests via `dotnet test` |
| `xunit` + `xunit.runner.visualstudio` | 2.9.3 / 3.1.0 | Unit testing framework | Test framework for domain/application unit tests |
| `NSubstitute` | 5.3.0 | Mocking library | Mocks repositories, token services, and loggers in service tests (`Substitute.For<T>()`) |
| `FluentAssertions` | 8.2.0 | Fluent assertion library | Assertion syntax in all tests (global `using FluentAssertions;` in `Usings.cs`) |

Integration tests with TestContainers are planned (Sprint 5) — no packages for them are installed yet.

---

## Frontend

The frontend is a React + Vite + TypeScript application. Dependencies are declared in `frontend/package.json`.

### Runtime Dependencies

| Package | Version | General Purpose | Role in Project |
|---------|---------|----------------|-----------------|
| `react` + `react-dom` | 19.2.0 | UI component library + DOM renderer | Core rendering for all pages and layouts (mounted via `ReactDOM.createRoot`) |
| `react-router-dom` | 7.15.1 | Client-side routing | Route definitions in `src/routes/index.tsx` (`createBrowserRouter`), protected routes |
| `@tanstack/react-query` | 5.100.10 | Asynchronous server state management | All server data fetching and caching via custom hooks; no global state library |
| `react-hook-form` | 7.75.0 | Performant form state management | Drives all form state and submission handling |
| `@hookform/resolvers` | 5.2.2 | Validation adapters for React Hook Form | Wires Zod schemas into forms via `zodResolver` |
| `zod` | 4.4.3 | TypeScript-first schema validation | Form validation schemas co-located with features (e.g. `features/budgets/schemas.ts`); `z.infer` types used as form data types |
| `lucide-react` | 1.16.0 | SVG icon library | Supplies all icons in the UI — the only icon library used |
| `clsx` + `tailwind-merge` | 2.1.1 / 3.6.0 | Conditional class composition | Composes conditional Tailwind class names without conflicts |
| `dotenv-cli` | 11.0.0 | Injects `.env` variables into commands | Loads `VITE_API_URL` / `VITE_ENVIRONMENT` in the `dev` and `build` npm scripts |

**HTTP client:** the app uses the native-fetch `apiClient` in `src/api/client.ts` (bearer token, 401 → refresh → retry, logout redirect) — there is no HTTP library dependency.

### Development Dependencies

| Package | Version | General Purpose | Role in Project |
|---------|---------|----------------|-----------------|
| `vite` | 7.2.4 | Build tool and dev server with HMR | Production bundling; dev server on port 3000 with the `/api` proxy to the backend |
| `@vitejs/plugin-react` | 5.1.1 | React Fast Refresh + JSX transform | Required for HMR and the automatic JSX runtime |
| `typescript` | 5.9.3 | Typed JavaScript compiler | Strict type-checking (`tsc -b`) runs before every build |
| `tailwindcss` + `@tailwindcss/vite` | 4.3.0 | Utility-first CSS framework + official Vite plugin | All styling via utility classes; the Vite plugin owns the CSS pipeline (no PostCSS config) |
| `eslint` + `@eslint/js` + `typescript-eslint` + `eslint-plugin-react-hooks` + `eslint-plugin-react-refresh` + `globals` | 9.39.1 / 8.46.4 / 7.0.1 / 0.4.24 / 16.5.0 | Linting toolchain | `npm run lint` — Rules of Hooks, Fast Refresh compatibility, TS-aware linting |
| `@types/react`, `@types/react-dom`, `@types/node` | 19.2.5 / 19.2.3 / 24.12.4 | TypeScript type definitions | Type safety for React APIs and Node APIs in `vite.config.ts` |

### Referenced — planned (installed ahead of need)

| Package | Version | Planned use |
|---------|---------|-------------|
| `recharts` | 3.8.1 | Dashboard/reporting charts (Sprint 4) — no chart code exists yet |
| `date-fns` | 4.1.0 | Date ranges and formatting in reporting (Sprint 4) — current formatting uses native `Intl` in `src/utils/formatters.ts` |
| `vitest` | 4.1.6 | Frontend test runner (Sprint 5) — no test scripts or config exist yet |
| `@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom` | 16.3.2 / 14.6.1 / 6.9.1 | Component testing (Sprint 5) |
| `msw` | 2.14.6 | API mocking for tests (Sprint 5) |

### Unused — removal candidates

| Package | Version | Why it is unused |
|---------|---------|------------------|
| `axios` | 1.16.1 | No imports anywhere; superseded by the fetch-based `apiClient`. Do not add usages |
| `postcss` | 8.5.14 | No `postcss.config` exists; the Tailwind CSS 4 Vite plugin handles CSS processing directly |
| `autoprefixer` | 10.5.0 | Vendor prefixing is handled by the Tailwind 4 pipeline; no PostCSS plugin is loaded |

## References

- [SPRINTS-OVERVIEW.md](./ai/sprints/SPRINTS-OVERVIEW.md) — timing of the planned packages (Sprints 4–6)
- [AGENTS.md](../AGENTS.md) — coding standards and dependency usage rules
- [05-Infrastructure.md](./05-Infrastructure.md) — production database and secrets management

---

*Last Updated: 09 Sep 2026*
