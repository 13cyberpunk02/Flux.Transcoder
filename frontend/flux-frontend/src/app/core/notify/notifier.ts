import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Короткие уведомления (MatSnackBar). */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, undefined, { duration: 3000, panelClass: 'snack-success' });
  }

  info(message: string): void {
    this.snackBar.open(message, undefined, { duration: 3000 });
  }

  /** Ошибка; если сервер вернул ProblemDetails — показываем его detail. */
  error(fallback: string, err?: unknown): void {
    const message =
      err instanceof HttpErrorResponse && typeof err.error?.detail === 'string'
        ? err.error.detail
        : fallback;
    this.snackBar.open(message, 'Закрыть', { duration: 6000, panelClass: 'snack-error' });
  }
}
