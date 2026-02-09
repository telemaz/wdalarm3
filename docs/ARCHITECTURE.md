# WatchDog Alarm - System Architecture

Comprehensive architecture documentation for the WatchDog Alarm system.

## Table of Contents
- [System Overview](#system-overview)
- [Architecture Style](#architecture-style)
- [Layer Structure](#layer-structure)
- [Core Components](#core-components)
- [Data Flow](#data-flow)
- [Background Services](#background-services)
- [Plugin Systems](#plugin-systems)
- [Security Architecture](#security-architecture)
- [Scalability Considerations](#scalability-considerations)

---

## System Overview

WatchDog Alarm is a deadman's switch / wellness check system built as a monolithic ASP.NET Core Web API with background workers for alarm processing. The system uses Clean Architecture principles and an **in-memory command bus** for fast inter-component communication (<100ms latency).

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                         Clients                              │
│  (Mobile Apps, Web Browsers, IoT Devices, Scripts)          │
└───────────────────────┬─────────────────────────────────────┘
                        │ HTTPS/REST
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                    WdAlarm.Api Layer                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              REST API Controllers                       │ │
│  │  • AuthController  • AlarmsController                  │ │
│  │  • PingController  • HistoryController                 │ │
│  └────────────────┬───────────────────────────────────────┘ │
│                   │                                          │
│  ┌────────────────▼───────────────────────────────────────┐ │
│  │      In-Memory Command Bus (Request-Reply)             │ │
│  │  • Channel-based communication (<100ms)                │ │
│  │  • Commands: CreateCycle, CancelCycle, ActivateAlarm  │ │
│  │  • Request-reply pattern with timeout                  │ │
│  └────────────────┬───────────────────────────────────────┘ │
│                   │                                          │
│  ┌────────────────▼───────────────────────────────────────┐ │
│  │           Background Workers (IHostedService)          │ │
│  │  • CommandHandlerWorker      (processes commands)      │ │
│  │  • AlarmCycleManagerWorker   (1-second polling)        │ │
│  │  • ChallengeRotationWorker   (30-second interval)      │ │
│  │  • HistoryCleanupWorker      (daily)                   │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                   WdAlarm.Core Layer                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                 Domain Entities                         │ │
│  │  Alarm, AlarmCycle, Ping, ActionExecution, etc.       │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              Service Interfaces                         │ │
│  │  IVerificationService, IActionExecutor,                │ │
│  │  IAlarmCycleService, IGlobalChallengeService          │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                Business Logic                           │ │
│  │  Alarm cycle calculations, verification rules,         │ │
│  │  action scheduling algorithms                          │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              WdAlarm.Infrastructure Layer                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │          Data Access (EF Core)                          │ │
│  │  • ApplicationDbContext                                 │ │
│  │  • Repositories (IAlarmRepository, etc.)               │ │
│  │  • Migrations                                           │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         Verification Plugins                            │ │
│  │  • TOTPVerificationMethod                              │ │
│  │  • RSASignatureVerificationMethod                      │ │
│  │  • ECDSASignatureVerificationMethod                    │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │            Action Plugins                               │ │
│  │  • EmailActionPlugin (MailKit)                         │ │
│  │  • SmsActionPlugin (Mock)                              │ │
│  │  • RestApiActionPlugin (HttpClient)                    │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         External Service Clients                        │ │
│  │  • SMTP Client (MailKit)                               │ │
│  │  • HTTP Client (REST API actions)                      │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                   PostgreSQL 16 Database                     │
│  Tables: Alarms, AlarmCycles, Pings, ActionExecutions,     │
│          AlarmDelayActions, AlarmPingActions, etc.          │
└─────────────────────────────────────────────────────────────┘
```

---

## Architecture Style

### Clean Architecture

The system follows **Clean Architecture** (aka Onion Architecture, Hexagonal Architecture) with clear separation of concerns:

```
┌──────────────────────────────────────────┐
│         Presentation Layer               │
│         (WdAlarm.Api)                    │
│  Controllers, Background Services        │
└──────────────────┬───────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────┐
│           Core/Domain Layer              │
│           (WdAlarm.Core)                 │
│  Entities, Interfaces, Business Logic    │
│  ✓ Framework-independent                 │
│  ✓ Testable without infrastructure       │
└──────────────────┬───────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────┐
│        Infrastructure Layer              │
│        (WdAlarm.Infrastructure)          │
│  Data Access, External Services          │
│  ✓ Implements Core interfaces            │
│  ✓ Framework-specific code               │
└──────────────────────────────────────────┘
```

**Key Principles:**
1. **Dependency Rule**: Dependencies point inward (Infrastructure → Core ← Api)
2. **Core Independence**: Domain logic doesn't depend on frameworks or databases
3. **Interface-Based**: Infrastructure implements interfaces defined in Core
4. **Testability**: Each layer can be tested independently

### Why Clean Architecture?

**Benefits:**
- Easy to test (mock infrastructure)
- Framework-agnostic core logic
- Clear separation of concerns
- Easy to swap implementations (change database, email provider, etc.)
- Maintainable as system grows

**Trade-offs:**
- More initial setup (3 projects instead of 1)
- More abstraction (interfaces everywhere)
- Steeper learning curve for new developers

---

## Layer Structure

### WdAlarm.Api (Presentation Layer)

**Responsibilities:**
- Handle HTTP requests/responses
- Route requests to appropriate services
- Validate input (data annotations, FluentValidation)
- Handle authentication/authorization
- Run background services
- Return formatted responses (JSON)

**Key Components:**
```
WdAlarm.Api/
├── Controllers/
│   ├── AuthController.cs          # Register, login, refresh token
│   ├── AlarmsController.cs        # CRUD operations for alarms
│   ├── PingController.cs          # Submit pings (uses ICommandBus)
│   ├── ChallengeController.cs     # Get global challenge
│   └── HistoryController.cs       # Query ping/action history
├── Workers/
│   ├── CommandHandlerWorker.cs        # Process commands from bus
│   ├── AlarmCycleManagerWorker.cs     # 1-second polling for actions
│   ├── ChallengeRotationWorker.cs     # Rotate challenge every 30s
│   └── HistoryCleanupWorker.cs        # Delete old records daily
├── Models/
│   ├── Requests/                  # DTOs for requests
│   └── Responses/                 # DTOs for responses
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs # Global error handler
│   └── RequestLoggingMiddleware.cs
├── Filters/
│   └── ValidateModelAttribute.cs  # Model validation
├── Program.cs                      # Application entry point
└── appsettings.json               # Configuration
```

**Dependencies:**
- Microsoft.AspNetCore.App (Web API framework)
- Microsoft.AspNetCore.Identity (user management)
- Microsoft.AspNetCore.Authentication.JwtBearer (JWT)
- Swashbuckle.AspNetCore (Swagger)
- References: WdAlarm.Core, WdAlarm.Infrastructure

---

### WdAlarm.Core (Domain Layer)

**Responsibilities:**
- Define domain entities (Alarm, Ping, etc.)
- Define business logic interfaces
- Implement domain logic (validation, calculations)
- Define enumerations and value objects
- NO dependencies on infrastructure or frameworks

**Key Components:**
```
WdAlarm.Core/
├── Entities/
│   ├── Alarm.cs                   # Alarm entity
│   ├── AlarmCycle.cs              # Cycle entity
│   ├── AlarmDelayAction.cs        # Timeline action
│   ├── AlarmPingAction.cs         # On-ping action
│   ├── Ping.cs                    # Ping record
│   ├── ActionExecution.cs         # Action execution tracking
│   ├── GlobalChallenge.cs         # Challenge storage
│   └── ApplicationUser.cs         # User entity (extends IdentityUser)
├── Commands/                       # NEW: Command/Response patterns
│   ├── ICommand.cs                # Command interface
│   ├── CommandResult.cs           # Result wrapper
│   ├── CreateCycleCommand.cs      # Create alarm cycle
│   ├── CancelCycleCommand.cs      # Cancel active cycle
│   └── ActivateAlarmCommand.cs    # Activate alarm
├── Interfaces/
│   ├── ICommandBus.cs             # NEW: Command bus abstraction
│   ├── IAlarmRepository.cs        # Alarm data access
│   ├── IAlarmCycleRepository.cs   # Cycle data access
│   ├── IPingRepository.cs         # Ping data access
│   ├── IActionExecutionRepository.cs
│   ├── IVerificationService.cs    # Verification abstraction
│   ├── IVerificationMethod.cs     # Plugin interface
│   ├── IActionExecutor.cs         # Action execution abstraction
│   ├── IActionPlugin.cs           # Action plugin interface
│   ├── IAlarmCycleService.cs      # Cycle management logic
│   └── IGlobalChallengeService.cs # Challenge management
├── Enums/
│   ├── AlarmDelayType.cs          # Timeout, Schedule
│   ├── VerificationMethodType.cs  # None, TOTP, RSA, ECDSA
│   ├── ActionType.cs              # Email, SMS, RestApi
│   ├── AlarmCycleStatus.cs        # Active, Completed, Cancelled
│   └── ActionExecutionStatus.cs   # Pending, Executing, Success, Failed
├── DTOs/
│   ├── VerificationResult.cs      # Verification outcome
│   └── ActionExecutionResult.cs   # Action execution outcome
└── Exceptions/
    ├── AlarmNotFoundException.cs
    ├── VerificationFailedException.cs
    └── ActionExecutionException.cs
```

**Dependencies:**
- NONE (pure C# and .NET standard libraries)
- This is key to Clean Architecture

---

### WdAlarm.Infrastructure (Infrastructure Layer)

**Responsibilities:**
- Implement Core interfaces
- Data access with EF Core
- External service integration (SMTP, HTTP)
- Cryptography implementations
- Caching, logging

**Key Components:**
```
WdAlarm.Infrastructure/
├── Messaging/                          # NEW: Command bus implementation
│   ├── InMemoryCommandBus.cs          # Channel-based command bus
│   ├── CommandEnvelope.cs             # Command wrapper
│   └── NatsCommandBus.cs              # Future: NATS JetStream impl
├── Data/
│   ├── ApplicationDbContext.cs        # EF Core DbContext
│   ├── Migrations/                     # EF Core migrations
│   └── Configurations/                 # Entity configurations
│       ├── AlarmConfiguration.cs
│       ├── AlarmCycleConfiguration.cs
│       └── ...
├── Repositories/
│   ├── AlarmRepository.cs             # Alarm data access
│   ├── AlarmCycleRepository.cs        # Cycle data access
│   ├── PingRepository.cs              # Ping data access
│   └── ActionExecutionRepository.cs   # Action execution data access
├── Services/
│   ├── AlarmCycleService.cs           # Cycle business logic
│   ├── GlobalChallengeService.cs      # Challenge management
│   └── ActionExecutor.cs              # Action execution coordinator
├── Verification/
│   ├── VerificationService.cs         # Main verification service
│   ├── TOTPVerificationMethod.cs      # TOTP implementation
│   ├── RSASignatureVerificationMethod.cs
│   └── ECDSASignatureVerificationMethod.cs
├── Actions/
│   ├── EmailActionPlugin.cs           # Email via MailKit
│   ├── SmsActionPlugin.cs             # SMS mock
│   └── RestApiActionPlugin.cs         # REST API calls
└── DependencyInjection.cs             # Service registration
```

**Dependencies:**
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL
- MailKit (email)
- Cronos (cron parsing)
- Otp.NET (TOTP)
- BouncyCastle or System.Security.Cryptography (RSA/ECDSA)
- References: WdAlarm.Core

---

## Core Components

### 1. In-Memory Command Bus

Fast inter-component communication system enabling <100ms latency between API and background workers.

```
┌─────────────────────────────────────────────────────────────┐
│          Command Bus Architecture                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  API Controller                                              │
│      │                                                       │
│      │ 1. Send Command                                      │
│      ▼                                                       │
│  ┌────────────────────────────────────────┐                │
│  │    ICommandBus.SendAsync<TResponse>    │                │
│  │  • Creates TaskCompletionSource        │                │
│  │  • Writes command to channel           │                │
│  │  • Waits for response (500ms timeout)  │                │
│  └────────────┬───────────────────────────┘                │
│               │                                              │
│               │ 2. Command flows through channel            │
│               ▼                                              │
│  ┌────────────────────────────────────────┐                │
│  │  Bounded Channel<CommandEnvelope>      │                │
│  │  • Capacity: 1000 commands             │                │
│  │  • FullMode: Wait (backpressure)       │                │
│  └────────────┬───────────────────────────┘                │
│               │                                              │
│               │ 3. Worker reads from channel                │
│               ▼                                              │
│  ┌────────────────────────────────────────┐                │
│  │   CommandHandlerWorker                 │                │
│  │  • Reads commands from channel         │                │
│  │  • Routes to appropriate handler       │                │
│  │  • Executes business logic             │                │
│  │  • Completes TaskCompletionSource      │                │
│  └────────────┬───────────────────────────┘                │
│               │                                              │
│               │ 4. Response flows back                      │
│               ▼                                              │
│  API Controller receives response                           │
│  Returns to HTTP client                                     │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Key Features:**

1. **Request-Reply Pattern**
   - API sends command and waits for response
   - Uses `TaskCompletionSource` for async coordination
   - 500ms timeout (configurable)

2. **Command Types**
   ```csharp
   public interface ICommand<TResponse>
   {
       Guid CommandId { get; }
   }
   
   // Commands
   CreateCycleCommand    → CreateCycleResponse
   CancelCycleCommand    → CancelCycleResponse
   ActivateAlarmCommand  → ActivateAlarmResponse
   ```

3. **Performance Characteristics**
   - **Latency**: <100ms typical (30-50ms for cycle creation)
   - **Throughput**: Handles 1000+ commands/sec
   - **Memory**: Bounded channel prevents memory leaks
   - **Backpressure**: Blocks API when queue full (prevents overload)

4. **Error Handling**
   - Command timeout → Returns error to API
   - Worker crash → Command times out, API retries
   - Queue full → API waits (backpressure)

5. **Future Migration Path**
   ```
   MVP: InMemoryCommandBus (same process)
     ↓
   Scale: NatsCommandBus (NATS JetStream)
     ↓
   Benefits: Multiple workers, persistence, load balancing
   ```

**Implementation Example:**

```csharp
// PingController.cs
var command = new CreateCycleCommand
{
    AlarmId = alarmId,
    LastPingId = ping.Id
};

var result = await _commandBus.SendAsync<CreateCycleResponse>(
    command, 
    cancellationToken
);

if (!result.Success)
{
    _logger.LogError("Cycle creation failed: {Error}", result.Error);
    return StatusCode(500);
}

// Continue with response...
```

**Why Command Bus?**

| Requirement | Solution |
|-------------|----------|
| <100ms latency | In-memory channel (no network) |
| Request-reply | TaskCompletionSource pattern |
| Decoupled components | API doesn't directly call worker |
| Scalability path | Easy swap to NATS later |
| Testability | Mock ICommandBus in tests |

---

### 2. Alarm Cycle Engine

The heart of the system, managing alarm lifecycles.

```
┌─────────────────────────────────────────────────────────┐
│           Alarm Cycle State Machine                     │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  [No Cycle] ──────────────┐                            │
│      ▲                    │                             │
│      │                    │ User activates alarm        │
│      │                    │ or ping resets timer        │
│      │                    ▼                             │
│      │              [Active Cycle]                      │
│      │                    │                             │
│      │     ┌──────────────┼──────────────┐             │
│      │     │              │              │             │
│      │     │ Ping         │ All actions  │ Manual      │
│      │     │ received     │ completed    │ cancel      │
│      │     ▼              ▼              ▼             │
│      │  [Completed]   [Completed]   [Cancelled]        │
│      │     │              │              │             │
│      └─────┴──────────────┴──────────────┘             │
│            Create new cycle if alarm still active       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Key Operations:**

**Cycle Creation:**
1. Calculate alarm point based on delay type
   - Timeout: `LastPingTime + TimeoutDuration`
   - Schedule: `NextCronOccurrence(after: LastPingTime)`
2. Create `AlarmCycle` record
3. Create `ActionExecution` records for each `AlarmDelayAction`
   - Calculate `ScheduledTime = AlarmPointTime + OffsetMinutes`

**Cycle Completion (on ping):**
1. Set `AlarmCycle.Status = Completed`
2. Set `AlarmCycle.CompletedAt = NOW()`
3. Set `AlarmCycle.CompletedByPingId = pingId`
4. Update `ActionExecutions`: Set `Status = Cancelled` for pending actions
5. Trigger new cycle creation

**Action Execution:**
1. Query pending actions: `WHERE Status = Pending AND ScheduledTime <= NOW()`
2. Group by `AlarmId` for parallel execution
3. For each action:
   - Set `Status = Executing`
   - Execute via `IActionExecutor`
   - On success: Set `Status = Success`, `ExecutedTime = NOW()`
   - On failure: Retry up to 3 times, then set `Status = Failed`

---

### 2. Verification System

Plugin-based verification supporting multiple methods.

```
┌─────────────────────────────────────────────────────────┐
│              Verification Flow                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Ping Request                                           │
│      │                                                  │
│      ▼                                                  │
│  Load Alarm                                             │
│      │                                                  │
│      ▼                                                  │
│  Check VerificationMethod                               │
│      │                                                  │
│      ├─── None ────────────► Auto Pass                 │
│      │                            │                     │
│      ├─── TOTP ────────────► Validate Code             │
│      │                            │                     │
│      ├─── RSA ─────────────► Verify Signature          │
│      │                            │                     │
│      └─── ECDSA ───────────► Verify Signature          │
│                                    │                     │
│                                    ▼                     │
│                              Pass or Fail                │
│                                    │                     │
│                     ┌──────────────┴──────────────┐     │
│                     ▼                             ▼     │
│              Return Success              Return 401      │
│              Record Ping                 with Error      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Verification Methods:**

**1. None (No Authentication)**
```csharp
public class NoneVerificationMethod : IVerificationMethod
{
    public VerificationMethodType Type => VerificationMethodType.None;
    
    public Task<VerificationResult> VerifyAsync(
        string config, 
        string proof, 
        string? globalChallenge)
    {
        return Task.FromResult(new VerificationResult { IsValid = true });
    }
}
```

**2. TOTP (Time-based One-Time Password)**
```csharp
Algorithm: HMAC-SHA1
Window: 30 seconds
Digits: 6
Process:
  1. Extract secret from alarm.VerificationConfig
  2. Generate expected code for current time window
  3. Compare with provided proof
  4. Allow ±1 window for clock skew (90 seconds total)
```

**3. RSA Signature**
```csharp
Algorithm: RSA-SHA256 (RS256)
Key Size: 2048-bit minimum
Process:
  1. Parse proof: "BASE64_SIGNATURE|TIMESTAMP_ISO8601"
  2. Validate timestamp within ±5 minutes
  3. Construct message: globalChallenge + timestamp
  4. Verify signature using public key from alarm.VerificationConfig
  5. Return result
```

**4. ECDSA Signature**
```csharp
Algorithm: ECDSA-SHA256 (ES256)
Curve: P-256 (secp256r1) or P-384
Process:
  1. Same as RSA but using elliptic curve cryptography
  2. Smaller keys (256-bit vs 2048-bit)
  3. Faster computation
```

---

### 3. Action Execution System

Plugin-based action execution with retry logic.

```
┌─────────────────────────────────────────────────────────┐
│           Action Execution Flow                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ActionExecution (Pending)                              │
│      │                                                  │
│      ▼                                                  │
│  Load Action Config                                     │
│      │                                                  │
│      ▼                                                  │
│  Determine Action Type                                  │
│      │                                                  │
│      ├─── Email ───────────► EmailActionPlugin         │
│      │                            │                     │
│      ├─── SMS ─────────────► SmsActionPlugin           │
│      │                            │                     │
│      └─── RestApi ─────────► RestApiActionPlugin       │
│                                    │                     │
│                                    ▼                     │
│                            Execute Action                │
│                                    │                     │
│                     ┌──────────────┴──────────────┐     │
│                     ▼                             ▼     │
│              Success                        Failure      │
│                │                                 │       │
│                ▼                                 ▼       │
│         Status = Success         Is Retriable?          │
│         ExecutedTime = NOW             │                │
│                                    ┌───┴───┐            │
│                                    ▼       ▼            │
│                                  Yes      No            │
│                                   │        │            │
│                           RetryCount++  Status=Failed   │
│                           Wait 5s    Log Error          │
│                           Retry                         │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Retry Logic:**
```csharp
MaxRetries: 3
RetryDelay: 5 seconds
Retriable Errors:
  - Network timeouts
  - Connection refused
  - HTTP 5xx errors
  - SMTP temporary failures (4xx)
  
Non-Retriable Errors:
  - Invalid configuration (missing fields)
  - Authentication failures (invalid credentials)
  - HTTP 4xx errors (except 429 rate limit)
  - Invalid recipient addresses
```

---

### 4. Global Challenge System

Rotating challenge for signature verification.

```
┌─────────────────────────────────────────────────────────┐
│         Challenge Rotation Service                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Timer (30 seconds)                                     │
│      │                                                  │
│      ▼                                                  │
│  Generate New Challenge                                 │
│  (UUID: e.g., a1b2c3d4-e5f6-4789-0abc-def123456789)   │
│      │                                                  │
│      ▼                                                  │
│  Store in IMemoryCache                                  │
│  Key: "GlobalChallenge:Current"                        │
│  Expiration: 35 seconds (5s overlap)                   │
│      │                                                  │
│      ▼                                                  │
│  Broadcast to all instances (future: Redis)            │
│      │                                                  │
│      ▼                                                  │
│  Wait 30 seconds                                        │
│      │                                                  │
│      └──────────► Repeat                                │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Usage:**
```csharp
// Client retrieves challenge
GET /api/challenge
Response: {
  "challenge": "a1b2c3d4-e5f6-4789-0abc-def123456789",
  "expiresAt": "2026-02-08T10:30:30Z"
}

// Client signs: challenge + timestamp
message = "a1b2c3d4-e5f6-4789-0abc-def123456789" + "2026-02-08T10:30:15Z"
signature = sign(message, privateKey)

// Client submits ping
POST /api/ping/{alarmId}
{
  "verificationProof": "BASE64_SIGNATURE|2026-02-08T10:30:15Z"
}

// Server verifies
1. Split proof into signature and timestamp
2. Validate timestamp within ±5 minutes
3. Retrieve current challenge from cache
4. Construct message: challenge + timestamp
5. Verify signature using stored public key
```

---

## Data Flow

### Scenario 1: User Creates Alarm

```
Client                  API                 Core                Infrastructure       Database
  │                      │                   │                        │                  │
  │  POST /api/alarms    │                   │                        │                  │
  ├─────────────────────►│                   │                        │                  │
  │                      │ Validate Request  │                        │                  │
  │                      │                   │                        │                  │
  │                      │ Create Alarm Entity                        │                  │
  │                      ├──────────────────►│                        │                  │
  │                      │                   │ IAlarmRepository.AddAsync                 │
  │                      │                   ├───────────────────────►│                  │
  │                      │                   │                        │ INSERT Alarm     │
  │                      │                   │                        ├─────────────────►│
  │                      │                   │                        │                  │
  │                      │                   │                        │ INSERT Actions   │
  │                      │                   │                        ├─────────────────►│
  │                      │                   │                        │                  │
  │                      │                   │◄───────────────────────┤                  │
  │                      │◄──────────────────┤                        │                  │
  │                      │                   │                        │                  │
  │  201 Created         │                   │                        │                  │
  │◄─────────────────────┤                   │                        │                  │
  │  { alarmId: ... }    │                   │                        │                  │
  │                      │                   │                        │                  │
```

### Scenario 2: Background Service Creates Cycle

```
CycleCreationService    Core                Infrastructure       Database
  │                      │                        │                  │
  │ Timer (5s)           │                        │                  │
  │                      │                        │                  │
  │ Find alarms without active cycle               │                  │
  ├─────────────────────►│                        │                  │
  │                      │ IAlarmRepository.GetWithoutActiveCycle    │
  │                      ├───────────────────────►│                  │
  │                      │                        │ SELECT query     │
  │                      │                        ├─────────────────►│
  │                      │                        │◄─────────────────┤
  │                      │◄───────────────────────┤                  │
  │                      │                        │                  │
  │ For each alarm:      │                        │                  │
  │                      │                        │                  │
  │ Calculate alarm point (timeout or cron)       │                  │
  │                      │                        │                  │
  │ Create AlarmCycle    │                        │                  │
  ├─────────────────────►│                        │                  │
  │                      │ IAlarmCycleRepository.AddAsync            │
  │                      ├───────────────────────►│                  │
  │                      │                        │ INSERT cycle     │
  │                      │                        ├─────────────────►│
  │                      │                        │                  │
  │ Create ActionExecutions                       │                  │
  ├─────────────────────►│                        │                  │
  │                      │ For each DelayAction:  │                  │
  │                      │   ScheduledTime =      │                  │
  │                      │   AlarmPoint + Offset  │                  │
  │                      ├───────────────────────►│                  │
  │                      │                        │ INSERT executions│
  │                      │                        ├─────────────────►│
  │                      │◄───────────────────────┤                  │
  │                      │                        │                  │
```

### Scenario 3: Alarm Triggers (1-Second Polling)

```
AlarmCycleManager       Core                Infrastructure       Action Plugins
  │                      │                        │                  │
  │ Timer (1s)           │                        │                  │
  │                      │                        │                  │
  │ Find pending actions WHERE ScheduledTime <= NOW()                │
  ├─────────────────────►│                        │                  │
  │                      │ IActionExecutionRepository.GetPendingAsync│
  │                      ├───────────────────────►│                  │
  │                      │◄───────────────────────┤                  │
  │                      │                        │                  │
  │ Group by AlarmId     │                        │                  │
  │                      │                        │                  │
  │ Execute in parallel: │                        │                  │
  │                      │                        │                  │
  │ For each action:     │                        │                  │
  │ Set Status = Executing                        │                  │
  │                      │                        │                  │
  │ Execute via IActionExecutor                   │                  │
  ├─────────────────────►│                        │                  │
  │                      │ Determine ActionType   │                  │
  │                      │                        │                  │
  │                      │ Get plugin (Email/SMS/RestApi)            │
  │                      ├───────────────────────►│                  │
  │                      │                        │ Execute          │
  │                      │                        ├─────────────────►│
  │                      │                        │                  │ Send Email
  │                      │                        │                  │ or SMS
  │                      │                        │                  │ or HTTP Request
  │                      │                        │                  │
  │                      │                        │◄─────────────────┤
  │                      │◄───────────────────────┤ Result           │
  │                      │                        │                  │
  │ Update ActionExecution                        │                  │
  │   Status = Success/Failed                     │                  │
  │   ExecutedTime = NOW()                        │                  │
  │                      │                        │                  │
  │ If failed and retriable: Retry (max 3x)       │                  │
  │                      │                        │                  │
```

### Scenario 4: User Pings Alarm (WITH COMMAND BUS)

```
Client          API              CommandBus         Worker          Infrastructure      Database
  │              │                    │               │                    │                │
  │ POST /ping   │                    │               │                    │                │
  ├─────────────►│                    │               │                    │                │
  │              │ 1. Load Alarm      │               │                    │                │
  │              ├────────────────────┼───────────────┼───────────────────►│                │
  │              │                    │               │                    │ SELECT Alarm   │
  │              │                    │               │                    ├───────────────►│
  │              │◄───────────────────┼───────────────┼────────────────────┤                │
  │              │                    │               │                    │                │
  │              │ 2. Verify Ping     │               │                    │                │
  │              │     (TOTP/Sig)     │               │                    │                │
  │              │                    │               │                    │                │
  │              │ 3. Save Ping       │               │                    │                │
  │              ├────────────────────┼───────────────┼───────────────────►│                │
  │              │                    │               │                    │ INSERT Ping    │
  │              │                    │               │                    ├───────────────►│
  │              │                    │               │                    │                │
  │              │ 4. Send Command ──►│               │                    │                │
  │              │   CreateCycle      │               │                    │                │
  │              │   (WAIT)           │               │                    │                │
  │              │                    │ 5. Channel    │                    │                │
  │              │                    ├──────────────►│                    │                │
  │              │                    │               │ 6. Handle Command  │                │
  │              │                    │               │   CreateCycle      │                │
  │              │                    │               ├───────────────────►│                │
  │              │                    │               │                    │ Complete Cycle │
  │              │                    │               │                    │ Cancel Actions │
  │              │                    │               │                    │ Create Cycle   │
  │              │                    │               │                    │ Insert Actions │
  │              │                    │               │                    ├───────────────►│
  │              │                    │               │◄───────────────────┤                │
  │              │                    │               │                    │                │
  │              │                    │ 7. Complete   │                    │                │
  │              │                    │◄──────────────┤                    │                │
  │              │ 8. Receive Response│               │                    │                │
  │              │◄───────────────────┤               │                    │                │
  │              │ { cycleId, ... }   │               │                    │                │
  │              │                    │               │                    │                │
  │              │ 9. Fire On-Ping Actions (async, no wait)               │                │
  │              │                    │               │                    │                │
  │  200 OK      │                    │               │                    │                │
  │◄─────────────┤                    │               │                    │                │
  │ { pingId,    │                    │               │                    │                │
  │   newAlarm   │                    │               │                    │                │
  │   Point }    │                    │               │                    │                │
  │              │                    │               │                    │                │
```

**Timeline:**
- Steps 1-3: ~10-20ms (DB queries, verification)
- Steps 4-8: ~30-50ms (command bus round-trip)
- **Total: ~50-70ms** ✅ Under 100ms target
- Step 9: Fire-and-forget (doesn't block response)

**Key Points:**
- API waits for Worker to complete cycle creation
- Command bus provides request-reply pattern
- On-ping actions execute async (don't block response)
- User receives confirmation with new alarm point time

---

## Background Services

### 1. CommandHandlerWorker

**Purpose:** Process commands from the command bus (real-time operations)

**Pattern:** Event loop reading from channel

**Algorithm:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("CommandHandlerWorker started");
    
    await foreach (var envelope in _commandBus.Reader.ReadAllAsync(stoppingToken))
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await HandleCommandAsync(envelope, stoppingToken);
            stopwatch.Stop();
            
            _logger.LogDebug(
                "Command {CommandType} processed in {Duration}ms",
                envelope.Command.GetType().Name,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling command {CommandId}", envelope.CommandId);
            _commandBus.CompleteCommand(envelope.CommandId, new CommandResult 
            { 
                Success = false, 
                Error = ex.Message 
            });
        }
    }
}

private async Task HandleCommandAsync(CommandEnvelope envelope, CancellationToken ct)
{
    using var scope = _serviceProvider.CreateScope();
    
    switch (envelope.Command)
    {
        case CreateCycleCommand cmd:
            var cycleService = scope.ServiceProvider
                .GetRequiredService<IAlarmCycleService>();
            var response = await cycleService.CreateCycleAsync(cmd.AlarmId, cmd.LastPingId);
            _commandBus.CompleteCommand(cmd.CommandId, response);
            break;
            
        case CancelCycleCommand cmd:
            var cycleService2 = scope.ServiceProvider
                .GetRequiredService<IAlarmCycleService>();
            await cycleService2.CancelActiveCycleAsync(cmd.AlarmId);
            _commandBus.CompleteCommand(cmd.CommandId, new { Success = true });
            break;
            
        case ActivateAlarmCommand cmd:
            var activationService = scope.ServiceProvider
                .GetRequiredService<IAlarmCycleService>();
            var activationResponse = await activationService.ActivateAlarmAsync(cmd.AlarmId);
            _commandBus.CompleteCommand(cmd.CommandId, activationResponse);
            break;
            
        default:
            throw new NotSupportedException($"Unknown command type: {envelope.Command.GetType()}");
    }
}
```

**Performance Characteristics:**
- **Latency**: 30-50ms typical for cycle creation
- **Throughput**: Handles 1000+ commands/sec
- **Critical for**: Ping endpoint (requires immediate cycle creation)

**Error Handling:**
- Completes command with error response
- API receives error and can return 500 to user
- User can retry ping

---

### 2. AlarmCycleManagerWorker

**Purpose:** Execute scheduled actions with 1-second precision

**Interval:** 1 second

**Algorithm:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var now = DateTime.UtcNow;
            
            // Get pending actions
            var pendingActions = await _actionExecutionRepository
                .GetPendingActionsAsync(now);
            
            // Group by alarm for parallel execution
            var groupedByAlarm = pendingActions.GroupBy(a => a.AlarmCycle.AlarmId);
            
            // Execute each group in parallel
            await Task.WhenAll(
                groupedByAlarm.Select(group => ExecuteActionsForAlarmAsync(group))
            );
            
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AlarmCycleManagerService");
            // Don't crash the service
        }
    }
}
```

**Error Handling:**
- Network errors: Retry up to 3 times
- Config errors: Log critical alert for sysadmin
- Service errors: Log and continue (don't crash background service)

---

### 3. ChallengeRotationWorker

**Purpose:** Rotate global challenge every 30 seconds

**Interval:** 30 seconds

**Algorithm:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var newChallenge = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddSeconds(35); // 5s overlap
            
            // Store in memory cache
            _memoryCache.Set(
                "GlobalChallenge:Current",
                newChallenge,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expiresAt
                }
            );
            
            _logger.LogInformation(
                "Rotated global challenge: {Challenge}, Expires: {ExpiresAt}",
                newChallenge, expiresAt
            );
            
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChallengeRotationWorker");
        }
    }
}
```

---

### 4. HistoryCleanupWorker

**Purpose:** Delete old ping and action execution records

**Interval:** Daily (configurable)

**Algorithm:**
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var globalRetentionDays = _configuration
                .GetValue<int>("HistorySettings:DefaultRetentionDays", 90);
            
            // Get all alarms with their retention settings
            var alarms = await _alarmRepository.GetAllAsync();
            
            foreach (var alarm in alarms)
            {
                var retentionDays = alarm.HistoryRetentionDays ?? globalRetentionDays;
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                
                // Delete old pings
                await _pingRepository.DeleteOlderThanAsync(alarm.Id, cutoffDate);
                
                // Delete old action executions
                await _actionExecutionRepository.DeleteOlderThanAsync(
                    alarm.Id, cutoffDate
                );
            }
            
            _logger.LogInformation("Completed history cleanup");
            
            // Wait 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HistoryCleanupService");
        }
    }
}
```

---

## Plugin Systems

### Verification Plugin System

**Interface:**
```csharp
public interface IVerificationMethod
{
    VerificationMethodType Type { get; }
    
    Task<VerificationResult> VerifyAsync(
        string config,
        string proof,
        string? globalChallenge
    );
}
```

**Registration (DI):**
```csharp
services.AddSingleton<IVerificationMethod, TOTPVerificationMethod>();
services.AddSingleton<IVerificationMethod, RSASignatureVerificationMethod>();
services.AddSingleton<IVerificationMethod, ECDSASignatureVerificationMethod>();

services.AddScoped<IVerificationService, VerificationService>();
```

**Usage:**
```csharp
public class VerificationService : IVerificationService
{
    private readonly IEnumerable<IVerificationMethod> _methods;
    
    public async Task<VerificationResult> VerifyPingAsync(
        Alarm alarm,
        string? proof,
        string? globalChallenge)
    {
        if (alarm.VerificationMethod == VerificationMethodType.None)
            return new VerificationResult { IsValid = true };
        
        var method = _methods.FirstOrDefault(m => m.Type == alarm.VerificationMethod);
        if (method == null)
            throw new Exception($"Unknown verification method: {alarm.VerificationMethod}");
        
        return await method.VerifyAsync(alarm.VerificationConfig, proof, globalChallenge);
    }
}
```

---

### Action Plugin System

**Interface:**
```csharp
public interface IActionPlugin
{
    ActionType Type { get; }
    
    Task<ActionExecutionResult> ExecuteAsync(
        string actionConfig,
        string? pingPayload = null
    );
}
```

**Registration (DI):**
```csharp
services.AddSingleton<IActionPlugin, EmailActionPlugin>();
services.AddSingleton<IActionPlugin, SmsActionPlugin>();
services.AddSingleton<IActionPlugin, RestApiActionPlugin>();

services.AddScoped<IActionExecutor, ActionExecutor>();
```

**Usage:**
```csharp
public class ActionExecutor : IActionExecutor
{
    private readonly IEnumerable<IActionPlugin> _plugins;
    
    public async Task<ActionExecutionResult> ExecuteAsync(
        ActionExecution execution,
        string actionConfig,
        string? pingPayload = null)
    {
        var plugin = _plugins.FirstOrDefault(p => p.Type == execution.ActionType);
        if (plugin == null)
            throw new Exception($"Unknown action type: {execution.ActionType}");
        
        var result = await plugin.ExecuteAsync(actionConfig, pingPayload);
        
        // Handle retries for retriable errors
        if (!result.Success && result.IsRetriable && execution.RetryCount < 3)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            execution.RetryCount++;
            return await ExecuteAsync(execution, actionConfig, pingPayload);
        }
        
        return result;
    }
}
```

---

## Security Architecture

### Authentication Flow

```
┌─────────────────────────────────────────────────────────┐
│            JWT Authentication Flow                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  1. Register/Login                                      │
│     POST /api/auth/login                                │
│     { email, password }                                 │
│          │                                              │
│          ▼                                              │
│     ASP.NET Identity validates credentials              │
│          │                                              │
│          ▼                                              │
│     Generate JWT access token (15 min)                  │
│     Generate refresh token (7 days)                     │
│          │                                              │
│          ▼                                              │
│     Return { accessToken, refreshToken }                │
│                                                         │
│  2. Subsequent Requests                                 │
│     Authorization: Bearer {accessToken}                 │
│          │                                              │
│          ▼                                              │
│     JWT Middleware validates token                      │
│          │                                              │
│          ├─── Valid ──────► Attach User to Context     │
│          │                   Continue to Controller     │
│          │                                              │
│          └─── Invalid ────► Return 401 Unauthorized     │
│                                                         │
│  3. Token Refresh                                       │
│     POST /api/auth/refresh                              │
│     { refreshToken }                                    │
│          │                                              │
│          ▼                                              │
│     Validate refresh token                              │
│          │                                              │
│          ▼                                              │
│     Issue new access token                              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Authorization

**Role-Based:** Not in MVP (all authenticated users equal)

**Resource-Based:**
- Users can only access their own alarms
- Enforced in controllers:
```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AlarmsController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAlarm(Guid id)
    {
        var alarm = await _alarmRepository.GetByIdAsync(id);
        
        // Ensure user owns this alarm
        if (alarm.UserId != User.GetUserId())
            return Forbid();
        
        return Ok(alarm);
    }
}
```

### Data Protection

**Client-Side Encryption:**
- Users encrypt sensitive fields before sending to API
- Server stores encrypted data (opaque to server)
- Example: action config passwords, API keys

**Transport Security:**
- HTTPS enforced in production
- TLS 1.2+ required
- HSTS enabled

**Database Security:**
- Connection string in secrets (not source control)
- Prepared statements (EF Core prevents SQL injection)
- Least privilege database user

---

## Scalability Considerations

### Current Limitations (MVP)

1. **Single Instance:**
   - In-memory challenge (no sync across instances)
   - Background services run on single node
   
2. **Database Bottleneck:**
   - All operations go through single PostgreSQL instance
   - 1-second polling may strain at high scale

3. **Action Execution:**
   - Synchronous execution within background service
   - Can block if actions take too long

### Future Scalability Improvements

**Horizontal Scaling:**
1. **Redis for Challenge Storage:**
   - Shared challenge across multiple API instances
   - Pub/sub for challenge rotation

2. **Message Queue for Actions:**
   - RabbitMQ or Kafka for action execution
   - Separate worker pool for actions
   - Retry queue with exponential backoff

3. **Database Read Replicas:**
   - Route history queries to read replicas
   - Primary for writes only

4. **Distributed Locking:**
   - Redis or database-based locks
   - Prevent duplicate action execution

**Vertical Scaling:**
1. **Database Optimization:**
   - More indexes
   - Partitioning by date (Pings, ActionExecutions)
   - Archive old data

2. **Connection Pooling:**
   - Increase pool size
   - Multiple database connections

**Architecture Evolution:**
```
MVP (Single Instance)
    ↓
Multiple API Instances + Redis (Challenge sync)
    ↓
Message Queue for Actions (RabbitMQ)
    ↓
Database Sharding (by UserId)
    ↓
Microservices (API, Alarm Engine, Action Executor)
    ↓
Serverless (AWS Lambda, Timestream)
```

---

## Monitoring & Observability

### Logging

**Structured Logging:**
```csharp
_logger.LogInformation(
    "Alarm triggered: AlarmId={AlarmId}, AlarmPointTime={AlarmPointTime}, ActionsScheduled={ActionsCount}",
    alarm.Id,
    cycle.AlarmPointTime,
    actionsScheduled.Count
);
```

**Log Levels:**
- **Trace:** Detailed diagnostic info
- **Debug:** Development debugging
- **Information:** General flow (alarm triggered, ping received)
- **Warning:** Unexpected but handled (retry on failure)
- **Error:** Errors requiring attention (action failed after retries)
- **Critical:** System errors (background service crashed)

### Metrics (Future)

- Alarms created/deleted per minute
- Pings received per second
- Actions executed per minute
- Action success/failure rates
- Average action execution time
- Database query performance
- API response times

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddCheck<AlarmCycleManagerHealthCheck>("alarm-cycle-manager")
    .AddCheck<ChallengeRotationHealthCheck>("challenge-rotation");

app.MapHealthChecks("/health");
```

---

**Document Version:** 1.0  
**Last Updated:** 2026-02-08  
**Status:** Architecture Documented, Ready for Implementation
