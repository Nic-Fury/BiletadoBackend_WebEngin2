# Biletado Reservations API

Projekt das im Rahmen der WebEngineering2 Vorlesung entstanden ist - A RESTful API for managing room reservations with soft-delete support.

## Overview

Biletado is a reservation management system built with ASP.NET Core 8.0 that provides CRUD operations for room reservations. The API integrates with an external Assets service for room validation and uses PostgreSQL for data persistence.

## Features

- **CRUD Operations**: Create, read, update, and delete room reservations
- **JWT Authentication**: Secure write operations (POST, PUT, DELETE) with JWT token validation
- **Soft Delete**: Reservations can be soft-deleted or permanently removed
- **Room Validation**: Integration with external Assets service to validate room existence
- **Conflict Detection**: Automatic detection of overlapping reservations
- **Health Checks**: Comprehensive health and readiness endpoints for Kubernetes
- **Structured Logging**: JSON-formatted logs with Serilog for observability
- **API Versioning**: RESTful API endpoints under `/api/v3/reservations`

## Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Authentication**: JWT Bearer tokens with Microsoft.AspNetCore.Authentication.JwtBearer
- **Database**: PostgreSQL with Entity Framework Core 8.0
- **Logging**: Serilog with structured JSON output
- **API Documentation**: Swagger/OpenAPI
- **Containerization**: Docker support

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL 12+
- Docker (optional, for containerized deployment)

## Getting Started

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Nic-Fury/BiletadoBackend_WebEngin2.git
   cd BiletadoBackend_WebEngin2/Biletado
   ```

2. **Configure the database connection:**
   
   Update `appsettings.json` with your PostgreSQL connection string:
   ```json
   {
     "ConnectionStrings": {
       "ReservationsDb": "Host=localhost;Port=5432;Database=reservations_v3;Username=postgres;Password=yourpassword"
     }
   }
   ```

3. **Run database migrations:**
   ```bash
   dotnet ef database update
   ```

4. **Start the application:**
   ```bash
   dotnet run
   ```

The API will be available at `http://localhost:5087` (or the port specified in your launch settings).

### Docker Deployment

Build and run with Docker:

```bash
docker build -t biletado-api .
docker run -p 8080:8080 -e ConnectionStrings__ReservationsDb="Host=your-db-host;..." biletado-api
```

## API Documentation

### Endpoints

#### Health & Status
- `GET /api/v3/reservations/status` - API version and authors
- `GET /api/v3/reservations/health` - Overall health status
- `GET /api/v3/reservations/health/live` - Liveness probe
- `GET /api/v3/reservations/health/ready` - Readiness probe

#### Reservations
- `GET /api/v3/reservations/reservations` - List all reservations (no authentication required)
  - Query params: `include_deleted`, `room_id`, `before`, `after`
- `GET /api/v3/reservations/reservations/{id}` - Get reservation by ID (no authentication required)
- `POST /api/v3/reservations/reservations` - Create new reservation (**requires JWT authentication**)
- `PUT /api/v3/reservations/reservations/{id}` - Update or create reservation (**requires JWT authentication**)
- `DELETE /api/v3/reservations/reservations/{id}` - Delete reservation (**requires JWT authentication**)
  - Query param: `permanent` (soft-delete by default)

### Swagger UI

Access interactive API documentation at `/swagger` when running in Development mode.

## Project Structure

```
Biletado/
├── Controllers/          # API endpoints
│   ├── ReservationsController.cs
│   └── StatusController.cs
├── Services/            # Business logic layer
│   ├── IReservationService.cs
│   └── ReservationStatusService.cs
├── Repository/          # Data access layer
│   └── ReservationServiceRepository.cs
├── Contexts/            # Entity Framework DbContext
│   └── ReservationsDbContext.cs
├── Domain/              # Domain models
│   └── Reservation.cs
├── DTOs/                # Data transfer objects
├── Program.cs           # Application entry point
├── appsettings.json     # Configuration
└── Dockerfile           # Container definition
```

## Configuration

