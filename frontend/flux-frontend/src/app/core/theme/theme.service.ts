import { Injectable, signal, effect } from '@angular/core';

type Theme = 'dark' | 'light';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _theme = signal<Theme>(this.resolveInitial());
  readonly theme = this._theme.asReadonly();

  constructor() {
    effect(() => {
      const t = this._theme();
      const html = document.documentElement;
      html.classList.remove('dark', 'light');
      html.classList.add(t);
      localStorage.setItem('theme', t);
    });
  }

  toggle(): void {
    this._theme.set(this._theme() === 'dark' ? 'light' : 'dark');
  }

  set(theme: Theme): void {
    this._theme.set(theme);
  }

  private resolveInitial(): Theme {
    const saved = localStorage.getItem('theme');
    if (saved === 'dark' || saved === 'light') return saved;

    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}
