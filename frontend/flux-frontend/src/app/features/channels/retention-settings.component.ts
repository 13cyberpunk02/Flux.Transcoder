import { Component, inject, input, signal, computed } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faClockRotateLeft, faFloppyDisk } from '@fortawesome/free-solid-svg-icons';

import { InputComponent } from '../../shared/ui/input.component';
import { SelectComponent, SelectOption } from '../../shared/ui/select.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { ChannelsApi } from '../../core/api/channels.api';
import { ChannelsStore } from './channels.store';
import { ToastService } from '../../core/toast/toast.service';

type Unit = 'minutes' | 'hours' | 'days';

@Component({
  selector: 'app-retention-settings',
  standalone: true,
  imports: [FontAwesomeModule, InputComponent, SelectComponent, ButtonComponent],
  template: `
    <div>
      <label class="flex items-center gap-1.5 text-xs text-[rgb(var(--text-muted))] mb-2">
        <fa-icon [icon]="faClockRotateLeft" class="text-xs" />
        Хранение архива
      </label>

      <div class="flex items-center gap-2 flex-wrap">
        <ui-input
          type="number" size="md"
          class="w-20"
          [(value)]="value" />

        <ui-select
          size="md"
          class="w-32"
          [options]="unitOptions"
          [(value)]="unit" />

        <ui-button
          label="Сохранить" [icon]="faFloppyDisk"
          variant="secondary" size="md"
          [loading]="saving()"
          (clicked)="save()" />

        <span class="text-xs text-[rgb(var(--text-muted))] ml-1">
          сейчас: {{ currentLabel() }}
        </span>
      </div>
    </div>
  `,
})
export class RetentionSettingsComponent {
  readonly channelId = input.required<string>();
  readonly currentMinutes = input.required<number>();

  private readonly api = inject(ChannelsApi);
  private readonly store = inject(ChannelsStore);
  private readonly toast = inject(ToastService);

  readonly faClockRotateLeft = faClockRotateLeft;
  readonly faFloppyDisk = faFloppyDisk;

  readonly value = signal('7');            // строка — для ui-input (model<string>)
  readonly unit = signal<Unit>('days');
  readonly saving = signal(false);

  readonly unitOptions: SelectOption[] = [
    { value: 'minutes', label: 'минут' },
    { value: 'hours', label: 'часов' },
    { value: 'days', label: 'дней' },
  ];

  readonly currentLabel = computed(() => {
    const m = this.currentMinutes();
    if (m % (24 * 60) === 0) return `${m / (24 * 60)} дн.`;
    if (m % 60 === 0) return `${m / 60} ч.`;
    return `${m} мин.`;
  });

  save(): void {
    const minutes = this.toMinutes();
    if (minutes < 1) { this.toast.error('Минимум 1 минута'); return; }
    if (minutes > 14 * 24 * 60) { this.toast.error('Максимум 14 дней'); return; }

    this.saving.set(true);
    this.api.updateRetention(this.channelId(), minutes).subscribe({
      next: () => {
        this.store.loadChannel(this.channelId());
        this.toast.success('Настройки хранения сохранены');
        this.saving.set(false);
      },
      error: () => {
        this.toast.error('Не удалось сохранить настройки');
        this.saving.set(false);
      },
    });
  }

  private toMinutes(): number {
    const val = parseInt(this.value(), 10) || 0;
    switch (this.unit()) {
      case 'days': return val * 24 * 60;
      case 'hours': return val * 60;
      default: return val;
    }
  }
}
