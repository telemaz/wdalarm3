# AGENTS.md

This file provides guidelines and commands for agentic coding agents working on the WatchDog Alarm (wdalarm3) project.

## Project Overview

**Type**: Deadman's switch / wellness check system (MVP stage)  
**Tech Stack**: .NET 10 (LTS), PostgreSQL 16, ASP.NET Core Web API, Entity Framework Core, Clean Architecture  
**Status**: Planning complete - no source code exists yet

## Build/Lint/Test Commands

### Core Commands
```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run single test (use filter by test name or class name)
dotnet test --filter "TestMethodName"
dotnet test --filter "ClassName"
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Run specific test project
dotnet test tests/WdAlarm.UnitTests
dotnet test tests/WdAlarm.IntegrationTests

# Run the API
dotnet run --project src/WdAlarm.Api

# Build in release mode
dotnet build -c Release
```

### Database Commands
```bash
# Add migration
dotnet ef migrations add MigrationName -p src/WdAlarm.Infrastructure -s src/WdAlarm.Api

# Update database
dotnet ef database update -p src/WdAlarm.Infrastructure -s src/WdAlarm.Api

# Drop database (dev only)
dotnet ef database drop -p src/WdAlarm.Infrastructure -s src/WdAlarm.Api
```

### Docker Commands
```bash
# Start PostgreSQL
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Code Style Guidelines

### C# Conventions
- Follow Microsoft C# coding conventions
- Use PascalCase for public members, camelCase for private members
- Do not Prefix private fields
- use camleCase for function parameter.
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Prefix interfaceName with 'I' and abstract class with 'C'

### Architecture Patterns

#### Clean Structure (3 Layers)
- **WdAlarm.Api**: Web API controllers, Program.cs
- **WdAlarm.Core**: Domain entities, business logic, interfaces, command definitions, background workers 
- **WdAlarm.Infrastructure**: Data access, external services, command bus implementation

#### Naming Conventions
- **Entities**: `Alarm`, `AlarmCycle`, `Ping`, `ActionExecution`
- **Services**: `IVerificationService`, `IActionExecutor`, `IAlarmCycleService`
- **Workers**: `CommandHandlerWorker`, `AlarmCycleManagerWorker`, `ChallengeRotationWorker`
- **Controllers**: `AuthController`, `AlarmsController`, `PingController`

### File Organization
```
src/
├── WdAlarm.Api/
│   ├── Controllers/          # REST API endpoints
│   └── Program.cs
├── WdAlarm.Core/
│   ├── Entities/             # Domain entities
│   ├── Commands/             # Command/Response definitions
│   ├── Interfaces/           # Service interfaces
│   ├── BackgroundWorkers/    # IHostedService implementations
│   └── Enums/                # Enumerations
└── WdAlarm.Infrastructure/
    ├── Data/                 # EF Core DbContext, migrations
    ├── Repositories/         # Data access implementations
    ├── Messaging/            # Command bus implementation
    ├── Verification/         # TOTP, RSA, ECDSA implementations
    └── Actions/              # Email, SMS, REST API plugins
```

### Import Guidelines
- Group System.* imports first, then Microsoft.*, then third-party, then project imports
- Use `global using` directives for common namespaces (System, Microsoft.Extensions.Logging, etc.)
- Remove unused imports using IDE cleanup

### Error Handling
- Use `Result<T>` pattern for operations that can fail
- Throw exceptions for truly exceptional conditions
- Log errors with appropriate severity levels
- Use structured logging with correlation IDs

### Testing Guidelines
- Write unit tests for all business logic in Core layer
- Mock Infrastructure layer for unit tests
- Write integration tests for database operations and API endpoints
- Use arrange-act-assert pattern
- Test both happy path and error conditions
- Mock external dependencies (email, SMS, REST APIs)

### Security Practices
- Never log sensitive data (passwords, tokens, private keys)
- Validate all inputs with proper error messages
- Use parameterized queries to prevent SQL injection
- Implement proper JWT token validation
- Enforce HTTPS in production
- Use UUIDs for alarm IDs in no-auth scenarios

### Async Patterns
- Use `async/await` for all I/O operations
- Configure `ConfigureAwait(false)` in library code
- Avoid `async void` except for event handlers
- Use `ValueTask` for operations that may complete synchronously

### Entity Framework Guidelines
- Use Code First migrations
- Define navigation properties for relationships
- Use appropriate data types (DateTimeOffset, decimal precision)
- Add indexes for frequently queried columns
- Use separate DTOs for API responses

### Configuration
- Use `appsettings.json` for environment-specific settings
- Use `IOptions<T>` pattern for strongly-typed configuration
- Store secrets in environment variables or secret manager
- Validate configuration on startup

## Development Workflow

1. **Create feature branch** from main
2. **Implement changes** following clean architecture
3. **Write tests** for new functionality
4. **Run tests** and ensure all pass
5. **Update documentation** if API, logics or behavior changed
6. **Submit pull request** with clear description

## Key Dependencies

- **Cronos**: Cron expression parsing
- **MailKit**: Email sending
- **Otp.NET**: TOTP authentication
- **ASP.NET Core Identity**: User management
- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT authentication

## Important Notes

- This is an MVP focused on standard server deployment
- Client-side encryption recommended for sensitive action configs
- Challenge rotation occurs every 30 seconds
- All timestamps use UTC
- Database uses PostgreSQL 16 with EF Core
- Command bus provides <100ms communication latency
