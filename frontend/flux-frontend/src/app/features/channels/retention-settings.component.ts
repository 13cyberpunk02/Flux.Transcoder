import { Component, computed, inject, input, linkedSignal } from '@angular/core';
import { form, FormField, min, submit } from '@angular/forms/signals';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatSelect, MatOption } from '@angular/material/select';
import { MatInput } from '@angular/material/input';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { ChannelsStore } from './channels.store';
import { formatRetention } from '../../core/format/format';

const MAX_MINUTES = 14 * 24 * 60;
const CUSTOM = -1;

const PRESETS = [
  { value: 60, label: '1 час' },
  { value: 360, label: '6 часов' },
  { value: 1440, label: '1 день' },
  { value: 4320, label: '3 дня' },
  { value: 10080, label: '7 дней' },
  { value: MAX_MINUTES, label: '14 дней (максимум)' },
  { value: CUSTOM, label: 'Другое…' },
];

type Unit = 'minutes' | 'hours' | 'days';
const UNIT_MINUTES: Record<Unit, number> = { minutes: 1, hours: 60, days: 1440 };
const UNITS: { value: Unit; label: string }[] = [
  { value: 'minutes', label: 'минут' },
  { value: 'hours', label: 'часов' },
  { value: 'days', label: 'дней' },
];

interface RetentionModel { preset: number; amount: number; unit: Unit; }

/** Минуты с сервера -> модель формы (пресет, если совпадает, иначе «другое»). */
function toModel(minutes: number): RetentionModel {
  if (PRESETS.some(p => p.value === minutes)) return { preset: minutes, amount: 1, unit: 'days' };
  if (minutes % 1440 === 0) return { preset: CUSTOM, amount: minutes / 1440, unit: 'days' };
  if (minutes % 60 === 0) return { preset: CUSTOM, amount: minutes / 60, unit: 'hours' };
  return { preset: CUSTOM, amount: minutes, unit: 'minutes' };
}

/** Время хранения архива: пресеты + произвольное значение (1 мин … 14 дней). */
@Component({
  selector: 'app-retention-settings',
  imports: [FormField, MatFormField, MatLabel, MatSelect, MatOption, MatInput, MatButton, MatIcon],
  template: `
    <form (submit)="onSubmit($event)" novalidate class="flex flex-col gap-2">
      <mat-form-field>
        <mat-label>Хранить архив</mat-label>
        <mat-select [formField]="retentionForm.preset">
          @for (p of presets; track p.value) {
            <mat-option [value]="p.value">{{ p.label }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      @if (model().preset === CUSTOM) {
        <div class="flex gap-2">
          <mat-form-field class="flex-1">
            <mat-label>Количество</mat-label>
            <input matInput type="number" [formField]="retentionForm.amount" />
          </mat-form-field>
          <mat-form-field class="w-32">
            <mat-label>Единица</mat-label>
            <mat-select [formField]="retentionForm.unit">
              @for (u of units; track u.value) {
                <mat-option [value]="u.value">{{ u.label }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </div>
      }

      @if (tooLong()) {
        <p class="text-xs text-error">Максимум — 14 дней</p>
      } @else if (retentionForm.amount().invalid()) {
        <p class="text-xs text-error">Минимум — 1</p>
      } @else {
        <p class="text-xs text-muted">Сейчас: {{ currentLabel() }} · старые сегменты удаляются автоматически</p>
      }

      <button matButton="tonal" type="submit" class="self-start mt-1"
              [disabled]="!changed() || tooLong() || retentionForm().submitting()">
        <mat-icon>check</mat-icon> Сохранить
      </button>
    </form>
  `,
})
export class RetentionSettingsComponent {
  readonly channelId = input.required<string>();
  readonly currentMinutes = input.required<number>();

  private readonly store = inject(ChannelsStore);

  protected readonly CUSTOM = CUSTOM;
  protected readonly presets = PRESETS;
  protected readonly units = UNITS;

  /** Сбрасывается к серверному значению, когда оно меняется. */
  protected readonly model = linkedSignal<RetentionModel>(() => toModel(this.currentMinutes()));
  protected readonly retentionForm = form(this.model, p => {
    min(p.amount, 1, { message: 'Минимум 1' });
  });

  protected readonly minutes = computed(() => {
    const m = this.model();
    return m.preset === CUSTOM ? Math.floor((m.amount ?? 0) * UNIT_MINUTES[m.unit]) : m.preset;
  });

  protected readonly tooLong = computed(() => this.minutes() > MAX_MINUTES);
  protected readonly changed = computed(() => this.minutes() !== this.currentMinutes());
  protected readonly currentLabel = computed(() => formatRetention(this.currentMinutes()));

  onSubmit(event: Event): void {
    event.preventDefault();
    if (this.tooLong()) return;
    submit(this.retentionForm, async () => {
      await this.store.updateRetention(this.channelId(), this.minutes());
    });
  }
}
