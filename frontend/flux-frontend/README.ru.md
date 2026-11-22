<div align="center">

# 🖥️ Flux.Transcoder · Пульт оператора

**Фронтенд [Flux.Transcoder](../../README.ru.md) на Angular: каналы, прямой эфир и DVR, профили кодирования.**

🇷🇺 Русский · [🇬🇧 English](README.md)

[![Angular](https://img.shields.io/badge/Angular-22-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.dev/)
[![Angular Material](https://img.shields.io/badge/Material-3-757575?style=for-the-badge&logo=materialdesign&logoColor=white)](https://material.angular.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-4-06B6D4?style=for-the-badge&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)

[![hls.js](https://img.shields.io/badge/hls.js-плеер-F05A28?style=flat-square)](https://github.com/video-dev/hls.js)
[![SignalR](https://img.shields.io/badge/SignalR-realtime-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/signalr/)
[![ApexCharts](https://img.shields.io/badge/ApexCharts-графики-008FFB?style=flat-square)](https://apexcharts.com/)
[![Vitest](https://img.shields.io/badge/Vitest-тесты-6E9F18?style=flat-square&logo=vitest&logoColor=white)](https://vitest.dev/)
[![Лицензия: MIT](https://img.shields.io/badge/Лицензия-MIT-yellow?style=flat-square)](../../LICENSE.txt)

</div>

---

## ✨ Экраны

- 📺 **Каналы:** таблица со статусом, живым битрейтом, аптаймом, потерянными кадрами и сроком хранения; фильтры, поиск, действия прямо в строке.
- 🎬 **Канал:** плеер эфира, DVR с любого момента (дата и время, пресеты, клик по графику), метрики, хранение архива, профиль кодирования.
- ⚙️ **Профили:** профили кодирования с рендишенами и пресетами.
- 🌗 Тёмная и светлая темы, интерфейс на русском.

## 🧰 Стек

Angular 22 (standalone, zoneless, signals, Signal Forms) · Angular Material 3 · Tailwind CSS 4 · hls.js · ApexCharts · SignalR · Material Symbols и Inter хранятся локально.

## 🚀 Разработка

Нужны Node.js 22.22+, 24.15+ или 26+ и запущенный API на http://localhost:5000.

```bash
npm install
npm start        # http://localhost:4200, /api и /hubs проксируются на :5000 (proxy.conf.json)
```

Прямой эфир HLS загружается с nginx; адрес задан в `src/app/core/config/endpoints.ts` (по умолчанию `http://localhost:3088`).

## 🛠️ Команды

| Команда | Назначение |
|---|---|
| `npm start` | dev-сервер с прокси |
| `npm run build` | production-сборка в `dist/` |
| `npm test` | модульные тесты (Vitest) |

## 📁 Структура

```text
src/app/
├── core/       # клиенты API, авторизация, SignalR, тема, форматирование, уведомления
├── features/   # вход, каркас, каналы, плеер, профили
└── shared/     # тег статуса, диалог подтверждения
```

## 📄 Лицензия

[MIT](../../LICENSE.txt) © 2026 13CyberPunk02
