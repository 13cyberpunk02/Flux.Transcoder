/** Форматирование метрик для интерфейса (чистые функции + тонкие пайпы рядом). */

const nf1 = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 1 });
const nf0 = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 0 });

/** 850 → «850 кбит/с», 4200 → «4,2 Мбит/с». */
export function formatBitrate(kbps: number | null | undefined): string {
  if (kbps == null || !Number.isFinite(kbps) || kbps <= 0) return '—';
  return kbps >= 1000 ? `${nf1.format(kbps / 1000)} Мбит/с` : `${nf0.format(kbps)} кбит/с`;
}

/** 12345 → «3 ч 25 мин», 90061 → «1 д 1 ч». */
export function formatDuration(seconds: number | null | undefined): string {
  if (seconds == null || !Number.isFinite(seconds) || seconds < 0) return '—';
  const s = Math.floor(seconds);
  const d = Math.floor(s / 86400);
  const h = Math.floor((s % 86400) / 3600);
  const m = Math.floor((s % 3600) / 60);
  if (d > 0) return h > 0 ? `${d} д ${h} ч` : `${d} д`;
  if (h > 0) return m > 0 ? `${h} ч ${m} мин` : `${h} ч`;
  if (m > 0) return `${m} мин`;
  return `${s} с`;
}

/** Время хранения архива в минутах → «7 дн.», «6 ч», «45 мин». */
export function formatRetention(minutes: number | null | undefined): string {
  if (minutes == null || minutes <= 0) return '—';
  if (minutes % 1440 === 0) return `${minutes / 1440} дн.`;
  if (minutes % 60 === 0) return `${minutes / 60} ч`;
  return `${minutes} мин`;
}

/** Локальное ISO со смещением (2026-07-02T23:44:00+03:00) — сегменты архива названы по локальному времени. */
export function toLocalIso(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  const offset = -date.getTimezoneOffset();
  const sign = offset >= 0 ? '+' : '-';
  const oh = pad(Math.floor(Math.abs(offset) / 60));
  const om = pad(Math.abs(offset) % 60);
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}${sign}${oh}:${om}`
  );
}
