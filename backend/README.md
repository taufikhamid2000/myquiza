# MyQuiza API

ASP.NET Core (.NET 10) Web API — the **business-logic layer** for an SPM learning platform.

Part of a three-repo product:

- **EduBridge** (Next.js) — web client
- **MyQuiza** (this repo) — the API
- **Syllabuzz** (Android) — mobile client

**Supabase owns auth and the database.** This API validates Supabase-issued JWTs and exposes
content / quiz / attempt / progress / leaderboard endpoints consumed by EduBridge and Syllabuzz.
It maps onto EduBridge's **existing** Supabase Postgres schema and **owns no migrations** — EduBridge's
`supabase/migrations` remain the schema source of truth.

## Stack

- .NET 10, ASP.NET Core controllers
- EF Core 10 + Npgsql, snake_case mapping via `EFCore.NamingConventions`
- JWT auth (`Microsoft.AspNetCore.Authentication.JwtBearer`) validating Supabase tokens
- OpenAPI + Scalar UI

## Structure

```
src/MyQuiza.Api/
├── Auth/         # Supabase JWT validation, CurrentUser, role policies
├── Data/         # AppDbContext (maps existing tables; NO migrations)
├── Models/       # entities: Subject/Chapter/Topic/Quiz/Question/Answer + UserProfile/UserRole/QuizAttempt/UserTopicProgress
├── Dtos/         # request/response shapes (taker DTOs omit is_correct)
└── Features/     # Content, Quizzes, Attempts, Me, Leaderboard
```

## Configuration

Set these via environment variables or user-secrets (never commit secrets):

| Key | Description |
|-----|-------------|
| `ConnectionStrings__DefaultConnection` | EduBridge's Supabase Postgres connection string (session pooler). Connect with a role that bypasses RLS — the API enforces authorization itself. |
| `Supabase__Issuer` | `https://<project-ref>.supabase.co/auth/v1` (enables JWKS validation) |
| `Supabase__JwtSecret` | *Alternative to Issuer:* legacy HS256 project JWT secret |
| `Supabase__Audience` | Defaults to `authenticated` |

```bash
cd src/MyQuiza.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=postgres;Username=...;Password=..."
dotnet user-secrets set "Supabase:Issuer" "https://<ref>.supabase.co/auth/v1"
```

## Run

```bash
dotnet run --project src/MyQuiza.Api
```

- Health: `GET /health`
- API docs (dev): `/scalar/v1`, raw spec at `/openapi/v1.json`

## Endpoints (v1)

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| GET | `/api/v1/me` | required | profile + platform role |
| GET | `/api/v1/me/progress` | required | topic progress |
| GET | `/api/v1/me/attempts` | required | attempt history |
| GET | `/api/v1/subjects` | anon | content tree |
| GET | `/api/v1/subjects/{id}/chapters` | anon | |
| GET | `/api/v1/chapters/{id}/topics` | anon | |
| GET | `/api/v1/topics/{id}/quizzes` | anon | verified quizzes |
| GET | `/api/v1/quizzes/{id}` | anon | **is_correct stripped** |
| POST | `/api/v1/quizzes` | required | create quiz + questions + answers |
| POST | `/api/v1/quizzes/{id}/verify` | Moderator | verify/unverify |
| POST | `/api/v1/quizzes/{id}/attempts` | required | **server-side scoring**, progress + XP |
| GET | `/api/v1/leaderboard` | anon | top users by XP (`?period=weekly`) |

## Deploy

`Dockerfile` builds a runtime image that binds to `$PORT` (Render/Azure). Set the env vars above on
the host. Vercel does not run .NET — use Render or Azure App Service.
