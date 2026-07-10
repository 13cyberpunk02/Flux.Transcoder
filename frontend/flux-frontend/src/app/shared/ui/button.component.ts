import { Component, input, output, computed } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

type Variant = 'primary' | 'secondary' | 'danger' | 'ghost';
type Size = 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-button',
  standalone: true,
  imports: [FontAwesomeModule],
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || loading()"
      [class]="classes()"
      (click)="clicked.emit($event)">
      @if (loading()) {
        <span class="inline-block w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin"></span>
      } @else if (icon()) {
        <fa-icon [icon]="icon()!" />
      }
      @if (label()) { <span>{{ loading() && loadingLabel() ? loadingLabel() : label() }}</span> }
      <ng-content />
    </button>
  `,
})
export class ButtonComponent {
  readonly label = input<string>();
  readonly loadingLabel = input<string>();      // текст во время загрузки (напр. "Входим…")
  readonly icon = input<IconDefinition>();
  readonly variant = input<Variant>('primary');
  readonly size = input<Size>('md');
  readonly type = input<'button' | 'submit'>('button');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly full = input(false);                 // растянуть на всю ширину
  readonly clicked = output<MouseEvent>();

  readonly classes = computed(() => {
    const base = 'inline-flex items-center justify-center gap-2 rounded-xl font-medium ' +
      'transition border disabled:opacity-40 disabled:cursor-not-allowed';

    const width = this.full() ? 'w-full' : '';

    const sizes: Record<Size, string> = {
      sm: 'px-3 py-1.5 text-sm',
      md: 'px-4 py-2 text-sm',
      lg: 'px-5 py-2.5 text-base',
    };

    const variants: Record<Variant, string> = {
      primary:
        'bg-[rgb(var(--accent)/0.25)] text-[rgb(var(--accent))] ' +
        'border-[rgb(var(--accent)/0.4)] hover:bg-[rgb(var(--accent)/0.35)]',
      secondary:
        'bg-[rgb(var(--surface)/var(--surface-opacity))] text-[rgb(var(--text))] ' +
        'border-[rgb(var(--border)/var(--border-opacity))] ' +
        'hover:border-[rgb(var(--accent)/0.4)]',
      danger:
        'bg-rose-500/20 text-rose-300 border-rose-500/30 hover:bg-rose-500/30',
      ghost:
        'bg-transparent text-[rgb(var(--text-muted))] border-transparent ' +
        'hover:bg-[rgb(var(--surface)/var(--surface-opacity))]',
    };

    return `${base} ${width} ${sizes[this.size()]} ${variants[this.variant()]}`;
  });
}
