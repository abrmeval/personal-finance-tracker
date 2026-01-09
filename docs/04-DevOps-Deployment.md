# Personal Finance Tracker - DevOps & Deployment

> **Azure Deployment** | App Service • Static Web Apps • GitHub Actions  
> CI/CD • Environment Configuration • Monitoring

---

## 1. Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              GitHub Repository                               │
│  ┌─────────────────────┐                    ┌─────────────────────┐         │
│  │   Push to main      │                    │   Pull Request      │         │
│  └──────────┬──────────┘                    └──────────┬──────────┘         │
└─────────────┼───────────────────────────────────────────┼───────────────────┘
              │                                           │
              ▼                                           ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           GitHub Actions                                     │
│  ┌─────────────────────────────┐    ┌─────────────────────────────┐         │
│  │   api-deploy.yml            │    │   ci-checks.yml             │         │
│  │   • Build .NET API          │    │   • Run unit tests          │         │
│  │   • Run tests               │    │   • Run integration tests   │         │
│  │   • Deploy to Azure         │    │   • Lint frontend           │         │
│  └──────────┬──────────────────┘    └─────────────────────────────┘         │
│             │                                                                │
│  ┌──────────┴──────────────────┐                                            │
│  │   frontend-deploy.yml       │                                            │
│  │   • Build React app         │                                            │
│  │   • Deploy to Static Web App│                                            │
│  └──────────┬──────────────────┘                                            │
└─────────────┼───────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                               Azure Cloud                                    │
│                                                                              │
│  ┌─────────────────────────┐         ┌─────────────────────────┐            │
│  │   Azure App Service     │         │  Azure Static Web Apps  │            │
│  │   (API Backend)         │         │  (React Frontend)       │            │
│  │   ┌─────────────────┐   │         │  ┌─────────────────┐    │            │
│  │   │ .NET 8 API      │   │◄────────│  │ React SPA       │    │            │
│  │   │ /api/*          │   │         │  │ CDN distributed │    │            │
│  │   └────────┬────────┘   │         │  └─────────────────┘    │            │
│  └────────────┼────────────┘         └─────────────────────────┘            │
│               │                                                              │
│               ▼                                                              │
│  ┌─────────────────────────┐         ┌─────────────────────────┐            │
│  │   Neon PostgreSQL       │         │   Azure Key Vault       │            │
│  │   (Serverless DB)       │         │   (Secrets Management)  │            │
│  └─────────────────────────┘         └─────────────────────────┘            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Azure Resources Setup

### Required Azure Resources

| Resource | Purpose | SKU/Tier |
|----------|---------|----------|
| **Resource Group** | Container for all resources | N/A |
| **App Service Plan** | Hosting plan for API | B1 (Basic) or higher |
| **App Service** | .NET API hosting | Linux |
| **Static Web App** | React frontend hosting | Free or Standard |
| **Key Vault** | Secrets management | Standard |
| **Application Insights** | Monitoring & telemetry | Per GB |

### Azure CLI Setup

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "Your Subscription Name"

# Create resource group
az group create \
  --name rg-finance-tracker \
  --location eastus

# Create App Service Plan
az appservice plan create \
  --name plan-finance-tracker \
  --resource-group rg-finance-tracker \
  --sku B1 \
  --is-linux

# Create App Service for API
az webapp create \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --plan plan-finance-tracker \
  --runtime "DOTNET|8.0"

# Create Static Web App for Frontend
az staticwebapp create \
  --name web-finance-tracker \
  --resource-group rg-finance-tracker \
  --source https://github.com/your-org/finance-tracker \
  --location eastus2 \
  --branch main \
  --app-location "/frontend" \
  --output-location "dist" \
  --login-with-github

# Create Key Vault
az keyvault create \
  --name kv-finance-tracker \
  --resource-group rg-finance-tracker \
  --location eastus

# Create Application Insights
az monitor app-insights component create \
  --app appi-finance-tracker \
  --location eastus \
  --resource-group rg-finance-tracker \
  --application-type web
```

### Configure App Service

```bash
# Enable managed identity
az webapp identity assign \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker

# Configure app settings
az webapp config appsettings set \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    APPLICATIONINSIGHTS_CONNECTION_STRING="@Microsoft.KeyVault(SecretUri=https://kv-finance-tracker.vault.azure.net/secrets/AppInsightsConnectionString)"

# Configure connection string (from Key Vault)
az webapp config connection-string set \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --connection-string-type PostgreSQL \
  --settings \
    FinanceDb="@Microsoft.KeyVault(SecretUri=https://kv-finance-tracker.vault.azure.net/secrets/FinanceDbConnection)"

# Grant Key Vault access to App Service
PRINCIPAL_ID=$(az webapp identity show --name api-finance-tracker --resource-group rg-finance-tracker --query principalId -o tsv)

az keyvault set-policy \
  --name kv-finance-tracker \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

---

## 3. GitHub Actions Workflows

### API Deployment Workflow

```yaml
# .github/workflows/api-deploy.yml
name: Deploy API to Azure

on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - 'tests/**'
      - '.github/workflows/api-deploy.yml'
  workflow_dispatch:

env:
  DOTNET_VERSION: '8.0.x'
  AZURE_WEBAPP_NAME: api-finance-tracker

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Run unit tests
        run: dotnet test tests/**/*.UnitTests.csproj --configuration Release --no-build --verbosity normal

      - name: Run integration tests
        run: dotnet test tests/**/*.IntegrationTests.csproj --configuration Release --no-build --verbosity normal
        env:
          ConnectionStrings__FinanceDb: ${{ secrets.TEST_DATABASE_URL }}

      - name: Publish
        run: dotnet publish backend/src/PersonalFinanceTracker.Api/PersonalFinanceTracker.Api.csproj --configuration Release --output ./publish

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: api-package
          path: ./publish

  deploy:
    needs: build-and-test
    runs-on: ubuntu-latest
    environment: production
    
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: api-package
          path: ./publish

      - name: Login to Azure
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          package: ./publish

      - name: Azure logout
        run: az logout
