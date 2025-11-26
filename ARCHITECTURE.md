# Expense Management System - Azure Architecture

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Azure Resource Group                          │
│                      (rg-expensemgmt-demo)                          │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                ┌─────────────────┼─────────────────┐
                │                 │                 │
                ▼                 ▼                 ▼
    ┌───────────────────┐ ┌──────────────┐ ┌──────────────────┐
    │   App Service     │ │  Azure SQL   │ │ User Assigned    │
    │   (Linux, S1)     │ │  Database    │ │ Managed Identity │
    │   .NET 8          │ │  (Northwind) │ │                  │
    └───────────────────┘ └──────────────┘ └──────────────────┘
            │                     │                 │
            │                     │                 │
            └─────────────────────┴─────────────────┘
                          │
                          │ (Managed Identity Auth)
                          │
                          ▼
            ┌──────────────────────────┐
            │   Stored Procedures      │
            │   - sp_GetExpenses       │
            │   - sp_CreateExpense     │
            │   - sp_GetUsers          │
            │   - sp_CreateUser        │
            └──────────────────────────┘


## Optional GenAI Components (deploy-with-chat.sh)

                ┌─────────────────┐
                │  App Service    │
                │  (with Chat UI) │
                └────────┬────────┘
                         │
                         │ (Managed Identity)
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Azure OpenAI │ │ Azure AI     │ │  Function    │
│ (Sweden      │ │ Search       │ │  Calling     │
│  Central)    │ │ (Basic)      │ │  APIs        │
│              │ │              │ │              │
│ GPT-4o Model │ │ RAG Pattern  │ │              │
└──────────────┘ └──────────────┘ └──────────────┘


## Authentication Flow

1. User Assigned Managed Identity created
2. Identity assigned to App Service
3. Identity granted:
   - SQL Database roles (db_datareader, db_datawriter, EXECUTE)
   - Cognitive Services OpenAI User role
   - Search Index Data Contributor role
4. SQL Server granted Directory Reader role (for Entra ID auth)
5. Connection strings use:
   - "Authentication=Active Directory Managed Identity"
   - User Id=[ClientId]


## Data Flow

1. User → App Service (HTTPS)
2. App Service → Azure SQL (Managed Identity Auth)
3. App Service → Stored Procedures (No direct table access)
4. Chat UI → App Service APIs
5. App Service → Azure OpenAI (Function Calling)
6. Azure OpenAI → Function Execution → Database
7. Results → Azure OpenAI → Formatted Response → User


## Security Features

✅ Azure AD-only authentication (no SQL auth)
✅ Managed Identity (no connection strings with passwords)
✅ HTTPS only
✅ Stored procedures (no direct table access)
✅ TLS 1.2+ encryption
✅ Firewall rules for Azure services
✅ Role-based access control


## Deployment Options

### Option 1: Basic Deployment (deploy.sh)
- App Service + Azure SQL
- REST APIs
- Razor Pages UI
- No GenAI features

### Option 2: Full Deployment (deploy-with-chat.sh)
- Everything from Option 1
- Azure OpenAI with GPT-4o
- Azure AI Search
- Chat UI with AI Assistant
- Function calling capabilities


## Cost Estimation (UK South)

### Basic Deployment:
- App Service (S1): ~£56/month
- Azure SQL (Basic): ~£4/month
- Total: ~£60/month

### With GenAI:
- Above plus:
- Azure OpenAI (S0): Pay per token
- Azure AI Search (Basic): ~£57/month
- Total: ~£117/month + token usage


## Endpoints

- Main App: https://[app-name].azurewebsites.net/Index
- Expenses: https://[app-name].azurewebsites.net/Expenses
- Users: https://[app-name].azurewebsites.net/Users
- Chat: https://[app-name].azurewebsites.net/Chat
- API Docs: https://[app-name].azurewebsites.net/swagger
```

## Azure Best Practices Implemented

1. **Identity & Access**
   - User-assigned managed identities
   - Azure AD-only authentication
   - Least privilege access

2. **Security**
   - HTTPS enforced
   - TLS 1.2 minimum
   - No hardcoded secrets
   - Network isolation with firewall rules

3. **Resilience**
   - S1 tier (no cold start)
   - Connection retry logic
   - Error handling with fallbacks

4. **Cost Optimization**
   - Basic tier for SQL (dev/test)
   - S0 tier for AI services
   - Pay-per-use for OpenAI

5. **Monitoring**
   - Application logging
   - Error tracking
   - Connection diagnostics

## Reference
Based on Azure best practices from: https://learn.microsoft.com/azure
