# Sprint 0 — Foundation & Tooling

**Duration:** 12/05/2026 — 19/05/2026
**Status:** Done
**Overview:** [SPRINTS-OVERVIEW.md](./SPRINTS-OVERVIEW.md)

---

## Overview

Sprint 0 brings both stacks from day-zero scaffolding to a runnable, properly configured baseline. No application features are built here — the goal is that after this sprint `dotnet build` and `npm run build` both pass cleanly, the API starts with a real middleware pipeline, the frontend starts with routing and layout visible, and all planned packages are installed and wired up.

**This sprint is a blocker for all subsequent sprints.** No feature work should begin until every task here is Done.

---

## Scope

### What's Included
- Fix `Directory.Build.props` framework version mismatch
- Register both backend projects into the empty `.sln`
- Install all planned NuGet packages across backend projects
- Build the Shared Kernel (middleware, base entity, exceptions, validation filter)
- Replace the Hello World `Program.cs` with a full pipeline
- Delete `Class1.cs` stub
- Install all planned npm packages in the frontend
- Configure `vite.config.ts` with path alias and dev proxy
- Set up Tailwind CSS
- Build the main layout shell (Sidebar, Header, MainLayout)
- Wire providers into `main.tsx` (QueryClient, BrowserRouter)
- Replace default `App.tsx` counter demo with the router outlet

### Out of Scope
- Any feature modules (Finance, Users, Reporting)
- Database migrations or EF Core DbContexts
- Authentication logic
- Any page components beyond the layout shell
- Deployment / CI/CD

### Known Gaps
- Local PostgreSQL instance required for future sprints — recommend Docker (`docker run -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16`)
- TickerQ package may not yet support `net10.0` — verify on NuGet before installing; substitute a cron-capable alternative if needed

---

## Tasks

---

### Task 1 — Fix Directory.Build.props Framework Mismatch

**Status:** New

**Description:**
`Directory.Build.props` currently declares `<TargetFramework>net8.0</TargetFramework>` but both `.csproj` files override it to `net10.0`. The props file should not set a target framework at all — that belongs in each project. Also align `Microsoft.CodeAnalysis.NetAnalyzers` to the version that ships with .NET 10.

**Steps:**
1. Open `backend/Directory.Build.props`
2. Remove the `<TargetFramework>net8.0</TargetFramework>` line entirely
3. Update the `Microsoft.CodeAnalysis.NetAnalyzers` package reference version to `9.0.0` (latest stable compatible with .NET 10 Roslyn)
4. Verify both `.csproj` files already declare `<TargetFramework>net10.0</TargetFramework>` — they do, no change needed there
5. Run `dotnet build backend/` and confirm it compiles

**Before:**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" ... />
  </ItemGroup>
</Project>
```

**After:**
```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0"
      PrivateAssets="all" IncludeAssets="runtime;build;native;contentfiles;analyzers" />
  </ItemGroup>
