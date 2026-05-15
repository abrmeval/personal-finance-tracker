# Dependencies

This document describes the runtime and development dependencies for both the backend and frontend projects, including their general purpose and their specific role in this application.

---

## Backend

The backend is an ASP.NET 10 Minimal API project targeting .NET 10. Dependencies are declared in the `.csproj` files under `backend/src/`.

### Runtime Dependencies

| Package | Version | General Purpose | Role in Project |
|---------|---------|----------------|-----------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.8 | Adds JWT bearer token authentication middleware to ASP.NET Core | Validates JWT access tokens on protected endpoints; integrates with the authorization pipeline |
| `Microsoft.EntityFrameworkCore` | 10.0.8 | .NET ORM for database access using strongly-typed models | Core ORM used across all modules for querying and persisting data via the repository pattern |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.1 | EF Core provider for PostgreSQL using Npgsql | Connects EF Core to the Neon PostgreSQL database; provides retry-on-failure for transient connection errors |
| `FluentValidation` | 12.1.1 | Defines and executes validation rules for .NET objects using a fluent API | Validates incoming request DTOs via `AbstractValidator<T>` classes; used in the `ValidationFilter<T>` endpoint filter |
| `FluentValidation.AspNetCore` | 11.3.1 | ASP.NET Core integration for FluentValidation (DI registration helpers) | Enables `services.AddValidatorsFromAssemblyContaining<T>()` for automatic validator discovery |
| `Swashbuckle.AspNetCore` | 10.1.7 | Generates OpenAPI (Swagger) documentation from ASP.NET Core Minimal API endpoints | Provides the `/swagger` UI and OpenAPI spec for exploring and testing the API |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | Adds a PostgreSQL health check endpoint for ASP.NET Core | Exposes a `/health` endpoint that verifies the database connection is live |
| `OpenTelemetry.Extensions.Hosting` | 1.15.3 | Integrates OpenTelemetry SDK with the .NET generic host lifecycle | Bootstraps the OpenTelemetry tracer, meter, and logger providers at startup |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.15.3 | Exports telemetry data via OTLP (gRPC or HTTP) to a collector | Sends traces, metrics, and logs to an external OTLP-compatible observability backend |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.15.2 | Auto-instruments ASP.NET Core request handling with traces and metrics | Captures HTTP request/response traces for all API endpoints without manual instrumentation |
| `OpenTelemetry.Instrumentation.Http` | 1.15.1 | Auto-instruments outbound `HttpClient` calls with distributed tracing | Propagates trace context on any outbound HTTP calls made by the API |

### Design-Time Dependencies

| Package | Version | General Purpose | Role in Project |
|---------|---------|----------------|-----------------|
| `Microsoft.EntityFrameworkCore.Design` | 10.0.8 | Provides EF Core design-time tooling (scaffolding, migrations) | Required by `dotnet ef migrations add` and `dotnet ef database update` commands; excluded from the published output |

---

## Frontend

The frontend is a React + Vite + TypeScript application. Dependencies are declared in `frontend/package.json`.

### Runtime Dependencies

| Package | Version | General Purpose | Role in Project |
|---------|---------|----------------|-----------------|
| `react` | 19.2.0 | UI component library for building declarative, component-based interfaces | Core rendering framework for all pages, layouts, and UI components |
| `react-dom` | 19.2.0 | DOM-specific rendering methods for React | Mounts the React application into the browser DOM via `ReactDOM.createRoot` |
| `react-router-dom` | 7.15.1 | Client-side routing for React applications | Handles all page navigation, route definitions, and route-level code splitting |
| `@tanstack/react-query` | 5.100.10 | Asynchronous server state management with caching, background refetching, and synchronization | Manages all server data fetching and caching via custom hooks; replaces the need for any global state library |
| `axios` | 1.16.1 | Promise-based HTTP client for the browser and Node.js | Makes all API requests; configured with a 401 interceptor that handles silent token refresh and redirects to `/login` on failure |
| `react-hook-form` | 7.75.0 | Performant, flexible form state management for React | Drives all form state, validation integration, and submission handling throughout the application |
| `@hookform/resolvers` | 5.2.2 | Adapters that connect validation libraries (e.g., Zod) to React Hook Form | Wires Zod schemas into React Hook Form via `zodResolver`, eliminating manual field-level validation |
| `zod` | 4.4.3 | TypeScript-first schema declaration and runtime validation library | Defines form validation schemas; inferred types (`z.infer`) are used as the TypeScript form data types |
| `recharts` | 3.8.1 | Composable charting library built on React and D3 | Renders all data visualisation components: income/expense charts, budget progress bars, and dashboard graphs |
| `lucide-react` | 1.16.0 | Icon library providing clean, consistent SVG icons as React components | Supplies all icons used across the UI; the only icon library used in this project |
| `clsx` | 2.1.1 | Utility for constructing conditional `className` strings | Used alongside `tailwind-merge` to compose conditional Tailwind class names cleanly |
| `tailwind-merge` | 3.6.0 | Merges Tailwind CSS class lists, resolving conflicting utility classes | Prevents duplicate or conflicting Tailwind classes when composing component variants |
| `date-fns` | 4.1.0 | Modular date utility library for parsing, formatting, and manipulating dates | Formats transaction dates, budget period labels, and report date ranges throughout the UI |
| `dotenv-cli` | 11.0.0 | CLI tool for injecting `.env` file variables into any command | Used in the `dev` npm script to load environment variables (e.g., `VITE_API_URL`) before starting Vite |

