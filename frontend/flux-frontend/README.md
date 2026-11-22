<div align="center">

# 🖥️ Flux.Transcoder · Operator console

**Angular front end of [Flux.Transcoder](../../README.md): channels, live and DVR playback, encoding profiles.**

[🇷🇺 Русский](README.ru.md) · 🇬🇧 English

[![Angular](https://img.shields.io/badge/Angular-22-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.dev/)
[![Angular Material](https://img.shields.io/badge/Material-3-757575?style=for-the-badge&logo=materialdesign&logoColor=white)](https://material.angular.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-6-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-4-06B6D4?style=for-the-badge&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)

[![hls.js](https://img.shields.io/badge/hls.js-player-F05A28?style=flat-square)](https://github.com/video-dev/hls.js)
[![SignalR](https://img.shields.io/badge/SignalR-realtime-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/signalr/)
[![ApexCharts](https://img.shields.io/badge/ApexCharts-charts-008FFB?style=flat-square)](https://apexcharts.com/)
[![Vitest](https://img.shields.io/badge/Vitest-tests-6E9F18?style=flat-square&logo=vitest&logoColor=white)](https://vitest.dev/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](../../LICENSE.txt)

</div>

---

## ✨ Screens

- 📺 **Channels:** table with status, live bitrate, uptime, dropped frames and retention; filters, search, actions in a row.
- 🎬 **Channel:** live player, DVR from any moment (date and time, presets, click on the chart), metrics, retention, encoding profile.
- ⚙️ **Profiles:** encoding profiles with renditions and presets.
- 🌗 Dark and light themes, Russian UI.

## 🧰 Stack

Angular 22 (standalone, zoneless, signals, Signal Forms) · Angular Material 3 · Tailwind CSS 4 · hls.js · ApexCharts · SignalR · Material Symbols and Inter served locally.

## 🚀 Development

Requires Node.js 22.22+, 24.15+ or 26+ and a running API on http://localhost:5000.

```bash
npm install
npm start        # http://localhost:4200, /api and /hubs are proxied to :5000 (proxy.conf.json)
```

Live HLS is loaded from nginx; its address is set in `src/app/core/config/endpoints.ts` (`http://localhost:3088` by default).

## 🛠️ Scripts

| Command | Purpose |
|---|---|
| `npm start` | dev server with proxy |
| `npm run build` | production build to `dist/` |
| `npm test` | unit tests (Vitest) |

## 📁 Structure

```text
src/app/
├── core/       # API clients, auth, SignalR, theme, formatting, notifications
├── features/   # auth, shell, channels, player, profiles
└── shared/     # status tag, confirm dialog
```

## 📄 License

[MIT](../../LICENSE.txt) © 2026 13CyberPunk02
