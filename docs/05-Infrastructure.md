# Personal Finance Tracker - Infrastructure Documentation

> **Infrastructure as Code** | Neon PostgreSQL • Azure Resources • Terraform  
> Database Schema • Connection Management • Scaling

---

## 1. Infrastructure Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Production Environment                             │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                         Azure Cloud                                  │    │
│  │                                                                      │    │
│  │   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐          │    │
│  │   │  Azure DNS   │    │  Key Vault   │    │  App Insights│          │    │
│  │   │              │    │  (Secrets)   │    │  (Monitoring)│          │    │
│  │   └──────────────┘    └──────────────┘    └──────────────┘          │    │
│  │                                                                      │    │
│  │   ┌─────────────────────────┐   ┌─────────────────────────┐         │    │
│  │   │    App Service Plan     │   │   Static Web Apps       │         │    │
│  │   │   ┌─────────────────┐   │   │   ┌─────────────────┐   │         │    │
│  │   │   │  App Service    │   │   │   │  React SPA      │   │         │    │
│  │   │   │  (.NET 8 API)   │   │   │   │  (CDN cached)   │   │         │    │
│  │   │   │  - Staging slot │   │   │   └─────────────────┘   │         │    │
│  │   │   │  - Prod slot    │   │   │                         │         │    │
│  │   │   └─────────────────┘   │   └─────────────────────────┘         │    │
│  │   └─────────────────────────┘                                       │    │
│  │                                                                      │    │
│  └──────────────────────────────────────────────────────────────────────┘    │
│                                     │                                        │
│                                     │ SSL/TLS                                │
│                                     ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                        Neon PostgreSQL                                │   │
│  │                    (Serverless Database)                              │   │
│  │                                                                       │   │
│  │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │   │
│  │   │ finances.*  │  │  users.*    │  │  reports.*  │                  │   │
│  │   │ - transactions │ - users     │  │ - monthly   │                  │   │
│  │   │ - categories │  │ - tokens   │  │   _summaries│                  │   │
│  │   │ - budgets   │  └─────────────┘  └─────────────┘                  │   │
│  │   └─────────────┘                                                     │   │
│  │                                                                       │   │
│  │   Features: Auto-scaling, Branching, Point-in-time recovery          │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Neon PostgreSQL Setup

### Why Neon?

| Feature | Benefit |
|---------|---------|
| **Serverless** | Auto-scales to zero, pay only for usage |
| **Branching** | Create database branches for development/testing |
| **Free Tier** | Generous free tier for side projects |
| **PostgreSQL Native** | Full PostgreSQL compatibility |
| **Connection Pooling** | Built-in pgbouncer for connection management |

### Create Neon Project

1. Sign up at [neon.tech](https://neon.tech)
2. Create a new project
3. Note the connection string

```bash
# Connection string format
postgresql://[user]:[password]@[endpoint].neon.tech/[database]?sslmode=require

# Example
postgresql://finance:abc123@ep-cool-wind-123456.us-east-2.aws.neon.tech/financetracker?sslmode=require
```

### Connection String Configuration

```json
// appsettings.json (local development)
{
  "ConnectionStrings": {
    "FinanceDb": "Host=ep-xxx.us-east-2.aws.neon.tech;Database=financetracker;Username=finance;Password=localdevpassword;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=1;Maximum Pool Size=20"
  }
}
```

### Neon Database Branching

```bash
# Create a development branch from main
neon branches create --name dev --parent main

# Create a branch for feature development
neon branches create --name feature/user-auth --parent main

# List branches
neon branches list

# Delete branch when done
neon branches delete feature/user-auth
```

### Using Branches in Development

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "FinanceDb": "Host=ep-xxx-dev.us-east-2.aws.neon.tech;Database=financetracker;..."
  }
}
```

---

## 3. Database Schema Design

### Schema Isolation by Module

```sql
-- Create schemas for each module
CREATE SCHEMA IF NOT EXISTS finances;
CREATE SCHEMA IF NOT EXISTS users;
CREATE SCHEMA IF NOT EXISTS reports;

