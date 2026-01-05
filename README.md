# BiletadoBackend_WebEngin2
Projekt das im Rahmen der WebEngineering2 Vorlesung entstanden ist

## Logging

This application uses **Serilog** for comprehensive structured logging in JSON format, compatible with Elastic Common Schema and OpenTelemetry standards.

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

### Privacy & Security

- No sensitive data (passwords, tokens) is logged
- Exception stack traces are logged for debugging
- Personal data (if any) follows GDPR guidelines
- Connection strings are not logged
