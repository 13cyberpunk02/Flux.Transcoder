import { Component, inject } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faCircleCheck, faCircleXmark, faCircleInfo, faTriangleExclamation, faXmark
} from '@fortawesome/free-solid-svg-icons';
import { ToastService, ToastType } from './toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [FontAwesomeModule],
  template: `
    <div class="fixed bottom-4 right-4 z-100 flex flex-col-reverse gap-2 w-80 max-w-[calc(100vw-2rem)]">
      @for (t of toast.toasts(); track t.id) {
        <div class="glass rounded-xl p-4 flex items-start gap-3 shadow-lg
                    animate-[slideIn_0.25s_ease-out]"
             [class]="accentBorder(t.type)">
          <fa-icon [icon]="iconFor(t.type)" [class]="iconColor(t.type)" class="mt-0.5" />
          <div class="flex-1 min-w-0">
            @if (t.title) {
              <p class="font-medium text-[rgb(var(--text))] text-sm">{{ t.title }}</p>
            }
            <p class="text-sm text-[rgb(var(--text-muted))] wrap-break-word">{{ t.message }}</p>
          </div>
          <button (click)="toast.dismiss(t.id)"
            class="text-[rgb(var(--text-muted))] hover:text-[rgb(var(--text))] transition">
            <fa-icon [icon]="faXmark" class="text-sm" />
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    @keyframes slideIn {
      from { opacity: 0; transform: translateX(1rem); }
      to   { opacity: 1; transform: translateX(0); }
    }
  `],
})
export class ToastContainerComponent {
  readonly toast = inject(ToastService);
  readonly faXmark = faXmark;

  private readonly icons = {
    success: faCircleCheck,
    error: faCircleXmark,
    info: faCircleInfo,
    warning: faTriangleExclamation,
  };

  iconFor(type: ToastType) { return this.icons[type]; }

  iconColor(type: ToastType): string {
    return {
      success: 'text-emerald-400',
      error: 'text-rose-400',
      info: 'text-[rgb(var(--accent))]',
      warning: 'text-amber-400',
    }[type];
  }

  accentBorder(type: ToastType): string {
    return {
      success: 'border-l-2 border-l-emerald-400',
      error: 'border-l-2 border-l-rose-400',
      info: 'border-l-2 border-l-[rgb(var(--accent))]',
      warning: 'border-l-2 border-l-amber-400',
    }[type];
  }
}
