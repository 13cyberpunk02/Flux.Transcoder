import { Component, inject } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faTriangleExclamation, faXmark } from '@fortawesome/free-solid-svg-icons';
import { ConfirmService } from './confirm.service';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  imports: [FontAwesomeModule],
  template: `
    @if (confirm.state(); as s) {
      <!-- затемняющий оверлей -->
      <div class="fixed inset-0 z-110 flex items-center justify-center p-4
                  bg-black/50 backdrop-blur-sm animate-[fadeIn_0.15s_ease-out]"
           (click)="confirm.cancel()">
        <div class="glass rounded-2xl p-6 w-full max-w-md animate-[popIn_0.2s_ease-out]"
             (click)="$event.stopPropagation()">
          <div class="flex items-start gap-4">
            <div [class]="iconWrapClass(s.danger)">
              <fa-icon [icon]="s.icon ?? faWarn" class="text-lg" />
            </div>
            <div class="flex-1">
              <h3 class="font-semibold text-[rgb(var(--text))] mb-1">{{ s.title }}</h3>
              <p class="text-sm text-[rgb(var(--text-muted))]">{{ s.message }}</p>
            </div>
          </div>

          <div class="flex justify-end gap-3 mt-6">
            <button (click)="confirm.cancel()"
              class="px-4 py-2 rounded-xl text-sm bg-white/5 text-[rgb(var(--text))]
                     border border-white/10 hover:bg-white/10 transition">
              {{ s.cancelLabel ?? 'Отмена' }}
            </button>
            <button (click)="confirm.confirm()"
              [class]="s.danger
                ? 'px-4 py-2 rounded-xl text-sm bg-rose-500/25 text-rose-300 border border-rose-500/40 hover:bg-rose-500/35 transition font-medium'
                : 'px-4 py-2 rounded-xl text-sm bg-[rgb(var(--accent)/0.25)] text-[rgb(var(--accent))] border border-[rgb(var(--accent)/0.4)] hover:bg-[rgb(var(--accent)/0.35)] transition font-medium'">
              {{ s.confirmLabel ?? 'Подтвердить' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
    @keyframes popIn {
      from { opacity: 0; transform: scale(0.95); }
      to   { opacity: 1; transform: scale(1); }
    }
  `],
})
export class ConfirmModalComponent {
  readonly confirm = inject(ConfirmService);
  readonly faWarn = faTriangleExclamation;

  iconWrapClass(danger?: boolean): string {
    const base = 'flex items-center justify-center w-11 h-11 rounded-full flex-shrink-0';
    return danger
      ? `${base} bg-rose-500/20 text-rose-400`
      : `${base} bg-[rgb(var(--accent)/0.2)] text-[rgb(var(--accent))]`;
  }
}