### Application Settings

Key configuration sections in `appsettings.json`:

**Database Connection:**
```json
{
  "ConnectionStrings": {
    "ReservationsDb": "Host=localhost;Port=5432;Database=reservations_v3;..."
  }
}
```

**External Services:**
```json
{
  "Services": {
    "Assets": {
      "BaseUrl": "http://localhost",
      "Port": "9090",
      "ReadyPath": "/api/v3/assets/health/ready",
      "RoomPath": "/api/v3/assets/rooms/{id}"
    }
  }
}
```

**JWT Authentication:**
```json
{
  "Jwt": {
    "Authority": "http://localhost:8080/realms/biletado",
    "Audience": "biletado-reservations",
    "RequireHttpsMetadata": false,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true
  }
}
```

The JWT authentication settings can be configured per environment:
- **Development**: Uses `localhost:8080` as the IAM authority
- **Production**: Uses `iam:8080` as the IAM authority (configured in `appsettings.Production.json`)

Configuration parameters:
- `Authority`: The OpenID Connect authority URL (e.g., Keycloak realm URL)
- `Audience`: The expected audience claim in the JWT token
- `RequireHttpsMetadata`: Whether HTTPS is required for metadata endpoint (set to `false` for local development)
- `ValidateIssuer`: Enable issuer validation
- `ValidateAudience`: Enable audience validation
- `ValidateLifetime`: Enable token expiration validation
- `ValidateIssuerSigningKey`: Enable signature validation

