# StockBridge

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Npgsql-336791?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)
![License](https://img.shields.io/badge/license-unlicensed-lightgrey)

A full-stack inventory & stock management API built with **ASP.NET Core 8**, featuring OAuth2/OIDC authentication via **Zitadel**, role-based access control, an AI-assisted product catalog powered by **Groq (Llama 3.1)**, and a single-page dashboard with no frontend framework — just HTML, CSS and vanilla JavaScript.

🔗 **Live demo:** _add your Render URL here after deploying_

---

## Features

**Inventory management**
- Product CRUD with soft delete, SKU search, and status filters
- Stock movement ledger (IN / OUT) with per-product history
- Low-stock alerts based on configurable reorder levels
- Reports dashboard: total stock value, most/least stocked items, movement summaries

**Authentication & authorization**
- OAuth2 Authorization Code + PKCE flow against [Zitadel](https://zitadel.com) (supports Google as an identity provider)
- Role-based access control (`admin` vs. read-only `user`) enforced both server-side (`[Authorize(Roles = "admin")]`) and in the UI
- JWT bearer validation against the Zitadel issuer/audience

**AI features (Groq API, `llama-3.1-8b-instant`, free tier)**
- One-click AI-generated Turkish product descriptions from SKU + name
- Stock depletion forecast (`~N days left`) computed from historical stock movement velocity, surfaced per product on the dashboard

**Modern dashboard UI**
- Dark / light theme toggle with persisted preference
- Animated metric counters, stock-fill progress bars, skeleton loading states
- Chart.js bar chart of recent stock movements
- Collapsible sidebar, mobile-responsive layout, toast notifications

**ERP integration (mocked)**
- Simulated ERP sync service (`IErpService`) with randomized transient failures
- Background hosted service that periodically re-syncs all active products

---

## Tech stack

| Layer          | Technology                                             |
|-----------------|---------------------------------------------------------|
| API             | ASP.NET Core 8 Web API                                  |
| ORM / Database  | Entity Framework Core 8 + PostgreSQL (Npgsql)            |
| Auth            | Zitadel (OIDC, PKCE), JWT Bearer                         |
| AI              | Groq API (`llama-3.1-8b-instant`)                        |
| Frontend        | Vanilla JS + Chart.js (single-page `wwwroot/index.html`) |
| Containerization| Docker, Docker Compose                                   |
| Deployment      | Render (Docker web service) + Neon (serverless Postgres) |

---

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (recommended — spins up Postgres for you)
- A free [Groq API key](https://console.groq.com) for the AI features
- A [Zitadel](https://zitadel.com) instance (free cloud tier) with an application configured for PKCE

### Run with Docker Compose (recommended)

```bash
git clone https://github.com/semaakyavuz/StockBridge.git
cd StockBridge

# Create a .env file with your Groq key (never committed — see .gitignore)
echo "GROQ_API_KEY=your-groq-key-here" > .env

docker compose up --build
```

The API + PostgreSQL will be available at `http://localhost:8080`. Open `http://localhost:8080/index.html` in your browser.

### Run locally without Docker

1. Install PostgreSQL locally and update `ConnectionStrings:DefaultConnection` in `appsettings.Development.json` (gitignored — create it if it doesn't exist).
2. Add your Groq key to the same file under `Groq:ApiKey`.
3. Apply migrations and run:

```bash
dotnet ef database update
dotnet run
```

---

## Environment variables

| Variable                          | Purpose                                                         |
|------------------------------------|------------------------------------------------------------------|
| `ConnectionStrings__DefaultConnection` | Postgres connection string (local/Docker dev)                |
| `DATABASE_URL`                     | Cloud Postgres connection string (`postgres://user:pass@host/db`), used in production (Render/Neon); overrides the above |
| `Groq__ApiKey`                     | Groq API key for AI description generation                     |
| `PORT`                             | Port Kestrel binds to in containerized environments (set automatically by Render) |

---

## Project structure

```
Controllers/     REST API endpoints (Products, StockMovements, Sync, AI, Auth)
Services/        GroqService, mocked ERP integration, background sync job
Models/          EF Core entities (Product, StockMovement, User)
Data/            AppDbContext + EF Core configuration
Auth/            Zitadel role-claim transformation
Migrations/      EF Core migrations (PostgreSQL)
wwwroot/         Single-page dashboard (HTML/CSS/JS, no build step)
```

---

## Deployment

The app ships with a multi-stage `Dockerfile` and is deployed as a Docker web service on [Render](https://render.com) (free tier) against a [Neon](https://neon.tech) serverless PostgreSQL database (both free, no credit card required). See `render.yaml` for the service definition. On startup, the app automatically applies any pending EF Core migrations — no manual database setup needed after the first deploy.

`docker-compose.yml` is provided for local development and mirrors the production container setup with a local PostgreSQL instance.
