import { Component, input, computed } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

type Tone = 'accent' | 'success' | 'warning' | 'danger' | 'neutral';

@Component({
  selector: 'ui-badge',
  standalone: true,
  imports: [FontAwesomeModule],
  template: `
    <span [class]="classes()">
      @if (icon()) { <fa-icon [icon]="icon()!" class="text-xs" /> }
      <ng-content />
    </span>
  `,
})
export class BadgeComponent {
  readonly tone = input<Tone>('neutral');
  readonly icon = input<IconDefinition>();

  readonly classes = computed(() => {
    const base = 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border';
    const tones: Record<Tone, string> = {
      accent:  'bg-[rgb(var(--accent)/0.2)] text-[rgb(var(--accent))] border-[rgb(var(--accent)/0.3)]',
      success: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30',
      warning: 'bg-amber-500/20 text-amber-300 border-amber-500/30',
      danger:  'bg-rose-500/20 text-rose-300 border-rose-500/30',
      neutral: 'bg-white/10 text-[rgb(var(--text-muted))] border-white/10',
    };
    return `${base} ${tones[this.tone()]}`;
  });
}
