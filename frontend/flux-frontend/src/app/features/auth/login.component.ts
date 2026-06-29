import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApi } from '../../core/api/auth.api';
import { TokenStore } from '../../core/auth/token.store';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="min-h-screen flex items-center justify-center px-4">
      <div class="w-full max-w-sm rounded-2xl border border-slate-800 bg-slate-900/60 p-8 shadow-xl">
        <h1 class="text-xl font-semibold mb-1">Вход</h1>
        <p class="text-sm text-slate-400 mb-6">Панель управления трансляциями</p>

        <label class="block text-sm mb-1 text-slate-300">Логин</label>
        <input [(ngModel)]="username"
          class="w-full mb-4 px-3 py-2 rounded-md bg-slate-800 border border-slate-700
                 focus:border-indigo-500 outline-none transition" />

        <label class="block text-sm mb-1 text-slate-300">Пароль</label>
        <input [(ngModel)]="password" type="password"
          class="w-full mb-6 px-3 py-2 rounded-md bg-slate-800 border border-slate-700
                 focus:border-indigo-500 outline-none transition" />

        @if (error()) {
          <p class="text-sm text-rose-400 mb-4">{{ error() }}</p>
        }

        <button (click)="submit()" [disabled]="loading()"
          class="w-full py-2.5 rounded-md bg-indigo-600 hover:bg-indigo-500
                 disabled:opacity-50 transition font-medium">
          {{ loading() ? 'Входим…' : 'Войти' }}
        </button>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly api = inject(AuthApi);
  private readonly tokens = inject(TokenStore);
  private readonly router = inject(Router);

  username = '';
  password = '';
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.login({ username: this.username, password: this.password }).subscribe({
      next: res => { this.tokens.set(res.token); this.router.navigate(['/channels']); },
      error: () => { this.error.set('Неверный логин или пароль'); this.loading.set(false); },
    });
  }
}
