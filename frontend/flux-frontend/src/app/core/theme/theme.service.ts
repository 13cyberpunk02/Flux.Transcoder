import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'dark' | 'light';

/** CSS-класс тёмной темы — тот же селектор передан в providePrimeNG (darkModeSelector). */
export const DARK_CLASS = 'app-dark';
const STORAGE_KEY = 'theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _theme = signal<Theme>(this.resolveInitial());
  readonly theme = this._theme.asReadonly();

  constructor() {
    effect(() => {
      const t = this._theme();
      const html = document.documentElement;
      html.classList.toggle(DARK_CLASS, t === 'dark');
      html.style.colorScheme = t;
      try { localStorage.setItem(STORAGE_KEY, t); } catch { /* приватный режим */ }
    });
  }

  toggle(): void {
    this._theme.update(t => (t === 'dark' ? 'light' : 'dark'));
  }

  private resolveInitial(): Theme {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved === 'dark' || saved === 'light') return saved;
    } catch { /* приватный режим */ }

    // по умолчанию — как в ОС
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
