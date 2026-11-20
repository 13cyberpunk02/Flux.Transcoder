import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

export interface ConfirmData {
  title: string;
  message: string;
  confirmLabel: string;
  icon?: string;
  danger?: boolean;
}

/** Подтверждение действия. Закрывается с true/undefined. */
@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogModule, MatButton, MatIcon],
  template: `
    <h2 mat-dialog-title class="flex items-center gap-2">
      @if (data.icon) {
        <mat-icon [class.text-error]="data.danger">{{ data.icon }}</mat-icon>
      }
      {{ data.title }}
    </h2>
    <mat-dialog-content>
      <p class="text-muted">{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button matButton cdkFocusInitial [mat-dialog-close]="undefined">Отмена</button>
      <button matButton="filled" [mat-dialog-close]="true" [class.danger-button]="data.danger">
        {{ data.confirmLabel }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialogComponent {
  protected readonly data = inject<ConfirmData>(MAT_DIALOG_DATA);
}
