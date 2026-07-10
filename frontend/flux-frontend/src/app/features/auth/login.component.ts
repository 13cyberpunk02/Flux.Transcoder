import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faTowerBroadcast, faUser, faLock, faRightToBracket, faSun, faMoon,
} from '@fortawesome/free-solid-svg-icons';

import { InputComponent } from '../../shared/ui/input.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { AuthApi } from '../../core/api/auth.api';
import { TokenStore } from '../../core/auth/token.store';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FontAwesomeModule, InputComponent, ButtonComponent],
  template: `
    <!-- переключатель темы -->
    <button (click)="theme.toggle()"
      class="fixed top-4 right-4 z-10 flex items-center justify-center w-10 h-10 rounded-xl
             glass text-[rgb(var(--text-muted))] hover:text-[rgb(var(--text))] transition">
      <fa-icon [icon]="theme.theme() === 'dark' ? faSun : faMoon" />
    </button>

    <div class="min-h-screen flex items-center justify-center px-4">
      <div class="w-full max-w-sm">

        <div class="flex flex-col items-center mb-8">
          <div class="flex items-center justify-center w-16 h-16 rounded-2xl mb-4
                      bg-[rgb(var(--accent)/0.2)] text-[rgb(var(--accent))]
                      shadow-lg shadow-[rgb(var(--accent)/0.15)]">
            <fa-icon [icon]="faTowerBroadcast" class="text-2xl" />
          </div>
          <h1 class="text-2xl font-semibold text-[rgb(var(--text))]">
            Flux<span class="text-[rgb(var(--accent))]">Transcoder</span>
          </h1>
          <p class="text-sm text-[rgb(var(--text-muted))] mt-1">
            Панель управления трансляциями
          </p>
        </div>

        <!-- ФОРМА -->
        <div class="glass rounded-2xl p-7">
          <div class="flex flex-col gap-4">

            <ui-input
              label="Логин" [labelIcon]="faUser"
              autocomplete="username"
              [(value)]="username"
              (enter)="submit()" />

            <ui-input
              label="Пароль" [labelIcon]="faLock" type="password"
              autocomplete="current-password"
              [(value)]="password"
              (enter)="submit()" />

            @if (error()) {
              <div class="flex items-center gap-2 px-3 py-2 rounded-lg
                          bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm">
                {{ error() }}
              </div>
            }

            <ui-button
              class="mt-1 block"
              label="Войти" loadingLabel="Входим…"
              [icon]="faRightToBracket"
              variant="primary" [full]="true"
              [loading]="loading()"
              (clicked)="submit()" />
          </div>
        </div>

        <p class="text-center text-xs text-[rgb(var(--text-muted))] mt-6">
          Flux Transcoder · система транскодирования и DVR
        </p>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly api = inject(AuthApi);
  private readonly tokens = inject(TokenStore);
  private readonly router = inject(Router);
  readonly theme = inject(ThemeService);

  readonly faTowerBroadcast = faTowerBroadcast;
  readonly faUser = faUser;
  readonly faLock = faLock;
  readonly faRightToBracket = faRightToBracket;
  readonly faSun = faSun;
  readonly faMoon = faMoon;

  readonly username = signal('');
  readonly password = signal('');
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    if (!this.username().trim() || !this.password()) {
      this.error.set('Введите логин и пароль');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.api.login({ username: this.username(), password: this.password() }).subscribe({
      next: res => {
        this.tokens.set(res.token);
        this.router.navigate(['/channels']);
      },
      error: () => {
        this.error.set('Неверный логин или пароль');
        this.loading.set(false);
      },
    });
  }
}