</Project>
```

**Success Criteria:**
- `dotnet build backend/` outputs `Build succeeded` with 0 errors and 0 warnings

---

### Task 2 — Register Projects in the Solution File

**Status:** New

**Description:**
The `.sln` file has no `Project(...)` entries — both `Personal.FinanceTracker.Api` and `Personal.FinanceTracker.Shared` are missing. Running `dotnet build` at the solution level does nothing.

**Steps:**
1. From the `backend/` directory, run:
   ```bash
   dotnet sln Personal.FinanceTracker.sln add src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj
   dotnet sln Personal.FinanceTracker.sln add src/Personal.FinanceTracker.Shared/Personal.FinanceTracker.Shared.csproj
   ```
2. Add a project reference from Api to Shared:
   ```bash
   dotnet add src/Personal.FinanceTracker.Api/Personal.FinanceTracker.Api.csproj reference src/Personal.FinanceTracker.Shared/Personal.FinanceTracker.Shared.csproj
   ```
3. Run `dotnet build Personal.FinanceTracker.sln` and confirm both projects build

**Success Criteria:**
- `dotnet build Personal.FinanceTracker.sln` succeeds for both projects
- `dotnet sln list` shows both projects registered

---

### Task 3 — Install Backend NuGet Packages

**Status:** New

**Description:**
Install all planned NuGet packages into the appropriate projects. Packages are split between the Api project and a new Infrastructure layer (which will be added in Sprint 1). For now, install everything into the Api project since module projects don't exist yet.

**Steps:**

1. Add packages to `Personal.FinanceTracker.Api`:
   ```bash
   cd backend
   dotnet add src/Personal.FinanceTracker.Api package Microsoft.AspNetCore.Authentication.JwtBearer
   dotnet add src/Personal.FinanceTracker.Api package Microsoft.EntityFrameworkCore
   dotnet add src/Personal.FinanceTracker.Api package Microsoft.EntityFrameworkCore.Design
   dotnet add src/Personal.FinanceTracker.Api package Npgsql.EntityFrameworkCore.PostgreSQL
   dotnet add src/Personal.FinanceTracker.Api package FluentValidation
   dotnet add src/Personal.FinanceTracker.Api package FluentValidation.AspNetCore
   dotnet add src/Personal.FinanceTracker.Api package Swashbuckle.AspNetCore
   dotnet add src/Personal.FinanceTracker.Api package Microsoft.AspNetCore.Diagnostics.HealthChecks
   dotnet add src/Personal.FinanceTracker.Api package AspNetCore.HealthChecks.Npgsql
   dotnet add src/Personal.FinanceTracker.Api package OpenTelemetry.Extensions.Hosting
   dotnet add src/Personal.FinanceTracker.Api package OpenTelemetry.Instrumentation.AspNetCore
   dotnet add src/Personal.FinanceTracker.Api package OpenTelemetry.Instrumentation.Http
   dotnet add src/Personal.FinanceTracker.Api package OpenTelemetry.Exporter.OpenTelemetryProtocol
   ```

2. Add packages to `Personal.FinanceTracker.Shared`:
   ```bash
   dotnet add src/Personal.FinanceTracker.Shared package FluentValidation
   ```

3. Run `dotnet build Personal.FinanceTracker.sln` — confirm no errors

**Note on TickerQ:** Add TickerQ only after confirming `net10.0` compatibility on NuGet. If unavailable, substitute `Quartz.NET` (`Quartz.AspNetCore`) as a drop-in replacement. This will be revisited in Sprint 4.

**Success Criteria:**
- All packages restore without errors
- `dotnet build` passes cleanly

---

### Task 4 — Build the Shared Kernel

**Status:** New

**Description:**
Replace the empty `Class1.cs` stub with the actual Shared Kernel components that every module will depend on: base entity, typed exceptions, global middleware, and the generic validation filter.

**Steps:**

1. Delete `backend/src/Personal.FinanceTracker.Shared/Class1.cs`

2. Create the folder structure:
   ```
   Personal.FinanceTracker.Shared/
   ├── Abstractions/
   │   └── Entity.cs
   ├── Exceptions/
   │   └── NotFoundException.cs
   ├── Middleware/
   │   └── ExceptionHandlingMiddleware.cs
   └── Filters/
       └── ValidationFilter.cs
   ```

3. Create `Abstractions/Entity.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Shared.Abstractions;

   public abstract class Entity
   {
       public Guid Id { get; protected set; }
       public DateTime CreatedAt { get; protected set; }
       public DateTime? UpdatedAt { get; protected set; }
   }
   ```

4. Create `Exceptions/NotFoundException.cs`:
   ```csharp
   namespace Personal.FinanceTracker.Shared.Exceptions;

   public sealed class NotFoundException(string resourceName, object key)
       : Exception($"{resourceName} with key '{key}' was not found.");
   ```

5. Create `Middleware/ExceptionHandlingMiddleware.cs`:
   ```csharp
   using Microsoft.AspNetCore.Mvc;
   using Personal.FinanceTracker.Shared.Exceptions;

   namespace Personal.FinanceTracker.Shared.Middleware;

   public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
   {
       public async Task InvokeAsync(HttpContext context)
       {
           try
           {
               await next(context);
           }
           catch (Exception ex)
           {
               logger.LogError(ex, "Unhandled exception occurred");
               await HandleExceptionAsync(context, ex);
           }
       }

       private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
       {
           var (statusCode, title) = exception switch
           {
               NotFoundException     => (StatusCodes.Status404NotFound,       "Resource Not Found"),
               UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
               FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed"),
               _                    => (StatusCodes.Status500InternalServerError, "Internal Server Error")
           };

           var problemDetails = new ProblemDetails
           {
               Status = statusCode,
               Title  = title,
               Detail = exception.Message
           };

           context.Response.StatusCode  = statusCode;
           context.Response.ContentType = "application/problem+json";
           await context.Response.WriteAsJsonAsync(problemDetails);
       }
   }
   ```

6. Create `Filters/ValidationFilter.cs`:
   ```csharp
   using FluentValidation;

   namespace Personal.FinanceTracker.Shared.Filters;

   public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
   {
       public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
       {
           var argument = context.Arguments.OfType<T>().FirstOrDefault();

           if (argument is null)
               return TypedResults.BadRequest("Request body is required.");

           var result = await validator.ValidateAsync(argument);

           if (!result.IsValid)
           {
               var errors = result.Errors
                   .GroupBy(e => e.PropertyName)
                   .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

               return TypedResults.ValidationProblem(errors);
           }

           return await next(context);
       }
   }
   ```

7. Run `dotnet build` — confirm 0 errors, 0 warnings

**Success Criteria:**
- `Class1.cs` is deleted
- All four new files compile cleanly
- `dotnet build Personal.FinanceTracker.sln` passes

---

### Task 5 — Replace Program.cs with Full Middleware Pipeline

**Status:** New

**Description:**
Replace the 7-line Hello World `Program.cs` with a production-ready pipeline that includes CORS, authentication, Swagger, health checks, and the exception middleware. Module registration will be added incrementally in later sprints — leave placeholder comments for those.

**Steps:**

1. Add a connection string placeholder to `appsettings.Development.json`:
   ```json
   {
     "Logging": {
       "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
     },
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=finance_tracker_dev;Username=postgres;Password=postgres"
     },
     "Jwt": {
       "SecretKey": "dev-secret-key-change-in-production-min-32-chars",
       "Issuer": "personal-finance-tracker",
       "Audience": "personal-finance-tracker-client",
       "ExpiryMinutes": 60
     },
     "AllowedHosts": "*"
   }
   ```

2. Replace `Program.cs` with:
   ```csharp
   using Personal.FinanceTracker.Shared.Middleware;

   var builder = WebApplication.CreateBuilder(args);

   // ── Services ───────────────────────────────────────────────────
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen(c =>
   {
       c.SwaggerDoc("v1", new() { Title = "Personal Finance Tracker API", Version = "v1" });
       c.AddSecurityDefinition("Bearer", new()
       {
           Name = "Authorization",
           Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
           Scheme = "bearer",
           BearerFormat = "JWT",
           In = Microsoft.OpenApi.Models.ParameterLocation.Header
       });
       c.AddSecurityRequirement(new()
       {
           [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
       });
   });

   builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           var jwtConfig = builder.Configuration.GetSection("Jwt");
           options.TokenValidationParameters = new()
           {
               ValidateIssuer           = true,
               ValidateAudience         = true,
               ValidateLifetime         = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer              = jwtConfig["Issuer"],
               ValidAudience            = jwtConfig["Audience"],
               IssuerSigningKey         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                   System.Text.Encoding.UTF8.GetBytes(jwtConfig["SecretKey"]!))
           };
       });

   builder.Services.AddAuthorization();

   builder.Services.AddCors(options =>
       options.AddDefaultPolicy(policy =>
           policy.WithOrigins("http://localhost:5173")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials()));

   builder.Services.AddHealthChecks();
       // .AddNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")!)  // enable in Sprint 1

   // TODO Sprint 1: builder.Services.AddUsersModule(builder.Configuration);
   // TODO Sprint 2: builder.Services.AddFinanceModule(builder.Configuration);
   // TODO Sprint 4: builder.Services.AddReportingModule(builder.Configuration);

   // ── Pipeline ───────────────────────────────────────────────────
   var app = builder.Build();

   app.UseMiddleware<ExceptionHandlingMiddleware>();

   if (app.Environment.IsDevelopment())
   {
       app.UseSwagger();
       app.UseSwaggerUI();
   }

   app.UseHttpsRedirection();
   app.UseCors();
   app.UseAuthentication();
   app.UseAuthorization();

   app.MapHealthChecks("/health/live");
   app.MapHealthChecks("/health/ready");

   // TODO Sprint 1: app.MapUsersEndpoints();
   // TODO Sprint 2: app.MapFinanceEndpoints();
   // TODO Sprint 4: app.MapReportingEndpoints();

   app.Run();
   ```

3. Run `dotnet build` — confirm 0 errors
4. Run `dotnet run --project backend/src/Personal.FinanceTracker.Api` and verify:
   - `GET http://localhost:5194/health/live` returns `200 Healthy`
   - `GET http://localhost:5194/swagger` loads the Swagger UI