```

### Frontend Deployment Workflow

```yaml
# .github/workflows/frontend-deploy.yml
name: Deploy Frontend to Azure Static Web Apps

on:
  push:
    branches: [main]
    paths:
      - 'frontend/**'
      - '.github/workflows/frontend-deploy.yml'
  pull_request:
    branches: [main]
    paths:
      - 'frontend/**'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        working-directory: frontend
        run: npm ci

      - name: Run linting
        working-directory: frontend
        run: npm run lint

      - name: Run tests
        working-directory: frontend
        run: npm run test -- --run

      - name: Build
        working-directory: frontend
        run: npm run build
        env:
          VITE_API_URL: ${{ vars.API_URL }}

      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "/frontend"
          output_location: "dist"
          skip_app_build: true
```

### CI Checks Workflow (Pull Requests)

```yaml
# .github/workflows/ci-checks.yml
name: CI Checks

on:
  pull_request:
    branches: [main, develop]

jobs:
  backend-checks:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal

      - name: Check formatting
        run: dotnet format --verify-no-changes

  frontend-checks:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        working-directory: frontend
        run: npm ci

      - name: Lint
        working-directory: frontend
        run: npm run lint

      - name: Type check
        working-directory: frontend
        run: npm run type-check

      - name: Test
        working-directory: frontend
        run: npm run test -- --run

      - name: Build
        working-directory: frontend
        run: npm run build
```

### Database Migration Workflow

```yaml
# .github/workflows/db-migrate.yml
name: Run Database Migrations

on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        default: 'staging'
        type: choice
        options:
          - staging
          - production

jobs:
  migrate:
    runs-on: ubuntu-latest
    environment: ${{ github.event.inputs.environment }}
    
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Install EF Core tools
        run: dotnet tool install --global dotnet-ef

      - name: Run Finance module migrations
        run: |
          dotnet ef database update \
            --project backend/src/Modules/Finance \
            --startup-project backend/src/PersonalFinanceTracker.Api \
            --context FinanceDbContext
        env:
          ConnectionStrings__FinanceDb: ${{ secrets.DATABASE_URL }}

      - name: Run Users module migrations
        run: |
          dotnet ef database update \
            --project backend/src/Modules/Users \
            --startup-project backend/src/PersonalFinanceTracker.Api \
            --context UsersDbContext
        env:
          ConnectionStrings__UsersDb: ${{ secrets.DATABASE_URL }}