-- Set search path for each application context
SET search_path TO finances, public;
```

### Finances Schema

```sql
-- finances.categories
CREATE TABLE finances.categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    icon VARCHAR(50),
    color VARCHAR(7),
    user_id UUID NOT NULL,
    is_default BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    
    CONSTRAINT fk_categories_user FOREIGN KEY (user_id) 
        REFERENCES users.users(id) ON DELETE CASCADE
);

CREATE INDEX idx_categories_user_id ON finances.categories(user_id);

-- finances.transactions
CREATE TABLE finances.transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    description VARCHAR(500) NOT NULL,
    amount DECIMAL(18, 2) NOT NULL CHECK (amount > 0),
    type VARCHAR(20) NOT NULL CHECK (type IN ('Income', 'Expense')),
    date DATE NOT NULL,
    user_id UUID NOT NULL,
    category_id UUID,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    
    CONSTRAINT fk_transactions_user FOREIGN KEY (user_id) 
        REFERENCES users.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_transactions_category FOREIGN KEY (category_id) 
        REFERENCES finances.categories(id) ON DELETE SET NULL
);

CREATE INDEX idx_transactions_user_id ON finances.transactions(user_id);
CREATE INDEX idx_transactions_date ON finances.transactions(date);
CREATE INDEX idx_transactions_user_date ON finances.transactions(user_id, date);
CREATE INDEX idx_transactions_category ON finances.transactions(category_id);

-- finances.budgets
CREATE TABLE finances.budgets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    amount DECIMAL(18, 2) NOT NULL CHECK (amount > 0),
    period VARCHAR(20) NOT NULL CHECK (period IN ('Daily', 'Weekly', 'Monthly', 'Yearly')),
    start_date DATE NOT NULL,
    end_date DATE,
    user_id UUID NOT NULL,
    category_id UUID NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    
    CONSTRAINT fk_budgets_user FOREIGN KEY (user_id) 
        REFERENCES users.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_budgets_category FOREIGN KEY (category_id) 
        REFERENCES finances.categories(id) ON DELETE CASCADE,
    CONSTRAINT chk_budget_dates CHECK (end_date IS NULL OR end_date > start_date)
);

CREATE INDEX idx_budgets_user_id ON finances.budgets(user_id);
CREATE INDEX idx_budgets_category_id ON finances.budgets(category_id);
```

### Users Schema

```sql
-- users.users
CREATE TABLE users.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    email_verified BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    last_login_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX idx_users_email ON users.users(email);

-- users.refresh_tokens
CREATE TABLE users.refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    token VARCHAR(500) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMPTZ,
    replaced_by_token VARCHAR(500),
    
    CONSTRAINT fk_refresh_tokens_user FOREIGN KEY (user_id) 
        REFERENCES users.users(id) ON DELETE CASCADE
);

CREATE INDEX idx_refresh_tokens_user_id ON users.refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token ON users.refresh_tokens(token);
```

### Reports Schema

```sql
-- reports.monthly_summaries (materialized/cached data)
CREATE TABLE reports.monthly_summaries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    year INT NOT NULL,
    month INT NOT NULL CHECK (month BETWEEN 1 AND 12),
    total_income DECIMAL(18, 2) DEFAULT 0,
    total_expenses DECIMAL(18, 2) DEFAULT 0,
    net_savings DECIMAL(18, 2) DEFAULT 0,
    transaction_count INT DEFAULT 0,
    generated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_monthly_summaries_user FOREIGN KEY (user_id) 
        REFERENCES users.users(id) ON DELETE CASCADE,
    CONSTRAINT uq_monthly_summary UNIQUE (user_id, year, month)
);

CREATE INDEX idx_monthly_summaries_user_year_month 
    ON reports.monthly_summaries(user_id, year, month);

-- reports.category_breakdowns
CREATE TABLE reports.category_breakdowns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    monthly_summary_id UUID NOT NULL,
    category_id UUID,
    category_name VARCHAR(100) NOT NULL,
    total_amount DECIMAL(18, 2) DEFAULT 0,
    transaction_count INT DEFAULT 0,
    percentage DECIMAL(5, 2) DEFAULT 0,
    
    CONSTRAINT fk_category_breakdowns_summary FOREIGN KEY (monthly_summary_id) 
        REFERENCES reports.monthly_summaries(id) ON DELETE CASCADE
);

CREATE INDEX idx_category_breakdowns_summary 
    ON reports.category_breakdowns(monthly_summary_id);
