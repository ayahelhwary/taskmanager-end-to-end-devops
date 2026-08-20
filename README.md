# Task Manager API — End-to-End DevOps Project

A complete DevOps project documenting the journey of building a full CI/CD pipeline — from a simple .NET application to a secure, optimized, containerized deployment.

**Current stage:** The application is fully running inside Docker, connected to PostgreSQL, and covered by 12 unit tests.

---

## 🎯 Problem Statement

Many applications take a long time to deploy and require significant manual intervention, which leads to errors and delays. This project documents building a complete pipeline (Build → Test → Docker → CI/CD → Deploy → Monitoring) in a gradual, well-understood way — not just copy-pasted boilerplate.

---

## 🏗️ Architecture

```
┌─────────────┐      ┌──────────────┐      ┌─────────────────┐
│  .NET API   │ ───▶ │  Docker      │ ───▶ │  PostgreSQL      │
│  (Controllers│      │  Multi-stage │      │  (separate       │
│  + EF Core)  │      │  build       │      │  container)      │
└─────────────┘      └──────────────┘      └─────────────────┘
       │                     │
       │              Alpine-based
       │              (55MB image)
       ▼
┌─────────────┐
│ Health Checks│
│ /health/live │  ← independent of the database
│ /health/ready│  ← verifies the database connection
└─────────────┘
```

---

## 🧰 Tech Stack

| Tool | Purpose |
|---|---|
| .NET 10 / C# | Backend language |
| PostgreSQL 16 (Alpine) | Database |
| Entity Framework Core | ORM + Migrations |
| xUnit + EF InMemory | Unit testing |
| Docker (Multi-stage) | Containerization |
| Git (Conventional Commits) | Version control |

---

## 📋 Available Endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/tasks` | Get all tasks |
| GET | `/api/tasks/{id}` | Get a single task |
| POST | `/api/tasks` | Create a new task |
| PUT | `/api/tasks/{id}` | Update a task |
| DELETE | `/api/tasks/{id}` | Delete a task |
| GET | `/health/live` | Liveness check (independent of the database) |
| GET | `/health/ready` | Readiness check (verifies the database connection) |

---

## 🚀 Running Locally (via Docker)

### Requirements
- Docker Desktop installed and running
- .NET 10 SDK (if you want to run migrations directly from your machine)

### 1. Start the database
```bash
docker compose -f src/TaskManagerApi/docker-compose.db-only.yml up -d
```

### 2. Build the Docker image
```bash
docker build -t taskmanager-api .
```

### 3. Run the application
```bash
docker run -d -p 8080:8080 \
  -e DB_CONNECTION_STRING="Host=host.docker.internal;Port=5432;Database=taskmanager;Username=postgres;Password=postgres" \
  --name taskmanager-container \
  taskmanager-api
```

### 4. Create the database tables (Migration) — first time only
```bash
cd src/TaskManagerApi
dotnet ef database update
cd ../..
```

### 5. Verify everything is running
```bash
curl http://localhost:8080/health/live
curl http://localhost:8080/health/ready
curl http://localhost:8080/api/tasks
```

### Running the unit tests
```bash
dotnet test TaskManagerApi.sln
```

---

## 📊 Metrics (Before / After)

| Metric | Before | After | Improvement |
|---|---|---|---|
| Docker Image (Content Size) | 95.4MB | **55MB** | ↓ 42% |
| Docker Image (Disk Usage) | 381MB | **196MB** | ↓ 48.5% |
| Unit Test Coverage | 0 tests | **12 tests** | Full controller coverage |

**Reason for the improvement:** Switching the runtime stage from `aspnet:10.0` (Debian) to `aspnet:10.0-alpine`, while preserving 100% of the original functionality.

---

## 🧠 Engineering Decisions and Rationale

### Why a multi-stage Docker build?
So the final image doesn't include build tools (the SDK) that aren't needed at runtime — this reduces image size and shrinks the attack surface.

### Why separate health checks (`live` vs `ready`)?
- **Liveness**: Answers "is the process itself alive?" — if it fails, the orchestrator (e.g., Kubernetes later on) restarts the application.
- **Readiness**: Answers "is the application ready to receive traffic right now?" — if it fails (e.g., a temporary database outage), the orchestrator stops routing traffic to it without triggering an unnecessary restart.

This separation will become essential once the project is deployed to Kubernetes in a later stage.

### Why a non-root user inside the container?
If the application were ever compromised, running as a limited, non-root user (`appuser`) reduces the potential damage compared to running with full `root` privileges.

---

## 🐛 Real Challenges and Their Solutions

Building the Dockerfile wasn't smooth on the first try — these are the real issues encountered and how each was resolved:

1. **`No .NET SDKs were found`** — An attempt was made to run `dotnet restore` inside the Runtime-only image (which has no build tools at all). **Fix:** A clear separation between the Build stage (SDK) and the Runtime stage.

2. **`adduser`/`useradd not found`** — Each Linux distribution (Debian vs. Alpine) has different commands for creating users. **Fix:** Matching the command to the distribution the base image is built on.

3. **`ArgumentNullException` on the connection string** — The code used `??`, which only checks for `null`, but `appsettings.json` contained an empty string `""` (not null). **Fix:** Replacing the logic with `string.IsNullOrEmpty()`.

4. **`/health/live` returning Unhealthy despite being meant to be independent** — It was evaluating every registered health check, including the database check. **Fix:** Using a `Predicate` to exclude checks tagged `"ready"`.

5. **`role "postgres" does not exist`** — First cause: a stale Docker volume containing data from a previous attempt (`POSTGRES_USER` and similar env vars are only applied on the *first* initialization of an *empty* volume). Second cause: a separate local PostgreSQL instance running on the same port 5432 (via Homebrew), conflicting with the Docker container.

6. **`relation "Tasks" does not exist`** — The database connection was working, but no EF Core migration had actually created the tables. A working database connection and the existence of tables inside it are two entirely different things.

---

## 🗺️ Roadmap

- [ ] GitHub Actions: automated Build + Test on every push
- [ ] Trivy security scanning for the Docker image
- [ ] Push the image to Docker Hub / GHCR
- [ ] Deployment to a real environment (a simple VPS first, then Kubernetes)
- [ ] Multi-environment setup (dev/staging/production) with approval gates
- [ ] Monitoring with Prometheus + Grafana

---

## 📁 Project Structure

```
TaskManagerApi/
├── Dockerfile                          # Multi-stage build (Alpine-based)
├── .dockerignore
├── .gitignore
├── TaskManagerApi.sln
├── src/TaskManagerApi/                 # Application source code
│   ├── Controllers/
│   ├── Models/
│   ├── Data/
│   ├── Migrations/
│   └── docker-compose.db-only.yml      # Runs PostgreSQL locally only
└── tests/TaskManagerApi.Tests/         # Tests (12 unit tests)
```
