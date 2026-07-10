import { Component, input, computed } from '@angular/core';

@Component({
  selector: 'ui-progress',
  standalone: true,
  template: `
    <div class="w-full">
      @if (label()) {
        <div class="flex justify-between text-xs text-[rgb(var(--text-muted))] mb-1">
          <span>{{ label() }}</span>
          <span>{{ value() }}%</span>
        </div>
      }
      <div class="h-2 rounded-full bg-white/10 overflow-hidden">
        <div class="h-full rounded-full transition-all duration-300"
             [style.width.%]="clamped()"
             [class]="'bg-[rgb(var(--accent))]'"></div>
      </div>
    </div>
  `,
})
export class ProgressComponent {
  readonly value = input(0);
  readonly label = input<string>();
  readonly clamped = computed(() => Math.max(0, Math.min(100, this.value())));
}
