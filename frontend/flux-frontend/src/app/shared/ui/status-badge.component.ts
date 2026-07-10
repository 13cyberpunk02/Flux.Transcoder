import { Component, input, computed } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faCirclePlay, faCircleStop, faCirclePause,
  faTriangleExclamation, faCircleXmark, faSpinner
} from '@fortawesome/free-solid-svg-icons';

type Status = 'Idle' | 'Starting' | 'Running' | 'Degraded' | 'Failed' | 'Stopped';

@Component({
  selector: 'ui-status-badge',
  standalone: true,
  imports: [FontAwesomeModule],
  template: `
    <span [class]="wrapClass()">
      <!-- пульсирующая точка для живых состояний -->
      @if (isLive()) {
        <span class="relative flex h-2 w-2">
          <span class="animate-ping absolute inline-flex h-full w-full rounded-full opacity-75"
                [class]="dotClass()"></span>
          <span class="relative inline-flex rounded-full h-2 w-2" [class]="dotClass()"></span>
        </span>
      } @else {
        <fa-icon [icon]="config().icon" class="text-xs" />
      }
      <span>{{ label() }}</span>
    </span>
  `,
})
export class StatusBadgeComponent {
  readonly status = input.required<Status>();

  private readonly configs = {
    Idle:     { icon: faCirclePause,        color: 'slate',   label: 'Ожидание' },
    Starting: { icon: faSpinner,            color: 'amber',   label: 'Запуск' },
    Running:  { icon: faCirclePlay,         color: 'emerald', label: 'В эфире' },
    Degraded: { icon: faTriangleExclamation,color: 'orange',  label: 'Проблемы' },
    Failed:   { icon: faCircleXmark,        color: 'rose',    label: 'Ошибка' },
    Stopped:  { icon: faCircleStop,         color: 'slate',   label: 'Остановлен' },
  } as const;

  readonly config = computed(() => this.configs[this.status()]);
  readonly label = computed(() => this.config().label);
  readonly isLive = computed(() =>
    this.status() === 'Running' || this.status() === 'Starting');

  readonly wrapClass = computed(() => {
    const c = this.config().color;
    const map: Record<string, string> = {
      emerald: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30',
      amber:   'bg-amber-500/20 text-amber-300 border-amber-500/30',
      orange:  'bg-orange-500/20 text-orange-300 border-orange-500/30',
      rose:    'bg-rose-500/20 text-rose-300 border-rose-500/30',
      slate:   'bg-white/10 text-[rgb(var(--text-muted))] border-white/10',
    };
    return `inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-sm font-medium border ${map[c]}`;
  });

  readonly dotClass = computed(() => {
    const c = this.config().color;
    return c === 'emerald' ? 'bg-emerald-400' : 'bg-amber-400';
  });
}
