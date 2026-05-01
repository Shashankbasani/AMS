# Agency Management System (AMS)

A comprehensive insurance agency management system built with ASP.NET Framework 4.8, SQL Server, and Azure services.

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         React Frontend                               │
│                    (TypeScript + Redux)                              │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       AMS.WebAPI                                     │
│              ASP.NET Framework 4.8 Web API                          │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────────┐ │
│  │ Controllers  │ │  Middleware  │ │ FluentValidation Validators  │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
┌─────────────────────┐ ┌──────────────────┐ ┌───────────────────────┐
│    AMS.Core         │ │  AMS.Functions   │ │   Azure Services      │
│  ┌───────────────┐  │ │  ┌────────────┐  │ │  ┌─────────────────┐  │
│  │ Entities      │  │ │  │ Timer      │  │ │  │ Key Vault       │  │
│  │ DTOs          │  │ │  │ Triggers   │  │ │  │ Service Bus     │  │
│  │ Interfaces    │  │ │  │ Queue      │  │ │  │ Storage         │  │
│  │ Exceptions    │  │ │  │ Processing │  │ │  │ App Insights    │  │
│  └───────────────┘  │ │  └────────────┘  │ │  └─────────────────┘  │
└─────────────────────┘ └──────────────────┘ └───────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      AMS.Infrastructure                              │
│  ┌─────────────────┐ ┌─────────────────┐ ┌────────────────────────┐ │
│  │ Entity Framework │ │ Repository     │ │ Azure Service Clients  │ │
│  │ DbContext       │ │ Pattern        │ │ (KeyVault, ServiceBus) │ │
│  └─────────────────┘ └─────────────────┘ └────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    SQL Server / Azure SQL                            │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────────────┐│
│  │ Client  │ │ Policy  │ │ Claim   │ │ User    │ │ AuditLog        ││
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────────────┘│
└─────────────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

```
AgencyManagementSystem/
├── src/
│   ├── AMS.Core/                  # Core domain layer
│   │   ├── Entities/              # Domain entities
│   │   ├── Enums/                 # Enumerations
│   │   ├── DTOs/                  # Data transfer objects
│   │   ├── Interfaces/            # Repository & service interfaces
│   │   └── Exceptions/            # Custom exceptions
│   │
│   ├── AMS.Infrastructure/        # Data access & external services
│   │   ├── Data/                  # EF DbContext & configurations
│   │   ├── Repositories/          # Repository implementations
│   │   └── Services/              # Azure service implementations
│   │
│   ├── AMS.WebAPI/               # Web API layer
│   │   ├── Controllers/          # API controllers
│   │   ├── Middleware/           # OWIN middleware
│   │   ├── Filters/              # Action filters
│   │   ├── Validators/           # FluentValidation validators
│   │   └── Services/             # Application services
│   │
│   └── AMS.Functions/            # Azure Functions (background tasks)
│
├── database/
│   └── schema/                   # SQL scripts
│       ├── 001_CreateTables.sql
│       ├── 002_StoredProcedures.sql
│       └── 003_SeedData.sql
│
├── tests/                        # Unit & integration tests
└── AMS.sln                       # Visual Studio solution
```

## 🚀 Getting Started

### Prerequisites

- Visual Studio 2022 (or Visual Studio Code)
- .NET Framework 4.8 SDK
- SQL Server 2019+ or Azure SQL Database
- Node.js 18+ (for React frontend)
- Azure subscription (optional, for cloud services)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/AgencyManagementSystem.git
   cd AgencyManagementSystem
   ```

2. **Setup the database**
   ```bash
   # Using SQL Server Management Studio or Azure Data Studio
   # Run scripts in order:
   database/schema/001_CreateTables.sql
   database/schema/002_StoredProcedures.sql
   database/schema/003_SeedData.sql
   ```

3. **Configure connection strings**
   
   Update `src/AMS.WebAPI/Web.config`:
   ```xml
   <connectionStrings>
     <add name="AMSConnection" 
          connectionString="Server=(localdb)\MSSQLLocalDB;Database=AgencyManagementSystem;Integrated Security=true;" 
          providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```

4. **Configure app settings**
   
   Update JWT and Azure settings in `Web.config`:
   ```xml
   <appSettings>
     <add key="Jwt:SecretKey" value="YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS" />
     <add key="Azure:KeyVaultUri" value="https://your-vault.vault.azure.net/" />
     <!-- Add other Azure connection strings -->
   </appSettings>
   ```

5. **Restore NuGet packages**
   ```bash
   nuget restore AMS.sln
   ```

6. **Build and run**
   ```bash
   msbuild AMS.sln
   # Or use Visual Studio: F5
   ```

7. **Access the API**
   - Swagger UI: `https://localhost:44300/swagger`
   - API Base URL: `https://localhost:44300/api`

