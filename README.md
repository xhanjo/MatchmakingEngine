# MatchmakingEngine

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-Real--time-512BD4?logo=dotnet)
![Hangfire](https://img.shields.io/badge/Hangfire-Background_Jobs-2C3E50)
![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?logo=docker&logoColor=white)
![AWS](https://img.shields.io/badge/AWS-Elastic_Beanstalk-FF9900?logo=amazonaws&logoColor=white)

A real-time competitive matchmaking backend built with .NET 10 and ASP.NET Core. The project implements core platform features analogous to FACEIT or CS2's competitive system: MMR-based queuing, party management, a map veto phase with AFK auto-ban, post-match statistics, and a real-time leaderboard.

---

## Architecture

The solution follows **Clean Architecture** principles with strict layer separation:

```
MatchmakingEngine/           # ASP.NET Core Web API (host, controllers, hubs, workers)
MatchmakingEngine.Application/  # Use cases, CQRS commands & queries, validators, interfaces
MatchmakingEngine.Domain/    # Domain entities, value objects, domain events
MatchmakingEngine.Infrastructure/  # EF Core, repositories, Redis services, caching
MatchmakingEngine.Tests/     # Unit and integration tests
MatchmakingClient/           # Vanilla JS/HTML/CSS frontend
```

**CQRS** is implemented via **MediatR**: every action in the system is expressed as an explicit Command or Query with its own handler. A pipeline behavior (`ValidationBehavior`) intercepts every command and runs **FluentValidation** before the handler executes, ensuring invalid data never reaches the domain layer.

**Domain Events** are raised from aggregate roots (e.g., `PlayerMmrChangedEvent` when a match completes) and dispatched by EF Core's `SaveChangesAsync` through registered MediatR notification handlers, keeping side effects decoupled from the core business logic.

---

## Key Technical Decisions

### Matchmaking Queue (Redis ZSET)
Players are stored in Redis Sorted Sets keyed by region and game mode, with their MMR as the score. The `MatchmakingWorker` (a `BackgroundService`) alternates between Solo and Duo modes every 2 seconds, pulls an anchor player, and queries Redis for candidates within a dynamic MMR delta. The delta starts at 50 and grows over time so that long-waiting players gradually accept broader matches. Trust Factor is also checked: the difference between players' trust values must stay within a widening threshold.

### Two-Level Cache
`TwoLevelCacheService` implements a read-through cache: it first checks in-process `IMemoryCache`, then falls back to distributed Redis cache, and only hits the database if both miss. This minimizes latency on frequently-read data like leaderboards.

### Map Veto & AFK Auto-Ban
When a match is accepted by all players, it transitions to `MapVeto` status. Captains (determined by MMR) alternate banning maps via SignalR. After each ban, `BanMapCommandHandler` schedules a new **Hangfire** delayed job that fires in 30 seconds. If the next captain hasn't acted by then, `MapVetoTimeoutEventHandler` selects and bans a random available map on their behalf. This guarantees the veto phase always completes.

### Match Cleanup
`MatchCleanupWorker` polls every 5 seconds and cancels any match stuck in `Pending` status for more than 30 seconds (i.e., at least one player failed to accept), notifying clients over SignalR.

### Optimistic Concurrency
The `Match` entity has a `[Timestamp]` (`xmin`) column in PostgreSQL, ensuring concurrent updates to the same match (e.g., two players simultaneously banning a map) are detected and handled safely.

### Rate Limiting
A global fixed-window rate limiter is applied at the ASP.NET Core middleware level: 30 requests per second per IP. Excess requests receive HTTP 429.

### Health Checks
`/health` endpoint exposes the status of both PostgreSQL and Redis connections, suitable for load balancer and AWS ECS health probes.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| Pattern | Clean Architecture, CQRS (MediatR), DDD |
| Database | PostgreSQL, Entity Framework Core 10 |
| Caching | Redis (StackExchange.Redis), IMemoryCache |
| Real-time | ASP.NET Core SignalR (Redis backplane) |
| Background Jobs | Hangfire (PostgreSQL storage) |
| Validation | FluentValidation (pipeline behavior) |
| Logging | Serilog (Console + rolling file sink) |
| Auth | JWT Bearer tokens (BCrypt password hashing) |
| Testing | xUnit, Moq, FluentAssertions, EF Core InMemory |
| Containerization | Docker, Docker Compose |
| Cloud | AWS Elastic Beanstalk, RDS (PostgreSQL), ElastiCache (Redis) |
| CI/CD | GitHub Actions |
| Frontend | Vanilla JavaScript, HTML5, CSS3 |

---

## API Endpoints

| Method | Route | Description |
|---|---|---|
| POST | `/api/Auth/login` | Authenticate and receive a JWT |
| POST | `/api/Players/register` | Register a new player |
| GET | `/api/Players/{id}` | Get player profile |
| GET | `/api/Players/my/history` | Get authenticated player's match history |
| POST | `/api/Matchmaking/join` | Join the matchmaking queue |
| POST | `/api/Matchmaking/leave` | Leave the matchmaking queue |
| GET | `/api/Matchmaking/status` | Poll current queue or match status |
| POST | `/api/Matchmaking/accept/{matchId}` | Accept a found match |
| POST | `/api/Matchmaking/decline/{matchId}` | Decline a found match |
| POST | `/api/Matchmaking/veto/{matchId}/{mapName}` | Ban a map during veto phase |
| POST | `/api/Matchmaking/complete/{matchId}` | (Admin) Complete a match and record stats |
| GET | `/api/Friends` | Get friend list |
| GET | `/api/Friends/pending` | Get incoming friend requests |
| POST | `/api/Friends/request/{targetId}` | Send a friend request |
| POST | `/api/Friends/accept/{requestId}` | Accept a friend request |
| DELETE | `/api/Friends/{friendshipId}` | Remove a friend |
| POST | `/api/Party/create` | Create a party |
| POST | `/api/Party/invite/{friendId}` | Invite a friend to party |
| POST | `/api/Party/join/{partyId}` | Join a party via invite |
| POST | `/api/Party/leave` | Leave current party |
| GET | `/api/Leaderboard` | Fetch global MMR leaderboard |
| GET | `/api/Admin/matches` | (Admin) Get all matches |
| GET | `/health` | Health check (PostgreSQL + Redis) |

**SignalR Hub:** `wss://{host}/hubs/matchmaking`

---

## SignalR Events (Server → Client)

| Event | Payload | Description |
|---|---|---|
| `MatchFound` | `matchId` | A match has been found for the player |
| `MatchAccepted` | — | All players accepted; veto phase begins |
| `MatchCanceled` | — | Match was canceled (player declined or timed out) |
| `MatchReadyForVeto` | `VetoStateDto` | Full veto state sent to clients |
| `MapVetoUpdated` | `VetoStateDto` | A map was banned; updated state |
| `MatchStarting` | `matchId` | Veto complete; server is starting |
| `MatchFinished` | `ResultDto` | Match ended; scoreboard and MMR changes |
| `PartyInviteReceived` | `partyId, senderId` | A friend sent a party invite |
| `PlayerJoinedParty` | `playerId` | A new member joined your party |
| `AdminMatchesUpdated` | — | (Admin broadcast) Match list changed |

---

## Local Development

**Prerequisites:** .NET 10 SDK, Docker Desktop

### 1. Clone the repository

```bash
git clone https://github.com/xhanjo/MatchmakingEngine.git
cd MatchmakingEngine
```

### 2. Start infrastructure

```bash
docker-compose up -d
```

This starts PostgreSQL on port `5432` and Redis on port `6379`. The compose file also defines two API replicas (`api1`, `api2`) behind a shared Redis SignalR backplane, demonstrating horizontal scalability.

### 3. Configure secrets

Copy `appsettings.json` and fill in the JWT key:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=MatchmakingDb;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "Issuer": "MatchmakingEngine",
    "Audience": "MatchmakingClients",
    "Key": "your_secret_key_at_least_32_characters"
  },
  "Redis": {
    "Configuration": "localhost:6379,abortConnect=false"
  }
}
```

### 4. Run the API

```bash
dotnet run --project MatchmakingEngine/MatchmakingEngine.csproj
```

EF Core migrations are applied automatically on startup with a retry policy (5 attempts, 2s interval). A default `admin` account (`Admin123!`) is seeded if it does not exist.

### 5. Run the frontend

Serve `MatchmakingClient/` with any static file server. In VS Code, use the Live Server extension and open `index.html`.

Swagger UI is available at `http://localhost:{port}/swagger` in Development mode.

