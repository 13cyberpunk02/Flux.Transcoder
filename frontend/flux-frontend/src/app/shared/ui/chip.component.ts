import { Component, input, output } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { faXmark } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'ui-chip',
  standalone: true,
  imports: [FontAwesomeModule],
  template: `
    <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-sm
                 bg-white/5 border border-white/10 text-[rgb(var(--text))]">
      @if (icon()) { <fa-icon [icon]="icon()!" class="text-xs opacity-70" /> }
      <ng-content />
      @if (removable()) {
        <button (click)="removed.emit()" class="ml-0.5 opacity-60 hover:opacity-100 transition">
          <fa-icon [icon]="faXmark" class="text-xs" />
        </button>
      }
    </span>
  `,
})
export class ChipComponent {
  readonly icon = input<IconDefinition>();
  readonly removable = input(false);
  readonly removed = output<void>();
  readonly faXmark = faXmark;
}
