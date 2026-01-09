# Test Integration Guide

This document describes the test automation setup for the Biletado Reservations API project, including integration with external test repositories.

## Overview

The project implements comprehensive test automation to meet the WebEngineering 2 course requirements (5% testautomation grade). Tests validate API functionality, business logic, and data models.

## Internal Test Suite

### Test Structure

```
Biletado.Tests/
├── Controllers/
│   └── StatusControllerTests.cs     # Tests for status and health endpoints
├── Domain/
│   └── ReservationTests.cs          # Tests for reservation domain model
└── Integration/                      # Future integration tests
```

### Running Tests Locally

**Basic test execution:**
```bash
dotnet test
```

**With detailed output:**
```bash
dotnet test --verbosity detailed
```

**With code coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

**View coverage report:**
```bash
# Install ReportGenerator tool
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html

# Open report
xdg-open ./TestResults/CoverageReport/index.html
```

### Current Test Coverage

- **15 tests** covering:
  - Status endpoint (GET /api/v3/reservations/status)
  - Health check endpoint (GET /api/v3/reservations/health)
  - Liveness probe (GET /api/v3/reservations/health/live)
  - Readiness probe (GET /api/v3/reservations/health/ready)
  - Reservation domain model properties and soft delete

## External Test Repository Integration

### biletado/apidocs Repository

The project specification references integration tests available at:
**https://gitlab.com/biletado/apidocs**

These tests are designed to validate API compliance with the OpenAPI specification and can be executed using:

#### Option 1: IntelliJ HTTP Client

If you have IntelliJ IDEA Ultimate:
1. Clone the apidocs repository
2. Open the `.http` files in IntelliJ
3. Configure the server URL to point to your running instance
4. Execute requests interactively

#### Option 2: Container-based Execution

The tests can be run without IntelliJ using a container approach:

```bash
# Clone the apidocs repository
git clone https://gitlab.com/biletado/apidocs.git
cd apidocs

# Follow instructions in the repository README for container-based execution
# This typically involves using a HTTP client container to execute tests
```

#### Option 3: Integration via CI/CD

To integrate external tests into your CI/CD pipeline:

1. **Add as a Git submodule** (recommended for version tracking):
   ```bash
   git submodule add https://gitlab.com/biletado/apidocs.git tests/external/apidocs
   ```

2. **Or clone during CI/CD**:
   ```yaml
   # Add to .github/workflows/test-automation.yml
   - name: Clone external tests
     run: |
       git clone https://gitlab.com/biletado/apidocs.git tests/external/apidocs
   
   - name: Run external API tests
     run: |
       # Execute tests using container or HTTP client
       # Requires application to be running
       cd tests/external/apidocs
       # Run test commands as specified in apidocs README
   ```

### Test Execution Prerequisites

Before running external tests:

1. **Start the application**:
   ```bash
   dotnet run --project Biletado/Biletado.csproj
   ```

2. **Or use Docker**:
   ```bash
   docker build -t biletado-api ./Biletado
   docker run -p 8080:8080 biletado-api
   ```

3. **Configure test environment**:
   - Set the `BASE_URL` environment variable to your running instance
   - Ensure the Assets service is available (or mock it)
   - Database should be populated with test data

### Test Data Setup

The external tests assume initial database population. You can:

1. **Use Entity Framework migrations**:
   ```bash
   dotnet ef database update
   ```

2. **Add seed data** in `Program.cs` or via a database script

3. **Use the development database** with pre-populated data from kustomize environment

## Automated Testing via GitHub Actions

### Workflow Configuration

Tests run automatically on:
- Every push to `main` branch
- Every pull request to `main` branch

The workflow (`.github/workflows/test-automation.yml`) performs:
1. ✅ Restores dependencies
2. ✅ Builds the project in Release configuration
3. ✅ Executes all tests
4. ✅ Collects code coverage
5. ✅ Uploads test results as artifacts (TRX format)
6. ✅ Uploads coverage reports as artifacts (Cobertura XML)
7. ✅ Publishes test report in PR checks

### Viewing Test Results

**In GitHub:**
1. Go to the Actions tab
2. Select a workflow run
3. View "Test Results" check annotation
4. Download artifacts: `test-results` and `code-coverage`

**Test Result Artifacts:**
- `test-results.trx` - MSTest result format
- `coverage.cobertura.xml` - Code coverage data

## Test Development Guidelines

### Writing New Tests

1. **Unit Tests**: Test individual components in isolation
   ```csharp
   [Fact]
   public void MyTest_Scenario_ExpectedBehavior()
   {
       // Arrange
       var service = new MyService();
       
       // Act
       var result = service.DoSomething();
       
       // Assert
       result.Should().Be(expectedValue);
   }
   ```

2. **Integration Tests**: Test API endpoints end-to-end
   ```csharp
   [Fact]
   public async Task GetEndpoint_ReturnsOk()
   {
       // Arrange
       var client = _factory.CreateClient();
       
       // Act
       var response = await client.GetAsync("/api/v3/reservations/status");
       
       // Assert
       response.StatusCode.Should().Be(HttpStatusCode.OK);
   }
   ```

### Test Naming Convention

Use descriptive names following the pattern:
```
MethodName_Scenario_ExpectedBehavior
```

Examples:
- `GetStatus_ShouldReturnOkWithCorrectData`
- `CreateReservation_WithInvalidData_ShouldReturnBadRequest`
- `DeleteReservation_WhenNotFound_ShouldReturn404`

## Requirements Fulfillment

✅ **testautomation (5% grade requirement):**
- Single self-created test requirement: **Exceeded** with 15 comprehensive tests
- Unit tests for controllers and domain models
- Integration test infrastructure ready
- Automated execution via GitHub Actions
- Test run documented in workflow artifacts

## Additional Resources

- **xUnit Documentation**: https://xunit.net/
- **FluentAssertions**: https://fluentassertions.com/
- **Moq**: https://github.com/moq/moq4
- **ASP.NET Testing**: https://learn.microsoft.com/en-us/aspnet/core/test/
- **biletado/apidocs**: https://gitlab.com/biletado/apidocs

## Future Enhancements

Potential improvements to the test suite:

1. **Integration Tests**: Full API endpoint testing with test database
2. **Performance Tests**: Load testing and benchmarking
3. **Security Tests**: Authentication/authorization validation
4. **Contract Tests**: API specification compliance
5. **External Test Integration**: Automated execution of apidocs tests in CI/CD
6. **Mutation Testing**: Verify test suite effectiveness
7. **Snapshot Testing**: Validate API response structures
