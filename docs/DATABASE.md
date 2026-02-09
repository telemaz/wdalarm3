# Database Schema Documentation

Complete database schema for WatchDog Alarm system using PostgreSQL 16.

## Table of Contents
- [Overview](#overview)
- [Entity Relationship Diagram](#entity-relationship-diagram)
- [Tables](#tables)
- [Indexes](#indexes)
- [Constraints](#constraints)
- [Sample Data](#sample-data)

---

## Overview

**Database:** PostgreSQL 16  
**ORM:** Entity Framework Core  
**Migration Strategy:** Code-first with EF Core Migrations

### Design Principles
1. **Normalization**: 3NF to reduce redundancy
2. **JSONB for flexibility**: Action configs stored as JSON
3. **Audit trail**: Timestamps on all entities
4. **Soft deletes**: Optional for compliance (future)
5. **UUIDs**: All primary keys use GUIDs for distribution
6. **Indexes**: Optimized for 1-second polling queries

---

## Entity Relationship Diagram

```
┌─────────────────┐
│   AspNetUsers   │
│  (Identity)     │
└────────┬────────┘
         │
         │ 1:N
         ▼
┌─────────────────┐
│     Alarms      │◄──────────┐
└────────┬────────┘           │
         │                    │
         │ 1:N                │ N:1
         ▼                    │
┌─────────────────────┐       │
│ AlarmDelayActions   │       │
└─────────────────────┘       │
         │                    │
┌─────────────────────┐       │
│  AlarmPingActions   │       │
└─────────────────────┘       │
         │                    │
         │ 1:N                │
         ▼                    │
┌─────────────────┐           │
│  AlarmCycles    │───────────┘
└────────┬────────┘
         │
         │ 1:N
         ▼
┌─────────────────┐
│      Pings      │
└────────┬────────┘
         │
         │ 1:N
         ▼
┌─────────────────────┐
│  ActionExecutions   │
└─────────────────────┘
```

---

## Tables

### AspNetUsers (ASP.NET Identity)

Standard ASP.NET Identity tables for user management.

**Key Tables:**
- `AspNetUsers`: User accounts
- `AspNetRoles`: Role definitions
- `AspNetUserRoles`: User-role mappings
- `AspNetUserClaims`: Additional user claims
- `AspNetUserLogins`: External login providers
- `AspNetUserTokens`: Authentication tokens

See [Microsoft Identity documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) for complete schema.

---

### Alarms

Stores alarm configurations.

```sql
CREATE TABLE Alarms (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID NOT NULL,
    Name VARCHAR(200) NOT NULL,
    Description TEXT NULL,
    
    -- Delay configuration
    DelayType VARCHAR(20) NOT NULL,
    TimeoutDuration INTERVAL NULL,
    CronExpression VARCHAR(100) NULL,
    
    -- Ping behavior
    AllowPingNoTimerReset BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Verification configuration
    VerificationMethod VARCHAR(20) NULL,
    VerificationConfig JSONB NULL,
    
    -- Metadata
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    
    -- Foreign keys
    CONSTRAINT FK_Alarms_Users FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    
    -- Check constraints
    CONSTRAINT CHK_DelayType CHECK (DelayType IN ('Timeout', 'Schedule')),
    CONSTRAINT CHK_VerificationMethod CHECK (
        VerificationMethod IS NULL OR 
        VerificationMethod IN ('None', 'TOTP', 'RSA', 'ECDSA')
    ),
    CONSTRAINT CHK_DelayConfig CHECK (
        (DelayType = 'Timeout' AND TimeoutDuration IS NOT NULL AND CronExpression IS NULL) OR
        (DelayType = 'Schedule' AND CronExpression IS NOT NULL AND TimeoutDuration IS NULL)
    )
);
```

**Columns:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | UUID | No | Primary key |
| UserId | UUID | No | Owner of the alarm |
| Name | VARCHAR(200) | No | Display name |
| Description | TEXT | Yes | Optional description |
| DelayType | VARCHAR(20) | No | 'Timeout' or 'Schedule' |
| TimeoutDuration | INTERVAL | Yes | Duration for Timeout type (e.g., '24:00:00') |
| CronExpression | VARCHAR(100) | Yes | Cron for Schedule type (e.g., '0 9 * * *') |
| AllowTimerReset | BOOLEAN | No | Can pings reset the timer? |
| PerPingResetOverride | BOOLEAN | No | Can individual pings override reset behavior? |
| VerificationMethod | VARCHAR(20) | Yes | 'None', 'TOTP', 'RSA', 'ECDSA', or NULL |
| VerificationConfig | JSONB | Yes | Verification-specific config (secret, public key) |
| IsActive | BOOLEAN | No | Is alarm currently active? |
| CreatedAt | TIMESTAMP | No | Creation timestamp |
| UpdatedAt | TIMESTAMP | No | Last update timestamp |

**VerificationConfig Examples:**

```json
// TOTP
{
  "secret": "BASE32ENCODEDSECRET",
  "period": 30,
  "digits": 6
}

// RSA
{
  "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgk...\n-----END PUBLIC KEY-----",
  "algorithm": "RS256"
}

// ECDSA
{
  "publicKey": "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZI...\n-----END PUBLIC KEY-----",
  "curve": "P-256",
  "algorithm": "ES256"
}
```

---

### AlarmDelayActions

Timeline-based actions (before/after alarm point).

```sql
CREATE TABLE AlarmDelayActions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    AlarmId UUID NOT NULL,
    
    -- Timeline position
    OffsetMinutes INT NOT NULL,
    ExecutionOrder INT NOT NULL DEFAULT 0,
    
    -- Action configuration
    ActionType VARCHAR(20) NOT NULL,
    ActionConfig JSONB NOT NULL,
    
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT FK_AlarmDelayActions_Alarms FOREIGN KEY (AlarmId) 
        REFERENCES Alarms(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_ActionType CHECK (ActionType IN ('Email', 'SMS', 'RestApi'))
);
```

**Columns:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | UUID | No | Primary key |
| AlarmId | UUID | No | Parent alarm |
| OffsetMinutes | INT | No | Minutes from alarm point (negative = before, positive = after) |
| ExecutionOrder | INT | No | Order for actions at same offset (0 = first) |
| ActionType | VARCHAR(20) | No | 'Email', 'SMS', or 'RestApi' |
| ActionConfig | JSONB | No | Action-specific configuration |
| CreatedAt | TIMESTAMP | No | Creation timestamp |

**ActionConfig Examples:**

```json
// Email
{
  "smtpHost": "smtp.gmail.com",
  "smtpPort": 587,
  "useSsl": true,
  "username": "alerts@example.com",
  "password": "encrypted_password",
  "from": "alerts@example.com",
  "to": "user@example.com",
  "cc": "backup@example.com",
  "bcc": "",
  "subject": "Alarm triggered",
  "body": "User has not checked in. Payload: {ping_payload}"
}

// SMS
{
  "provider": "twilio",
  "accountSid": "encrypted_sid",
  "authToken": "encrypted_token",
  "from": "+15551234567",
  "to": "+15559876543",
  "message": "ALERT: No check-in. {ping_payload}"
}

// REST API
{
  "url": "https://api.example.com/webhook",
  "method": "POST",
  "headers": {
    "Authorization": "Bearer encrypted_token",
    "Content-Type": "application/json"
  },
  "payload": "{\"alarm\": \"wellness_check\", \"status\": \"triggered\", \"data\": \"{ping_payload}\"}"
}
```

---

### AlarmPingActions

Actions triggered immediately on ping reception.

```sql
CREATE TABLE AlarmPingActions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    AlarmId UUID NOT NULL,
    
    -- Action configuration
    ActionType VARCHAR(20) NOT NULL,
    ActionConfig JSONB NOT NULL,
    ExecutionOrder INT NOT NULL DEFAULT 0,
    
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT FK_AlarmPingActions_Alarms FOREIGN KEY (AlarmId) 
        REFERENCES Alarms(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_ActionType CHECK (ActionType IN ('Email', 'SMS', 'RestApi'))
);
```

**Columns:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | UUID | No | Primary key |
| AlarmId | UUID | No | Parent alarm |
| ActionType | VARCHAR(20) | No | 'Email', 'SMS', or 'RestApi' |
| ActionConfig | JSONB | No | Action-specific configuration |
| ExecutionOrder | INT | No | Order for multiple on-ping actions |
| CreatedAt | TIMESTAMP | No | Creation timestamp |

**ActionConfig:** Same format as AlarmDelayActions, can use `{ping_payload}` placeholder.

---

### AlarmCycles

Tracks individual alarm activation cycles.

```sql
CREATE TABLE AlarmCycles (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    AlarmId UUID NOT NULL,
    
    -- Cycle timing
    AlarmPointTime TIMESTAMP NOT NULL,
    StartedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    CompletedAt TIMESTAMP NULL,
    CompletedByPingId UUID NULL,
    
    -- Cycle status
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    
    CONSTRAINT FK_AlarmCycles_Alarms FOREIGN KEY (AlarmId) 
        REFERENCES Alarms(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AlarmCycles_Pings FOREIGN KEY (CompletedByPingId) 
        REFERENCES Pings(Id) ON DELETE SET NULL,
    CONSTRAINT CHK_Status CHECK (Status IN ('Active', 'Completed', 'Cancelled'))
);
```

**Columns:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | UUID | No | Primary key |
| AlarmId | UUID | No | Parent alarm |
| AlarmPointTime | TIMESTAMP | No | The T=0 moment for this cycle |
| StartedAt | TIMESTAMP | No | When cycle was created |
| CompletedAt | TIMESTAMP | Yes | When cycle was completed (NULL if active) |
| CompletedByPingId | UUID | Yes | Which ping completed this cycle |
| Status | VARCHAR(20) | No | 'Active', 'Completed', or 'Cancelled' |

**Lifecycle:**

1. **Created**: Status = 'Active', CompletedAt = NULL
2. **Ping Received (reset timer)**: Status = 'Completed', CompletedAt = NOW(), CompletedByPingId = ping.Id
3. **Manual Cancel**: Status = 'Cancelled', CompletedAt = NOW()

---

### Pings

Records all ping attempts.

```sql
CREATE TABLE Pings (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    AlarmId UUID NOT NULL,
    AlarmCycleId UUID NULL,
    
    -- Ping data
    ReceivedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    Payload TEXT NULL,
    
    -- Verification
    VerificationProof TEXT NULL,
    VerificationResult BOOLEAN NOT NULL,
    VerificationError TEXT NULL,
    
    -- Behavior
    ResetTimerRequested BOOLEAN NOT NULL DEFAULT TRUE,
    TimerWasReset BOOLEAN NOT NULL,
    
    -- Metadata
    IpAddress VARCHAR(45) NULL,
    UserAgent TEXT NULL,
    
    CONSTRAINT FK_Pings_Alarms FOREIGN KEY (AlarmId) 
        REFERENCES Alarms(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Pings_AlarmCycles FOREIGN KEY (AlarmCycleId) 
        REFERENCES AlarmCycles(Id) ON DELETE SET NULL
);
```

**Columns:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | UUID | No | Primary key |
| AlarmId | UUID | No | Which alarm was pinged |
| AlarmCycleId | UUID | Yes | Active cycle when ping received |
| ReceivedAt | TIMESTAMP | No | When ping was received |
| Payload | TEXT | Yes | User-provided payload (max 4096 bytes) |
| VerificationProof | TEXT | Yes | TOTP code, signature, or NULL for None verification |
| VerificationResult | BOOLEAN | No | Did verification pass? |
| VerificationError | TEXT | Yes | Error message if verification failed |
| ResetTimerRequested | BOOLEAN | No | Did client request timer reset? |
| TimerWasReset | BOOLEAN | No | Did server actually reset timer? |
| IpAddress | VARCHAR(45) | Yes | Client IP address (for rate limiting) |
| UserAgent | TEXT | Yes | Client user agent string |

**Verification Logic:**

```
if (VerificationMethod == 'None'):
    VerificationResult = true
else:
    VerificationResult = VerifyProof(VerificationMethod, VerificationProof)
    if (!VerificationResult):
        VerificationError = "Invalid TOTP code" | "Invalid signature" | etc.
```

---

### ActionExecutions

Unified tracking for all action executions.

```sql
CREATE TABLE ActionExecutions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Source tracking
    AlarmDelayActionId UUID NULL,
    AlarmPingActionId UUID NULL,
    AlarmCycleId UUID NULL,
    PingId UUID NULL,
    
    -- Execution details
    ActionType VARCHAR(20) NOT NULL,
    ScheduledTime TIMESTAMP NOT NULL,
    ExecutedTime TIMESTAMP NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    
    -- Retry tracking
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage TEXT NULL,
    
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT FK_ActionExecutions_AlarmDelayActions FOREIGN KEY (AlarmDelayActionId) 
        REFERENCES AlarmDelayActions(Id) ON DELETE SET NULL,
    CONSTRAINT FK_ActionExecutions_AlarmPingActions FOREIGN KEY (AlarmPingActionId) 
        REFERENCES AlarmPingActions(Id) ON DELETE SET NULL,
    CONSTRAINT FK_ActionExecutions_AlarmCycles FOREIGN KEY (AlarmCycleId) 
        REFERENCES AlarmCycles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ActionExecutions_Pings FOREIGN KEY (PingId) 
        REFERENCES Pings(Id) ON DELETE SET NULL,
    
    CONSTRAINT CHK_ActionType CHECK (ActionType IN ('Email', 'SMS', 'RestApi')),
    CONSTRAINT CHK_Status CHECK (Status IN ('Pending', 'Executing', 'Success', 'Failed', 'Cancelled')),
    CONSTRAINT CHK_ActionSource CHECK (
        (AlarmDelayActionId IS NOT NULL AND AlarmPingActionId IS NULL) OR
        (AlarmPingActionId IS NOT NULL AND AlarmDelayActionId IS NULL)
    ),
    CONSTRAINT CHK_PingActionHasPing CHECK (
        (AlarmPingActionId IS NULL) OR 
        (AlarmPingActionId IS NOT NULL AND PingId IS NOT NULL)
    )
);
```

**Columns:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | UUID | No | Primary key |
| AlarmDelayActionId | UUID | Yes | Source delay action (mutually exclusive with PingActionId) |
| AlarmPingActionId | UUID | Yes | Source ping action (mutually exclusive with DelayActionId) |
| AlarmCycleId | UUID | Yes | Associated cycle (for delay actions) |
| PingId | UUID | Yes | Associated ping (for ping actions) |
| ActionType | VARCHAR(20) | No | 'Email', 'SMS', or 'RestApi' |
| ScheduledTime | TIMESTAMP | No | When action should execute |
| ExecutedTime | TIMESTAMP | Yes | When action actually executed (NULL if pending) |
| Status | VARCHAR(20) | No | Execution status |
| RetryCount | INT | No | Number of retry attempts |
| ErrorMessage | TEXT | Yes | Error details if failed |
| CreatedAt | TIMESTAMP | No | Creation timestamp |

**Status Flow:**

```
Pending → Executing → Success
                  ↓
                Failed (retry) → Executing → Success
                                          ↓
                                       Failed (final)
```

**Status Definitions:**

- **Pending**: Scheduled but not yet executed
- **Executing**: Currently being executed
- **Success**: Executed successfully
- **Failed**: Failed (may retry if RetryCount < MaxRetries)
- **Cancelled**: Cancelled before execution (e.g., cycle reset)

---

### GlobalChallenges

Stores current global challenge for signature verification.

```sql
CREATE TABLE GlobalChallenges (
    Id INT PRIMARY KEY DEFAULT 1,
    CurrentChallenge VARCHAR(100) NOT NULL,
    GeneratedAt TIMESTAMP NOT NULL,
    ExpiresAt TIMESTAMP NOT NULL,
    
    CONSTRAINT CHK_Singleton CHECK (Id = 1)
);

-- Initialize with first challenge
INSERT INTO GlobalChallenges (Id, CurrentChallenge, GeneratedAt, ExpiresAt)
VALUES (1, gen_random_uuid()::TEXT, NOW(), NOW() + INTERVAL '30 seconds')
ON CONFLICT (Id) DO NOTHING;
```

**Columns:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | INT | No | Always 1 (singleton pattern) |
| CurrentChallenge | VARCHAR(100) | No | Current challenge string (UUID) |
| GeneratedAt | TIMESTAMP | No | When challenge was generated |
| ExpiresAt | TIMESTAMP | No | When challenge expires |

**Usage:**

```sql
-- ChallengeRotationService updates every 30 seconds
UPDATE GlobalChallenges
SET CurrentChallenge = gen_random_uuid()::TEXT,
    GeneratedAt = NOW(),
    ExpiresAt = NOW() + INTERVAL '30 seconds'
WHERE Id = 1;

-- API endpoint retrieves current challenge
SELECT CurrentChallenge, ExpiresAt
FROM GlobalChallenges
WHERE Id = 1;
```

**Note:** For MVP, in-memory cache (IMemoryCache) is preferred. This table is an alternative for persistent storage across restarts.

---

## Indexes

### Performance Indexes

```sql
-- Alarms
CREATE INDEX IX_Alarms_UserId ON Alarms(UserId);
CREATE INDEX IX_Alarms_IsActive ON Alarms(IsActive) WHERE IsActive = TRUE;

-- AlarmDelayActions
CREATE INDEX IX_AlarmDelayActions_AlarmId ON AlarmDelayActions(AlarmId);

-- AlarmPingActions
CREATE INDEX IX_AlarmPingActions_AlarmId ON AlarmPingActions(AlarmId);

-- AlarmCycles (critical for performance)
CREATE INDEX IX_AlarmCycles_AlarmId_Status ON AlarmCycles(AlarmId, Status) 
    WHERE Status = 'Active';
CREATE INDEX IX_AlarmCycles_AlarmPointTime ON AlarmCycles(AlarmPointTime) 
    WHERE Status = 'Active';

-- Pings (for history queries)
CREATE INDEX IX_Pings_AlarmId_ReceivedAt ON Pings(AlarmId, ReceivedAt DESC);
CREATE INDEX IX_Pings_AlarmCycleId ON Pings(AlarmCycleId);

-- ActionExecutions (critical for 1-second polling)
CREATE INDEX IX_ActionExecutions_ScheduledTime_Status ON ActionExecutions(ScheduledTime, Status) 
    WHERE Status = 'Pending';
CREATE INDEX IX_ActionExecutions_AlarmCycleId ON ActionExecutions(AlarmCycleId);
CREATE INDEX IX_ActionExecutions_PingId ON ActionExecutions(PingId);
```

### Query Patterns

**1-Second Polling Query:**
```sql
SELECT * FROM ActionExecutions
WHERE Status = 'Pending' 
  AND ScheduledTime <= NOW()
ORDER BY ScheduledTime
LIMIT 1000;
```
Uses: `IX_ActionExecutions_ScheduledTime_Status`

**Find Active Cycle:**
```sql
SELECT * FROM AlarmCycles
WHERE AlarmId = @AlarmId
  AND Status = 'Active'
LIMIT 1;
```
Uses: `IX_AlarmCycles_AlarmId_Status`

**Ping History:**
```sql
SELECT * FROM Pings
WHERE AlarmId = @AlarmId
ORDER BY ReceivedAt DESC
LIMIT 50 OFFSET 0;
```
Uses: `IX_Pings_AlarmId_ReceivedAt`

---

## Constraints

### Foreign Key Constraints

| Table | Column | References | On Delete |
|-------|--------|------------|-----------|
| Alarms | UserId | AspNetUsers(Id) | CASCADE |
| AlarmDelayActions | AlarmId | Alarms(Id) | CASCADE |
| AlarmPingActions | AlarmId | Alarms(Id) | CASCADE |
| AlarmCycles | AlarmId | Alarms(Id) | CASCADE |
| AlarmCycles | CompletedByPingId | Pings(Id) | SET NULL |
| Pings | AlarmId | Alarms(Id) | CASCADE |
| Pings | AlarmCycleId | AlarmCycles(Id) | SET NULL |
| ActionExecutions | AlarmDelayActionId | AlarmDelayActions(Id) | SET NULL |
| ActionExecutions | AlarmPingActionId | AlarmPingActions(Id) | SET NULL |
| ActionExecutions | AlarmCycleId | AlarmCycles(Id) | CASCADE |
| ActionExecutions | PingId | Pings(Id) | SET NULL |

### Check Constraints

**Alarms:**
- `CHK_DelayType`: DelayType IN ('Timeout', 'Schedule')
- `CHK_VerificationMethod`: VerificationMethod IN ('None', 'TOTP', 'RSA', 'ECDSA') OR NULL
- `CHK_DelayConfig`: Timeout type has TimeoutDuration, Schedule type has CronExpression

**AlarmCycles:**
- `CHK_Status`: Status IN ('Active', 'Completed', 'Cancelled')

**ActionExecutions:**
- `CHK_ActionType`: ActionType IN ('Email', 'SMS', 'RestApi')
- `CHK_Status`: Status IN ('Pending', 'Executing', 'Success', 'Failed', 'Cancelled')
- `CHK_ActionSource`: Either AlarmDelayActionId OR AlarmPingActionId is set, not both
- `CHK_PingActionHasPing`: If AlarmPingActionId is set, PingId must also be set

---

## Sample Data

### Example Alarm (Timeout-based with Escalation)

```sql
-- User
INSERT INTO AspNetUsers (Id, UserName, Email, EmailConfirmed)
VALUES ('00000000-0000-0000-0000-000000000001', 'john@example.com', 'john@example.com', TRUE);

-- Alarm
INSERT INTO Alarms (Id, UserId, Name, DelayType, TimeoutDuration, AllowTimerReset, VerificationMethod, VerificationConfig)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    '00000000-0000-0000-0000-000000000001',
    'Daily Wellness Check',
    'Timeout',
    '24:00:00',
    TRUE,
    'TOTP',
    '{"secret": "JBSWY3DPEHPK3PXP", "period": 30, "digits": 6}'
);

-- Delay Actions (Timeline)
INSERT INTO AlarmDelayActions (Id, AlarmId, OffsetMinutes, ActionType, ActionConfig)
VALUES
    -- Reminder 2 hours before
    ('22222222-2222-2222-2222-222222222221', '11111111-1111-1111-1111-111111111111', -120, 'Email',
     '{"to": "john@example.com", "subject": "Reminder: Check in soon!", "body": "Don''t forget to ping within 2 hours"}'),
    
    -- Alarm point
    ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 0, 'Email',
     '{"to": "emergency@example.com", "subject": "ALERT: No check-in", "body": "John has not checked in"}'),
    
    -- Escalation 1 hour after
    ('22222222-2222-2222-2222-222222222223', '11111111-1111-1111-1111-111111111111', 60, 'SMS',
     '{"to": "+15559876543", "message": "URGENT: John has not checked in for 25 hours"}'),
    
    -- Final escalation 24 hours after
    ('22222222-2222-2222-2222-222222222224', '11111111-1111-1111-1111-111111111111', 1440, 'RestApi',
     '{"url": "https://api.example.com/emergency", "method": "POST", "payload": "{\"user\": \"john\", \"status\": \"missing\"}"}');

-- On-Ping Action
INSERT INTO AlarmPingActions (Id, AlarmId, ActionType, ActionConfig)
VALUES
    ('33333333-3333-3333-3333-333333333331', '11111111-1111-1111-1111-111111111111', 'Email',
     '{"to": "log@example.com", "subject": "Ping received", "body": "John checked in. Payload: {ping_payload}"}');

-- Active Cycle (created after user activated alarm)
INSERT INTO AlarmCycles (Id, AlarmId, AlarmPointTime, StartedAt, Status)
VALUES (
    '44444444-4444-4444-4444-444444444441',
    '11111111-1111-1111-1111-111111111111',
    '2026-02-09 09:00:00',  -- Alarm point = now + 24 hours
    '2026-02-08 09:00:00',  -- Started now
    'Active'
);

-- Action Executions (scheduled by CycleCreationService)
INSERT INTO ActionExecutions (Id, AlarmDelayActionId, AlarmCycleId, ActionType, ScheduledTime, Status)
VALUES
    ('55555555-5555-5555-5555-555555555551', '22222222-2222-2222-2222-222222222221', '44444444-4444-4444-4444-444444444441', 'Email', '2026-02-09 07:00:00', 'Pending'),
    ('55555555-5555-5555-5555-555555555552', '22222222-2222-2222-2222-222222222222', '44444444-4444-4444-4444-444444444441', 'Email', '2026-02-09 09:00:00', 'Pending'),
    ('55555555-5555-5555-5555-555555555553', '22222222-2222-2222-2222-222222222223', '44444444-4444-4444-4444-444444444441', 'SMS', '2026-02-09 10:00:00', 'Pending'),
    ('55555555-5555-5555-5555-555555555554', '22222222-2222-2222-2222-222222222224', '44444444-4444-4444-4444-444444444441', 'RestApi', '2026-02-10 09:00:00', 'Pending');
```

### Example Ping (Successful)

```sql
-- User pings at 8:45am (before alarm point)
INSERT INTO Pings (Id, AlarmId, AlarmCycleId, ReceivedAt, Payload, VerificationProof, VerificationResult, ResetTimerRequested, TimerWasReset)
VALUES (
    '66666666-6666-6666-6666-666666666661',
    '11111111-1111-1111-1111-111111111111',
    '44444444-4444-4444-4444-444444444441',
    '2026-02-09 08:45:00',
    'All is well. Weather: sunny, temp: 72F',
    '123456',  -- TOTP code
    TRUE,      -- Verification passed
    TRUE,      -- Requested reset
    TRUE       -- Timer was reset
);

-- On-ping action execution (triggered immediately)
INSERT INTO ActionExecutions (Id, AlarmPingActionId, PingId, ActionType, ScheduledTime, ExecutedTime, Status)
VALUES (
    '77777777-7777-7777-7777-777777777771',
    '33333333-3333-3333-3333-333333333331',
    '66666666-6666-6666-6666-666666666661',
    'Email',
    '2026-02-09 08:45:00',
    '2026-02-09 08:45:01.234',
    'Success'
);

-- Old cycle completed
UPDATE AlarmCycles
SET Status = 'Completed',
    CompletedAt = '2026-02-09 08:45:00',
    CompletedByPingId = '66666666-6666-6666-6666-666666666661'
WHERE Id = '44444444-4444-4444-4444-444444444441';

-- Pending actions cancelled
UPDATE ActionExecutions
SET Status = 'Cancelled'
WHERE AlarmCycleId = '44444444-4444-4444-4444-444444444441'
  AND Status = 'Pending';

-- New cycle created (alarm point = ping time + 24 hours)
INSERT INTO AlarmCycles (Id, AlarmId, AlarmPointTime, StartedAt, Status)
VALUES (
    '44444444-4444-4444-4444-444444444442',
    '11111111-1111-1111-1111-111111111111',
    '2026-02-10 08:45:00',  -- New alarm point
    '2026-02-09 08:45:00',
    'Active'
);

-- New action executions scheduled
-- (Same as before, but with new times and new cycle ID)
```

---

## Database Maintenance

### Backup Strategy

```bash
# Daily backup
pg_dump -U postgres -d wdalarm -F c -b -v -f /backups/wdalarm_$(date +%Y%m%d).backup

# Restore
pg_restore -U postgres -d wdalarm -v /backups/wdalarm_20260208.backup
```

### Vacuum and Analyze

```sql
-- Regular maintenance (run weekly)
VACUUM ANALYZE Alarms;
VACUUM ANALYZE AlarmCycles;
VACUUM ANALYZE Pings;
VACUUM ANALYZE ActionExecutions;

-- Full vacuum (run monthly during low traffic)
VACUUM FULL;
```

### History Cleanup

```sql
-- Delete old pings (run daily via HistoryCleanupService)
DELETE FROM Pings
WHERE ReceivedAt < NOW() - INTERVAL '90 days';

-- Delete old action executions
DELETE FROM ActionExecutions
WHERE CreatedAt < NOW() - INTERVAL '90 days';
```

### Monitoring Queries

```sql
-- Count active alarms
SELECT COUNT(*) FROM Alarms WHERE IsActive = TRUE;

-- Count active cycles
SELECT COUNT(*) FROM AlarmCycles WHERE Status = 'Active';

-- Count pending actions
SELECT COUNT(*) FROM ActionExecutions WHERE Status = 'Pending';

-- Actions scheduled in next hour
SELECT COUNT(*) FROM ActionExecutions
WHERE Status = 'Pending'
  AND ScheduledTime BETWEEN NOW() AND NOW() + INTERVAL '1 hour';

-- Failed actions in last 24 hours
SELECT COUNT(*) FROM ActionExecutions
WHERE Status = 'Failed'
  AND ExecutedTime > NOW() - INTERVAL '24 hours';

-- Table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

---

## Migration Scripts

### Initial Migration

```bash
# Create initial migration
dotnet ef migrations add InitialCreate -p src/WdAlarm.Infrastructure -s src/WdAlarm.Api

# Apply to database
dotnet ef database update -p src/WdAlarm.Infrastructure -s src/WdAlarm.Api
```

### Seed Data

```csharp
// Infrastructure/Data/ApplicationDbContextSeed.cs
public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed GlobalChallenges
        if (!await context.GlobalChallenges.AnyAsync())
        {
            context.GlobalChallenges.Add(new GlobalChallenge
            {
                Id = 1,
                CurrentChallenge = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(30)
            });
            await context.SaveChangesAsync();
        }
    }
}
```

---

## Performance Considerations

### Expected Load

| Metric | MVP | Production |
|--------|-----|------------|
| Active alarms | 100 | 100,000 |
| Pings/second | 1 | 1,000 |
| Pending actions | 500 | 500,000 |
| Database size | 1 GB | 100 GB |

### Optimization Strategies

1. **Indexes**: All critical queries have covering indexes
2. **Partitioning**: Consider partitioning Pings and ActionExecutions by date (future)
3. **Archival**: Move old data to archive tables after retention period
4. **Connection pooling**: EF Core default pool size (100)
5. **Read replicas**: For history queries (future)

### Query Performance Targets

- 1-second polling query: <10ms
- Find active cycle: <5ms
- Insert ping: <50ms
- Create new cycle: <100ms