**Success Criteria:**
- API starts without errors
- `/health/live` returns HTTP 200
- Swagger UI accessible at `/swagger`

---

### Task 6 — Install Frontend npm Packages

**Status:** New

**Description:**
Install all planned runtime and dev dependencies into the frontend project. The current `package.json` only has bare React 19.

**Steps:**

1. From `frontend/`:
   ```bash
   npm install react-router-dom @tanstack/react-query axios react-hook-form @hookform/resolvers zod date-fns clsx tailwind-merge lucide-react recharts
   ```

2. Install dev dependencies:
   ```bash
   npm install -D tailwindcss @tailwindcss/vite postcss autoprefixer @types/node vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event msw
   ```

3. Run `npm run build` — confirm it still passes (no TypeScript errors from newly added packages)

**Success Criteria:**
- `npm install` completes without errors
- `npm run build` passes with 0 TypeScript errors
- `npm run lint` passes with 0 ESLint errors

---

### Task 7 — Configure Tailwind CSS

**Status:** New

**Description:**
Tailwind CSS v4 uses a Vite plugin instead of a PostCSS config. Set it up and replace the default Vite CSS with a Tailwind base stylesheet.

**Steps:**

1. Update `vite.config.ts` to include the Tailwind plugin (will be extended further in Task 8):
   ```typescript
   import { defineConfig } from 'vite'
   import react from '@vitejs/plugin-react'
   import tailwindcss from '@tailwindcss/vite'

   export default defineConfig({
     plugins: [react(), tailwindcss()],
   })
   ```

