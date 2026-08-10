# 🎮 MatchmakingEngine

A high-performance, real-time competitive matchmaking engine and platform built with **.NET 10** and **ASP.NET Core**. This project is designed to handle competitive matchmaking logic (similar to CS2 or FACEIT), including real-time party management, MMR-based queues, map veto systems, and real-time leaderboards.

## 🌟 Key Features & Strong Sides

### 🏗️ Enterprise-Grade Architecture
- **Clean Architecture (DDD):** Strict separation of concerns across `Domain`, `Application`, `Infrastructure`, and API layers. This ensures the business logic is independent of frameworks, databases, and UI.
- **CQRS Pattern:** Utilizes **MediatR** to completely separate read operations (Queries) from write operations (Commands), adhering to the Single Responsibility Principle and making complex workflows highly maintainable.
- **Comprehensive Testing:** Includes a robust test suite (`MatchmakingEngine.Tests`) using **xUnit**, **Moq**, and **FluentAssertions** to guarantee the reliability of critical matchmaking and domain logic.

### ⚡ Real-Time & High Performance
- **SignalR WebSockets:** Delivers instant, real-time updates to connected clients for party invites, queue status, match found notifications, and the interactive map veto phase.
- **Redis Queues & Leaderboards:** Leverages **Redis Sorted Sets** for ultra-fast, memory-based matchmaking queues (grouping players by MMR and Game Mode) and real-time global leaderboards.
- **Background Processing with Hangfire:** Handles delayed, background, and recurring tasks seamlessly. For example, the **AFK Auto-Ban** system during the map veto phase relies on Hangfire to automatically trigger a timeout if a captain fails to ban a map within 30 seconds.

### ☁️ Cloud-Native & DevOps Ready
- **AWS Elastic Beanstalk:** Pre-configured and optimized for deployment on AWS infrastructure.
- **Dockerized:** Fully containerized using `docker-compose` for rapid local development and seamless production deployment.
- **Automatic EF Core Migrations:** Safely applies Entity Framework Core migrations to the AWS RDS PostgreSQL database automatically upon application startup.
- **CI/CD Pipelines:** Includes GitHub Actions workflows for automated building, testing, and deployment.

### 🎨 Modern Vanilla Frontend
- **Zero-Dependency Architecture:** A lightning-fast, Vanilla JS and CSS frontend that requires no heavy frameworks (React/Angular/Vue).
- **Premium Aesthetics:** Implements modern UI/UX principles, including glassmorphism, subtle micro-animations, custom scrollbars, and dynamic color-coded MMR badges.
- **Secure Authentication:** Utilizes JWT-based authentication passed securely via HTTP headers to bypass modern cross-origin browser restrictions.

---

## 🛠️ Technology Stack

| Category | Technology |
|---|---|
| **Backend Framework** | .NET 10, ASP.NET Core Web API |
| **Architecture** | Clean Architecture, CQRS (MediatR) |
| **Database & ORM** | PostgreSQL, Entity Framework Core |
| **Caching & Queues** | Redis (StackExchange.Redis) |
| **Real-Time Comm.** | SignalR |
| **Background Jobs** | Hangfire |
| **Testing** | xUnit, Moq, FluentAssertions, In-Memory DB |
| **Containerization** | Docker, Docker Compose |
| **Cloud Provider** | AWS (Elastic Beanstalk, RDS, ElastiCache) |
| **Frontend** | Vanilla JavaScript, HTML5, CSS3 |

---

## 🚀 Getting Started (Local Development)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Node.js / Live Server (for serving the frontend)

### 1. Start Infrastructure Services
Use Docker Compose to spin up the local PostgreSQL and Redis instances:
```bash
docker-compose up -d
```

### 2. Run the Backend (API)
Navigate to the root directory and start the API:
```bash
dotnet run --project MatchmakingEngine/MatchmakingEngine.csproj
```
*Note: EF Core will automatically create the database and apply the latest migrations on startup.*

### 3. Run the Frontend (Client)
Serve the `MatchmakingClient` folder using any static file server. If using VS Code, simply right-click `index.html` and select **"Open with Live Server"**.

---

## 📖 Domain Logic Highlights

- **Matchmaking Service:** Players can queue for Solo or Duo (Party) modes. A background worker constantly evaluates the Redis queue, matching players with a similar MMR range.
- **Party System:** Players can invite friends to their lobby. The party leader controls the matchmaking queue for the entire group.
- **Map Veto System:** Once 10 players are found, a match is created in `MapVeto` state. Captains (highest MMR players of each team) take turns banning maps via SignalR. If a captain is AFK for 30s, Hangfire automatically bans a random available map.
- **Trust Factor:** Integrated player trust mechanics to potentially group toxic players or leavers together, keeping high-quality games clean.

---

*Built with ❤️ for competitive integrity.*