```

### Entity Relationship Diagram

```
┌─────────────────────┐         ┌─────────────────────┐
│     users.users     │         │ users.refresh_tokens│
├─────────────────────┤         ├─────────────────────┤
│ id (PK)             │◄────────│ user_id (FK)        │
│ email               │         │ token               │
│ password_hash       │         │ expires_at          │
│ first_name          │         └─────────────────────┘
│ last_name           │
└─────────┬───────────┘
          │
          │ 1:N
          │
┌─────────▼───────────┐         ┌─────────────────────┐
│ finances.categories │◄────────│ finances.budgets    │
├─────────────────────┤    1:N  ├─────────────────────┤
│ id (PK)             │         │ id (PK)             │
│ user_id (FK)        │         │ user_id (FK)        │
│ name                │         │ category_id (FK)    │
│ icon                │         │ amount              │
│ color               │         │ period              │
└─────────┬───────────┘         └─────────────────────┘
          │
          │ 1:N
          │
┌─────────▼───────────┐
│finances.transactions│
├─────────────────────┤
│ id (PK)             │
│ user_id (FK)        │
│ category_id (FK)    │
│ description         │
│ amount              │
│ type                │
│ date                │
└─────────────────────┘
```

---

## 4. Connection Management

### Connection Pooling with Npgsql

```csharp
// FinanceModule.cs
services.AddDbContext<FinanceDbContext>(options =>
{
    options.UseNpgsql(
        configuration.GetConnectionString("FinanceDb"),
        npgsqlOptions =>
        {
            // Connection pooling settings
            npgsqlOptions.CommandTimeout(30);
            
            // Enable retry for transient failures
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
});

// Configure Npgsql connection pool at startup
NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
```

### Connection String Best Practices

```csharp
// Connection string builder for production
var builder = new NpgsqlConnectionStringBuilder
{
    Host = Environment.GetEnvironmentVariable("DB_HOST"),
    Database = "financetracker",
    Username = Environment.GetEnvironmentVariable("DB_USER"),
    Password = Environment.GetEnvironmentVariable("DB_PASSWORD"),
    SslMode = SslMode.Require,
    TrustServerCertificate = true,
    
    // Pool settings
    Pooling = true,
    MinPoolSize = 1,
    MaxPoolSize = 20,
    ConnectionIdleLifetime = 300,  // 5 minutes
    ConnectionPruningInterval = 10,
    
    // Timeout settings
    Timeout = 30,
    CommandTimeout = 30,
    
    // Keepalive for serverless
    KeepAlive = 30
};
```

### Neon Connection Pooling

Neon provides built-in pgbouncer pooling. Use the pooled connection endpoint:

```
# Standard endpoint (direct connection)
postgresql://user:pass@ep-xxx.us-east-2.aws.neon.tech/db

# Pooled endpoint (recommended for production)
postgresql://user:pass@ep-xxx-pooler.us-east-2.aws.neon.tech/db
```

---

## 5. Terraform Infrastructure as Code

### Project Structure

```
infrastructure/
├── main.tf
├── variables.tf
├── outputs.tf
├── providers.tf
├── modules/
│   ├── app-service/
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── outputs.tf
│   ├── static-web-app/
│   │   └── ...
│   └── key-vault/
│       └── ...
└── environments/
    ├── dev.tfvars
    ├── staging.tfvars
    └── prod.tfvars
```

### Main Terraform Configuration

```hcl
# infrastructure/providers.tf
terraform {
  required_version = ">= 1.5.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.80"
    }
  }
  
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "stterraformstate"
    container_name       = "tfstate"
    key                  = "finance-tracker.tfstate"
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = false
    }
  }
}
```

```hcl
# infrastructure/main.tf
locals {
  environment = var.environment
  location    = var.location
  project     = "finance-tracker"
  
  tags = {
    Project     = local.project
    Environment = local.environment
    ManagedBy   = "Terraform"
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "rg-${local.project}-${local.environment}"
  location = local.location
  tags     = local.tags
}

# App Service Plan
resource "azurerm_service_plan" "main" {
  name                = "plan-${local.project}-${local.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.app_service_sku
  tags                = local.tags
}

# App Service
resource "azurerm_linux_web_app" "api" {
  name                = "api-${local.project}-${local.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id
  https_only          = true
  tags                = local.tags

  site_config {
    always_on = var.app_service_sku != "F1"
    
    application_stack {
      dotnet_version = "8.0"
    }
    
    cors {
      allowed_origins = var.cors_allowed_origins
    }
  }

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                = var.environment == "prod" ? "Production" : "Development"
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
  }

  connection_string {
    name  = "FinanceDb"
    type  = "PostgreSQL"
    value = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.db_connection.id})"
  }
}

