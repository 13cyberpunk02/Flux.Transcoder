import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { form, FormField, required, submit } from '@angular/forms/signals';
import { MatFormField, MatLabel, MatError, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

import { AuthApi } from '../../core/api/auth.api';
import { TokenStore } from '../../core/auth/token.store';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-login',
  imports: [
    FormField, MatFormField, MatLabel, MatError, MatSuffix, MatInput,
    MatButton, MatIconButton, MatIcon, MatProgressSpinner,
  ],
  template: `
    <button matIconButton class="!fixed top-4 right-4" (click)="theme.toggle()" aria-label="Сменить тему">
      <mat-icon>{{ theme.theme() === 'dark' ? 'light_mode' : 'dark_mode' }}</mat-icon>
    </button>

    <div class="min-h-screen flex items-center justify-center px-4">
      <div class="w-full max-w-sm">
        <div class="flex flex-col items-center mb-6">
          <span class="flex items-center justify-center w-12 h-12 rounded-xl bg-primary text-on-primary mb-3">
            <mat-icon>sensors</mat-icon>
          </span>
          <h1 class="text-xl font-semibold">Flux<span class="text-primary">Transcoder</span></h1>
          <p class="text-sm text-muted mt-1">Панель управления трансляциями</p>
        </div>

        <form (submit)="onSubmit($event)" novalidate class="panel flex flex-col gap-3 p-6">
          <mat-form-field>
            <mat-label>Логин</mat-label>
            <input matInput autocomplete="username" [formField]="loginForm.username" />
            @if (loginForm.username().invalid()) {
              <mat-error>{{ loginForm.username().errors()[0]?.message }}</mat-error>
            }
          </mat-form-field>

          <mat-form-field>
            <mat-label>Пароль</mat-label>
            <input matInput [type]="showPassword() ? 'text' : 'password'" autocomplete="current-password"
                   [formField]="loginForm.password" />
            <button matIconButton matSuffix type="button" (click)="showPassword.update(v => !v)"
                    [attr.aria-label]="showPassword() ? 'Скрыть пароль' : 'Показать пароль'">
              <mat-icon>{{ showPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
            @if (loginForm.password().invalid()) {
              <mat-error>{{ loginForm.password().errors()[0]?.message }}</mat-error>
            }
          </mat-form-field>

          @if (error()) {
            <div role="alert" class="flex items-center gap-2 rounded-lg px-3 py-2 text-sm bg-error-container text-on-error-container">
              <span class="material-symbols-outlined" aria-hidden="true">error</span>{{ error() }}
            </div>
          }

          <button matButton="filled" type="submit" class="!h-11" [disabled]="loginForm().submitting()">
            @if (loginForm().submitting()) {
              <mat-progress-spinner mode="indeterminate" diameter="18" class="mr-2" />
            }
            Войти
          </button>
        </form>

        <p class="text-center text-xs text-muted mt-6">Flux Transcoder · транскодирование и DVR</p>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly api = inject(AuthApi);
  private readonly tokens = inject(TokenStore);
  private readonly router = inject(Router);
  protected readonly theme = inject(ThemeService);

  private readonly model = signal({ username: '', password: '' });
  protected readonly loginForm = form(this.model, p => {
    required(p.username, { message: 'Введите логин' });
    required(p.password, { message: 'Введите пароль' });
  });

  protected readonly showPassword = signal(false);
  protected readonly error = signal<string | undefined>(undefined);

  onSubmit(event: Event): void {
    event.preventDefault();
    this.error.set(undefined);

    submit(this.loginForm, async () => {
      try {
        const res = await firstValueFrom(this.api.login(this.model()));
        this.tokens.set(res.token);
        await this.router.navigate(['/channels']);
      } catch (err) {
        const status = err instanceof HttpErrorResponse ? err.status : 0;
        this.error.set(
          status === 401 ? 'Неверный логин или пароль'
          : status === 429 ? 'Слишком много попыток входа. Подождите минуту и попробуйте снова'
          : 'Сервер недоступен, попробуйте позже');
      }
    });
  }
}
