# Agency Management System (AMS)

## Project Overview
Full-stack insurance agency management system built with ASP.NET Framework Web API, SQL Server, React, and Azure services.

## Architecture
- **Backend**: ASP.NET Framework 4.8 Web API (C#)
- **Database**: Azure SQL Server with Entity Framework 6
- **Frontend**: React with TypeScript and Redux
- **Azure Services**: Function App, Application Insights, Key Vault, Service Bus, Storage

## Key Features
- Client, Policy, and Claims Management
- JWT Authentication with Role-Based Authorization
- JSON-based FluentValidation
- Azure Service Bus for async messaging
- Application Insights for telemetry

## Development Guidelines
- Follow Repository Pattern with Unit of Work
- Use FluentValidation for all input validation
- Log all operations via Application Insights
- Store secrets in Azure Key Vault
- Use stored procedures for complex queries
