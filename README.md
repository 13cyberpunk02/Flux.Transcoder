<div align="center">

# 🎬 Flux.Transcoder

**Live video transcoding and DVR: SRT / RTMP / UDP in, HLS live and archive out.**

[🇷🇺 Русский](README.ru.md) · 🇬🇧 English

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-22-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.dev/)
[![Angular Material](https://img.shields.io/badge/Material-3-757575?style=for-the-badge&logo=materialdesign&logoColor=white)](https://material.angular.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)

[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![TimescaleDB](https://img.shields.io/badge/TimescaleDB-metrics-FDB515?style=for-the-badge&logo=timescale&logoColor=black)](https://www.timescale.com/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-broker-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![FFmpeg](https://img.shields.io/badge/FFmpeg-HLS-007808?style=for-the-badge&logo=ffmpeg&logoColor=white)](https://ffmpeg.org/)
[![Docker](https://img.shields.io/badge/Docker-compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://docs.docker.com/compose/)

[![Wolverine](https://img.shields.io/badge/Wolverine-6.15-6E4AA8?style=flat-square)](https://wolverinefx.net/)
[![SignalR](https://img.shields.io/badge/SignalR-realtime-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/signalr/)
[![hls.js](https://img.shields.io/badge/hls.js-player-F05A28?style=flat-square)](https://github.com/video-dev/hls.js)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-4-06B6D4?style=flat-square&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE.txt)

</div>

---

## 📖 About

Flux.Transcoder takes live streams from SRT, RTMP or UDP sources, encodes them with FFmpeg (x264, NVENC or QSV) and publishes two HLS outputs from a single encode: a short live window and a time-indexed archive on shared storage (NAS). The operator console shows every channel with live bitrate, uptime and dropped frames, plays live or any moment of the archive, and controls channels with start, stop and restart.

## ✨ Features

- 📡 **Ingest:** SRT, RTMP and UDP (MPEG-TS) sources.
- ⚙️ **Encoding profiles:** resolution, bitrate and encoder (x264 CPU, NVENC, QSV); presets 4K, 1080p, 720p, 480p.
- 🎞️ **One encode, two outputs:** FFmpeg `tee` writes live HLS and the archive at the same time.
- ⏪ **DVR:** watch from any moment by date and time, quick presets or a click on the bitrate chart.
- 🗑️ **Retention:** per channel from 1 minute up to 14 days; a separate cleaner removes old segments.
- 📊 **Metrics:** bitrate, uptime and dropped frames in real time (SignalR) and history in TimescaleDB.
- 🛡️ **Reliability:**
  - the worker keeps processes in line with the database, so channels come back after a worker restart;
  - commands go through a transactional outbox and survive broker or API outages;
  - automatic restart with exponential backoff, `Failed` after 8 attempts;
  - FFmpeg never outlives the worker (Job Object on Windows, `setpriv --pdeathsig` on Linux);
  - a standby worker takes over when the active one dies.
- 🖥️ **Operator console:** Angular Material 3, dark and light themes, Russian UI.
- 🔐 **Auth:** JWT, users stored in the database.

## 🏗️ Architecture

```mermaid
flowchart LR
    SRC["📡 Sources<br/>SRT / RTMP / UDP"] --> FF

    subgraph Worker["🛠️ Flux.Transcoder.Worker"]
        RC["ChannelReconciler"] --> FF["FFmpeg<br/>(1 encode, tee)"]
    end

    FF -->|"HLS live + archive"| NAS[("💾 Storage / NAS")]
    NAS --> NGINX["nginx<br/>:3088"]
    NAS --> CL["🧹 Cleaner<br/>retention"]

    UI["🖥️ Angular UI<br/>:4200"] -->|"REST /api"| API["🌐 Flux.Transcoder.Api<br/>:5000"]
    API -->|"SignalR /hubs"| UI
    UI -->|"HLS"| NGINX

    API -->|"commands (outbox)"| MQ[["🐇 RabbitMQ"]]
    MQ --> RC
    Worker -->|"health reports"| MQ
    MQ --> API

    API --> DB[("🐘 PostgreSQL<br/>+ TimescaleDB")]
    RC --> DB
    CL --> DB
```

The database is the source of truth: a channel status and its `run_revision` say which FFmpeg process must be running. Queue messages only speed up reconciliation, so a lost, duplicated or reordered message does no harm.

## 🧰 Tech stack

| Area | Technologies |
|---|---|
| Backend | .NET 10, ASP.NET Core, Wolverine (mediator, HTTP endpoints, messaging, outbox), FluentValidation, Ardalis.Specification |
| Data | PostgreSQL 16, TimescaleDB (hypertable, continuous aggregates), Entity Framework Core 10 |
| Messaging | RabbitMQ |
| Video | FFmpeg, HLS, nginx |
| Frontend | Angular 22, Angular Material 3, Signal Forms, Tailwind CSS 4, hls.js, ApexCharts, SignalR |
| Logging | Serilog |
| Tests | xUnit, FluentAssertions, Vitest |

## 📁 Project structure

```text
Flux.Transcoder/
├── deploy/                        # docker-compose (PostgreSQL, RabbitMQ, nginx), nginx.conf
├── frontend/flux-frontend/        # Angular operator console
├── src/
│   ├── api/Flux.Transcoder.Api                # REST API, SignalR hub, DVR playlists, metrics
│   ├── application/
│   │   ├── Flux.Transcoder.Application        # commands, queries, validators, specifications
│   │   └── Flux.Transcoder.Contracts          # messages between API and worker
│   ├── domain/Flux.Transcoder.Domain          # channels, encoding profiles, users
│   ├── infrastructure/
│   │   ├── Flux.Transcoder.Ffmpeg             # FFmpeg arguments, process supervision, restarts
│   │   └── Flux.Transcoder.Infrastructure.Database  # EF Core, migrations, JWT, password hashing
│   └── worker/
│       ├── Flux.Transcoder.Worker             # runs FFmpeg, health reports, reconciliation
│       └── Flux.Transcoder.Cleaner            # archive retention
└── tests/Flux.Transcoder.Domain.Tests
```

## 🚀 Getting started

### Requirements

| Tool | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10 |
| [Node.js](https://nodejs.org/) | 22.22+, 24.15+ or 26+ |
| [FFmpeg](https://ffmpeg.org/download.html) | in `PATH` (or set `Ffmpeg:BinaryPath`); NVENC / QSV need a matching GPU |
| [Docker](https://www.docker.com/) | for PostgreSQL, RabbitMQ and nginx |

### 1. Infrastructure

Set the storage folder in `deploy/docker-compose.yml` (the `nginx` volume, `D:/fluxcast/storage` by default), then:

```bash
cd deploy
docker compose up -d
```

### 2. Backend

```bash
# API: applies migrations and creates the first user on start
dotnet run --project src/api/Flux.Transcoder.Api

# Worker: runs FFmpeg for active channels
dotnet run --project src/worker/Flux.Transcoder.Worker

# Cleaner: removes archive segments older than retention
dotnet run --project src/worker/Flux.Transcoder.Cleaner
```

### 3. Frontend

```bash
cd frontend/flux-frontend
npm install
npm start
```

Open http://localhost:4200 and sign in as `admin` / `admin`.

> [!WARNING]
> The repository ships development credentials: the `admin` / `admin` user, database and RabbitMQ passwords and the JWT signing key in `appsettings.json`. Change all of them before any non-local use.

### Useful URLs

| Service | URL |
|---|---|
| Operator console | http://localhost:4200 |
| API | http://localhost:5000 |
| API reference (Scalar, Development) | http://localhost:5000/scalar |
| HLS (nginx) | http://localhost:3088 |
| RabbitMQ management | http://localhost:15672 |

## ⚙️ Configuration

All settings live in `appsettings.json` of each service and can be overridden with environment variables (`Section__Key`, for example `Ffmpeg__SegmentRoot`).

| Key | Service | Default | Meaning |
|---|---|---|---|
| `ConnectionStrings:flux_transcoder_db` | API, Worker, Cleaner | local PostgreSQL | database |
| `ConnectionStrings:RabbitMq` | API, Worker | `amqp://...@localhost:5672` | message broker |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey` | API | dev values | JWT tokens |
| `Archive:SegmentRoot` | API | `D:\fluxcast\storage` | storage root for DVR playlists |
| `Archive:NginxBaseUrl` | API | `http://localhost:3088` | public HLS base URL |
| `Ffmpeg:BinaryPath` | Worker | `ffmpeg` | FFmpeg executable |
| `Ffmpeg:SegmentRoot` | Worker | `D:\fluxcast\storage` | where HLS is written |
| `Ffmpeg:HlsTimeSeconds` | Worker | `4` | segment length |
| `Ffmpeg:LiveListSize` | Worker | `6` | segments in the live playlist |
| `Ffmpeg:StartupTimeoutSeconds` | Worker | `30` | wait for the first segment before a restart |
| `Cleaner:ArchiveRoot` | Cleaner | `D:\fluxcast\storage` | storage root |
| `Cleaner:IntervalMinutes` | Cleaner | `1` | cleanup interval |

The storage root must be the same for the worker, API, cleaner and nginx.

## 🔌 API

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/auth/login` | get a JWT |
| `GET` `POST` | `/api/channels` | list, create channels |
| `GET` `DELETE` | `/api/channels/{id}` | get, delete a channel |
| `POST` | `/api/channels/{id}/start` · `/stop` · `/restart` | control a channel |
| `PUT` | `/api/channels/{id}/retention` | archive retention |
| `GET` | `/api/channels/{id}/metrics?minutes=60` | bitrate history |
| `GET` | `/api/channels/{id}/archive?from=...` | DVR playlist from a moment |
| `GET` `POST` | `/api/profiles` | encoding profiles |
| WS | `/hubs/channels` | SignalR: `channelHealth`, `channelStatusChanged` |

## 🧪 Tests

```bash
dotnet test                        # domain tests
cd frontend/flux-frontend
npm test                           # frontend unit tests (Vitest)
```

## 📄 License

[MIT](LICENSE.txt) © 2026 13CyberPunk02