## 🔐 Authentication

The API uses JWT Bearer token authentication.

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@123"
}
```

### Response
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
    "expiresAt": "2024-01-15T10:30:00Z"
  }
}
```

### Using the token
```http
GET /api/clients
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 📚 API Endpoints

### Clients
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/clients` | Get all clients (paginated) |
| GET | `/api/clients/{id}` | Get client by ID |
| POST | `/api/clients` | Create new client |
| PUT | `/api/clients/{id}` | Update client |
| DELETE | `/api/clients/{id}` | Delete client |
| GET | `/api/clients/{id}/policies` | Get client's policies |
| GET | `/api/clients/{id}/claims` | Get client's claims |

### Policies
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/policies` | Get all policies (paginated) |
| GET | `/api/policies/{id}` | Get policy by ID |
| POST | `/api/policies` | Create new policy |
| PUT | `/api/policies/{id}` | Update policy |
| POST | `/api/policies/{id}/cancel` | Cancel policy |
| POST | `/api/policies/{id}/renew` | Renew policy |
| GET | `/api/policies/expiring` | Get expiring policies |

### Claims
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/claims` | Get all claims (paginated) |
| GET | `/api/claims/{id}` | Get claim by ID |
| POST | `/api/claims` | Submit new claim |
| PUT | `/api/claims/{id}` | Update claim |
| POST | `/api/claims/{id}/status` | Update claim status |
| POST | `/api/claims/{id}/assign` | Assign adjuster |
| POST | `/api/claims/{id}/approve` | Approve claim |
| POST | `/api/claims/{id}/deny` | Deny claim |
| POST | `/api/claims/{id}/payment` | Record payment |

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/register` | User registration |
| POST | `/api/auth/refresh` | Refresh token |
| POST | `/api/auth/logout` | User logout |
| POST | `/api/auth/forgot-password` | Request password reset |
| POST | `/api/auth/change-password` | Change password |
| GET | `/api/auth/me` | Get current user |

## 🔒 Role-Based Access Control

| Role | Permissions |
|------|-------------|
| Admin | Full access to all resources |
| Agent | Manage clients, policies, and submit claims |
| Underwriter | Review and approve policies |
| Adjuster | Manage claim investigations and payments |
| Viewer | Read-only access |

## ☁️ Azure Integration

### Required Azure Services

1. **Azure Key Vault** - Secure storage for secrets and connection strings
2. **Azure Service Bus** - Message queue for async processing
3. **Azure Blob Storage** - Document and file storage
4. **Azure Application Insights** - Monitoring and telemetry

### Configuration

Set the following in Azure App Service configuration or local `Web.config`:

```xml
<appSettings>
  <add key="Azure:KeyVaultUri" value="https://ams-keyvault.vault.azure.net/" />
  <add key="Azure:ServiceBusConnectionString" value="Endpoint=sb://..." />
  <add key="Azure:StorageConnectionString" value="DefaultEndpointsProtocol=https;..." />
  <add key="Azure:AppInsightsConnectionString" value="InstrumentationKey=..." />
</appSettings>
```

## 🧪 Testing

```bash
# Run unit tests
msbuild AMS.sln /t:Test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📦 Deployment

### Azure App Service

1. Create an Azure App Service (Windows, .NET Framework 4.8)
2. Configure connection strings in App Service Configuration
3. Deploy using Visual Studio, Azure DevOps, or GitHub Actions

### CI/CD Pipeline

See `build/azure-pipelines.yml` for Azure DevOps pipeline configuration.

## 🛡️ Security Best Practices

1. **Secrets Management**: Use Azure Key Vault for all secrets
2. **HTTPS Only**: Always use HTTPS in production
3. **Input Validation**: FluentValidation on all endpoints
4. **SQL Injection**: Use Entity Framework parameterized queries
5. **XSS Prevention**: Proper encoding in responses
6. **CORS**: Configure allowed origins in production
7. **Rate Limiting**: Implement API rate limiting
8. **Audit Logging**: All changes are audited

## 📊 Monitoring

- **Application Insights**: Tracks requests, dependencies, exceptions
- **Audit Logs**: Database table records all data changes
- **Health Checks**: `/api/health` endpoint for monitoring

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For support, email support@ams-insurance.com or create an issue in this repository.
