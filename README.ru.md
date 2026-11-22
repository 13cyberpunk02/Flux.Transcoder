<div align="center">

# 🎬 Flux.Transcoder

**Транскодирование прямых эфиров и DVR: на входе SRT / RTMP / UDP, на выходе HLS эфир и архив.**

🇷🇺 Русский · [🇬🇧 English](README.md)

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-22-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.dev/)
[![Angular Material](https://img.shields.io/badge/Material-3-757575?style=for-the-badge&logo=materialdesign&logoColor=white)](https://material.angular.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)

[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![TimescaleDB](https://img.shields.io/badge/TimescaleDB-метрики-FDB515?style=for-the-badge&logo=timescale&logoColor=black)](https://www.timescale.com/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-брокер-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![FFmpeg](https://img.shields.io/badge/FFmpeg-HLS-007808?style=for-the-badge&logo=ffmpeg&logoColor=white)](https://ffmpeg.org/)
[![Docker](https://img.shields.io/badge/Docker-compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://docs.docker.com/compose/)

[![Wolverine](https://img.shields.io/badge/Wolverine-6.15-6E4AA8?style=flat-square)](https://wolverinefx.net/)
[![SignalR](https://img.shields.io/badge/SignalR-realtime-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/signalr/)
[![hls.js](https://img.shields.io/badge/hls.js-плеер-F05A28?style=flat-square)](https://github.com/video-dev/hls.js)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-4-06B6D4?style=flat-square&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![Лицензия: MIT](https://img.shields.io/badge/Лицензия-MIT-yellow?style=flat-square)](LICENSE.txt)

</div>

---

## 📖 О проекте

Flux.Transcoder принимает прямые эфиры из SRT, RTMP или UDP, кодирует их через FFmpeg (x264, NVENC или QSV) и одним энкодом пишет два HLS-выхода: короткое окно прямого эфира и архив с разбивкой по времени на общем хранилище (NAS). Пульт оператора показывает все каналы с живым битрейтом, аптаймом и потерянными кадрами, проигрывает эфир или любой момент архива и управляет каналами: запуск, остановка, перезапуск.

## ✨ Возможности

- 📡 **Источники:** SRT, RTMP и UDP (MPEG-TS).
- ⚙️ **Профили кодирования:** разрешение, битрейт и кодек (x264 на CPU, NVENC, QSV); пресеты 4K, 1080p, 720p, 480p.
- 🎞️ **Один энкод, два выхода:** FFmpeg `tee` пишет эфир и архив одновременно.
- ⏪ **DVR:** просмотр с любого момента по дате и времени, быстрые пресеты или клик по графику битрейта.
- 🗑️ **Хранение архива:** для каждого канала от 1 минуты до 14 дней; старые сегменты удаляет отдельный чистильщик.
- 📊 **Метрики:** битрейт, аптайм и потерянные кадры в реальном времени (SignalR) и история в TimescaleDB.
- 🛡️ **Надёжность:**
  - воркер приводит процессы к состоянию в БД, поэтому каналы поднимаются после его перезапуска;
  - команды идут через транзакционный outbox и переживают падение брокера или API;
  - автоматический перезапуск с экспоненциальной паузой, после 8 попыток статус `Failed`;
  - FFmpeg не переживает воркер (Job Object на Windows, `setpriv --pdeathsig` на Linux);
  - резервный воркер подхватывает каналы, если активный упал.
- 🖥️ **Пульт оператора:** Angular Material 3, тёмная и светлая темы, интерфейс на русском.
- 🔐 **Авторизация:** JWT, пользователи в базе данных.

## 🏗️ Архитектура

```mermaid
flowchart LR
    SRC["📡 Источники<br/>SRT / RTMP / UDP"] --> FF

    subgraph Worker["🛠️ Flux.Transcoder.Worker"]
        RC["ChannelReconciler"] --> FF["FFmpeg<br/>(1 энкод, tee)"]
    end

    FF -->|"HLS эфир + архив"| NAS[("💾 Хранилище / NAS")]
    NAS --> NGINX["nginx<br/>:3088"]
    NAS --> CL["🧹 Cleaner<br/>хранение"]

    UI["🖥️ Angular UI<br/>:4200"] -->|"REST /api"| API["🌐 Flux.Transcoder.Api<br/>:5000"]
    API -->|"SignalR /hubs"| UI
    UI -->|"HLS"| NGINX

    API -->|"команды (outbox)"| MQ[["🐇 RabbitMQ"]]
    MQ --> RC
    Worker -->|"отчёты о здоровье"| MQ
    MQ --> API

    API --> DB[("🐘 PostgreSQL<br/>+ TimescaleDB")]
    RC --> DB
    CL --> DB
```

Источник истины здесь база данных: статус канала и его `run_revision` определяют, какой процесс FFmpeg должен работать. Сообщения из очереди лишь ускоряют сверку, поэтому потерянное, повторное или пришедшее не по порядку сообщение ничего не ломает.

## 🧰 Технологии

| Область | Технологии |
|---|---|
| Бэкенд | .NET 10, ASP.NET Core, Wolverine (медиатор, HTTP-эндпоинты, сообщения, outbox), FluentValidation, Ardalis.Specification |
| Данные | PostgreSQL 16, TimescaleDB (hypertable, continuous aggregates), Entity Framework Core 10 |
| Сообщения | RabbitMQ |
| Видео | FFmpeg, HLS, nginx |
| Фронтенд | Angular 22, Angular Material 3, Signal Forms, Tailwind CSS 4, hls.js, ApexCharts, SignalR |
| Логи | Serilog |
| Тесты | xUnit, FluentAssertions, Vitest |

## 📁 Структура проекта

```text
Flux.Transcoder/
├── deploy/                        # docker-compose (PostgreSQL, RabbitMQ, nginx), nginx.conf
├── frontend/flux-frontend/        # пульт оператора на Angular
├── src/
│   ├── api/Flux.Transcoder.Api                # REST API, SignalR-хаб, DVR-плейлисты, метрики
│   ├── application/
│   │   ├── Flux.Transcoder.Application        # команды, запросы, валидаторы, спецификации
│   │   └── Flux.Transcoder.Contracts          # сообщения между API и воркером
│   ├── domain/Flux.Transcoder.Domain          # каналы, профили кодирования, пользователи
│   ├── infrastructure/
│   │   ├── Flux.Transcoder.Ffmpeg             # аргументы FFmpeg, надзор за процессами, перезапуски
│   │   └── Flux.Transcoder.Infrastructure.Database  # EF Core, миграции, JWT, хеширование паролей
│   └── worker/
│       ├── Flux.Transcoder.Worker             # запуск FFmpeg, отчёты о здоровье, сверка с БД
│       └── Flux.Transcoder.Cleaner            # удаление архива по сроку хранения
└── tests/Flux.Transcoder.Domain.Tests
```

## 🚀 Быстрый старт

### Требования

| Инструмент | Версия |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10 |
| [Node.js](https://nodejs.org/) | 22.22+, 24.15+ или 26+ |
| [FFmpeg](https://ffmpeg.org/download.html) | в `PATH` (или задать `Ffmpeg:BinaryPath`); для NVENC / QSV нужна подходящая видеокарта |
| [Docker](https://www.docker.com/) | для PostgreSQL, RabbitMQ и nginx |

### 1. Инфраструктура

Укажите папку хранилища в `deploy/docker-compose.yml` (том сервиса `nginx`, по умолчанию `D:/fluxcast/storage`), затем:

```bash
cd deploy
docker compose up -d
```

### 2. Бэкенд

```bash
# API: при старте применяет миграции и создаёт первого пользователя
dotnet run --project src/api/Flux.Transcoder.Api

# Воркер: запускает FFmpeg для активных каналов
dotnet run --project src/worker/Flux.Transcoder.Worker

# Чистильщик: удаляет сегменты архива старше срока хранения
dotnet run --project src/worker/Flux.Transcoder.Cleaner
```

### 3. Фронтенд

```bash
cd frontend/flux-frontend
npm install
npm start
```

Откройте http://localhost:4200 и войдите как `admin` / `admin`.

> [!WARNING]
> В репозитории лежат данные для разработки: пользователь `admin` / `admin`, пароли базы данных и RabbitMQ, ключ подписи JWT в `appsettings.json`. Замените их все перед любым использованием вне локальной машины.

### Полезные адреса

| Сервис | Адрес |
|---|---|
| Пульт оператора | http://localhost:4200 |
| API | http://localhost:5000 |
| Документация API (Scalar, Development) | http://localhost:5000/scalar |
| HLS (nginx) | http://localhost:3088 |
| Управление RabbitMQ | http://localhost:15672 |

## ⚙️ Настройка

Все настройки лежат в `appsettings.json` каждого сервиса и переопределяются переменными окружения (`Раздел__Ключ`, например `Ffmpeg__SegmentRoot`).

| Ключ | Сервис | По умолчанию | Назначение |
|---|---|---|---|
| `ConnectionStrings:flux_transcoder_db` | API, Worker, Cleaner | локальный PostgreSQL | база данных |
| `ConnectionStrings:RabbitMq` | API, Worker | `amqp://...@localhost:5672` | брокер сообщений |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey` | API | значения для разработки | JWT-токены |
| `Archive:SegmentRoot` | API | `D:\fluxcast\storage` | корень хранилища для DVR-плейлистов |
| `Archive:NginxBaseUrl` | API | `http://localhost:3088` | публичный адрес HLS |
| `Ffmpeg:BinaryPath` | Worker | `ffmpeg` | исполняемый файл FFmpeg |
| `Ffmpeg:SegmentRoot` | Worker | `D:\fluxcast\storage` | куда пишется HLS |
| `Ffmpeg:HlsTimeSeconds` | Worker | `4` | длительность сегмента |
| `Ffmpeg:LiveListSize` | Worker | `6` | сегментов в плейлисте эфира |
| `Ffmpeg:StartupTimeoutSeconds` | Worker | `30` | сколько ждать первый сегмент до перезапуска |
| `Cleaner:ArchiveRoot` | Cleaner | `D:\fluxcast\storage` | корень хранилища |
| `Cleaner:IntervalMinutes` | Cleaner | `1` | интервал чистки |

Корень хранилища должен совпадать у воркера, API, чистильщика и nginx.

## 🔌 API

| Метод | Маршрут | Назначение |
|---|---|---|
| `POST` | `/api/auth/login` | получить JWT |
| `GET` `POST` | `/api/channels` | список, создание каналов |
| `GET` `DELETE` | `/api/channels/{id}` | получить, удалить канал |
| `POST` | `/api/channels/{id}/start` · `/stop` · `/restart` | управление каналом |
| `PUT` | `/api/channels/{id}/retention` | срок хранения архива |
| `GET` | `/api/channels/{id}/metrics?minutes=60` | история битрейта |
| `GET` | `/api/channels/{id}/archive?from=...` | DVR-плейлист с указанного момента |
| `GET` `POST` | `/api/profiles` | профили кодирования |
| WS | `/hubs/channels` | SignalR: `channelHealth`, `channelStatusChanged` |

## 🧪 Тесты

```bash
dotnet test                        # доменные тесты
cd frontend/flux-frontend
npm test                           # модульные тесты фронтенда (Vitest)
```

## 📄 Лицензия

[MIT](LICENSE.txt) © 2026 13CyberPunk02