### Development Dependencies

| Package | Version | General Purpose | Role in Project |
|---------|---------|----------------|-----------------|
| `vite` | 7.2.4 | Fast frontend build tool and dev server with HMR | Bundles the application for production and runs the development server with hot module replacement |
| `@vitejs/plugin-react` | 5.1.1 | Vite plugin that enables React Fast Refresh and JSX transform | Required for React component HMR and automatic JSX runtime during development and build |
| `typescript` | 5.9.3 | Typed superset of JavaScript with a compiler and language server | Provides static type checking across the entire frontend with strict mode and `verbatimModuleSyntax` enforced |
| `tailwindcss` | 4.3.0 | Utility-first CSS framework | Provides all styling via utility classes; no custom CSS is written — all styling goes through Tailwind |
| `@tailwindcss/vite` | 4.3.0 | Official Vite plugin for Tailwind CSS v4 | Integrates Tailwind's build process directly into the Vite pipeline |
| `postcss` | 8.5.14 | CSS transformation tool used as part of the build pipeline | Required by Tailwind CSS for processing and transforming CSS output |
| `autoprefixer` | 10.5.0 | PostCSS plugin that adds vendor prefixes to CSS rules automatically | Ensures cross-browser CSS compatibility in the production build |
| `eslint` | 9.39.1 | Pluggable JavaScript/TypeScript linter | Enforces code quality and style rules across all `.ts` and `.tsx` files |
| `@eslint/js` | 9.39.1 | ESLint's official recommended rule set for JavaScript | Provides the base ESLint rule configuration used in `eslint.config.js` |
| `typescript-eslint` | 8.46.4 | TypeScript-aware ESLint rules and parser | Enables type-aware linting rules specific to TypeScript code |
| `eslint-plugin-react-hooks` | 7.0.1 | ESLint plugin enforcing the Rules of Hooks | Catches incorrect hook usage (e.g., hooks inside conditionals) at lint time |
| `eslint-plugin-react-refresh` | 0.4.24 | ESLint plugin that enforces React Fast Refresh compatibility | Warns when component exports are structured in a way that breaks HMR |
| `globals` | 16.5.0 | Provides lists of global variables for different environments (browser, Node.js) | Used in `eslint.config.js` to declare the correct global scope for linting |
| `@types/react` | 19.2.5 | TypeScript type definitions for React | Provides type safety for React APIs, JSX, hooks, and component props |
| `@types/react-dom` | 19.2.3 | TypeScript type definitions for React DOM | Provides type safety for `ReactDOM.createRoot` and DOM-specific React APIs |
| `@types/node` | 24.12.4 | TypeScript type definitions for Node.js built-ins | Required for `vite.config.ts` to use Node.js APIs (e.g., `path.resolve`) with full type safety |
| `vitest` | 4.1.6 | Vite-native unit testing framework compatible with the Jest API | Runs all frontend unit and component tests; configured to run in a jsdom environment |
| `@testing-library/react` | 16.3.2 | Testing utilities for rendering React components and querying the DOM | Used to render components in tests and query by accessible roles and labels |
| `@testing-library/user-event` | 14.6.1 | Simulates real user interactions (typing, clicking) in tests | Used alongside Testing Library to test form submissions, button clicks, and input interactions |
| `@testing-library/jest-dom` | 6.9.1 | Custom Jest/Vitest matchers for asserting on DOM state | Provides matchers like `toBeInTheDocument()` and `toHaveValue()` in component tests |
| `msw` | 2.14.6 | Mock Service Worker — intercepts network requests in tests and the browser | Mocks all API calls in frontend tests without modifying application code; follows the handler-per-endpoint pattern |
