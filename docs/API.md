# WatchDog Alarm - REST API Documentation

Complete REST API documentation with request/response examples.

## Table of Contents
- [Base URL](#base-url)
- [Authentication](#authentication)
- [Error Handling](#error-handling)
- [Authentication Endpoints](#authentication-endpoints)
- [Alarm Management Endpoints](#alarm-management-endpoints)
- [Ping Endpoints](#ping-endpoints)
- [Challenge Endpoint](#challenge-endpoint)
- [History Endpoints](#history-endpoints)
- [Rate Limiting](#rate-limiting)

---

## Base URL

**Development:** `https://localhost:5001`  
**Production:** `https://api.wdalarm.com` (example)

All endpoints are prefixed with `/api`.

---

## Authentication

Most endpoints require JWT authentication via the `Authorization` header:

```
Authorization: Bearer {access_token}
```

### Obtaining Access Token

1. Register or login to get tokens
2. Use access token for subsequent requests
3. Refresh when access token expires (15 minutes)

### Token Types

| Token | Lifetime | Purpose |
|-------|----------|---------|
| Access Token | 15 minutes | API authentication |
| Refresh Token | 7 days | Renew access token |

---

## Error Handling

### Standard Error Response

```json
{
  "error": "ErrorCode",
  "message": "Human-readable error message",
  "details": {
    "field": "fieldName",
    "code": "VALIDATION_CODE"
  },
  "traceId": "00-abc123-def456-00"
}
```

### HTTP Status Codes

| Code | Meaning | Usage |
|------|---------|-------|
| 200 | OK | Successful GET, PUT, DELETE |
| 201 | Created | Successful POST creating resource |
| 202 | Accepted | Async operation started |
| 400 | Bad Request | Invalid input, validation error |
| 401 | Unauthorized | Missing/invalid token, verification failed |
| 403 | Forbidden | User doesn't own resource |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Duplicate resource |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service temporarily down |

### Common Error Codes

| Error Code | Description |
|------------|-------------|
| `ValidationError` | Input validation failed |
| `AuthenticationError` | Login failed, invalid credentials |
| `AuthorizationError` | User doesn't have permission |
| `NotFoundError` | Resource not found |
| `ConflictError` | Resource already exists |
| `VerificationFailedError` | Ping verification failed |
| `InternalError` | Unexpected server error |

---

## Authentication Endpoints

### Register User

Create a new user account.

**Endpoint:** `POST /api/auth/register`  
**Authentication:** None

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd123",
  "confirmPassword": "SecureP@ssw0rd123"
}
```

**Request Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | Yes | Valid email address |
| password | string | Yes | Min 8 chars, 1 uppercase, 1 lowercase, 1 number, 1 special |
| confirmPassword | string | Yes | Must match password |

**Success Response:** `201 Created`
```json
{
  "userId": "a1b2c3d4-e5f6-4789-0abc-def123456789",
  "email": "user@example.com",
  "message": "Registration successful"
}
```

**Error Responses:**

`400 Bad Request` - Validation error
```json
{
  "error": "ValidationError",
  "message": "Password does not meet requirements",
  "details": {
    "field": "password",
    "code": "WEAK_PASSWORD"
  }
}
```

`409 Conflict` - Email already exists
```json
{
  "error": "ConflictError",
  "message": "User with this email already exists"
}
```

---

### Login

Authenticate and receive tokens.

**Endpoint:** `POST /api/auth/login`  
**Authentication:** None

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd123"
}
```

**Success Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "def456-ghi789-jkl012-mno345-pqr678",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| accessToken | string | JWT token for API authentication |
| refreshToken | string | Token to renew access token |
| expiresIn | int | Access token lifetime in seconds (900 = 15 min) |
| tokenType | string | Always "Bearer" |

**Error Response:** `401 Unauthorized`
```json
{
  "error": "AuthenticationError",
  "message": "Invalid email or password"
}
```

---

### Refresh Token

Get new access token using refresh token.

**Endpoint:** `POST /api/auth/refresh`  
**Authentication:** None

**Request Body:**
```json
{
  "refreshToken": "def456-ghi789-jkl012-mno345-pqr678"
}
```

**Success Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "new-refresh-token-here",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Error Response:** `401 Unauthorized`
```json
{
  "error": "AuthenticationError",
  "message": "Invalid or expired refresh token"
}
```

---

## Alarm Management Endpoints

### Create Alarm

Create a new alarm with timeline and on-ping actions.

**Endpoint:** `POST /api/alarms`  
**Authentication:** Required

**Request Body (Timeout-based):**
```json
{
  "name": "Daily Wellness Check",
  "description": "Check in every 24 hours or emergency contacts will be notified",
  "delayType": "Timeout",
  "timeoutDuration": "24:00:00",
  "allowTimerReset": true,
  "perPingResetOverride": false,
  "verificationMethod": "TOTP",
  "verificationConfig": {
    "secret": "JBSWY3DPEHPK3PXP",
    "period": 30,
    "digits": 6
  },
  "delayActions": [
    {
      "offsetMinutes": -120,
      "executionOrder": 0,
      "actionType": "Email",
      "actionConfig": {
        "smtpHost": "smtp.gmail.com",
        "smtpPort": 587,
        "useSsl": true,
        "username": "alerts@example.com",
        "password": "app_specific_password",
        "from": "alerts@example.com",
        "to": "user@example.com",
        "subject": "Reminder: Check in soon!",
        "body": "Don't forget to ping the system within 2 hours."
      }
    },
    {
      "offsetMinutes": 0,
      "executionOrder": 0,
      "actionType": "Email",
      "actionConfig": {
        "smtpHost": "smtp.gmail.com",
        "smtpPort": 587,
        "useSsl": true,
        "username": "alerts@example.com",
        "password": "app_specific_password",
        "from": "alerts@example.com",
        "to": "emergency@example.com",
        "subject": "ALERT: No check-in received",
        "body": "User has not checked in. Please investigate."
      }
    },
    {
      "offsetMinutes": 1440,
      "executionOrder": 0,
      "actionType": "RestApi",
      "actionConfig": {
        "url": "https://api.example.com/emergency",
        "method": "POST",
        "headers": {
          "Authorization": "Bearer secret_api_key",
          "Content-Type": "application/json"
        },
        "payload": "{\"user\": \"john\", \"status\": \"missing\", \"duration_hours\": 48}"
      }
    }
  ],
  "pingActions": [
    {
      "executionOrder": 0,
      "actionType": "Email",
      "actionConfig": {
        "smtpHost": "smtp.gmail.com",
        "smtpPort": 587,
        "useSsl": true,
        "username": "alerts@example.com",
        "password": "app_specific_password",
        "from": "alerts@example.com",
        "to": "log@example.com",
        "subject": "Ping received from user",
        "body": "User checked in successfully. Payload: {ping_payload}"
      }
    }
  ]
}
```

**Request Body (Schedule-based):**
```json
{
  "name": "Daily 9am Check-in",
  "description": "Must check in before 9am every day",
  "delayType": "Schedule",
  "cronExpression": "0 9 * * *",
  "allowTimerReset": true,
  "perPingResetOverride": false,
  "verificationMethod": "RSA",
  "verificationConfig": {
    "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----",
    "algorithm": "RS256"
  },
  "delayActions": [
    {
      "offsetMinutes": -30,
      "executionOrder": 0,
      "actionType": "SMS",
      "actionConfig": {
        "provider": "twilio",
        "accountSid": "encrypted_sid",
        "authToken": "encrypted_token",
        "from": "+15551234567",
        "to": "+15559876543",
        "message": "Don't forget to check in before 9am!"
      }
    },
    {
      "offsetMinutes": 0,
      "executionOrder": 0,
      "actionType": "Email",
      "actionConfig": {
        "to": "backup@example.com",
        "subject": "Missed 9am check-in",
        "body": "User did not check in by 9am."
      }
    }
  ],
  "pingActions": []
}
```

**Request Body (No Authentication):**
```json
{
  "name": "Public Status Monitor",
  "description": "Public endpoint, no authentication required",
  "delayType": "Timeout",
  "timeoutDuration": "00:05:00",
  "allowTimerReset": true,
  "verificationMethod": "None",
  "verificationConfig": null,
  "delayActions": [
    {
      "offsetMinutes": 0,
      "executionOrder": 0,
      "actionType": "RestApi",
      "actionConfig": {
        "url": "https://status.example.com/webhook/alert",
        "method": "POST",
        "payload": "{\"service\": \"api\", \"status\": \"down\"}"
      }
    }
  ],
  "pingActions": []
}
```

**Request Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| name | string | Yes | Alarm display name (max 200 chars) |
| description | string | No | Alarm description |
| delayType | string | Yes | "Timeout" or "Schedule" |
| timeoutDuration | string | Conditional | Required if delayType="Timeout". Format: "HH:MM:SS" (min "00:00:15") |
| cronExpression | string | Conditional | Required if delayType="Schedule". Standard cron format |
| allowTimerReset | boolean | Yes | Can pings reset the timer? |
| perPingResetOverride | boolean | Yes | Can individual pings override reset behavior? |
| verificationMethod | string | Yes | "None", "TOTP", "RSA", or "ECDSA" |
| verificationConfig | object | Conditional | Required if verificationMethod != "None" |
| delayActions | array | No | Timeline-based actions (can be empty) |
| pingActions | array | No | On-ping actions (can be empty) |

**DelayAction Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| offsetMinutes | int | Yes | Minutes from alarm point (negative = before, positive = after) |
| executionOrder | int | Yes | Order for actions at same offset (0 = first) |
| actionType | string | Yes | "Email", "SMS", or "RestApi" |
| actionConfig | object | Yes | Action-specific configuration (see below) |

**PingAction Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| executionOrder | int | Yes | Order for multiple on-ping actions |
| actionType | string | Yes | "Email", "SMS", or "RestApi" |
| actionConfig | object | Yes | Action-specific configuration |

**ActionConfig Schemas:**

**Email:**
```json
{
  "smtpHost": "smtp.gmail.com",
  "smtpPort": 587,
  "useSsl": true,
  "username": "user@example.com",
  "password": "password",
  "from": "alerts@example.com",
  "to": "recipient@example.com",
  "cc": "cc@example.com",
  "bcc": "bcc@example.com",
  "subject": "Subject line",
  "body": "Email body. Use {ping_payload} to include ping data."
}
```

**SMS:**
```json
{
  "provider": "twilio",
  "accountSid": "AC...",
  "authToken": "...",
  "from": "+15551234567",
  "to": "+15559876543",
  "message": "SMS text. Use {ping_payload} for ping data. Max 160 chars."
}
```

**RestApi:**
```json
{
  "url": "https://api.example.com/webhook",
  "method": "POST",
  "headers": {
    "Authorization": "Bearer token",
    "Content-Type": "application/json"
  },
  "payload": "{\"key\": \"value\", \"ping\": \"{ping_payload}\"}"
}
```

**Success Response:** `201 Created`
```json
{
  "id": "alarm-guid-here",
  "name": "Daily Wellness Check",
  "description": "Check in every 24 hours...",
  "delayType": "Timeout",
  "timeoutDuration": "24:00:00",
  "allowTimerReset": true,
  "perPingResetOverride": false,
  "verificationMethod": "TOTP",
  "isActive": true,
  "delayActionsCount": 3,
  "pingActionsCount": 1,
  "createdAt": "2026-02-08T10:00:00Z",
  "updatedAt": "2026-02-08T10:00:00Z"
}
```

**Error Responses:**

`400 Bad Request` - Validation error
```json
{
  "error": "ValidationError",
  "message": "Timeout duration must be at least 15 seconds",
  "details": {
    "field": "timeoutDuration",
    "code": "DURATION_TOO_SHORT"
  }
}
```

---

### List Alarms

Get all alarms for authenticated user.

**Endpoint:** `GET /api/alarms`  
**Authentication:** Required

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number (1-indexed) |
| pageSize | int | No | 50 | Items per page (max 100) |
| isActive | boolean | No | null | Filter by active status |

**Example Request:**
```
GET /api/alarms?page=1&pageSize=20&isActive=true
```

**Success Response:** `200 OK`
```json
{
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8,
  "alarms": [
    {
      "id": "alarm-guid-1",
      "name": "Daily Wellness Check",
      "delayType": "Timeout",
      "timeoutDuration": "24:00:00",
      "verificationMethod": "TOTP",
      "isActive": true,
      "currentCycle": {
        "id": "cycle-guid-1",
        "alarmPointTime": "2026-02-09T10:00:00Z",
        "status": "Active"
      },
      "createdAt": "2026-02-01T10:00:00Z"
    },
    {
      "id": "alarm-guid-2",
      "name": "Weekly Report",
      "delayType": "Schedule",
      "cronExpression": "0 9 * * MON",
      "verificationMethod": "None",
      "isActive": true,
      "currentCycle": null,
      "createdAt": "2026-01-15T14:30:00Z"
    }
  ]
}
```

---

### Get Alarm by ID

Get detailed alarm information.

**Endpoint:** `GET /api/alarms/{id}`  
**Authentication:** Required

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| id | UUID | Alarm ID |

**Success Response:** `200 OK`
```json
{
  "id": "alarm-guid",
  "userId": "user-guid",
  "name": "Daily Wellness Check",
  "description": "Check in every 24 hours...",
  "delayType": "Timeout",
  "timeoutDuration": "24:00:00",
  "cronExpression": null,
  "allowTimerReset": true,
  "perPingResetOverride": false,
  "verificationMethod": "TOTP",
  "verificationConfig": {
    "secret": "JBSWY3DPEHPK3PXP",
    "period": 30,
    "digits": 6
  },
  "isActive": true,
  "delayActions": [
    {
      "id": "action-guid-1",
      "offsetMinutes": -120,
      "executionOrder": 0,
      "actionType": "Email",
      "actionConfig": {
        "to": "user@example.com",
        "subject": "Reminder: Check in soon!",
        "body": "..."
      }
    }
  ],
  "pingActions": [
    {
      "id": "action-guid-2",
      "executionOrder": 0,
      "actionType": "Email",
      "actionConfig": {
        "to": "log@example.com",
        "subject": "Ping received"
      }
    }
  ],
  "createdAt": "2026-02-01T10:00:00Z",
  "updatedAt": "2026-02-08T10:00:00Z"
}
```

**Error Response:** `404 Not Found`
```json
{
  "error": "NotFoundError",
  "message": "Alarm not found"
}
```

---

### Update Alarm

Update alarm configuration.

**Endpoint:** `PUT /api/alarms/{id}`  
**Authentication:** Required

**Request Body:** Same structure as Create Alarm

**Success Response:** `200 OK`
```json
{
  "id": "alarm-guid",
  "name": "Updated Alarm Name",
  ...
  "updatedAt": "2026-02-08T11:00:00Z"
}
```

**Note:** Updating alarm configuration will:
- Complete current active cycle
- Create new cycle with updated settings
- Cancel pending actions from old cycle

---

### Delete Alarm

Delete an alarm (soft delete by default).

**Endpoint:** `DELETE /api/alarms/{id}`  
**Authentication:** Required

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| hard | boolean | No | false | Permanently delete (true) or soft delete (false) |

**Success Response:** `200 OK`
```json
{
  "message": "Alarm deleted successfully",
  "id": "alarm-guid"
}
```

---

### Activate Alarm

Activate an alarm and create first cycle.

**Endpoint:** `POST /api/alarms/{id}/activate`  
**Authentication:** Required

**Success Response:** `200 OK`
```json
{
  "message": "Alarm activated",
  "alarmId": "alarm-guid",
  "cycleId": "cycle-guid",
  "alarmPointTime": "2026-02-09T10:00:00Z",
  "actionsScheduled": 3
}
```

---

### Get Alarm Status

Get current alarm status including active cycle and pending actions.

**Endpoint:** `GET /api/alarms/{id}/status`  
**Authentication:** Required

**Success Response:** `200 OK`
```json
{
  "alarmId": "alarm-guid",
  "name": "Daily Wellness Check",
  "isActive": true,
  "currentCycle": {
    "id": "cycle-guid",
    "alarmPointTime": "2026-02-09T10:00:00Z",
    "startedAt": "2026-02-08T10:00:00Z",
    "status": "Active",
    "pendingActions": [
      {
        "id": "execution-guid-1",
        "scheduledTime": "2026-02-09T08:00:00Z",
        "offsetMinutes": -120,
        "actionType": "Email",
        "status": "Pending"
      },
      {
        "id": "execution-guid-2",
        "scheduledTime": "2026-02-09T10:00:00Z",
        "offsetMinutes": 0,
        "actionType": "Email",
        "status": "Pending"
      }
    ],
    "executedActions": [],
    "timeUntilAlarmPoint": "23:55:30"
  },
  "lastPing": {
    "id": "ping-guid",
    "receivedAt": "2026-02-08T10:04:30Z",
    "verificationResult": true,
    "timerWasReset": true
  }
}
```

**Response with Completed Cycle:**
```json
{
  "alarmId": "alarm-guid",
  "name": "Daily Wellness Check",
  "isActive": true,
  "currentCycle": {
    "id": "cycle-guid",
    "alarmPointTime": "2026-02-09T10:00:00Z",
    "startedAt": "2026-02-08T10:00:00Z",
    "status": "Completed",
    "completedAt": "2026-02-09T08:45:00Z",
    "completedByPingId": "ping-guid",
    "pendingActions": [],
    "executedActions": [
      {
        "id": "execution-guid",
        "scheduledTime": "2026-02-09T08:45:00Z",
        "executedTime": "2026-02-09T08:45:01.234Z",
        "actionType": "Email",
        "status": "Success",
        "source": "PingAction",
        "pingId": "ping-guid"
      }
    ]
  },
  "lastPing": {
    "id": "ping-guid",
    "receivedAt": "2026-02-09T08:45:00Z",
    "verificationResult": true,
    "timerWasReset": true
  }
}
```

---

## Ping Endpoints

### Submit Ping

Submit a ping to an alarm.

**Endpoint:** `POST /api/ping/{alarmId}`  
**Authentication:** Optional (depends on alarm's verification method)

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| alarmId | UUID | Alarm ID |

**Request Body:**
```json
{
  "verificationProof": "123456",
  "payload": "All is well. Weather: sunny, temperature: 72F",
  "resetTimer": true
}
```

**Request Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| verificationProof | string | Conditional | Required if verification method != "None". Format depends on method (see below) |
| payload | string | No | User-provided data (max 4096 bytes) |
| resetTimer | boolean | No | Override alarm's reset behavior (if PerPingResetOverride=true) |

**Verification Proof Formats:**

| Method | Format | Example |
|--------|--------|---------|
| None | Not required | null |
| TOTP | 6-digit code | "123456" |
| RSA | "BASE64_SIGNATURE\|TIMESTAMP" | "SGVsbG8gV29ybGQ=\|2026-02-08T10:30:15Z" |
| ECDSA | "BASE64_SIGNATURE\|TIMESTAMP" | "SGVsbG8gV29ybGQ=\|2026-02-08T10:30:15Z" |

**Success Response:** `200 OK`
```json
{
  "pingId": "ping-guid",
  "receivedAt": "2026-02-08T10:30:00Z",
  "verificationResult": true,
  "timerWasReset": true,
  "newAlarmPointTime": "2026-02-09T10:30:00Z",
  "onPingActionsTriggered": 1,
  "message": "Ping received successfully. Timer reset."
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| pingId | UUID | Unique ping identifier |
| receivedAt | timestamp | When server received ping |
| verificationResult | boolean | Did verification pass? |
| timerWasReset | boolean | Was alarm timer reset? |
| newAlarmPointTime | timestamp | New alarm point (null if timer not reset) |
| onPingActionsTriggered | int | Number of on-ping actions executed |
| message | string | Human-readable status |

**Error Response:** `401 Unauthorized` - Verification failed
```json
{
  "error": "VerificationFailedError",
  "message": "Invalid TOTP code",
  "pingId": "ping-guid",
  "verificationResult": false
}
```

**Performance:**

The ping endpoint uses an **in-memory command bus** for fast cycle creation:

| Operation | Typical Time | Cumulative |
|-----------|--------------|------------|
| Load alarm from database | 5-10ms | 10ms |
| Verify TOTP/signature | 5-10ms | 20ms |
| Save ping to database | 10-15ms | 35ms |
| Send command to worker | 1-2ms | 37ms |
| Worker creates cycle | 30-40ms | 77ms |
| Receive response | 1-2ms | 79ms |
| **Total Response Time** | | **~50-85ms** |

**Target**: <100ms ✅

**Key Features:**
- **Request-reply pattern**: API waits for worker confirmation
- **Timeout**: 500ms (if worker doesn't respond, returns 500 error)
- **On-ping actions**: Execute asynchronously (don't block response)

**Note:** Even failed pings are recorded in history for audit purposes.

**Error Response:** `404 Not Found`
```json
{
  "error": "NotFoundError",
  "message": "Alarm not found or inactive"
}
```

**Error Response:** `500 Internal Server Error` - Cycle creation timeout
```json
{
  "error": "InternalError",
  "message": "Failed to create alarm cycle: command timeout",
  "details": "Worker did not respond within 500ms. Ping was recorded but cycle not created."
}
```

**Note:** If cycle creation fails, ping is still recorded in database. User can retry immediately.

---

### Ping with RSA Signature Example

**Step 1: Get current challenge**
```bash
curl https://api.wdalarm.com/api/challenge
```
Response:
```json
{
  "challenge": "a1b2c3d4-e5f6-4789-0abc-def123456789",
  "expiresAt": "2026-02-08T10:30:30Z",
  "algorithm": "SHA256"
}
```

**Step 2: Sign challenge + timestamp**
```python
import base64
from datetime import datetime
from Crypto.PublicKey import RSA
from Crypto.Signature import pkcs1_15
from Crypto.Hash import SHA256

# Load private key
private_key = RSA.import_key(open('private_key.pem').read())

# Construct message
challenge = "a1b2c3d4-e5f6-4789-0abc-def123456789"
timestamp = datetime.utcnow().isoformat() + "Z"
message = challenge + timestamp

# Sign message
hash_obj = SHA256.new(message.encode())
signature = pkcs1_15.new(private_key).sign(hash_obj)
signature_b64 = base64.b64encode(signature).decode()

# Construct proof
proof = f"{signature_b64}|{timestamp}"
print(proof)
```

**Step 3: Submit ping**
```bash
curl -X POST https://api.wdalarm.com/api/ping/alarm-guid \
  -H "Content-Type: application/json" \
  -d '{
    "verificationProof": "SGVsbG8gV29ybGQ=|2026-02-08T10:30:15Z",
    "payload": "All systems operational"
  }'
```

---

## Challenge Endpoint

### Get Current Challenge

Get current global challenge for signature verification.

**Endpoint:** `GET /api/challenge`  
**Authentication:** None

**Success Response:** `200 OK`
```json
{
  "challenge": "a1b2c3d4-e5f6-4789-0abc-def123456789",
  "generatedAt": "2026-02-08T10:30:00Z",
  "expiresAt": "2026-02-08T10:30:30Z",
  "algorithm": "SHA256"
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| challenge | string | Current challenge (UUID) |
| generatedAt | timestamp | When challenge was generated |
| expiresAt | timestamp | When challenge expires (30 seconds) |
| algorithm | string | Hash algorithm for signing (SHA256) |

**Usage:**
- Challenge rotates every 30 seconds
- Clients should retrieve challenge immediately before signing
- Server validates timestamp within ±5 minutes to account for clock skew

---

## History Endpoints

### Get Ping History

Get ping history for an alarm.

**Endpoint:** `GET /api/history/pings/{alarmId}`  
**Authentication:** Required

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| alarmId | UUID | Alarm ID |

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 50 | Items per page (max 100) |
| from | timestamp | No | null | Start date (ISO 8601) |
| to | timestamp | No | null | End date (ISO 8601) |

**Example Request:**
```
GET /api/history/pings/alarm-guid?page=1&pageSize=20&from=2026-02-01T00:00:00Z&to=2026-02-08T23:59:59Z
```

**Success Response:** `200 OK`
```json
{
  "alarmId": "alarm-guid",
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8,
  "from": "2026-02-01T00:00:00Z",
  "to": "2026-02-08T23:59:59Z",
  "pings": [
    {
      "id": "ping-guid-1",
      "receivedAt": "2026-02-08T10:30:00Z",
      "payload": "All is well. Weather: sunny",
      "verificationResult": true,
      "verificationError": null,
      "resetTimerRequested": true,
      "timerWasReset": true,
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0..."
    },
    {
      "id": "ping-guid-2",
      "receivedAt": "2026-02-07T10:15:00Z",
      "payload": "Status update",
      "verificationResult": false,
      "verificationError": "Invalid TOTP code",
      "resetTimerRequested": true,
      "timerWasReset": false,
      "ipAddress": "192.168.1.100",
      "userAgent": "curl/7.68.0"
    }
  ]
}
```

---

### Get Action Execution History

Get action execution history for an alarm.

**Endpoint:** `GET /api/history/actions/{alarmId}`  
**Authentication:** Required

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| alarmId | UUID | Alarm ID |

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 50 | Items per page (max 100) |
| from | timestamp | No | null | Start date |
| to | timestamp | No | null | End date |
| status | string | No | null | Filter by status: Pending, Success, Failed, Cancelled |
| actionType | string | No | null | Filter by type: Email, SMS, RestApi |

**Example Request:**
```
GET /api/history/actions/alarm-guid?page=1&pageSize=20&status=Success&actionType=Email
```

**Success Response:** `200 OK`
```json
{
  "alarmId": "alarm-guid",
  "totalCount": 45,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3,
  "actions": [
    {
      "id": "execution-guid-1",
      "actionType": "Email",
      "source": "DelayAction",
      "delayActionId": "delay-action-guid",
      "pingActionId": null,
      "cycleId": "cycle-guid",
      "pingId": null,
      "offsetMinutes": -120,
      "scheduledTime": "2026-02-08T08:00:00Z",
      "executedTime": "2026-02-08T08:00:01.234Z",
      "status": "Success",
      "retryCount": 0,
      "errorMessage": null
    },
    {
      "id": "execution-guid-2",
      "actionType": "Email",
      "source": "PingAction",
      "delayActionId": null,
      "pingActionId": "ping-action-guid",
      "cycleId": "cycle-guid",
      "pingId": "ping-guid",
      "offsetMinutes": null,
      "scheduledTime": "2026-02-08T10:30:00Z",
      "executedTime": "2026-02-08T10:30:01.567Z",
      "status": "Success",
      "retryCount": 0,
      "errorMessage": null
    },
    {
      "id": "execution-guid-3",
      "actionType": "RestApi",
      "source": "DelayAction",
      "delayActionId": "delay-action-guid-2",
      "pingActionId": null,
      "cycleId": "cycle-guid-old",
      "pingId": null,
      "offsetMinutes": 0,
      "scheduledTime": "2026-02-07T10:00:00Z",
      "executedTime": "2026-02-07T10:00:03.891Z",
      "status": "Failed",
      "retryCount": 3,
      "errorMessage": "Connection timeout after 3 retries"
    }
  ]
}
```

---

## Rate Limiting

**Current Status:** Not implemented in MVP

**Future Implementation:**

| Endpoint | Rate Limit | Window |
|----------|------------|--------|
| POST /api/auth/register | 5 requests | 1 hour |
| POST /api/auth/login | 10 requests | 15 minutes |
| POST /api/ping/{id} | 100 requests | 1 hour (per alarm) |
| All other endpoints | 1000 requests | 1 hour (per user) |

**Rate Limit Response:** `429 Too Many Requests`
```json
{
  "error": "RateLimitExceeded",
  "message": "Too many requests. Please try again later.",
  "retryAfter": 300
}
```

Headers:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1644318000
Retry-After: 300
```

---

## Webhook Events (Future)

Not implemented in MVP. Future feature for real-time notifications:

```json
POST {user_webhook_url}
{
  "event": "alarm.triggered",
  "timestamp": "2026-02-08T10:00:00Z",
  "data": {
    "alarmId": "alarm-guid",
    "alarmName": "Daily Wellness Check",
    "cycleId": "cycle-guid",
    "alarmPointTime": "2026-02-08T10:00:00Z"
  }
}
```

---

## Complete cURL Examples

### Full Workflow Example

```bash
# 1. Register
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecureP@ss123",
    "confirmPassword": "SecureP@ss123"
  }'

# 2. Login
TOKEN=$(curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecureP@ss123"
  }' | jq -r '.accessToken')

# 3. Create Alarm
ALARM_ID=$(curl -X POST https://localhost:5001/api/alarms \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Alarm",
    "delayType": "Timeout",
    "timeoutDuration": "24:00:00",
    "allowTimerReset": true,
    "verificationMethod": "None",
    "delayActions": [
      {
        "offsetMinutes": 0,
        "executionOrder": 0,
        "actionType": "Email",
        "actionConfig": {
          "to": "alert@example.com",
          "subject": "Alert!",
          "body": "No ping received"
        }
      }
    ],
    "pingActions": []
  }' | jq -r '.id')

# 4. Activate Alarm
curl -X POST https://localhost:5001/api/alarms/$ALARM_ID/activate \
  -H "Authorization: Bearer $TOKEN"

# 5. Submit Ping
curl -X POST https://localhost:5001/api/ping/$ALARM_ID \
  -H "Content-Type: application/json" \
  -d '{
    "payload": "All is well",
    "resetTimer": true
  }'

# 6. Check Status
curl https://localhost:5001/api/alarms/$ALARM_ID/status \
  -H "Authorization: Bearer $TOKEN"

# 7. View Ping History
curl "https://localhost:5001/api/history/pings/$ALARM_ID?page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Testing with Postman

Import the provided Postman collection (future):
- `docs/WdAlarm.postman_collection.json`
- Includes all endpoints with example requests
- Environment variables for base URL and tokens