### 6. Run tests

```bash
dotnet test MatchmakingEngine.Tests/MatchmakingEngine.Tests.csproj
```

---

## Deployment (AWS)

The application is deployed to **AWS Elastic Beanstalk** (Docker platform):

- The API container connects to **Amazon RDS** (PostgreSQL) and **Amazon ElastiCache** (Redis).
- EF Core migrations are applied at startup, so no manual database access is required after deployment.
- Hangfire uses the same PostgreSQL instance for job persistence.
- GitHub Actions builds and pushes the Docker image on every push to `main`.

---

## Project Structure Details

```
MatchmakingEngine.Domain/
  Common/              # Entity base class with domain event dispatch
  Events/              # Domain events (e.g., PlayerMmrChangedEvent)
  Exceptions/          # Domain-specific exceptions (NotFoundException, ConflictException)

MatchmakingEngine.Application/
  Application/
    Behaviors/         # MediatR pipeline: ValidationBehavior
    Commands/          # One folder per domain area (Auth, Matchmaking, Friends, Party, Players)
    Queries/           # Leaderboard, MatchHistory, Status
  EventHandlers/       # MediatR notification handlers (domain event side effects)
  Interfaces/          # Repository contracts, ILeaderboardService, ICacheService, IMatchmakingQueue

MatchmakingEngine.Infrastructure/
  Data/                # EF Core DbContext, entity configurations
  Migrations/          # EF Core migrations
  Repositories/        # Concrete repository implementations
  Services/            # LeaderboardService (Redis ZSET), MatchmakingQueue, TwoLevelCacheService

MatchmakingEngine/ (API host)
  Controllers/         # Auth, Players, Matchmaking, Friends, Party, Leaderboard, Admin
  Hubs/                # MatchmakingHub (SignalR)
  HostedServices/      # MatchmakingWorker, MatchCleanupWorker, LeaderboardSeederWorker
  Events/              # Application-level event handlers (MapVetoTimeout)
  Middlewares/         # GlobalExceptionHandler
```
