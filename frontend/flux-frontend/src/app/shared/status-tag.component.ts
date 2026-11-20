import { Component, computed, input } from '@angular/core';
import { ChannelStatus } from '../core/api/models';

interface StatusView { label: string; icon?: string; cls: string; }

/** Цвета статусов — фиксированные (зелёный/синий/жёлтый/красный/серый), одинаково читаются в обеих темах. */
const STATUS_VIEW: Record<ChannelStatus, StatusView> = {
  Running: { label: 'В эфире', cls: 'bg-green-500/15 text-green-700 dark:text-green-400' },
  Starting: { label: 'Запуск', icon: 'progress_activity', cls: 'bg-sky-500/15 text-sky-700 dark:text-sky-400' },
  Degraded: { label: 'Нестабилен', icon: 'warning', cls: 'bg-amber-500/15 text-amber-700 dark:text-amber-400' },
  Failed: { label: 'Ошибка', icon: 'error', cls: 'bg-red-500/15 text-red-700 dark:text-red-400' },
  Stopped: { label: 'Остановлен', icon: 'stop_circle', cls: 'bg-zinc-500/15 text-zinc-700 dark:text-zinc-300' },
  Idle: { label: 'Не запущен', icon: 'radio_button_unchecked', cls: 'bg-zinc-500/15 text-zinc-700 dark:text-zinc-300' },
};

/** Статус канала: цвет + текст + иконка (не только цвет — для доступности). */
@Component({
  selector: 'app-status-tag',
  template: `
    <span class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium whitespace-nowrap"
          [class]="view().cls">
      @if (status() === 'Running') {
        <span class="live-dot" aria-hidden="true"></span>
      } @else if (view().icon) {
        <span class="material-symbols-outlined !text-[15px]" [class.animate-spin]="status() === 'Starting'"
              aria-hidden="true">{{ view().icon }}</span>
      }
      {{ view().label }}
    </span>
  `,
})
export class StatusTagComponent {
  readonly status = input.required<ChannelStatus>();
  protected readonly view = computed(() => STATUS_VIEW[this.status()] ?? STATUS_VIEW.Idle);
}
