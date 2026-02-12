# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Commands

### Local Development

**Start all services (LocalStack + API):**
```bash
docker-compose up -d localstack invoice-api
```

**Initialize DynamoDB table:**
```bash
docker-compose --profile init up db-initializer
```

**Build and run the API locally:**
```bash
cd invoice-api
dotnet run
```

**Build the Docker image:**
```bash
docker-compose build invoice-api
```

**Stop all services:**
```bash
docker-compose down
```

### Project Management

**Restore dependencies:**
```bash
dotnet restore
```

**Build project:**
```bash
dotnet build
```

**Publish for Lambda:**
```bash
dotnet publish invoice-api/InvoiceApi.csproj -c Release -o out
```

### Testing

**Run health checks:**
```bash
# Basic health check
curl http://localhost:5000/health

# Enhanced health check (with database connectivity test)
curl http://localhost:5000/healthz
```

**Test API endpoints:**
```bash
# Get all customers
curl http://localhost:5000/customers

# Create a customer
curl -X POST http://localhost:5000/customers \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Customer","email":"test@example.com"}'
```

## Code Architecture

### Project Structure
```
invoice-app/
├── invoice-api/                    # Main API application
│   ├── Program.cs                  # Minimal API endpoints and DI configuration
│   ├── Models/                     # Data models
│   │   ├── Customer.cs
│   │   └── Invoice.cs
│   ├── Repositories/               # Data access layer
│   │   ├── IDynamoRepository.cs    # Generic repository interface
│   │   ├── DynamoRepository.cs     # Generic implementation
│   │   ├── IInvoiceRepository.cs   # Invoice-specific interface
│   │   └── InvoiceRepository.cs    # Invoice-specific implementation
│   ├── Utils/                      # Helper classes
│   │   └── ValidationHelper.cs     # Validation logic
│   ├── Dockerfile                  # Docker build configuration
│   ├── InvoiceApi.csproj           # Project file
│   └── appsettings*.json           # Configuration files
├── InitializeLocalDb/              # Database initialization tool
│   ├── Program.cs                  # Table creation logic
│   └── InitializeLocalDb.csproj
├── docker-compose.yml              # Docker Compose configuration
└── invoice-app.sln                 # Visual Studio solution
```

### Technology Stack
- **Framework:** .NET 9 Minimal API
- **Deployment:** AWS Lambda with Amazon.Lambda.AspNetCoreServer.Hosting
- **Database:** Amazon DynamoDB (Single Table Design)
- **Local Testing:** LocalStack (mock AWS services)
- **Containerization:** Docker

### Key Patterns

#### Dependency Injection
Services are registered in `Program.cs`:
- `IAmazonDynamoDB` - Configured based on environment (LocalStack vs AWS)
- `IDynamoRepository<T>` - Generic repository for Customer and Invoice
- `IInvoiceRepository` - Specialized repository for invoice queries

#### Environment-based Configuration
In `Program.cs:16-57`, DynamoDB client is configured differently based on `ASPNETCORE_ENVIRONMENT`:
- **Development:** Uses LocalStack at `http://localhost:4566`
- **Production:** Uses AWS region without service URL override

#### DynamoDB Single Table Design
- **Table name:** `InvoiceAppDev` (dev), `InvoiceApp` (production)
- **Primary Key:** `pk` (partition) + `sk` (sort)
- **GSI:** `gsi_pk` + `gsi_sk`
- **Entity formats:**
  - Customer: `pk=CUSTOMER#{id}`, `sk=CUSTOMER#{id}`
  - Invoice: `pk=INVOICE#{id}`, `sk=INVOICE#{id}`, `gsi_pk=CUSTOMER#{customerId}`, `gsi_sk=INVOICE#{date}`

#### Repository Pattern
Generic repository interface `IDynamoRepository<T>` supports CRUD operations for both Customer and Invoice models, with a specialized `IInvoiceRepository` for customer-specific invoice queries.

### API Endpoints

**Customer Management:**
- `GET /customers` - Get all customers
- `GET /customers/{id}` - Get specific customer
- `POST /customers` - Create customer
- `PUT /customers/{id}` - Update customer
- `DELETE /customers/{id}` - Delete customer

**Invoice Management:**
- `GET /invoices` - Get all invoices
- `GET /invoices/{id}` - Get specific invoice
- `POST /invoices` - Create invoice
- `PUT /invoices/{id}` - Update invoice
- `DELETE /invoices/{id}` - Delete invoice
- `GET /customers/{customerId}/invoices` - Get invoices by customer

**Health Checks:**
- `GET /health` - Basic health check
- `GET /healthz` - Enhanced health check with database connectivity test

### Configuration Files

**appsettings.Development.json:**
- Uses LocalStack endpoint: `http://localhost:4566`
- Table name: `InvoiceAppDev`
- Debug logging enabled
- AWS credentials: `localstack/localstack`

**appsettings.json:**
- Production configuration
- Table name: `InvoiceApp`
- Region: `us-east-1`

### Docker Compose Services

- **localstack:** AWS service mock (DynamoDB)
- **db-initializer:** .NET tool that creates DynamoDB table
- **invoice-api:** ASP.NET Core application running on Lambda-compatible runtime

## Important Notes

- The application is configured for **serverless deployment** on AWS Lambda
- Use `docker-compose --profile init` to run the database initializer
- The `/healthz` endpoint performs an actual database query to verify connectivity
- LocalStack data persists in `.localstack/` directory
- Customer IDs and Invoice IDs are auto-generated as GUIDs if not provided

## Deployment

For production deployment to AWS Lambda:
1. Remove `AWS_ENDPOINT_URL` environment variable
2. Configure IAM role with DynamoDB permissions
3. Deploy using `dotnet lambda deploy-function` or AWS SAM