2. Replace `src/index.css` content with:
   ```css
   @import "tailwindcss";
   ```

3. Delete `src/App.css` (default Vite demo styles — no longer needed)

4. Run `npm run dev` and confirm the browser loads without CSS errors

**Success Criteria:**
- Tailwind utility classes render correctly in the browser
- No console errors on startup

---

### Task 8 — Configure vite.config.ts with Path Alias and Dev Proxy

**Status:** New

**Description:**
Add the `@/` path alias (required by `verbatimModuleSyntax` import grouping rules) and a dev API proxy so frontend calls to `/api/*` forward to the ASP.NET backend on port 5194.

**Steps:**

1. Replace `vite.config.ts` with:
   ```typescript
   import path from 'path'
   import { defineConfig } from 'vite'
   import react from '@vitejs/plugin-react'
   import tailwindcss from '@tailwindcss/vite'

   export default defineConfig({
     plugins: [react(), tailwindcss()],
     resolve: {
       alias: {
         '@': path.resolve(__dirname, './src'),
       },
     },
     server: {
       proxy: {
         '/api': {
           target: 'http://localhost:5194',
           changeOrigin: true,
         },
       },
     },
   })
   ```

2. Update `tsconfig.app.json` to add the path alias so TypeScript resolves `@/`:
   ```json
   {
     "compilerOptions": {
       "paths": {
         "@/*": ["./src/*"]
       }
     }
   }
   ```

3. Run `npm run build` — confirm 0 errors

**Success Criteria:**
- `import { something } from '@/components/Foo'` resolves without TypeScript error
- Dev server proxies `/api/*` to the backend

---

### Task 9 — Build Main Layout Shell

**Status:** New

**Description:**
Replace the Vite counter demo with a real application shell: `MainLayout`, `Sidebar`, and `Header` components. These will be the outer wrapper for all feature pages in subsequent sprints.

**Steps:**

1. Create folder structure:
   ```
   src/
   ├── components/
   │   └── layout/
   │       ├── MainLayout.tsx
   │       ├── Sidebar.tsx
   │       └── Header.tsx
   └── pages/
       └── NotFoundPage.tsx
   ```

2. Create `src/components/layout/Sidebar.tsx`:
   ```tsx
   import { NavLink } from 'react-router-dom'

   const navItems = [
     { to: '/',             label: 'Dashboard' },
     { to: '/transactions', label: 'Transactions' },
     { to: '/categories',   label: 'Categories' },
     { to: '/budgets',      label: 'Budgets' },
     { to: '/reports',      label: 'Reports' },
   ]

   export function Sidebar() {
     return (
       <aside className="w-64 bg-gray-900 text-white h-screen flex flex-col p-4">
         <h1 className="text-xl font-bold mb-8">Finance Tracker</h1>
         <nav className="flex flex-col gap-1">
           {navItems.map((item) => (
             <NavLink
               key={item.to}
               to={item.to}
               end={item.to === '/'}
               className={({ isActive }) =>
                 `px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                   isActive ? 'bg-indigo-600 text-white' : 'text-gray-300 hover:bg-gray-700'
                 }`
               }
             >
               {item.label}
             </NavLink>
           ))}
         </nav>
       </aside>
     )
   }
   ```

