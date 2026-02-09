# WatchDog Alarm - Documentation Index

Complete documentation for the WatchDog Alarm MVP.

## Documentation Overview

| Document | Purpose | Status |
|----------|---------|--------|
| [PLANNING.md](PLANNING.md) | Complete planning session, design decisions, requirements analysis | ✅ Complete |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System architecture, components, data flow, design patterns | ✅ Complete |
| [DATABASE.md](DATABASE.md) | Database schema, tables, indexes, relationships, queries | ✅ Complete |
| [API.md](API.md) | REST API endpoints, request/response examples, authentication | ✅ Complete |

---

## Quick Navigation

### Getting Started
1. Read [README.md](../README.md) for project overview and quick start
2. Review [PLANNING.md](PLANNING.md) to understand requirements and decisions
3. Study [ARCHITECTURE.md](ARCHITECTURE.md) to understand system design

### For Developers
1. [ARCHITECTURE.md](ARCHITECTURE.md) - Understand the system structure
2. [DATABASE.md](DATABASE.md) - Learn the data model
3. [API.md](API.md) - Explore the REST endpoints
4. [PLANNING.md](PLANNING.md) - Reference design decisions

### For API Consumers
1. [API.md](API.md) - Complete API reference
2. [README.md](../README.md) - Authentication and examples
3. [ARCHITECTURE.md](ARCHITECTURE.md) - Verification methods and action plugins

### For Database Administrators
1. [DATABASE.md](DATABASE.md) - Schema, indexes, maintenance
2. [ARCHITECTURE.md](ARCHITECTURE.md) - Background services and data flow

---

## Document Summaries

### PLANNING.md

**Content:**
- Requirements analysis from original README
- Architecture Decisions
  - Deployment architecture, database choice, .NET version, etc.
- Design Decisions
  - Escalation timeline, alarm cycles, verification methods, etc.
- Technical specifications overview
- Identified risks with mitigation strategies
- Open questions for future consideration
- Success criteria checklist

**Key Sections:**
- Architecture Decisions Log
- Design Decisions with rationale
- Technical Specifications
- Open Questions & Risks
- Design Patterns Used

### ARCHITECTURE.md

**Content:**
- High-level system architecture diagram
- Clean Architecture explanation (3 layers)
- Detailed component descriptions:
  - Alarm Cycle Engine (state machine)
  - Verification System (plugin architecture)
  - Action Execution System (retry logic)
  - Global Challenge System (30-second rotation)
- Complete data flow diagrams for 4 scenarios:
  - User creates alarm
  - Background service creates cycle
  - Alarm triggers (1-second polling)
  - User pings alarm
- Background services implementation details
- Plugin system architecture
- Security architecture (authentication, authorization, data protection)
- Scalability considerations and evolution path

**Key Sections:**
- System Overview with architecture diagram
- Layer Structure (API, Core, Infrastructure)
- Core Components (Alarm Engine, Verification, Actions)
- Data Flow scenarios
- Background Services
- Plugin Systems
- Security Architecture
- Scalability Considerations

### DATABASE.md

**Content:**
- Entity Relationship Diagram
- Complete schema for 9 tables:
  - ASP.NET Identity tables
  - Alarms, AlarmDelayActions, AlarmPingActions
  - AlarmCycles, Pings, ActionExecutions
  - GlobalChallenges
- All column definitions with types and descriptions
- Foreign key and check constraints
- Indexes for performance (with query patterns)
- Sample data with complete example
- Database maintenance procedures
- Monitoring queries
- Migration scripts

**Key Sections:**
- Entity Relationship Diagram
- Tables (detailed schemas)
- Indexes (performance optimization)
- Constraints (data integrity)
- Sample Data (example alarm setup)
- Database Maintenance
- Performance Considerations

### API.md

**Content:**
- Base URL and authentication overview
- Standard error response format
- Complete endpoint documentation:
  - Authentication (register, login, refresh)
  - Alarm Management (CRUD, activate, status)
  - Ping (submit ping with verification)
  - Challenge (get current challenge)
  - History (pings and actions)
- Request/response examples for every endpoint
- Verification proof formats (TOTP, RSA, ECDSA)
- Complete cURL examples
- Rate limiting specification (future)

**Key Sections:**
- Authentication Endpoints
- Alarm Management Endpoints
- Ping Endpoints
- Challenge Endpoint
- History Endpoints
- Complete cURL Examples

---

## Key Design Highlights

### 1. Escalation Timeline System
Actions can be scheduled **before** alarm point (warnings) and **after** alarm point (escalation) using negative and positive offset minutes.

**Example:**
```
-120 min: Email reminder "Check in soon!"
  -30 min: SMS "Last chance!"
    0 min: Email "ALERT: No check-in" ← Alarm Point
  +60 min: Email to backup contact
+1440 min: REST API emergency protocol
```

### 2. Alarm Cycle State Machine
Each alarm activation creates a **cycle** with scheduled action executions. Ping resets create a fresh cycle, cancelling old pending actions.

### 3. Verification Plugin Architecture
Support for 4 verification methods:
- **None** (no authentication)
- **TOTP** (6-digit codes, 30-second window)
- **RSA** (2048-bit signature verification)
- **ECDSA** (P-256/P-384 elliptic curve signatures)

### 4. Action Plugin System
Support for 3 action types:
- **Email** (SMTP via MailKit)
- **SMS** (mock for MVP, plugin architecture for real providers)
- **REST API** (HTTP client with configurable methods)

All actions support:
- Retry logic (3 attempts, 5-second intervals)
- Parallel execution (same offset)
- Payload templating (`{ping_payload}` placeholder)

### 5. On-Ping Actions
Separate action list that executes immediately (async) on every valid ping, independent of timer reset behavior.

