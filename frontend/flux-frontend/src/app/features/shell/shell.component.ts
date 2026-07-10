import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { SignalRService } from '../../core/realtime/signalr.service';
import { ChannelsStore } from '../channels/channels.store';
import { TokenStore } from '../../core/auth/token.store';
import { Router } from '@angular/router';
import {ThemeService} from '../../core/theme/theme.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <div class="min-h-screen flex flex-col">
      <header class="glass sticky top-0 z-50 border-x-0 border-t-0 rounded-none">
        <div class="mx-auto max-w-6xl px-4 h-14 flex items-center justify-between">
          <a routerLink="/channels" class="font-semibold tracking-tight text-lg">
            Flux<span class="text-indigo-400">Transcoder</span>
          </a>
          <div class="flex items-center gap-4 text-sm">
            <a routerLink="/channels" class="text-slate-300 hover:text-white transition">Каналы</a>
            <a routerLink="/profiles" class="text-slate-300 hover:text-white transition">Профили</a>
            <span class="text-slate-400">
          в эфире: <span class="text-emerald-400 font-medium">{{ store.runningCount() }}</span>
        </span>
            <button (click)="themeService.toggle()"
                    class="px-3 py-1.5 rounded-lg bg-white/10 hover:bg-white/20 border border-white/10 transition">
              {{ themeService.theme() === 'dark' ? '☀ Светлая' : '🌙 Тёмная' }}
            </button>
            <button (click)="logout()"
                    class="px-3 py-1.5 rounded-lg bg-white/10 hover:bg-white/20 border border-white/10 transition">
              Выйти
            </button>
          </div>
        </div>
      </header>
      <main class="flex-1 mx-auto w-full max-w-6xl px-4 py-6">
        <router-outlet />
      </main>
    </div>
  `,
})
export class ShellComponent implements OnInit {
  readonly store = inject(ChannelsStore);
  private readonly signalr = inject(SignalRService);
  private readonly tokens = inject(TokenStore);
  private readonly router = inject(Router);
  readonly themeService = inject(ThemeService);

  ngOnInit(): void {
    this.signalr.start();   // real-time стартует после входа
  }

  logout(): void {
    this.tokens.clear();
    this.router.navigate(['/login']);
  }
}