3. Create `src/components/layout/Header.tsx`:
   ```tsx
   export function Header() {
     return (
       <header className="h-14 bg-white border-b border-gray-200 flex items-center px-6">
         <h2 className="text-sm font-medium text-gray-500">Personal Finance Tracker</h2>
       </header>
     )
   }
   ```

4. Create `src/components/layout/MainLayout.tsx`:
   ```tsx
   import { Outlet } from 'react-router-dom'
   import { Header } from '@/components/layout/Header.tsx'
   import { Sidebar } from '@/components/layout/Sidebar.tsx'

   export function MainLayout() {
     return (
       <div className="flex h-screen bg-gray-50">
         <Sidebar />
         <div className="flex flex-col flex-1 overflow-hidden">
           <Header />
           <main className="flex-1 overflow-y-auto p-6">
             <Outlet />
           </main>
         </div>
       </div>
     )
   }
   ```

5. Create `src/pages/NotFoundPage.tsx`:
   ```tsx
   export function NotFoundPage() {
     return (
       <div className="flex flex-col items-center justify-center h-64">
         <h2 className="text-2xl font-bold text-gray-900">404 — Page Not Found</h2>
         <p className="text-gray-500 mt-2">The page you're looking for doesn't exist.</p>
       </div>
     )
   }
   ```

6. Run `npm run lint` — confirm 0 errors

**Success Criteria:**
- All four components created and lint-clean
- No TypeScript errors

---

### Task 10 — Wire Providers and Router into main.tsx

**Status:** New

**Description:**
Replace the bare `main.tsx` and default `App.tsx` counter demo with a proper provider tree (`QueryClientProvider`, `BrowserRouter`) and a `createBrowserRouter` configuration with the layout shell. Placeholder routes for future feature pages are included.

**Steps:**

1. Replace `src/App.tsx` with:
   ```tsx
   import { createBrowserRouter, RouterProvider } from 'react-router-dom'
   import { MainLayout } from '@/components/layout/MainLayout.tsx'
   import { NotFoundPage } from '@/pages/NotFoundPage.tsx'

   const router = createBrowserRouter([
     {
       element: <MainLayout />,
       children: [
         { index: true, element: <div className="text-gray-500">Dashboard — coming in Sprint 4</div> },
         { path: 'transactions', element: <div className="text-gray-500">Transactions — coming in Sprint 2</div> },
         { path: 'categories',   element: <div className="text-gray-500">Categories — coming in Sprint 2</div> },
         { path: 'budgets',      element: <div className="text-gray-500">Budgets — coming in Sprint 3</div> },
         { path: 'reports',      element: <div className="text-gray-500">Reports — coming in Sprint 4</div> },
         { path: '*',            element: <NotFoundPage /> },
       ],
     },
   ])

   export function App() {
     return <RouterProvider router={router} />
   }
   ```

2. Replace `src/main.tsx` with:
   ```tsx
   import { StrictMode } from 'react'
   import { createRoot } from 'react-dom/client'
   import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
   import { App } from './App.tsx'
   import './index.css'

   const queryClient = new QueryClient({
     defaultOptions: {
       queries: {
         staleTime: 1000 * 60 * 5, // 5 minutes
         retry: 1,
       },
     },
   })

   createRoot(document.getElementById('root')!).render(
     <StrictMode>
       <QueryClientProvider client={queryClient}>
         <App />
       </QueryClientProvider>
     </StrictMode>,
   )
   ```

3. Run `npm run build` — confirm 0 TypeScript errors, 0 lint errors
4. Run `npm run dev` and confirm:
   - Sidebar renders with all nav links
   - Navigating between routes updates the active nav item
   - Header is visible
   - No console errors

**Success Criteria:**
- `npm run build` passes with 0 errors
- `npm run lint` passes with 0 errors
- Browser shows the layout with sidebar navigation
- All routes navigate without errors

---

## Definition of Done

This sprint is complete when:

- [ ] `dotnet build Personal.FinanceTracker.sln` passes with 0 errors and 0 warnings
- [ ] `dotnet run --project backend/src/Personal.FinanceTracker.Api` starts successfully
- [ ] `GET /health/live` returns HTTP 200
- [ ] Swagger UI loads at `/swagger`
- [ ] `npm run build` passes with 0 TypeScript errors
- [ ] `npm run lint` passes with 0 ESLint errors
- [ ] `npm run dev` shows the layout shell with sidebar in the browser
- [ ] All 10 tasks are in **Done** status
- [ ] `designer-enforcer` agent has been invoked and its report is clean

---

*Last updated: 12/05/2026*
