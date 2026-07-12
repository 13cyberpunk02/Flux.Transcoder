import { Component, input, model, output, computed } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

type Size = 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-input',
  standalone: true,
  imports: [FontAwesomeModule],
  template: `
    <div class="w-full">
      @if (label()) {
        <label class="flex items-center gap-1.5 text-sm mb-1.5 text-[rgb(var(--text-muted))]">
          @if (labelIcon()) { <fa-icon [icon]="labelIcon()!" class="text-xs" /> }
          {{ label() }}
        </label>
      }
      <div class="relative">
        @if (icon()) {
          <fa-icon [icon]="icon()!"
                   class="absolute left-3 top-1/2 -translate-y-1/2 text-[rgb(var(--text-muted))] text-sm" />
        }
        <input
          [type]="type()"
          [placeholder]="placeholder()"
          [value]="value()"
          [attr.autocomplete]="autocomplete()"
          (input)="value.set($any($event.target).value)"
          (keyup.enter)="enter.emit()"
          [class]="inputClass()" />
      </div>
    </div>
  `,
})
export class InputComponent {
  readonly label = input<string>();
  readonly labelIcon = input<IconDefinition>();
  readonly icon = input<IconDefinition>();
  readonly placeholder = input('');
  readonly type = input('text');
  readonly size = input<Size>('md');
  readonly autocomplete = input<string | null>(null);
  readonly value = model('');
  readonly enter = output<void>();

  private readonly sizes: Record<Size, string> = {
    sm: 'h-9 text-sm',
    md: 'h-11 text-sm',
    lg: 'h-12 text-base',
  };

  readonly inputClass = computed(() =>
    'w-full rounded-xl outline-none transition ' +
    this.sizes[this.size()] + ' ' +
    'bg-[rgb(var(--surface)/var(--surface-opacity))] ' +
    'border border-[rgb(var(--border)/var(--border-opacity))] ' +
    'text-[rgb(var(--text))] placeholder:text-[rgb(var(--text-muted)/0.6)] ' +
    'focus:border-[rgb(var(--accent)/0.5)] ' +
    (this.icon() ? 'pl-10 pr-3' : 'px-3'));
}