# Static Web App
resource "azurerm_static_web_app" "frontend" {
  name                = "web-${local.project}-${local.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = "eastus2"  # Limited regions for SWA
  sku_tier            = var.static_web_app_sku
  sku_size            = var.static_web_app_sku
  tags                = local.tags
}

# Key Vault
resource "azurerm_key_vault" "main" {
  name                = "kv-${local.project}-${local.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"
  tags                = local.tags

  purge_protection_enabled   = true
  soft_delete_retention_days = 7
}

# Key Vault Access Policy for App Service
resource "azurerm_key_vault_access_policy" "api" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_linux_web_app.api.identity[0].principal_id

  secret_permissions = ["Get", "List"]
}

# Key Vault Secret for DB Connection
resource "azurerm_key_vault_secret" "db_connection" {
  name         = "FinanceDbConnection"
  value        = var.database_connection_string
  key_vault_id = azurerm_key_vault.main.id
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "appi-${local.project}-${local.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  application_type    = "web"
  tags                = local.tags
}

# Data source for current Azure config
data "azurerm_client_config" "current" {}
```

```hcl
# infrastructure/variables.tf
variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus"
}

variable "app_service_sku" {
  description = "App Service Plan SKU"
  type        = string
  default     = "B1"
}

variable "static_web_app_sku" {
  description = "Static Web App SKU"
  type        = string
  default     = "Free"
}

variable "cors_allowed_origins" {
  description = "Allowed CORS origins"
  type        = list(string)
  default     = []
}

