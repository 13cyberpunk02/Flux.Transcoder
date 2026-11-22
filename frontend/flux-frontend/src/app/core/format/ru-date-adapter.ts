import { Injectable } from '@angular/core';
import { NativeDateAdapter } from '@angular/material/core';

// дд.мм.гггг, также через / или -; год только из 4 цифр, чтобы недописанный ввод не стал датой
const RU_DATE = /^(\d{1,2})[./-](\d{1,2})[./-](\d{4})$/;

/**
 * NativeDateAdapter разбирает ввод через Date.parse, то есть как мм.дд.гггг:
 * 05.11.2026 становилось 11 мая, а 21.11.2026 — ошибкой.
 */
@Injectable()
export class RuDateAdapter extends NativeDateAdapter {
  override parse(value: unknown, parseFormat?: unknown): Date | null {
    if (typeof value !== 'string') return super.parse(value, parseFormat);

    const text = value.trim();
    if (!text) return null;

    const match = RU_DATE.exec(text);
    if (!match) return this.invalid();

    const [day, month, year] = match.slice(1).map(Number);
    const date = new Date(year, month - 1, day);
    // Date молча переносит 31.11 на 01.12 — такой ввод отклоняем
    const exists = date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
    return exists ? date : this.invalid();
  }
}
