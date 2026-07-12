import { Component, input, model, output, signal, computed, HostListener, ElementRef, inject } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { faChevronDown, faCheck } from '@fortawesome/free-solid-svg-icons';

export interface SelectOption {
  value: any;
  label: string;
}

type Size = 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-select',
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
        <button type="button" (click)="toggle($event)" [class]="triggerClass()">
          <span [class]="selectedLabel() ? '' : 'text-[rgb(var(--text-muted))]'">
            {{ selectedLabel() || placeholder() }}
          </span>
          <fa-icon [icon]="faChevronDown"
                   class="text-xs text-[rgb(var(--text-muted))] transition flex-shrink-0"
                   [class.rotate-180]="open()" />
        </button>

        @if (open()) {
          <div class="absolute left-0 right-0 mt-1.5 z-[70] rounded-xl p-1.5 shadow-xl max-h-64 overflow-auto
                      border border-[rgb(var(--border)/var(--border-opacity))]
                      bg-[rgb(var(--bg))] backdrop-blur-xl
                      animate-[uiSelectFade_0.12s_ease-out]">
            @for (opt of options(); track opt.value) {
              <button type="button" (click)="select(opt)" [class]="optionClass(opt)">
                <span>{{ opt.label }}</span>
                @if (opt.value === value()) {
                  <fa-icon [icon]="faCheck" class="text-xs text-[rgb(var(--accent))]" />
                }
              </button>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    @keyframes uiSelectFade { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: translateY(0); } }
  `],
})
export class SelectComponent {
  private readonly host = inject(ElementRef);

  readonly label = input<string>();
  readonly labelIcon = input<IconDefinition>();
  readonly options = input.required<SelectOption[]>();
  readonly placeholder = input('— выберите —');
  readonly size = input<Size>('md');
  readonly value = model<any>(null);
  readonly changed = output<any>();

  readonly open = signal(false);
  readonly faChevronDown = faChevronDown;
  readonly faCheck = faCheck;

  private readonly sizes: Record<Size, string> = {
    sm: 'h-9 text-sm',
    md: 'h-11 text-sm',
    lg: 'h-12 text-base',
  };

  readonly selectedLabel = computed(() => {
    const opt = this.options().find(o => o.value === this.value());
    return opt?.label ?? '';
  });

  readonly triggerClass = computed(() => {
    const base = 'w-full flex items-center justify-between gap-2 px-3.5 rounded-xl outline-none transition ' +
      this.sizes[this.size()] + ' ' +
      'bg-[rgb(var(--surface)/var(--surface-opacity))] text-[rgb(var(--text))] text-left border ';
    return base + (this.open()
      ? 'border-[rgb(var(--accent)/0.5)]'
      : 'border-[rgb(var(--border)/var(--border-opacity))] hover:border-[rgb(var(--accent)/0.4)]');
  });

  optionClass(opt: SelectOption): string {
    const base = 'w-full flex items-center justify-between gap-2 px-3 py-2 rounded-lg text-sm text-left transition ' +
      'text-[rgb(var(--text))] hover:bg-[rgb(var(--accent)/0.12)] ';
    return base + (opt.value === this.value() ? 'bg-[rgb(var(--accent)/0.15)]' : '');
  }

  toggle(e: MouseEvent): void {
    e.stopPropagation();
    this.open.update(v => !v);
  }

  select(opt: SelectOption): void {
    this.value.set(opt.value);
    this.changed.emit(opt.value);
    this.open.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (!this.host.nativeElement.contains(e.target)) {
      this.open.set(false);
    }
  }
}