variable "database_connection_string" {
  description = "PostgreSQL connection string"
  type        = string
  sensitive   = true
}
```

```hcl
# infrastructure/outputs.tf
output "api_url" {
  value = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "frontend_url" {
  value = "https://${azurerm_static_web_app.frontend.default_host_name}"
}

output "app_insights_instrumentation_key" {
  value     = azurerm_application_insights.main.instrumentation_key
  sensitive = true
}
```

### Deploy with Terraform

```bash
# Initialize Terraform
cd infrastructure
terraform init

# Plan for production
terraform plan -var-file=environments/prod.tfvars -out=tfplan

# Apply
terraform apply tfplan

# Destroy (be careful!)
terraform destroy -var-file=environments/prod.tfvars
```

---

## 6. Database Migrations Strategy

### EF Core Migrations

```bash
# Create initial migration for Finance module
dotnet ef migrations add InitialFinanceSchema \
  --project backend/src/Modules/Finance \
  --startup-project backend/src/PersonalFinanceTracker.Api \
  --context FinanceDbContext \
  --output-dir Infrastructure/Migrations

# Create migration script for production
dotnet ef migrations script \
  --project backend/src/Modules/Finance \
  --startup-project backend/src/PersonalFinanceTracker.Api \
  --context FinanceDbContext \
  --idempotent \
  --output migrations/finance_migrations.sql
```

### Migration Strategy for Production

```csharp
// Program.cs - Safe migration approach
if (app.Environment.IsDevelopment())
{
    // Auto-migrate in development only
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    await dbContext.Database.MigrateAsync();
}
else
{
    // In production, use deployment pipeline to run migrations
    // This ensures migrations are reviewed before execution
}
```

### Migration Verification Script

```sql
-- Check pending migrations
SELECT * FROM "__EFMigrationsHistory" 
WHERE "MigrationId" NOT IN (
    SELECT "MigrationId" FROM applied_migrations
);

-- Rollback script (example)
-- DROP TABLE IF EXISTS finances.transactions;
-- DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240101000000_InitialCreate';
```

---

## 7. Backup and Recovery

### Neon Point-in-Time Recovery

```bash
# Neon automatically maintains 7-day PITR (free tier)
# For longer retention, upgrade to paid plan

# Restore to specific point in time via Neon Console
# Or use Neon CLI
neon branches create \
  --name recovery-branch \
  --parent main \
  --timestamp "2024-01-15T10:30:00Z"
```

### Manual Backup Script

```bash
#!/bin/bash
# backup.sh - Manual backup script

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups"
DB_URL="postgresql://user:pass@host/db"

# Create backup
pg_dump "$DB_URL" | gzip > "$BACKUP_DIR/financetracker_$DATE.sql.gz"

# Upload to Azure Blob Storage
az storage blob upload \
  --account-name stfinancebackups \
  --container-name backups \
  --file "$BACKUP_DIR/financetracker_$DATE.sql.gz" \
  --name "financetracker_$DATE.sql.gz"

# Clean up old local backups (keep last 7 days)
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +7 -delete
```

---

## 8. Scaling Considerations

### Vertical Scaling (App Service)

```bash
# Scale up App Service Plan
az appservice plan update \
  --name plan-finance-tracker \
  --resource-group rg-finance-tracker \
  --sku P1V2
```

### Horizontal Scaling (App Service)

```bash
# Manual scale out
az webapp scale \
  --name api-finance-tracker \
  --resource-group rg-finance-tracker \
  --instance-count 3

# Auto-scale rule
az monitor autoscale create \
  --resource-group rg-finance-tracker \
  --resource api-finance-tracker \
  --resource-type Microsoft.Web/sites \
  --name autoscale-finance \
  --min-count 1 \
  --max-count 5 \
  --count 2

az monitor autoscale rule create \
  --resource-group rg-finance-tracker \
  --autoscale-name autoscale-finance \
  --condition "CpuPercentage > 70 avg 5m" \
  --scale out 1
```

### Database Scaling (Neon)

```
# Neon Auto-scaling
- Free tier: 0.25 - 2 compute units
- Pro tier: 0.25 - 8 compute units
- Custom: Up to 56 compute units

# No manual scaling needed - Neon scales automatically
# based on workload
```

---

## 9. Disaster Recovery Plan

### Recovery Time Objective (RTO): 4 hours
### Recovery Point Objective (RPO): 1 hour

### DR Checklist

1. **Database Recovery**
   - Use Neon PITR to restore to latest point
   - Or restore from Azure Blob backup

2. **Application Recovery**
   - Redeploy from GitHub Actions
   - Restore App Service from deployment slots

3. **Configuration Recovery**
   - Key Vault secrets are geo-replicated
   - App settings stored in Terraform state

### Runbook: Full Recovery

```bash
# 1. Restore database
neon branches create --name recovery --parent main --timestamp "2024-01-15T10:00:00Z"

# 2. Update connection string in Key Vault
az keyvault secret set --vault-name kv-finance-tracker \
  --name FinanceDbConnection \
  --value "new-connection-string"

# 3. Restart App Service
az webapp restart --name api-finance-tracker --resource-group rg-finance-tracker

# 4. Verify health
curl https://api-finance-tracker.azurewebsites.net/health/ready
```

---

## 10. Cost Estimation

### Monthly Infrastructure Costs

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| App Service Plan | B1 (Basic) | ~$13 |
| Static Web App | Free | $0 |
| Neon PostgreSQL | Free tier | $0 |
| Key Vault | Standard | ~$0.03/10K ops |
| Application Insights | Pay-as-you-go | ~$2-5 |
| Azure DNS | Per zone | ~$0.50 |
| **Total (Dev/Small)** | | **~$16-20/month** |

### Production Scaling Costs

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| App Service Plan | P1V2 | ~$73 |
| Static Web App | Standard | $9 |
| Neon PostgreSQL | Pro | $19+ |
| Key Vault | Standard | ~$1 |
| Application Insights | Pay-as-you-go | ~$10-50 |
| **Total (Production)** | | **~$112-152/month** |

---

## 11. References

- [Neon PostgreSQL Documentation](https://neon.tech/docs)
- [Azure App Service Documentation](https://learn.microsoft.com/en-us/azure/app-service/)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Npgsql Documentation](https://www.npgsql.org/doc/)

---

*Previous: [04-DevOps-Deployment.md](./04-DevOps-Deployment.md)*
