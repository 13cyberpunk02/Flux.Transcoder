import { Injectable, signal } from '@angular/core';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;       // красная кнопка для удаления
  icon?: IconDefinition;
}

interface ConfirmState extends ConfirmOptions {
  resolve: (value: boolean) => void;
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly _state = signal<ConfirmState | null>(null);
  readonly state = this._state.asReadonly();

  ask(options: ConfirmOptions): Promise<boolean> {
    return new Promise<boolean>(resolve => {
      this._state.set({ ...options, resolve });
    });
  }

  confirm(): void {
    this._state()?.resolve(true);
    this._state.set(null);
  }

  cancel(): void {
    this._state()?.resolve(false);
    this._state.set(null);
  }
}