```

---

## 4. GitHub Secrets Configuration

### Required Secrets

| Secret Name | Description | Where to Get |
|-------------|-------------|--------------|
| `AZURE_CREDENTIALS` | Service principal JSON | Azure CLI |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | SWA deployment token | Azure Portal |
| `DATABASE_URL` | Neon PostgreSQL connection string | Neon Dashboard |
| `TEST_DATABASE_URL` | Test database connection | Neon Dashboard |

### Creating Azure Service Principal

```bash
# Create service principal with contributor access
az ad sp create-for-rbac \
  --name "github-finance-tracker" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-finance-tracker \
  --sdk-auth

# Output will be JSON - copy this to AZURE_CREDENTIALS secret
{
  "clientId": "xxx",
  "clientSecret": "xxx",
  "subscriptionId": "xxx",
  "tenantId": "xxx",
  ...
}
```

### Getting Static Web App Token

```bash
# Get deployment token
az staticwebapp secrets list \
  --name web-finance-tracker \
  --resource-group rg-finance-tracker \
  --query "properties.apiKey" -o tsv
```

---

## 5. Environment Configuration

### API Configuration (appsettings.Production.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "https://web-finance-tracker.azurestaticapps.net",
      "https://your-custom-domain.com"
    ]
  },
  "Jwt": {
    "Issuer": "https://api-finance-tracker.azurewebsites.net",
    "Audience": "finance-tracker-api",
    "ExpirationMinutes": 60
  },
  "Otlp": {
    "Endpoint": "https://your-otlp-endpoint"
  }
}
```

### Frontend Environment (.env.production)

```bash
VITE_API_URL=https://api-finance-tracker.azurewebsites.net/api
VITE_APP_NAME=Personal Finance Tracker
```

### Static Web App Configuration

```json
// frontend/staticwebapp.config.json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*", "/*.js", "/*.css"]
  },
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["authenticated"]
    }
  ],
  "responseOverrides": {
    "401": {
      "statusCode": 302,
      "redirect": "/login"
    }
  },
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "Content-Security-Policy": "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';"
  }
}
```

---

## 6. Azure Key Vault Integration

### Storing Secrets

```bash
# Store database connection string
az keyvault secret set \
  --vault-name kv-finance-tracker \
  --name FinanceDbConnection \
  --value "Host=ep-xxx.us-east-2.aws.neon.tech;Database=financetracker;Username=finance;Password=xxx;SSL Mode=Require"

# Store JWT secret
az keyvault secret set \
  --vault-name kv-finance-tracker \
  --name JwtSecret \
  --value "your-super-secret-jwt-signing-key-at-least-32-chars"

# Store Application Insights connection string
az keyvault secret set \
  --vault-name kv-finance-tracker \
  --name AppInsightsConnectionString \
  --value "InstrumentationKey=xxx;IngestionEndpoint=xxx"
```

### Using Key Vault in .NET

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl!),
        new DefaultAzureCredential());
}
```

```xml
<!-- Add NuGet packages -->
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.3.0" />
<PackageReference Include="Azure.Identity" Version="1.10.4" />
```

---

## 7. Monitoring & Observability

### Application Insights Setup

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// Add custom telemetry
builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
```

### Custom Telemetry Initializer

```csharp
// Infrastructure/Telemetry/CustomTelemetryInitializer.cs
public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "PersonalFinanceTracker.Api";
        telemetry.Context.Component.Version = 
            Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    }
}
```

### Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("FinanceDb")!,
        name: "database",
        tags: new[] { "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

// Map health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Azure Monitor Alerts

```bash
# Create alert for high error rate
az monitor metrics alert create \
  --name "High Error Rate" \
  --resource-group rg-finance-tracker \
  --scopes /subscriptions/{sub}/resourceGroups/rg-finance-tracker/providers/Microsoft.Web/sites/api-finance-tracker \
  --condition "count requests/failed > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group ag-finance-alerts

# Create alert for high response time
az monitor metrics alert create \
  --name "High Response Time" \
  --resource-group rg-finance-tracker \
  --scopes /subscriptions/{sub}/resourceGroups/rg-finance-tracker/providers/Microsoft.Web/sites/api-finance-tracker \
  --condition "avg requests/duration > 2000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group ag-finance-alerts
```

---

## 8. Custom Domain & SSL

### Configure Custom Domain for App Service

```bash
# Add custom domain
az webapp config hostname add \
  --webapp-name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --hostname api.yourfinanceapp.com

# Create managed certificate
az webapp config ssl create \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --hostname api.yourfinanceapp.com

# Bind SSL certificate
az webapp config ssl bind \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --certificate-thumbprint <thumbprint> \
  --ssl-type SNI
```

### Configure Custom Domain for Static Web App

```bash
# Add custom domain
az staticwebapp hostname set \
  --name web-finance-tracker \
  --resource-group rg-finance-tracker \
  --hostname app.yourfinanceapp.com

# SSL is automatically provisioned for Static Web Apps
```

### DNS Configuration

```
# API subdomain (CNAME)
api.yourfinanceapp.com → api-finance-tracker.azurewebsites.net

# Frontend subdomain (CNAME)  
app.yourfinanceapp.com → web-finance-tracker.azurestaticapps.net

# Or use Azure DNS zone
```

---

## 9. Rollback Strategy

### App Service Deployment Slots

```bash
# Create staging slot
az webapp deployment slot create \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --slot staging

# Deploy to staging first (modify workflow)
# Then swap when ready
az webapp deployment slot swap \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --slot staging \
  --target-slot production

# Rollback by swapping back
az webapp deployment slot swap \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --slot production \
  --target-slot staging
```

### GitHub Actions with Staging

```yaml
# Updated deploy job with staging slot
deploy:
  needs: build-and-test
  runs-on: ubuntu-latest
  environment: staging
  
  steps:
    - name: Deploy to staging slot
      uses: azure/webapps-deploy@v3
      with:
        app-name: api-finance-tracker
        slot-name: staging
        package: ./publish

    - name: Run smoke tests
      run: |
        response=$(curl -s -o /dev/null -w "%{http_code}" https://api-finance-tracker-staging.azurewebsites.net/health/ready)
        if [ $response != "200" ]; then
          echo "Smoke test failed!"
          exit 1
        fi

    - name: Swap to production
      uses: azure/cli@v2
      with:
        inlineScript: |
          az webapp deployment slot swap \
            --name api-finance-tracker \
            --resource-group rg-finance-tracker \
            --slot staging \
            --target-slot production
```

---

## 10. Cost Optimization

### Estimated Monthly Costs

| Resource | SKU | Estimated Cost |
|----------|-----|----------------|
| App Service Plan (B1) | Basic | ~$13/month |
| Static Web App | Free | $0 |
| Neon PostgreSQL | Free tier | $0 (up to limits) |
| Key Vault | Standard | ~$0.03/10K operations |
| Application Insights | Pay-as-you-go | ~$2-5/month |
| **Total** | | **~$15-20/month** |

### Cost Saving Tips

1. **Use Free tiers** where possible (Static Web Apps, Neon)
2. **Auto-shutdown** App Service during off-hours (dev environments)
3. **Right-size** App Service plan based on actual load
4. **Set up budget alerts** in Azure Cost Management

```bash
# Create budget alert
az consumption budget create \
  --budget-name finance-tracker-budget \
  --amount 50 \
  --category cost \
  --resource-group rg-finance-tracker \
  --time-grain monthly \
  --start-date 2024-01-01 \
  --end-date 2025-12-31
```

---

## 11. Security Checklist

- [ ] Enable HTTPS only on App Service
- [ ] Configure CORS properly (no wildcards in production)
- [ ] Store all secrets in Key Vault
- [ ] Enable managed identity for App Service
- [ ] Set up Application Insights for security monitoring
- [ ] Configure Web Application Firewall (WAF) if needed
- [ ] Enable diagnostic logging
- [ ] Review and rotate secrets regularly
- [ ] Set up IP restrictions if applicable
- [ ] Enable Azure Defender for App Service

```bash
# Enable HTTPS only
az webapp update \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --https-only true

# Set minimum TLS version
az webapp config set \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --min-tls-version 1.2
```

---

## 12. References

- [Azure App Service Documentation](https://learn.microsoft.com/en-us/azure/app-service/)
- [Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [GitHub Actions for Azure](https://learn.microsoft.com/en-us/azure/developer/github/github-actions)
- [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

---

*Previous: [03-Frontend-Documentation.md](./03-Frontend-Documentation.md)*  
*Next: [05-Infrastructure.md](./05-Infrastructure.md)*
