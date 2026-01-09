# Personal Finance Tracker 💰

> A full-stack personal finance management application built with **ASP.NET 8** and **React 18**  
> Track expenses, manage budgets, and gain insights into your financial health.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)](https://react.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 📋 Table of Contents

- [Features](#-features)
- [Architecture](#-architecture)
- [Tech Stack](#-tech-stack)
- [Getting Started](#-getting-started)
- [Project Structure](#-project-structure)
- [Development](#-development)
- [Deployment](#-deployment)
- [Documentation](#-documentation)
- [Contributing](#-contributing)
- [License](#-license)

---

## ✨ Features

### Core Functionality
- 💳 **Transaction Management** - Track income and expenses with categories
- 📊 **Budget Planning** - Set monthly budgets and monitor spending
- 📈 **Financial Reports** - Visualize spending patterns and trends
- 🏷️ **Category Management** - Organize transactions with custom categories
- 👤 **User Authentication** - Secure JWT-based authentication
- 🔍 **Search & Filters** - Find transactions by date, category, or amount

### Technical Features
- 🎯 **Modular Monolith** - Clean architecture with module isolation
- 🚀 **Minimal APIs** - High-performance ASP.NET 8 endpoints
- 📱 **Responsive Design** - Mobile-first UI with Tailwind CSS
- 🔄 **Real-time Updates** - TanStack Query for optimistic updates
- 🧪 **Comprehensive Testing** - Unit and integration tests
- 📊 **Observability** - OpenTelemetry integration for monitoring

---

## 🏗️ Architecture

This project follows a **Modular Monolith** architecture pattern, combining the organizational benefits of microservices with the simplicity of monolithic deployment.

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure App Service                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              Personal Finance Tracker API            │    │
│  │  ┌─────────────┐ ┌─────────────┐ ┌──────────────┐   │    │
│  │  │   Finance   │ │    Users    │ │  Reporting   │   │    │
│  │  │   Module    │ │   Module    │ │   Module     │   │    │
│  │  └──────┬──────┘ └──────┬──────┘ └──────┬───────┘   │    │
│  └─────────┼───────────────┼───────────────┼───────────┘    │
└────────────┼───────────────┼───────────────┼────────────────┘
             │               │               │
             ▼               ▼               ▼
┌─────────────────────────────────────────────────────────────┐
│                   Neon PostgreSQL Database                   │
│  ┌─────────────┐ ┌─────────────┐ ┌──────────────┐           │
│  │ finances.*  │ │   users.*   │ │  reports.*   │           │
│  └─────────────┘ └─────────────┘ └──────────────┘           │
└─────────────────────────────────────────────────────────────┘
```

### Module Structure

Each module follows Clean Architecture principles:

| Layer | Responsibility | Dependencies |
|-------|---------------|--------------|
| **Domain** | Business entities & rules | None |
| **Application** | Use cases & services | Domain |
| **Infrastructure** | Data access & external services | Domain, Application |
| **Api** | HTTP endpoints | Application |

---

## 🛠️ Tech Stack

### Backend
- **Framework**: ASP.NET 8 with Minimal APIs
- **Database**: Neon PostgreSQL (serverless)
- **ORM**: Entity Framework Core 8
- **Validation**: FluentValidation
- **Authentication**: JWT Bearer tokens
- **Background Jobs**: TickerQ
- **Testing**: xUnit, TestContainers

### Frontend
- **Framework**: React 18 with TypeScript
- **Build Tool**: Vite
- **Styling**: Tailwind CSS
- **State Management**: TanStack Query (React Query)
- **Forms**: React Hook Form + Zod
- **Charts**: Chart.js
- **Routing**: React Router v6

### DevOps & Infrastructure
- **Hosting**: Azure App Service + Azure Static Web Apps
- **CI/CD**: GitHub Actions
- **Monitoring**: Azure Application Insights + OpenTelemetry
- **Secrets**: Azure Key Vault

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for local database)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for deployment)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/abrmeval/personal-finance-tracker.git
   cd personal-finance-tracker
   ```

2. **Set up local database**
   ```bash
   docker run -d --name finance-db \
     -e POSTGRES_USER=finance \
     -e POSTGRES_PASSWORD=localdev \
     -e POSTGRES_DB=financetracker \
     -p 5432:5432 \
     postgres:16
   ```

3. **Configure backend**
   ```bash
   cd backend/src/Personal.FinanceTracker.Api
   
   # Update appsettings.Development.json with your connection string
   dotnet user-secrets set "ConnectionStrings:FinanceDb" "Host=localhost;Port=5432;Database=financetracker;Username=finance;Password=localdev"
   ```

4. **Run database migrations**
   ```bash
   # From backend directory
   dotnet ef database update --project src/Modules/Finance
   dotnet ef database update --project src/Modules/Users
   dotnet ef database update --project src/Modules/Reporting
   ```

5. **Start the API**
   ```bash
   dotnet run --project src/Personal.FinanceTracker.Api
   # API will run at https://localhost:5001
   ```

6. **Set up frontend**
   ```bash
   cd ../../frontend
   npm install
   
   # Create .env.local
   echo "VITE_API_URL=https://localhost:5001" > .env.local
   ```

7. **Start the frontend**
   ```bash
   npm run dev
   # Frontend will run at http://localhost:5173
   ```

---

## 📁 Project Structure

```
personal-finance-tracker/
│
├── 📁 backend/                          # .NET Backend
│   ├── 📄 Personal.FinanceTracker.sln   # Solution file
│   ├── 📄 Directory.Build.props         # Shared build properties
│   │
│   ├── 📁 src/
│   │   ├── 📁 Personal.FinanceTracker.Api/      # API Host
│   │   ├── 📁 Personal.FinanceTracker.Shared/   # Shared Kernel
│   │   └── 📁 Modules/                          # Feature Modules
│   │       ├── 📁 Finance/              # Finance module (transactions, budgets)
│   │       ├── 📁 Users/                # Users module (auth, profiles)
│   │       └── 📁 Reporting/            # Reporting module (analytics)
│   │
│   └── 📁 tests/                        # Test Projects
│       ├── 📁 Finance.UnitTests/
│       ├── 📁 Finance.IntegrationTests/
│       └── 📁 Api.IntegrationTests/
│
├── 📁 frontend/                         # React Frontend
│   ├── 📁 src/
│   │   ├── 📁 api/                      # API client
│   │   ├── 📁 components/               # Reusable components
│   │   ├── 📁 features/                 # Feature-specific components
│   │   ├── 📁 hooks/                    # Custom React hooks
│   │   └── 📁 types/                    # TypeScript types
│   ├── 📄 package.json
│   └── 📄 vite.config.ts
│
├── 📁 .github/workflows/                # CI/CD Pipelines
│   ├── 📄 api-deploy.yml
│   ├── 📄 frontend-deploy.yml
│   └── 📄 ci-checks.yml
│
├── 📁 docs/                             # Detailed Documentation
│   ├── 📄 01-Project-Structure.md       # Architecture overview
│   ├── 📄 02-Backend-Documentation.md   # Backend details
│   ├── 📄 03-Frontend-Documentation.md  # Frontend details
│   ├── 📄 04-DevOps-Deployment.md       # Deployment guide
│   └── 📄 05-Infrastructure.md          # Infrastructure setup
│
├── 📄 .gitignore
├── 📄 LICENSE
└── 📄 README.md
```

---

## 💻 Development

### Backend Commands

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/Personal.FinanceTracker.Api

# Create migration (example for Finance module)
dotnet ef migrations add InitialCreate --project src/Modules/Finance

# Update database
dotnet ef database update --project src/Modules/Finance

# Format code
dotnet format
```

### Frontend Commands

```bash
# Install dependencies
npm install

# Start dev server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linting
npm run lint

# Run type checking
npm run type-check
```

### Testing

```bash
# Backend - Run all tests
dotnet test

# Backend - Run specific test project
dotnet test tests/Finance.UnitTests

# Backend - Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Frontend - Run tests
npm test

# Frontend - Run with coverage
npm run test:coverage
```

---

## 🚢 Deployment

### Azure Deployment

The application is deployed to Azure using GitHub Actions:

- **API**: Azure App Service (Linux)
- **Frontend**: Azure Static Web Apps
- **Database**: Neon PostgreSQL (external)

### Deployment Workflow

1. Push to `main` branch triggers CI/CD
2. Backend is built and tested
3. Frontend is built and optimized
4. API is deployed to Azure App Service
5. Frontend is deployed to Azure Static Web Apps

### Manual Deployment

```bash
# Deploy API
az webapp up \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --runtime "DOTNETCORE:8.0"

# Deploy Frontend
cd frontend
npm run build
az staticwebapp deploy \
  --name frontend-finance-tracker \
  --resource-group rg-finance-tracker
```

For detailed deployment instructions, see [04-DevOps-Deployment.md](docs/04-DevOps-Deployment.md).

---

## 📚 Documentation

Comprehensive documentation is available in the `docs/` folder:

| Document | Description |
|----------|-------------|
| [01-Project-Structure.md](docs/01-Project-Structure.md) | Architecture and project organization |
| [02-Backend-Documentation.md](docs/02-Backend-Documentation.md) | Backend implementation details |
| [03-Frontend-Documentation.md](docs/03-Frontend-Documentation.md) | Frontend implementation details |
| [04-DevOps-Deployment.md](docs/04-DevOps-Deployment.md) | CI/CD and deployment guide |
| [05-Infrastructure.md](docs/05-Infrastructure.md) | Database and infrastructure setup |

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Coding Standards

- Follow C# coding conventions for backend
- Use ESLint and Prettier for frontend
- Write unit tests for new features
- Update documentation as needed

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👤 Author

**abrmeval**
- GitHub: [@abrmeval](https://github.com/abrmeval)

---

## 🙏 Acknowledgments

- [ASP.NET Core](https://docs.microsoft.com/aspnet/core/) - Backend framework
- [React](https://react.dev/) - Frontend library
- [Neon](https://neon.tech/) - Serverless PostgreSQL
- [Azure](https://azure.microsoft.com/) - Cloud hosting
- [Tailwind CSS](https://tailwindcss.com/) - CSS framework

---

<div align="center">
  <p>Built with ❤️ using .NET and React</p>
</div>