**Logging Configuration:** See [Logging](#logging) section below.

## Authentication

This API uses **JWT (JSON Web Token) Bearer authentication** to secure write operations. The authentication is configured to work with an OpenID Connect-compatible Identity and Access Management (IAM) system, such as Keycloak.

### Protected Endpoints

The following endpoints require a valid JWT token in the `Authorization` header:
- `POST /api/v3/reservations/reservations` - Create reservation
- `PUT /api/v3/reservations/reservations/{id}` - Update reservation
- `DELETE /api/v3/reservations/reservations/{id}` - Delete reservation

### Public Endpoints

These endpoints do NOT require authentication:
- All GET endpoints (read operations)
- All health and status endpoints

### JWT Token Requirements

The JWT token must:
1. Be issued by the configured authority (IAM service)
2. Have a valid signature
3. Not be expired
4. Contain the correct audience claim (`biletado-reservations`)

**Note**: Scopes/permissions are NOT validated - only token validity and signature are checked.

### Using JWT Tokens

Include the JWT token in the `Authorization` header using the Bearer scheme:

```bash
curl -X POST http://localhost:5087/api/v3/reservations/reservations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "room_id": "123e4567-e89b-12d3-a456-426614174000",
    "from": "2026-01-15",
    "to": "2026-01-20"
  }'
```

### Configuration

JWT authentication is configured via environment-specific `appsettings.json` files. The IAM authority URL can be overridden using environment variables:

```bash
# Using environment variable
export Jwt__Authority="http://your-iam-server:8080/realms/biletado"
dotnet run
```

### Troubleshooting

If authentication fails, check the application logs for JWT validation errors. Common issues:
- Token expired
- Invalid signature
- Wrong audience claim
- IAM service not reachable
- Incorrect authority URL

## Development

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

## Logging

This application uses **Serilog** for comprehensive structured logging in JSON format, compatible with Elastic Common Schema and OpenTelemetry standards.

### Where are logs output?

By default, logs are written to the **console** (standard output) in JSON format. When you run the application with `dotnet run`, you'll see the structured JSON logs in your terminal/console.

To also write logs to a **file**, you can add the File sink (see "Adding File Logging" section below).

### Configuration

Log levels and output format can be configured in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

### Log Output Format

All logs are output in **Compact JSON format** with structured properties:

```json
{
  "@t": "2026-01-05T14:41:02.3210343Z",
  "@mt": "Starting Biletado Reservations API",
  "MachineName": "server01",
  "ThreadId": 1,
  "EnvironmentName": "Development",
  "Application": "BiletadoReservations"
}
```

### Structured Properties

Logs include contextual information for better observability:

- **Request Context**: `RequestId`, `RequestPath`, `ActionId`, `ActionName`
- **Trace Correlation**: `@tr` (trace ID), `@sp` (span ID) for distributed tracing
- **Operation Context**: `ReservationId`, `RoomId`, operation details
- **Performance**: `ElapsedMs` for timing information
- **Results**: Entity counts, success/failure status
- **Enrichment**: `MachineName`, `ThreadId`, `EnvironmentName`, `ClientIP`, `UserAgent`

### Log Levels

- **Information**: Normal operations, successful requests, entity counts
- **Warning**: Validation errors, service unavailability, not-found scenarios
- **Error**: Exceptions, connection failures, database errors
- **Debug**: Low-level operations, detailed diagnostics

### Example Logs

**HTTP Request:**
```json
{
  "@t": "2026-01-05T14:41:47.0363393Z",
  "@mt": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms",
  "RequestHost": "localhost:5087",
  "RequestMethod": "GET",
  "RequestPath": "/api/v3/reservations/status",
  "StatusCode": 200,
  "Elapsed": 362.99,
  "UserAgent": "curl/8.5.0",
  "ClientIP": "::1"
}
```

**CRUD Operation:**
```json
{
  "@t": "2026-01-05T14:42:03Z",
  "@mt": "Creating reservation: ReservationId={ReservationId}, RoomId={RoomId}, From={From}, To={To}",
  "ReservationId": "123e4567-e89b-12d3-a456-426614174000",
  "RoomId": "789e0123-e89b-12d3-a456-426614174001",
  "From": "2026-01-10",
  "To": "2026-01-15"
}
```

**Error with Exception:**
```json
{
  "@t": "2026-01-05T14:42:04.0914787Z",
  "@mt": "Database connection failed: ElapsedMs={ElapsedMs}",
  "@l": "Error",
  "@x": "Npgsql.NpgsqlException: Failed to connect...",
  "ElapsedMs": 669
}
```

### What is Logged

#### Controllers
- All HTTP endpoints (GET, POST, PUT, DELETE)
- Request parameters and validation results
- Response status codes
- Entity IDs for audit trail

#### Services
- External API calls (Assets service)
- Database connectivity checks
- Business logic operations (create, update, delete)
- Room availability checks
- Elapsed time for performance monitoring

#### Repository
- Database query operations
- Entity counts retrieved

#### DbContext
- SaveChanges operations
- Number of entities affected
- Database exceptions

### Observability

The structured JSON logs can be ingested into:
- **Elasticsearch/Kibana** for log aggregation and search
- **Splunk** for enterprise logging
- **Grafana Loki** for cloud-native logging
- **Azure Monitor** or **AWS CloudWatch** for cloud deployments
- Any system supporting **Elastic Common Schema** or **OpenTelemetry**

### Adding File Logging (Optional)

To also write logs to files, follow these steps:

1. **Add the File sink package:**
   ```bash
   dotnet add package Serilog.Sinks.File
   ```

2. **Update `appsettings.json`** to include the File sink:
   ```json
   {
     "Serilog": {
       "WriteTo": [
         {
           "Name": "Console",
           "Args": {
             "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
           }
         },
         {
           "Name": "File",
           "Args": {
             "path": "logs/biletado-.log",
             "rollingInterval": "Day",
             "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
           }
         }
       ]
     }
   }
   ```

This will create log files in the `logs/` directory with daily rotation (e.g., `biletado-20260105.log`).

### Privacy & Security

- No sensitive data (passwords, tokens) is logged
- Exception stack traces are logged for debugging
- Personal data (if any) follows GDPR guidelines
- Connection strings are not logged

## Contributing

This project was created as part of the WebEngineering2 course. Contributions are welcome:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Authors

- **Nic Nouisser**
- **Jakob Kaufmann**

## Acknowledgments

Created as part of the WebEngineering2 course project.
