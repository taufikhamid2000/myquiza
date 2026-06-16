# MyQuiza API

The API / business-logic layer for an SPM learning platform. MyQuiza validates
Supabase-issued JWTs and serves content, quiz, attempt, progress, and leaderboard
endpoints. The web client (**EduBridge**, Next.js) and mobile client (**Syllabuzz**,
Android) consume this API.

- **Live:** https://myquiza-api.onrender.com
- **API docs (Scalar):** https://myquiza-api.onrender.com/scalar/v1
- **Health:** https://myquiza-api.onrender.com/health

> This repository is API-only. It was originally scaffolded as a Next.js app; that
> boilerplate (and its Vercel deployment) has been removed. The deliverable is the
> ASP.NET Core project under [`backend/`](backend/).

## Stack

- ASP.NET Core (.NET 10) Web API, controller-based, feature-folder structure
- EF Core 10 + Npgsql, snake_case via `EFCore.NamingConventions`
- Auth: Supabase JWT validation (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- OpenAPI + [Scalar](https://scalar.com) for interactive docs

**Maps onto EduBridge's existing Supabase Postgres schema and owns no EF migrations** —
EduBridge's `supabase/migrations` remain the schema source of truth. Do not run
`dotnet ef migrations` / `database update` against this database.

## Run locally

```bash
cd backend
dotnet run --project src/MyQuiza.Api
```

Configuration comes from user-secrets / environment variables (never committed):

| Key | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | EduBridge's Supabase Postgres (session pooler) |
| `Supabase__JwtSecret` | Legacy HS256 JWT secret (this project signs tokens with it; JWKS is empty) |
| `Supabase__Issuer` | `https://<ref>.supabase.co/auth/v1` — validates the `iss` claim |
| `Supabase__Audience` | Defaults to `authenticated` |

## Endpoints (v1)

All under `/api/v1`. Reads are public unless noted; writes require a Bearer token.

| Method | Path | Notes |
| --- | --- | --- |
| `GET` | `/subjects` | `?includeDisabled=true` requires Moderator |
| `GET` | `/subjects/{id}/chapters` | |
| `GET` | `/chapters/{id}/topics` | |
| `GET` | `/topics/{id}/quizzes` | Verified-only; `?includeUnverified=true` to include pending |
| `GET` | `/quizzes/{id}` | Answer options omit `is_correct` (taker-safe) |
| `POST` | `/quizzes` | Auth — create a quiz (starts unverified) |
| `POST` | `/quizzes/{id}/verify` | Moderator — verify/unverify |
| `POST` | `/quizzes/{id}/attempts` | Auth — server-side scoring; only verified quizzes affect progress/XP |
| `GET` | `/me` | Auth — profile + role |
| `GET` | `/me/attempts` | Auth — attempt history |
| `GET` | `/me/progress` | Auth — per-topic progress (verified quizzes only) |
| `GET` | `/leaderboard` | `?period=weekly`, `?limit=` (max 100) |

## Deployment

Deployed on **Render** as a Docker web service (auto-deploys on push to `master`).
See [`backend/render.yaml`](backend/render.yaml) and [`backend/Dockerfile`](backend/Dockerfile).
Secrets (`CONNECTIONSTRINGS__DEFAULTCONNECTION`, `SUPABASE__ISSUER`, `SUPABASE__JWTSECRET`)
are set in the Render dashboard, not in git.
