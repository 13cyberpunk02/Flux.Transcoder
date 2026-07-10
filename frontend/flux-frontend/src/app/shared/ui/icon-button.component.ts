import { Component, input, output, computed } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

type Variant = 'surface' | 'ghost' | 'accent' | 'danger';
type Size = 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-icon-button',
  standalone: true,
  imports: [FontAwesomeModule],
  template: `
    <button
      [disabled]="disabled()"
      [title]="title()"
      [class]="classes()"
      (click)="clicked.emit($event)">
      <fa-icon [icon]="icon()" [class]="iconSize()" />
    </button>
  `,
})
export class IconButtonComponent {
  readonly icon = input.required<IconDefinition>();
  readonly variant = input<Variant>('surface');
  readonly size = input<Size>('md');
  readonly title = input('');
  readonly disabled = input(false);
  readonly clicked = output<MouseEvent>();

  readonly iconSize = computed(() => ({
    sm: 'text-xs', md: 'text-sm', lg: 'text-base',
  }[this.size()]));

  readonly classes = computed(() => {
    const base = 'inline-flex items-center justify-center rounded-xl border transition ' +
      'disabled:opacity-40 disabled:cursor-not-allowed';

    const sizes: Record<Size, string> = {
      sm: 'w-8 h-8',
      md: 'w-9 h-9',
      lg: 'w-11 h-11',
    };

    const variants: Record<Variant, string> = {
      surface:
        'bg-[rgb(var(--surface)/var(--surface-opacity))] ' +
        'border-[rgb(var(--border)/var(--border-opacity))] ' +
        'text-[rgb(var(--text-muted))] hover:text-[rgb(var(--text))] ' +
        'hover:border-[rgb(var(--accent)/0.4)]',
      ghost:
        'bg-transparent border-transparent text-[rgb(var(--text-muted))] ' +
        'hover:text-[rgb(var(--text))] hover:bg-[rgb(var(--surface)/var(--surface-opacity))]',
      accent:
        'bg-[rgb(var(--accent)/0.2)] border-[rgb(var(--accent)/0.35)] ' +
        'text-[rgb(var(--accent))] hover:bg-[rgb(var(--accent)/0.3)]',
      danger:
        'bg-rose-500/20 border-rose-500/30 text-rose-300 hover:bg-rose-500/30',
    };

    return `${base} ${sizes[this.size()]} ${variants[this.variant()]}`;
  });
}
