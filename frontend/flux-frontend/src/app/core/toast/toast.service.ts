import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
  title?: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();
  private counter = 0;

  private show(type: ToastType, message: string, title?: string, durationMs = 4000): void {
    const id = ++this.counter;
    this._toasts.update(list => [...list, { id, type, message, title }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }

  success(message: string, title?: string) { this.show('success', message, title); }
  error(message: string, title?: string)   { this.show('error', message, title, 6000); }
  info(message: string, title?: string)    { this.show('info', message, title); }
  warning(message: string, title?: string) { this.show('warning', message, title); }

  dismiss(id: number): void {
    this._toasts.update(list => list.filter(t => t.id !== id));
  }
}
