import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { MatTooltip } from '@angular/material/tooltip';

import { SignalRService } from '../../core/realtime/signalr.service';
import { ThemeService } from '../../core/theme/theme.service';
import { TokenStore } from '../../core/auth/token.store';
import { ChannelsStore } from '../channels/channels.store';

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, MatIconButton, MatIcon,
    MatMenu, MatMenuItem, MatMenuTrigger, MatTooltip,
  ],
  template: `
    <div class="min-h-screen flex flex-col">
      <header class="sticky top-0 z-40 border-b border-outline-variant bg-surface-low">
        <div class="mx-auto w-full max-w-screen-2xl px-4 lg:px-6 h-14 flex items-center gap-6">

          <a routerLink="/channels" class="flex items-center gap-2 shrink-0">
            <span class="flex items-center justify-center w-8 h-8 rounded-lg bg-primary text-on-primary">
              <mat-icon class="!w-5 !h-5 !text-[20px]">sensors</mat-icon>
            </span>
            <span class="font-semibold tracking-tight">Flux<span class="text-primary">Transcoder</span></span>
          </a>

          <nav class="flex items-center gap-1" aria-label="Разделы">
            @for (item of nav; track item.link) {
              <a [routerLink]="item.link" routerLinkActive="!text-on-surface bg-surface-high"
                 class="flex items-center gap-2 px-3 py-1.5 rounded-full text-sm text-muted
                        hover:text-on-surface hover:bg-surface-container transition-colors">
                <span class="material-symbols-outlined" aria-hidden="true">{{ item.icon }}</span>
                <span class="hidden sm:inline">{{ item.label }}</span>
              </a>
            }
          </nav>

          <div class="ml-auto flex items-center gap-1">
            <!-- сводка: в эфире / проблемы -->
            <div class="hidden md:flex items-center gap-4 mr-3 text-sm">
              <span class="flex items-center gap-2 text-muted">
                @if (channels.runningCount() > 0) {
                  <span class="live-dot" aria-hidden="true"></span>
                } @else {
                  <span class="w-2 h-2 rounded-full bg-outline" aria-hidden="true"></span>
                }
                в эфире <b class="text-on-surface num">{{ channels.runningCount() }}</b>
              </span>
              @if (channels.problemCount() > 0) {
                <span class="flex items-center gap-1.5 text-error">
                  <span class="material-symbols-outlined" aria-hidden="true">error</span>
                  проблемы <b class="num">{{ channels.problemCount() }}</b>
                </span>
              }
            </div>

            <button matIconButton (click)="theme.toggle()"
                    [matTooltip]="theme.theme() === 'dark' ? 'Светлая тема' : 'Тёмная тема'"
                    [attr.aria-label]="theme.theme() === 'dark' ? 'Светлая тема' : 'Тёмная тема'">
              <mat-icon>{{ theme.theme() === 'dark' ? 'light_mode' : 'dark_mode' }}</mat-icon>
            </button>

            <button matIconButton [matMenuTriggerFor]="userMenu" aria-label="Меню пользователя">
              <mat-icon>account_circle</mat-icon>
            </button>
            <mat-menu #userMenu xPosition="before">
              <button mat-menu-item (click)="logout()">
                <mat-icon>logout</mat-icon>
                <span>Выйти</span>
              </button>
            </mat-menu>
          </div>
        </div>
      </header>

      <main class="flex-1 mx-auto w-full max-w-screen-2xl px-4 lg:px-6 py-6">
        <router-outlet />
      </main>
    </div>
  `,
})
export class ShellComponent implements OnInit {
  private readonly signalr = inject(SignalRService);
  private readonly tokens = inject(TokenStore);
  private readonly router = inject(Router);
  protected readonly theme = inject(ThemeService);
  protected readonly channels = inject(ChannelsStore);

  protected readonly nav = [
    { link: '/channels', label: 'Каналы', icon: 'live_tv' },
    { link: '/profiles', label: 'Профили', icon: 'tune' },
  ];

  ngOnInit(): void {
    this.signalr.start();
    // счётчики в шапке нужны на любой странице
    if (!this.channels.loaded()) this.channels.loadAll();
  }

  protected logout(): void {
    this.signalr.stop();
    this.tokens.clear();
    this.router.navigate(['/login']);
  }
}
