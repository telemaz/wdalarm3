# WatchDog Alarm - Planning Session Documentation

This document captures the complete planning session, design decisions, and requirements analysis for the WatchDog Alarm MVP.

## Table of Contents
- [Requirements Analysis](#requirements-analysis)
- [Architecture Decisions](#architecture-decisions)
- [Design Decisions](#design-decisions)
- [Technical Specifications](#technical-specifications)
- [Open Questions & Risks](#open-questions--risks)

---

## Requirements Analysis

### Original Requirements (from README)

#### Core Functionality
1. Actions triggered when alarm goes active (no ping received)
2. Alarm timer reset by pings
3. Actions can be triggered ON ping reception
4. Pings may contain payloads
5. Pings may need to be authenticated with Challenge-response verification or JWT
6. Complete history of pings and triggered actions
7. Action types: Email, SMS, REST API
8. Trigger precision: within 5 seconds of target time
9. Multiple delay types: timeout, schedule, dynamic from payload
10. Multiple delays per alarm with different actions
11. Ping not necessarily postponing alarm (configurable)
12. Infinitely scalable architecture

#### Action Specifications

**Email:**
- Max payload: 4096 bytes
- Parameters: smtp, encryption (STARTTLS/TLS), user, password, to, cc, bcc, subject, payload

**SMS:**
- Max payload: 160 bytes
- Parameters: number, payload

**REST API:**
- Max payload: 4096 bytes
- Parameters: url, method (GET/POST/PUT/DELETE), user, password, payload

#### Security Challenges
- Payload should be encrypted before reaching the service
- If there is a leak, the payload and action endpoint can be leaked and linked to a user.

---

## Architecture Decisions

### Decision Log

#### AD-001: Deployment Architecture
**Decision:** Single monolithic ASP.NET Core Web API (not microservices/single executable file) but project structure should reflect potential micro-services architecture.
**Rationale:** 
- Simpler for MVP
- Easier to develop and deploy
- Can be refactored to microservices later
- Still supports background services for alarm processing

**Alternatives Considered:**
- Microservices with message queue
- Serverless (AWS Lambda + Timestream)

#### AD-002: Database Choice
**Decision:** PostgreSQL 16  
**Rationale:**
- Robust relational DB for structured data
- Excellent JSON support (JSONB) for flexible action configs
- TimescaleDB extension available if timeseries optimizations needed
- Free, open-source, self-hosted
- Excellent performance for complex queries

**Alternatives Considered:**
- SQLite (too simple for production)
- SQL Server (more enterprise, heavier)
- MongoDB (less suitable for relational data)

#### AD-003: .NET Version
**Decision:** .NET 10 (LTS, released Nov 2025)  
**Rationale:**
- Latest LTS version available (Feb 2026)
- Long-term support until Nov 2027
- Best performance and features
- Current on development machine

#### AD-004: Project Structure
**Decision:** Clean Architecture with 3 layers (API, Core, Infrastructure)  
**Rationale:**
- Clear separation of concerns
- Domain logic independent of infrastructure
- Easier to test
- Scales well as project grows
- Industry best practice

**Structure:**
```
WdAlarm.Api/              # Presentation layer (web UI and API)
WdAlarm.Core/             # Business logic (data models, background services, interfaces)
WdAlarm.Infrastructure/   # Data access & external services (DB Drivers, actions logics)
```

#### AD-005: Authentication
**Decision:** ASP.NET Identity + JWT tokens  
**Rationale:**
- Full-featured user management out of the box
- Industry standard JWT for API authentication
- Supports password reset, email confirmation (future)
- Extensible for 2FA later

**Token Strategy:**
- Access token: 25h (short-lived)
- Refresh token: 7 days (persistent)

**Alternatives Considered:**
- Simple API keys (too limited)
- OAuth2/OIDC (overkill for MVP)
- Basic Auth (insecure)

#### AD-006: SMS Implementation
**Decision:** Plugin architecture with mock implementation for MVP  
**Rationale:**
- Allows adding real providers (Twilio, Vonage) later
- No external dependencies or costs for MVP
- Can test full flow without sending real SMS
- Plugin interface ensures future compatibility

#### AD-007: Email Implementation
**Decision:** Built-in SMTP client using MailKit
**Rationale:**
- No external service dependencies
- Works with any SMTP server
- Users configure their own email settings
- MailKit is industry standard, robust library
- .Net impementation not recommended for new design.

#### AD-008: Separated alarm checking logic
**Decision:** alarms reached within 1 minutes are checked every 10 ms for ping or reached point
**Rationale:**
- Highest precision
- Usually the alarms should be pinged before reaching this point.
- No need to check all alarms if they are nowhere near reached.


#### AD-009: Separate watcher for alarm target point actions
**Decision:** Alarm target is processed indpendantly form other actions of alarm.
**Rationale:**
- Target represent the turning point and is more important than other alarms so they should impact its processing
- priority of target actions over other pre and post actions.

#### AD-010: Inter-Component Communication
**Decision:** In-memory command bus for API-to-Worker communication (MVP) with migration path to NATS JetStream

**Rationale:**
- **Performance Requirement**: <100ms latency for ping endpoint
- **Request-Reply Pattern**: API needs confirmation from worker before responding to user
- **Zero Dependencies**: In-memory channel requires no external infrastructure for MVP
- **Scalability Path**: Interface-based design allows swap to NATS without code changes

**Architecture:**
```
API Controller → ICommandBus.SendAsync() → Channel → CommandHandlerWorker → Response
```

**Technology:**
- **MVP**: `System.Threading.Channels` (bounded channel, 1000 capacity)
- **Future**: NATS JetStream (distributed, persistent, multiple workers)

**Performance:**
- Command processing: 30-50ms typical
- Total ping latency: 50-85ms (under 100ms target)
- Timeout: 500ms (configurable)

**Commands:**
- `CreateCycleCommand`: When ping resets timer (critical path)
- `CancelCycleCommand`: When alarm deleted
- `ActivateAlarmCommand`: When alarm activated

**Alternatives Considered:**
- Database polling: Too slow (multi-second latency)
- RabbitMQ: Overkill for MVP, adds infrastructure dependency
- gRPC: Worker would need to expose service, more complex
- HTTP between services: Requires separate worker process for MVP
- Redis Pub/Sub: Fire-and-forget only, no request-reply

**Migration Path:**
```
InMemoryCommandBus (same process)
  ↓
NatsCommandBus (multiple servers, persistence, HA)
  ↓
Same ICommandBus interface, no code changes
```

---

## Design Decisions

### DD-001: Escalation Timeline System

**Decision:** Actions can be scheduled before AND after alarm point using negative/positive offsets

**Concept:**
- **Alarm Point (T=0)**: The deadline/target time
- **Negative offsets**: Actions BEFORE alarm point (warnings, reminders)
- **Positive offsets**: Actions AFTER alarm point (escalation)

**Example:**
```
Alarm Point: 09:00 (Feb 9)
Timeline:
  07:00 [-120 min] → Email: "Remember to check in"
  08:30 [ -30 min] → SMS: "Last chance!"
  09:00 [   0 min] → Email: "ALERT: No check-in" ← Alarm Point
  10:00 [ +60 min] → Email: "Escalation to backup contact"
  09:00 [+1440 min] → REST API: "Emergency protocol"
```

**Rationale:**
- Prevents false alarms (user gets warnings)
- Enables gradual escalation (minor → major consequences)
- Flexible for many use cases (reminders, notifications, escalations)

### DD-002: Alarm Cycle State Machine

**Decision:** Implement explicit alarm cycle lifecycle with state tracking

**States:**
- **No Active Cycle**: Alarm exists but not started
- **Active Cycle**: Cycle running, actions scheduled
- **Completed**: User pinged, cycle satisfied
- **Failed**: User failed to ping before alarm point
- **Cancelled**: Manual cancellation or alarm disabled

**Cycle Creation Triggers:**
1. User activates alarm (first time/new cycle)
2. User pings and resets timer (new cycle)
3. Previous cycle reach alarm target point. (alarm failed/new cycle)

**Database Tables:**
- `AlarmCycles`: Tracks each cycle instance
- `ActionExecutions`: Individual action attempts

**Rationale:**
- Complete audit trail (which cycle, which actions fired)
- Clean reset behavior (cancel old, create new)
- Supports complex scenarios (partial execution, failures)
- Query optimization (index on active cycles)

### DD-003: Ping Reset Behavior

**Decision:** Two-level control system for ping reset behavior

**Levels:**
1. **Alarm default**: `Alarm.AllowPingNoTimerReset` (boolean)
2. **Ping request**: `Ping.ResetTimerRequested` parameter

**Logic:**
```
if (!Alarm.AllowPingNoTimerReset) {
    timerReset = true;
} else {
    // Always reset
    timerReset = pingRequest.ResetTimerRequested;
}
```

**Rationale:**
- Flexibility: some use cases don't want resets (monitoring)
- Per-ping control: emergency ping vs regular ping
- Safe defaults: most users want reset behavior

### DD-004: Alarm Delay Types

**Decision:** Two delay types (not three for MVP)

**Supported:**
1. **Timeout**: Relative duration from last ping (e.g., 24 hours)
2. **Schedule**: Absolute times using cron expressions (e.g., daily at 9am)

**NOT in MVP:**
3. Dynamic timeout from ping payload (deferred to v2)

**Timeout Calculation:**
```
AlarmPoint = LastPingTime + TimeoutDuration
```

**Schedule Calculation:**
```
AlarmPoint = NextCronOccurrence(CronExpression, after: LastPingTime)
```

**Schedule + Ping Interaction:**
- Ping BEFORE scheduled time: Prevents that occurrence, moves to next
- Ping AFTER scheduled time: Still recorded, but alarm already triggered, end previously failed alarm actions.
- Example: Cron = "0 9 * * *" (9am daily)
  - User pings at 8:50am → Today's 9am alarm cancelled, moves to tomorrow 9am
  - User pings at 9:10am → Today's 9am already triggered, tomorrow 9am scheduled

**Rationale:**
- Covers majority of use cases
- Dynamic payload timeout adds complexity (parsing, validation)
- Can be added in v2 without breaking changes

### DD-005: Verification Methods

**Decision:** Plugin architecture with 5 verification types

**Types:**
1. **None**: No authentication (public alarms)
   - Use case: Public status page, non-sensitive pings
   - Security: UUID alarm IDs for obscurity

2. **TOTP** (Time-based One-Time Password):
   - Standard 6-digit codes (Google Authenticator compatible)
   - 30-second window
   - Stored: Base32-encoded secret
   - Library: Otp.NET

3. **RSA Signature**:
   - Client signs `(globalChallenge + timestamp)` with private key
   - Server verifies with stored public key (PEM format)
   - Timestamp tolerance: ±1 minutes (prevents replay attacks)
   - Reject call with same timestamp as previously received in last 35s
   - Key size: 2048-bit minimum

4. **ECDSA Signature**:
   - Same signing mechanism as RSA
   - Elliptic curve cryptography (smaller keys)
   - Curve: P-256 (secp256r1) or P-384

5. **JWT tokken**
   - Use the same auth method as user
   - Require constant up-to-date tokené

**Global Challenge System:**
- 30-second rotating challenge (UUID)
- Stored in-memory (IMemoryCache)
- ChallengeRotationService updates every 30 seconds
- Exposed via `GET /api/challenge` endpoint
- Client retrieves, signs, submits within 30-second window

**Rationale:**
- None: Supports simple use cases without complexity
- TOTP: User-friendly, widely supported (mobile apps)
- RSA/ECDSA: Strong cryptographic verification, prevents replay attacks
- Plugin architecture: Easy to add new methods (hardware tokens, etc.)

### DD-006: Action Plugin Architecture

**Decision:** Separate plugin per action type with retry logic

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

public interface ActionExecutionResult
{
    bool Success { get; set; }
    string? ErrorMessage { get; set; }
    bool IsRetriable { get; set; }  // Key design point
}
```

**Retry Logic:**
- **Retriable errors** (network failures, timeouts): Retry 3 times with 5-second intervals
- **Non-retriable errors** (auth failures, invalid config): Log critical error, no retry
- **System errors**: Log for sysadmin, send alert

**Plugins:**

1. **EmailActionPlugin** (MailKit):
   - Supports SMTP with TLS/STARTTLS
   - Template substitution: `{ping_payload}` placeholder
   - Timeout: 30 seconds per send
   - Retriable: connection refused, timeout
   - Non-retriable: auth failure, invalid recipient

2. **SmsActionPlugin** (Mock for MVP):
   - Logs to console: `[SMS] To: +1234567890, Message: ...`
   - Always returns success
   - Future: Twilio/Vonage HTTP API integration

3. **RestApiActionPlugin**:
   - HttpClient with configurable timeout
   - Supports GET/POST/PUT/DELETE
   - Basic auth support
   - Payload passed as separate field + template substitution
   - Retriable: HTTP 5xx, network errors
   - Non-retriable: HTTP 4xx (except 429 rate limit)

**Rationale:**
- Plugin architecture: Easy to add new action types (Slack, Discord, etc.)
- IsRetriable flag: Prevents infinite retries on config errors
- Separation of concerns: Each plugin handles one action type
- Testability: Can mock plugins in unit tests

### DD-007: On-Ping Actions

**Decision:** Separate action list executed on every valid ping

**Configuration:**
- `Alarm.PingActions[]`: Array of actions to execute on ping
- Independent from `Alarm.DelayActions[]` (timeline actions)

**Execution:**
- Triggers on ANY valid ping (passes verification)
- Executes asynchronously (doesn't block ping response)
- Receives both configured payload AND ping payload
- Logged in `ActionExecutions` table with `PingId` reference

**Payload Handling:**
```json
{
  "actionType": "Email",
  "actionConfig": {
    "to": "log@example.com",
    "subject": "Ping received",
    "body": "User checked in. Payload: {ping_payload}"
  }
}
```

**Interaction with Timer Reset:**
- On-ping actions execute REGARDLESS of timer reset
- Even if `TimerReset = false`, on-ping actions still fire
- Use case: Logging all pings even if not resetting timer

**Rationale:**
- Real-time notifications (user checked in successfully)
- Logging/auditing (external system tracking)
- Separate concern from timeline actions (different triggers)
- Flexibility (can have both types on same alarm)

### DD-008: Parallel Action Execution

**Decision:** Actions at same offset execute concurrently

**Rationale:**
- Faster notification (all recipients get alert simultaneously)
- Reduces total execution time
- No dependencies between actions
- Failure isolation (one failure doesn't block others)

### DD-009: History Retention Policy

**Decision:** Two-level retention policy (global + per-alarm override)

**Settings:**
1. **Global default**: `HistorySettings:DefaultRetentionDays` (e.g., 90 days)
2. **Per-alarm override**: `Alarm.HistoryRetentionDays` (nullable)

**Logic:**
```
retentionDays = alarm.HistoryRetentionDays ?? globalSettings.DefaultRetentionDays;
deleteBeforeDate = DateTime.UtcNow.AddDays(-retentionDays);
```

**Cleanup Service:**
- Runs daily via `HistoryCleanupService`
- Deletes `Pings` and `ActionExecutions` older than retention period
- Soft delete option (set DeletedAt timestamp) for audit compliance

**Rationale:**
- Complies with data retention regulations
- Reduces database size over time
- Per-alarm override for special cases (legal hold, investigation)
- Configurable for different jurisdictions

### DD-010: No-Auth Alarm Security

**Decision:** UUID-based obscurity + rate limiting (future)

**Security Measures:**
1. **UUID alarm IDs**: Hard to guess (128-bit random)
   - Example: `a7b2c3d4-e5f6-4789-0abc-def123456789`
   - Brute force impractical (2^128 combinations)

2. **Rate limiting** (future, not MVP):
   - Per IP: 10 pings/minute
   - Per alarm: 100 pings/hour
   - DDoS protection

3. **Documentation warning**:
   - Clearly state no-auth alarms are less secure
   - Recommend verification methods for sensitive use cases

**Trade-off:**
- Convenience (no auth needed) vs Security (anyone with ID can ping)
- Acceptable for non-sensitive use cases (public status monitoring)

**Rationale:**
- Some use cases don't need security (public dashboards)
- UUID provides reasonable obscurity for casual use
- Rate limiting prevents abuse
- User choice (can enable verification if needed)

---

## Technical Specifications

### Database Schema

See [DATABASE.md](DATABASE.md) for complete schema documentation.

**Key Design Points:**

1. **AlarmCycles table**: Tracks each alarm activation instance
   - Enables complete audit trail
   - Clean separation between cycles
   - Efficient querying with indexes

2. **ActionExecutions table**: Unified tracking for all actions
   - Both timeline actions and on-ping actions
   - Tracks retry attempts
   - Records success/failure with error messages
   - Foreign keys to source (DelayAction or PingAction)

3. **Separate action tables**: `AlarmDelayActions` vs `AlarmPingActions`
   - Clear separation of concerns
   - Different lifecycles (timeline vs immediate)
   - Easier to query and manage

4. **JSONB for action configs**:
   - Flexible schema (different configs per action type)
   - PostgreSQL native JSON querying
   - Client-side encryption support

5. **Indexes for performance**:
   - `ActionExecutions.ScheduledTime` for alarm target point queries
   - `AlarmCycles.AlarmPointTime` for cycle queries
   - `Pings.AlarmId + ReceivedAt` for history queries

### Background Services

#### 1. ChallengeRotationService
```csharp
Interval: 30 seconds
Purpose: Generate and distribute new global challenge
Storage: IMemoryCache (in-memory)
Future: Redis for distributed systems
```

#### 2. AlarmCycleTargetManagerService
```csharp
Interval: 10 milliseconds
Purpose: Execute scheduled actions
Process:
  1. Query: ActionExecutions WHERE Status=Pending AND ScheduledTime <= NOW() AND AlarmDelayActionsID.Offset=0
  2. Group by AlarmId for parallel execution
  3. Execute via IActionExecutor
  4. Update status (Success/Failed/Retry)
  5. Handle errors (log, alert sysadmin)
```

#### 3. AlarmCycleManagerService
```csharp
Interval: 10 milliseconds
Purpose: Execute scheduled actions
Process:
  1. Query: ActionExecutions WHERE Status=Pending AND ScheduledTime <= NOW() AND AlarmDelayActionsID.Offset <> 0
  2. Group by AlarmId for parallel execution
  3. Execute via IActionExecutor
  4. Update status (Success/Failed/Retry)
  5. Handle errors (log, alert sysadmin)
```

#### 4. CycleCreationService
```csharp
Interval: 5 seconds
Purpose: Create new alarm cycles
Process:
  1. Find alarms without active cycle
  2. Calculate next alarm point (timeout or cron)
  3. Create AlarmCycle record
  4. Create ActionExecutions for all DelayActions
Trigger: Also runs immediately after successful ping reset
```

#### 5. HistoryCleanupService
```csharp
Interval: Daily (configurable)
Purpose: Delete old history records
Process:
  1. For each alarm, get retention policy
  2. Delete Pings older than retention
  3. Delete ActionExecutions older than retention
  4. Log deletion statistics
```

### API Design Principles

1. **RESTful**: Standard HTTP methods (GET, POST, PUT, DELETE)
2. **JWT Authentication**: Bearer token in Authorization header
3. **JSON payloads**: All request/response bodies
4. **Consistent error format**:
```json
{
  "error": "ValidationError",
  "message": "Alarm name is required",
  "details": {
    "field": "name",
    "code": "REQUIRED"
  }
}
```
5. **Pagination**: `?page=1&pageSize=50` or HTTP Range parameters for list endpoints
6. **Filtering**: `?from=2026-01-01&to=2026-12-31` for date ranges
7. **Versioning**: `/api/v1/...` (future-proofing)

### Configuration Management

**appsettings.json structure:**
```json
{
  "ConnectionStrings": { ... },
  "JwtSettings": { ... },
  "AlarmSettings": { ... },
  "ActionSettings": { ... },
  "HistorySettings": { ... }
}
```

**Environment overrides:**
- `appsettings.Development.json`: Local dev settings
- `appsettings.Production.json`: Production secrets (not in repo)
- Environment variables: `ConnectionStrings__DefaultConnection`

**Secrets management:**
- MVP: appsettings + .gitignore
- Production: Azure Key Vault / AWS Secrets Manager / HashiCorp Vault

---

## Open Questions & Risks

### Known Risks

#### R-001: Email Deliverability
**Risk:** SMTP sending may be slow, fail, or hit rate limits

**Mitigation:**
- Retry logic (3 attempts)
- Timeout (30 seconds)
- User provides own SMTP (distributed load)
- Log failures for manual review

**Future Solution:**
- Transactional email service (SendGrid, Mailgun)
- Queue with exponential backoff
- Dead letter queue for permanent failures

#### R-002: Challenge Synchronization
**Risk:** In-memory challenge doesn't work with multiple instances

**Mitigation:**
- MVP: Single instance deployment (acceptable)
- Document limitation
- Design supports distributed cache (IDistributedCache interface)

**Future Solution:**
- Redis for shared challenge storage
- Database table with frequent updates (fallback)

#### R-003: Client-Side Encryption Usability
**Risk:** Users may not know how to encrypt sensitive data

**Mitigation:**
- Clear documentation with examples
- Provide reference implementation (JavaScript, Python)
- Warn about storing plaintext secrets

**Future Solution:**
- Server-side key management with user-provided key
- Integration with password managers (1Password, Bitwarden)

#### R-004: Cron Expression Validation
**Risk:** Invalid cron expressions may cause runtime errors

**Mitigation:**
- Validate on alarm creation using Cronos library
- Return clear error messages
- Test with common patterns
- Provide cron expression builder in UI (future)

### Open Questions

#### Q-001: Action Config Encryption
**Question:** Should server encrypt action configs at rest?

**Current Decision:** Client-side encryption (user responsibility)

**Future Consideration:**
- Server encrypts with master key (stored in Key Vault)
- User provides encryption key (zero-knowledge)
- Hybrid: Server encrypts non-sensitive, client encrypts sensitive fields

#### Q-002: Alarm Template Library
**Question:** Should we provide pre-built alarm templates?

**Examples:**
- "Daily wellness check" (24hr timeout, email escalation)
- "Expedition tracking" (6hr timeout, SMS + REST API)
- "Server monitoring" (5min timeout, no auth)

**Benefits:**
- Easier for users to get started
- Best practices encoded
- Common use cases covered

**Decision:** Defer to v2 (focus on core functionality in MVP)

#### Q-003: Multi-User Alarm Collaboration
**Question:** Should multiple users be able to manage same alarm?

**Use Case:** 
- Team expedition tracking
- Shared family wellness check

**Complexity:**
- Permissions system (owner, editor, viewer)
- Audit log (who modified what)
- Notification routing (who gets pinged?)

**Decision:** Single user per alarm in MVP, consider for v2

#### Q-004: Alarm Pause/Snooze
**Question:** Should users be able to pause alarms temporarily?

**Use Case:**
- Vacation (don't want notifications)
- System maintenance

**Implementation:**
- `Alarm.IsPaused` flag
- `Alarm.PausedUntil` timestamp

**Decision:** Useful feature, add to v1.1 (post-MVP)

#### Q-005: Webhook Support
**Question:** Should we support outbound webhooks for events?

**Events:**
- Alarm triggered
- Ping received
- Action failed

**Benefits:**
- Integration with external systems
- Custom notification channels
- Analytics and monitoring

**Decision:** REST API action type covers basic use case, dedicated webhook system for v1.2

---

## Design Patterns Used

### 1. Repository Pattern
**Location:** Infrastructure layer
**Purpose:** Abstract data access, enable testing  
**Example:** `IAlarmRepository`, `IPingRepository`

### 2. Plugin Architecture
**Location:** Verification and Action systems  
**Purpose:** Extensibility, loose coupling  
**Example:** `IVerificationMethod`, `IActionPlugin`

### 3. Background Service Pattern
**Location:** API layer  
**Purpose:** Long-running background tasks  
**Example:** `AlarmCycleManagerService : BackgroundService`

### 4. Factory Pattern
**Location:** Service creation  
**Purpose:** Dynamic instantiation based on type  
**Example:** `ActionPluginFactory`, `VerificationMethodFactory`

### 5. Strategy Pattern
**Location:** Verification and Action execution  
**Purpose:** Interchangeable algorithms  
**Example:** Different verification methods, different action types

### 6. Unit of Work Pattern
**Location:** EF Core DbContext  
**Purpose:** Transaction management  
**Example:** `SaveChangesAsync()` commits all changes atomically

---

## Success Criteria

### MVP Complete When:

#### Core Functionality
- [ ] Users can register and login with JWT
- [ ] Users can create alarms with timeout or schedule delay types
- [ ] Users can configure before/after action timeline
- [ ] Users can configure on-ping actions
- [ ] Users can choose verification method (None, TOTP, RSA, ECDSA)
- [ ] Users can ping alarms with optional payload
- [ ] System triggers actions within 5 seconds
- [ ] System enforces 15-second minimum delay
- [ ] Ping resets cycle and creates fresh timeline
- [ ] On-ping actions execute asynchronously
- [ ] Full history available

#### Technical Quality
- [ ] Swagger documentation accessible
- [ ] Docker Compose brings up PostgreSQL
- [ ] System runs continuously without crashes
- [ ] Database migrations work correctly
- [ ] Error handling and logging implemented
- [ ] Unit tests for core logic
- [ ] Integration tests for critical paths

#### Documentation
- [x] README with quick start guide
- [x] ARCHITECTURE.md with system design
- [x] DATABASE.md with schema documentation
- [x] API.md with endpoint examples
- [ ] Code comments on complex logic

---

## Timeline Estimate

### Phase 1: Foundation
- Solution structure and project setup
- Database schema and migrations
- ASP.NET Identity + JWT authentication
- Domain models and interfaces

### Phase 2: Core Features
- Verification plugins
- Action plugins
- Alarm cycle engine
- Background services

### Phase 3: API & Integration
- REST API endpoints
- Swagger documentation
- Error handling
- Logging

### Phase 4: Testing & Polish
- Unit and integration tests
- Docker setup
- Documentation
- Bug fixes


---

## Lessons Learned (Post-Implementation)

*This section will be filled after MVP is complete*

---

## References

- [Original Requirements](../README.md)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Cronos Library](https://github.com/HangfireIO/Cronos)
- [MailKit](https://github.com/jstedfast/MailKit)
- [Otp.NET](https://github.com/kspearrin/Otp.NET)
