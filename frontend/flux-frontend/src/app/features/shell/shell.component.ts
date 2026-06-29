import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { SignalRService } from '../../core/realtime/signalr.service';
import { ChannelsStore } from '../channels/channels.store';
import { TokenStore } from '../../core/auth/token.store';
import { Router } from '@angular/router';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <div class="min-h-screen flex flex-col">
      <header class="border-b border-slate-800 bg-slate-900/60 backdrop-blur">
        <div class="mx-auto max-w-6xl px-4 h-14 flex items-center justify-between">
          <a routerLink="/channels" class="font-semibold tracking-tight text-lg">
            Flux<span class="text-indigo-400">Transcoder</span>
          </a>
          <div class="flex items-center gap-4 text-sm">
            <span class="text-slate-400">
              в эфире: <span class="text-emerald-400 font-medium">{{ store.runningCount() }}</span>
            </span>
            <button (click)="logout()"
              class="px-3 py-1.5 rounded-md bg-slate-800 hover:bg-slate-700 transition">
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

  ngOnInit(): void {
    this.signalr.start();   // real-time стартует после входа
  }

  logout(): void {
    this.tokens.clear();
    this.router.navigate(['/login']);
  }
}
