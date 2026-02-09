# WatchDog Alarm

A deadman's switch / wellness check system that triggers actions when certain conditions are not met. Perfect for solo living situations, expedition tracking, or automated emergency notifications.

## Overview

The goal of this software is to facilitate triggering actions when users fail to check in within specified timeframes. It implements a mechanism where, if no action is taken to prevent an alarm state, configured actions are automatically triggered after set delays.

### Use Cases

- **Wellness checks** for people living alone to indicate they are still alive
- **Emergency protocols** that activate if someone becomes incapacitated
- **Expedition tracking** to monitor people in dangerous zones
- **Automated notifications** for password vaults or important information delivery

## Key Features

### Alarm System
- **Multiple delay types**: Timeout-based (e.g., 24 hours after last ping) or Schedule-based (e.g., daily at 9am)
- **Escalation timeline**: Actions can trigger BEFORE alarm point (warnings) and AFTER (escalation)
- **Flexible ping behavior**: Configurable whether pings reset the timer or just log activity
- **High precision**: Actions trigger within 5 seconds of scheduled time

### Verification Methods
- **No authentication**: Public alarms that accept any ping
- **TOTP (Time-based One-Time Passwords)**: According to RFC6238, 30s, 6 digits
- **RSA signatures**: Client signs challenge with private key, server verifies with public key
- **ECDSA signatures**: Elliptic curve cryptography for smaller keys

### Action Types
- **Email**: Send via SMTP with full HTML support (4096 byte payload limit)
- **SMS**: Send text messages (160 byte payload limit, mock in MVP)
- **REST API**: Call external webhooks with any HTTP method (4096 byte payload limit)

### Advanced Features
- **On-ping actions**: Trigger actions immediately when receiving a ping
- **Ping payloads**: Include data with pings that gets passed to actions
- **Complete audit trail**: Full history of all pings and action executions
- **Configurable retention**: Set how long history is kept (global + per-alarm override)
- **User management**: Multi-user system with JWT authentication

## Architecture

**Tech Stack:**
- .NET 10 (LTS)
- PostgreSQL 16
- ASP.NET Core Web API
- Entity Framework Core
- Clean Architecture (API, Core, Infrastructure layers)
- In-Memory Command Bus (fast inter-component communication)

**Key Components:**
- **Command Bus**: <100ms latency for API-to-worker communication (request-reply pattern)
- **CommandHandlerWorker**: Processes commands (cycle creation, activation, cancellation)
- **AlarmCycleManagerWorker**: 10-millisecond polling for precise action timing
- **ActionExecutor**: Handles email, SMS, REST API with retry logic
- **VerificationService**: Plugin architecture for TOTP, RSA, ECDSA auth

**Performance:**
- Ping endpoint: 50-85ms typical (under 100ms target)
- Action trigger precision: <5 seconds
- Command timeout: 500ms

## Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL)
- [Git](https://git-scm.com/)

### Setup

1. **Clone the repository**
```bash
git clone https://github.com/yourusername/wdalarm3.git
cd wdalarm3
```

2. **Start PostgreSQL with Docker Compose**
```bash
docker-compose up -d
```

3. **Update database connection string** (if needed)
Edit `src/WdAlarm.Api/appsettings.Development.json`

4. **Run database migrations**
```bash
cd src/WdAlarm.Api
dotnet ef database update
```

5. **Run the application**
```bash
dotnet run
```

6. **Access Swagger UI**
Open browser to `https://localhost:5001/swagger`

## Documentation

Detailed documentation is available in the `/docs` folder:

- **[PLANNING.md](docs/PLANNING.md)** - Complete planning session and design decisions
- **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** - System architecture and component design
- **[DATABASE.md](docs/DATABASE.md)** - Database schema and entity relationships
- **[API.md](docs/API.md)** - REST API endpoint documentation
- **[DEPLOYMENT.md](docs/DEPLOYMENT.md)** - Deployment and configuration guide

## Project Structure

```
wdalarm3/
├── src/
│   ├── WdAlarm.Api/              # ASP.NET Core Web API
│   │   ├── Controllers/          # REST API endpoints
│   │   └── Program.cs
│   ├── WdAlarm.Core/             # Domain models, interfaces, business logic + Background Workers
│   │   ├── Entities/             # Domain entities (Alarm, Ping, etc.)
│   │   ├── Commands/             # Command/Response patterns (NEW)
│   │   ├── Interfaces/           # Service interfaces (includes ICommandBus)
│   │   ├── Workers/              # Background workers (IHostedService)
│   │   │   ├── AlarmCycleManagerWorker.cs    # 10-millisecond action polling
│   │   │   ├── ChallengeRotationWorker.cs    # Challenge rotation
│   │   │   └── HistoryCleanupWorker.cs       # Cleanup old records
│   │   └── Enums/                # Enumerations
│   └── WdAlarm.Infrastructure/   # Data access, external services
│       ├── Messaging/            # Command bus implementation (NEW)
│       ├── Data/                 # EF Core DbContext, migrations
│       ├── Repositories/         # Data access layer
│       ├── Verification/         # TOTP, RSA, ECDSA implementations
│       └── Actions/              # Email, SMS, REST API plugins
├── tests/
│   ├── WdAlarm.UnitTests/
│   └── WdAlarm.IntegrationTests/
├── docs/                         # Documentation
│   ├── PLANNING.md               # Planning session & design decisions
│   ├── ARCHITECTURE.md           # System architecture with command bus
│   ├── DATABASE.md               # Database schema
│   ├── API.md                    # REST API documentation
│   ├── CHANGELOG.md              # Architecture changes
│   └── INDEX.md                  # Documentation index
├── docker-compose.yml            # PostgreSQL container
└── README.md
```

## Example: Creating an Alarm

```bash
# 1. Register a user
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecureP@ss123",
    "confirmPassword": "SecureP@ss123"
  }'

# 2. Login to get JWT token
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecureP@ss123"
  }'
# Returns: {"accessToken": "...", "refreshToken": "..."}

# 3. Create an alarm with escalation timeline
curl -X POST https://localhost:5001/api/alarms \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Daily Wellness Check",
    "delayType": "Timeout",
    "timeoutDuration": "24:00:00",
    "verificationMethod": "TOTP",
    "verificationConfig": {"secret": "BASE32SECRET"},
    "delayActions": [
      {
        "offsetMinutes": -120,
        "actionType": "Email",
        "actionConfig": {
          "to": "me@example.com",
          "subject": "Reminder: Check in soon!",
          "body": "Don't forget to ping within 2 hours"
        }
      },
      {
        "offsetMinutes": 0,
        "actionType": "Email",
        "actionConfig": {
          "to": "emergency@example.com",
          "subject": "ALERT: No check-in",
          "body": "User has not checked in"
        }
      }
    ]
  }'

# 4. Get current global challenge
curl https://localhost:5001/api/challenge

# 5. Send a ping with TOTP code
curl -X POST https://localhost:5001/api/ping/YOUR_ALARM_ID \
  -H "Content-Type: application/json" \
  -d '{
    "verificationProof": "123456",
    "payload": "All is well",
    "resetTimer": true
  }'
```

## Core Concepts

### Alarm Cycles

Each alarm activation creates a **cycle** with a calculated alarm point (T=0). Actions are scheduled relative to this point:

```
User creates alarm: 24-hour timeout
User pings at: 09:00 (Feb 8)

Timeline created:
  07:00 Feb 9  [-120 min]  Email: "Reminder to check in"
  08:30 Feb 9  [ -30 min]  SMS: "Last chance!"
  09:00 Feb 9  [   0 min]  Email: "ALERT: No check-in" ← Alarm Point
  10:00 Feb 9  [ +60 min]  Email: "Escalation to backup"
  09:00 Feb 10 [+1440 min] REST: "Emergency protocol"

If user pings at 08:45 Feb 9:
  ✗ All pending actions cancelled
  ✓ New cycle created with alarm point at 08:45 Feb 10
  ✓ Fresh timeline scheduled
```

### Verification Flow

For cryptographic verification (RSA/ECDSA):

1. Client requests current global challenge: `GET /api/challenge`
2. Server returns challenge (rotates every 30 seconds)
3. Client constructs message: `challenge + timestamp`
4. Client signs message with private key
5. Client sends ping with signature: `BASE64_SIGNATURE|TIMESTAMP_ISO8601`
6. Server verifies signature using stored public key
7. Server validates timestamp is within tolerance (±5 minutes)

### Action Execution

**Timeline Actions** (delay-based):
- Scheduled relative to alarm point (negative = before, positive = after)
- Execute at precise scheduled time
- Retry up to 3 times with 5-second intervals for network failures
- Track execution state (Pending → Executing → Success/Failed)

**On-Ping Actions**:
- Execute immediately when valid ping received (async)
- Receive both configured payload AND ping payload
- Independent of timer reset behavior
- Logged in action execution history with PingId reference

## Configuration

Key settings in `appsettings.json`:

```json
{
  "AlarmSettings": {
    "PollingIntervalSeconds": 1,
    "MinimumDelaySeconds": 15,
    "ChallengeRotationSeconds": 30
  },
  "ActionSettings": {
    "MaxRetries": 3,
    "RetryDelaySeconds": 5,
    "MaxPayloadBytes": 4096
  },
  "HistorySettings": {
    "DefaultRetentionDays": 90
  }
}
```

## Security Considerations

### MVP Approach
- **Client-side encryption**: Sensitive action config (passwords, API keys) should be encrypted by client before sending
- **JWT tokens**: Access tokens valid for 25 hours with refresh tokens (7 days)
- **UUID alarm IDs**: Hard-to-guess identifiers for no-auth alarms
- **HTTPS enforced**: All communication over TLS in production
- **Input validation**: Strict validation and size limits on all inputs

### Future Enhancements
- Server-side encryption at rest for sensitive fields
- Rate limiting on ping endpoint
- IP-based abuse detection
- Zero-knowledge encryption architecture

## Development

### Running Tests
```bash
dotnet test
```

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName -p src/WdAlarm.Infrastructure -s src/WdAlarm.Api

# Update database
dotnet ef database update -p src/WdAlarm.Infrastructure -s src/WdAlarm.Api
```

### Docker Compose
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Roadmap

### MVP (Current)
- [x] Planning and architecture
- [ ] Core alarm cycle engine
- [ ] Authentication and user management
- [ ] Verification methods (None, TOTP, RSA, ECDSA)
- [ ] Action plugins (Email, SMS mock, REST API)
- [ ] REST API endpoints
- [ ] Swagger documentation

### Future Enhancements
- [ ] Real SMS provider integration (Twilio, Vonage)
- [ ] Redis for distributed challenge storage
- [ ] Message queue for action execution (RabbitMQ/Kafka)
- [ ] Web UI dashboard
- [ ] Mobile app
- [ ] Webhook support for custom integrations
- [ ] Advanced scheduling (multiple schedules per alarm)
- [ ] Alarm templates and sharing
- [ ] Multi-factor authentication
- [ ] Rate limiting and abuse prevention

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Documentation**: See `/docs` folder for detailed guides
- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/yourusername/wdalarm3/issues)
- **Discussions**: Join conversations in [GitHub Discussions](https://github.com/yourusername/wdalarm3/discussions)

## Acknowledgments

- Built with .NET 10 and PostgreSQL
- Uses [Cronos](https://github.com/HangfireIO/Cronos) for cron expression parsing
- Email via [MailKit](https://github.com/jstedfast/MailKit)
- TOTP via [Otp.NET](https://github.com/kspearrin/Otp.NET)

---

**Note**: This is an MVP implementation focused on standard server deployment. The system is designed with scalability in mind and can be adapted for serverless/cloud architectures (AWS Lambda, Timestream) in future iterations.
