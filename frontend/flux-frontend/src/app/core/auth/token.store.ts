import { Injectable, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenStore {
  private readonly _token = signal<string | null>(localStorage.getItem('jwt'));
  readonly token = this._token.asReadonly();
  readonly isAuthenticated = computed(() => this._token() !== null);

  set(token: string): void {
    localStorage.setItem('jwt', token);
    this._token.set(token);
  }
  clear(): void {
    localStorage.removeItem('jwt');
    this._token.set(null);
  }
}