### 6. Background Services
- **AlarmCycleManagerService**: 1-second polling for precise action timing
- **CycleCreationService**: 5-second polling to create new cycles
- **ChallengeRotationService**: 30-second rotation of global challenge
- **HistoryCleanupService**: Daily cleanup of old records

---

## Database Schema Summary

**Core Tables:**
- `Alarms` (10 fields) - Alarm configuration
- `AlarmDelayActions` (6 fields) - Timeline-based actions
- `AlarmPingActions` (5 fields) - On-ping actions
- `AlarmCycles` (7 fields) - Cycle instances
- `Pings` (11 fields) - Ping records
- `ActionExecutions` (10 fields) - Action execution tracking
- `GlobalChallenges` (4 fields) - Challenge storage

**Key Indexes:**
- `IX_ActionExecutions_ScheduledTime_Status` - Critical for 1-second polling
- `IX_AlarmCycles_AlarmId_Status` - Find active cycles
- `IX_Pings_AlarmId_ReceivedAt` - History queries

**Expected Load (MVP):**
- Active alarms: 100
- Pings/second: 1
- Pending actions: 500
- Database size: 1 GB

---

## API Endpoints Summary

**Authentication (3 endpoints):**
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/refresh

**Alarm Management (6 endpoints):**
- GET /api/alarms
- GET /api/alarms/{id}
- POST /api/alarms
- PUT /api/alarms/{id}
- DELETE /api/alarms/{id}
- POST /api/alarms/{id}/activate
- GET /api/alarms/{id}/status

**Ping (1 endpoint):**
- POST /api/ping/{alarmId}

**Challenge (1 endpoint):**
- GET /api/challenge

**History (2 endpoints):**
- GET /api/history/pings/{alarmId}
- GET /api/history/actions/{alarmId}

**Total:** 13 REST endpoints

---

## Technology Stack

**Backend:**
- .NET 10 (LTS)
- ASP.NET Core Web API
- Entity Framework Core

**Database:**
- PostgreSQL 16

**Libraries:**
- Microsoft.AspNetCore.Identity (user management)
- Microsoft.AspNetCore.Authentication.JwtBearer (JWT)
- Npgsql.EntityFrameworkCore.PostgreSQL (database)
- Swashbuckle.AspNetCore (Swagger)
- MailKit (email)
- Cronos (cron parsing)
- Otp.NET (TOTP)
- System.Security.Cryptography (RSA/ECDSA)

**Development:**
- Docker Compose (PostgreSQL)
- EF Core Migrations

---

## Implementation Status

### Completed
- ✅ Complete planning session (requirements, architecture, design decisions)
- ✅ Comprehensive documentation (142 KB)
- ✅ Database schema design
- ✅ API endpoint specification
- ✅ Architecture diagrams and data flows

### Next Steps
1. Create solution structure (3 projects)
2. Set up PostgreSQL with Docker Compose
3. Implement domain models (Core layer)
4. Set up EF Core with migrations
5. Implement ASP.NET Identity + JWT
6. Create verification plugins
7. Create action plugins
8. Implement background services
9. Create API controllers
10. Add Swagger documentation
11. Write unit and integration tests

---

## Estimated Implementation Timeline

**Total:** 6-8 weeks for single developer

**Phase 1 - Foundation (Week 1-2):**
- Solution structure and project setup
- Database schema and migrations
- ASP.NET Identity + JWT authentication
- Domain models and interfaces

**Phase 2 - Core Features (Week 3-5):**
- Verification plugins (TOTP, RSA, ECDSA)
- Action plugins (Email, SMS mock, REST API)
- Alarm cycle engine
- Background services

**Phase 3 - API & Integration (Week 6):**
- REST API endpoints
- Swagger documentation
- Error handling and logging

**Phase 4 - Testing & Polish (Week 7-8):**
- Unit and integration tests
- Docker setup
- Documentation updates
- Bug fixes and optimization

---

## Success Criteria

MVP is complete when:
- [x] Complete documentation exists
- [ ] Users can register and login with JWT
- [ ] Users can create alarms with timeout or schedule delay types
- [ ] Users can configure before/after action timeline
- [ ] Users can choose verification method (None, TOTP, RSA, ECDSA)
- [ ] Users can ping alarms with optional payload
- [ ] System triggers actions within 5 seconds
- [ ] System enforces 15-second minimum delay
- [ ] Ping resets cycle and creates fresh timeline
- [ ] On-ping actions execute asynchronously
- [ ] Full history available via API
- [ ] Swagger documentation accessible
- [ ] Docker Compose brings up PostgreSQL
- [ ] System runs continuously without crashes

---

## Additional Resources

### External Documentation
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

### Libraries
- [Cronos](https://github.com/HangfireIO/Cronos) - Cron expression parsing
- [MailKit](https://github.com/jstedfast/MailKit) - SMTP client
- [Otp.NET](https://github.com/kspearrin/Otp.NET) - TOTP implementation

---

## Contributing

When contributing, please:
1. Read all documentation first
2. Follow Clean Architecture principles
3. Write unit tests for new features
4. Update relevant documentation
5. Follow C# coding conventions
6. Create feature branch from main
7. Submit pull request with description

---

## Questions or Issues?

If you have questions about the documentation:
1. Check the relevant document (PLANNING, ARCHITECTURE, DATABASE, or API)
2. Look for related design decisions in PLANNING.md
3. Review the architecture diagrams in ARCHITECTURE.md
4. Consult the API examples in API.md

For implementation questions:
1. Refer to the component descriptions in ARCHITECTURE.md
2. Check the database schema in DATABASE.md
3. Review the design patterns section in PLANNING.md
